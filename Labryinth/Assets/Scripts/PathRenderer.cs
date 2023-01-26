using Newtonsoft.Json.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PathRenderer : MonoBehaviour
{

	/* I have a list of nodes and paths connecting them
	 * Lets start by making 3 wickets on each path ID 
	 * 
	 *		*****
	 *		*   *
	 *		*   *
	 * 
	 *  Each wicket will just have the 4 corners for now
	 * 
	 * 
	 * 
	 * One in the middle, and one near each edge
	 * And then at each node, i need to connect all of the adjacent wickets
	 * 
	 * Lets make our vertices first, and then we'll figure out how we want to draw our triangles
	 */
	private List<Vector3> _vertices = new List<Vector3>();
	private List<Vector3> _normals = new List<Vector3>();

	private List<int> _triangles = new List<int>();

	private float _ceilingHeight = 3f;

	public bool BothSides = true;
	public void Generate(MazeGenerator maze)
	{

		_vertices = new List<Vector3>();
		_normals = new List<Vector3>();
		_triangles = new List<int>();

		foreach ((var currentNodeAddress, var currentNode) in maze.NodeMap)
		{
			var adjacentWickets = new List<Wicket>();

			foreach (var neighborAddress in currentNode.Neighbors)
			{
				var neighborNode = maze.NodeMap[neighborAddress];
				var pathID = new PathID(currentNodeAddress, neighborAddress);



				if (maze.Paths.TryGetValue(pathID, out var path))
				{
					// Make the closest wicket at each intersection
					var wicket = MakeWicket(currentNode.GameObject.transform.position, neighborNode.GameObject.transform.position, 0.22f);
					currentNode.Wickets[neighborAddress] = wicket;
					adjacentWickets.Add(wicket);
				}

			}

			if(adjacentWickets.Count == 0)
			{
				continue;
			}


			var basePoint = currentNode.GameObject.transform.position;

			if (adjacentWickets.Count == 1)
			{
				SealWicket(basePoint, adjacentWickets[0]);
			}

			var intersectionCenter = Vector3.zero;

			foreach (var wicket in adjacentWickets)
			{
				intersectionCenter += AverageWicketLocation(wicket);
			}

			intersectionCenter /= adjacentWickets.Count;
			

			if (adjacentWickets.Count == 2)
			{
				foreach(var wicket in adjacentWickets)
				{
					wicket.Vector = AverageWicketLocation(wicket) - basePoint;
				}

				var twoWicketAngle = Vector3.Angle(adjacentWickets[0].Vector, adjacentWickets[1].Vector);

				// add a dummy wicket to help with spacing on sharp turn intersections
				if (twoWicketAngle <= 150)
				{
					var distanceToCenter = basePoint - intersectionCenter;
					var newWicket = new Wicket(new int[]
					{
						_vertices.Count,
						_vertices.Count+1,
						_vertices.Count+1,
						_vertices.Count
					});


					_vertices.Add(basePoint + distanceToCenter);
					_normals.Add(Vector3.up);

					_vertices.Add(basePoint + distanceToCenter + Vector3.up * _ceilingHeight);
					_normals.Add(Vector3.down);


					var averagePoint = AverageWicketLocation(newWicket);
					newWicket.Vector = averagePoint - basePoint;

					intersectionCenter *= adjacentWickets.Count;
					intersectionCenter += AverageWicketLocation(newWicket);

					adjacentWickets.Add(newWicket);
					intersectionCenter /= adjacentWickets.Count;
				}

			}
			var floorVertex = _vertices.Count;
			_vertices.Add(intersectionCenter);
			_normals.Add(Vector3.up);
			var ceilingVertex = _vertices.Count;
			_vertices.Add(intersectionCenter + Vector3.up * _ceilingHeight);
			_normals.Add(Vector3.down);

			var orderedWickets = adjacentWickets.OrderBy(wicket =>
			{
				var averagePoint = AverageWicketLocation(wicket);
				wicket.Vector = averagePoint - basePoint;
				var angle = Vector3.SignedAngle(Vector3.forward, wicket.Vector, Vector3.up);
				return angle;
			}).ToList();

			for (var i = 0; i < orderedWickets.Count; i++)
			{
				var wicket = orderedWickets[i];

				var nextIndex = i + 1 >= orderedWickets.Count ? 0 : i + 1;
				var nextWicket = orderedWickets[nextIndex];


				AddConnectionTriangles(intersectionCenter, floorVertex, ceilingVertex, wicket, nextWicket);
			}
		}

		// add wickets in each path
		var checkedPaths = new HashSet<PathID>();

		foreach ((var currentNodeAddress, var currentNode) in maze.NodeMap)
		{
			var adjacentWickets = new List<Wicket>();

			foreach (var neighborAddress in currentNode.Neighbors)
			{
				var neighborNode = maze.NodeMap[neighborAddress];
				var pathID = new PathID(currentNodeAddress, neighborAddress);

				if (checkedPaths.Contains(pathID))
				{
					continue;
				}
				if (maze.Paths.TryGetValue(pathID, out var path))
				{
					var wicket1 = currentNode.Wickets[neighborAddress];
					var wicket2 = MakeWicket(currentNode.GameObject.transform.position, neighborNode.GameObject.transform.position, 0.5f);
					var wicket3 = neighborNode.Wickets[currentNodeAddress];

					AddTriangles(wicket1, wicket2);
					AddTriangles(wicket2, wicket3, true);
				}
				checkedPaths.Add(pathID);
			}

		}

		var mesh = new Mesh
		{
			vertices = _vertices.ToArray(),
			triangles = _triangles.ToArray(),
			normals = _normals.ToArray(),
			name = "Maze"
		};

		GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;

	}


	private struct WicketConnection
	{
		public Wicket Wicket1;
		public Wicket Wicket2;
	
		public WicketConnection(Wicket wicket1, Wicket wicket2)
		{
			Wicket1 = wicket1;
			Wicket2 = wicket2;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			if(obj is WicketConnection other){

				if(Wicket1 == other.Wicket1 && Wicket2 == other.Wicket2)
				{
					return true;
				}
				if (Wicket1 == other.Wicket2 && Wicket2 == other.Wicket1)
				{
					return true;
				}
			}
			return false;
		}
	}

	private void SealWicket(Vector3 basePoint, Wicket wicket)
	{
		_triangles.AddRange(OrientedTriangle(basePoint, new int[]
		{
			wicket[0],
			wicket[1],
			wicket[2]
		}));
		_triangles.AddRange(OrientedTriangle(basePoint, new int[]
{
			wicket[1],
			wicket[2],
			wicket[3]
}));

	}

	private void AddConnectionTriangles(Vector3 basePoint, int floorPoint, int ceilingPoint, Wicket wicket1, Wicket wicket2)
	{
		_triangles.AddRange(OrientedTriangle(basePoint, new int[]
		{
			wicket1[0],
			wicket2[3],
			wicket1[1]
		}));
		_triangles.AddRange(OrientedTriangle(basePoint, new int[]
		{
			wicket2[2],
			wicket1[1],
			wicket2[3],
		}));
		_triangles.AddRange(OrientedTriangle(basePoint, new int[]
		{
			wicket1[1],
			wicket2[2],
			ceilingPoint
		}));
		_triangles.AddRange(OrientedTriangle(basePoint, new int[]
		{
			wicket1[0],
			wicket2[3],
			floorPoint
		}));
		_triangles.AddRange(OrientedTriangle(basePoint, new int[]
		{
			wicket1[1],
			wicket1[2],
			ceilingPoint
		}));
		_triangles.AddRange(OrientedTriangle(basePoint, new int[]
		{
			wicket1[0],
			wicket1[3],
			floorPoint
		}));

	}

	private List<int> OrientedTriangle(Vector3 basePoint, int[] triangle)
	{
		if (BothSides)
		{
			return new List<int>()
			{
				triangle[0],
				triangle[2],
				triangle[1],
				triangle[0],
				triangle[1],
				triangle[2],
			};
		}
		var plane = new Plane(_vertices[triangle[0]], _vertices[triangle[1]], _vertices[triangle[2]]);
		if (plane.GetSide(basePoint + Vector3.up)){
			return triangle.ToList();
		}

		return new List<int>()
		{
			triangle[0],
			triangle[2],
			triangle[1],
		};
	}

	/// <summary>
	/// Returns the center of the wicket on the floor (y = 0)
	/// </summary>
	private Vector3 AverageWicketLocation(Wicket wicket)
	{
		var sum = _vertices[wicket[0]] + _vertices[wicket[^1]];

		return sum / 2f;
	}


	private void AddTriangles(Wicket wicket1, Wicket wicket2, bool flipped = false)
	{
		var triangles = new List<int>();

		for(var i = 0; i < 4; i++)
		{
			var next = i + 1;
			if (next >= 4)
			{
				next = 0;
			}

			var i2 = flipped? 3 - i: i;
			var next2 = flipped ? 3 - next : next;


			triangles.Add(wicket2[next2]);
			triangles.Add(wicket1[next]);
			triangles.Add(wicket1[i]);


			triangles.Add(wicket2[i2]);
			triangles.Add(wicket2[next2]);
			triangles.Add(wicket1[i]);

			if (BothSides)
			{
				triangles.Add(wicket2[next2]);
				triangles.Add(wicket1[i]);
				triangles.Add(wicket1[next]);


				triangles.Add(wicket2[i2]);
				triangles.Add(wicket1[i]);
				triangles.Add(wicket2[next2]);
			}
			
		};

		_triangles.AddRange(triangles);

	}

	private Wicket MakeWicket(Vector3 start, Vector3 end, float distance)
	{
		var basePoint = Vector3Lerp(start, end, distance);
		var vertices = GetPlanarVertices(basePoint, start);

		var indices = new List<int>();
		foreach (var vertex in vertices)
		{
			indices.Add(_vertices.Count);
			_vertices.Add(vertex);

			_normals.Add(new Vector3(vertex.x - basePoint.x, vertex.y - basePoint.y, vertex.z - basePoint.z));

		}
		var index = 0;
		if (distance == 0.5f)
		{
			index = 1;
		}else if(distance > 0.5f)
		{
			index = 2;
		}
		return new Wicket(indices.ToArray(), index);

	}

	private Vector3 Vector3Lerp(Vector3 start, Vector3 end, float distance)
	{
		var x = Mathf.Lerp(start.x, end.x, distance);
		var y = Mathf.Lerp(start.y, end.y, distance);
		var z = Mathf.Lerp(start.z, end.z, distance);

		return new Vector3(x, y, z);
	}

	private List<Vector3> GetPlanarVertices(Vector3 basePoint, Vector3 normal)
	{
		var perpindicular = basePoint.PerpindicularTo(normal);
		var vertices = new List<Vector3>()
		{
			basePoint + perpindicular*2f,
			basePoint + perpindicular + Vector3.up*_ceilingHeight,
			basePoint - perpindicular + Vector3.up*_ceilingHeight,
			basePoint - perpindicular*2f,
		};
		return vertices;
	}

}

public class Wicket
{
	protected int[] Points;

	public int Index;
	public int ConnectedCorner = -1;
	public Vector3 Vector;

	public Wicket(int[] points)
	{
		Points = points;
	}

	public Wicket(int[] points, int index)
	{
		Points = points;
	}

	public static implicit operator int[](Wicket wicket) => wicket.Points;
	public static implicit operator Wicket(int[] points) => new(points);

	public int this[Index index]
	{
		get
		{
			return Points[index];
		}
		set
		{
			Points[index] = value;
		}
	}
}