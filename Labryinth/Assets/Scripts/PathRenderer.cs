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

		// add wickets in each path
		var checkedPaths = new HashSet<PathID>();

		foreach ((var currentNodeAddress, var currentNode) in maze.NodeMap) {

			foreach (var neighborAddress in currentNode.Neighbors)
			{
				var neighborNode = maze.NodeMap[neighborAddress];
				var pathID = new PathID(currentNodeAddress, neighborAddress);

				if (checkedPaths.Contains(pathID)) {
					continue;
				}

				if (maze.Paths.TryGetValue(pathID, out var path))
				{

					var wicket1 = MakeWicket(currentNode.GameObject.transform.position, neighborNode.GameObject.transform.position, 0.2f);
					var wicket2 = MakeWicket(currentNode.GameObject.transform.position, neighborNode.GameObject.transform.position, 0.5f);
					var wicket3 = MakeWicket(currentNode.GameObject.transform.position, neighborNode.GameObject.transform.position, 0.8f);

					AddTriangles(wicket1, wicket2);
					AddTriangles(wicket2, wicket3);

					path.Wickets = new Wicket[]
					{
						wicket1,
						wicket2,
						wicket3
					};

					checkedPaths.Add(pathID);
				}

			}
		}

		// connect the wickets at the nodes
		foreach ((var currentNodeAddress, var currentNode) in maze.NodeMap)
		{
			var basePoint = currentNode.GameObject.transform.position;
			var adjacentWickets = new List<Wicket>();

			var floorVertex = _vertices.Count;
			_vertices.Add(basePoint);
			_normals.Add(Vector3.up);
			var ceilingVertex = _vertices.Count;
			_vertices.Add(basePoint + Vector3.up * _ceilingHeight);
			_normals.Add(Vector3.down);


			foreach (var neighborAddress in currentNode.Neighbors)
			{

				var pathID = new PathID(currentNodeAddress, neighborAddress);
				if (maze.Paths.TryGetValue(pathID, out var path))
				{
					adjacentWickets.Add(ClosestWicket(basePoint, path.Wickets));

				}
			}

			if (adjacentWickets.Count <= 1)
			{
				// TODO: this is a dead end
				continue;
			}

			var orderedWickets =  adjacentWickets.OrderBy(wicket =>
			{
				var averagePoint = AverageWicketLocation(wicket);
				var direction = averagePoint - basePoint;
				var angle = Vector3.SignedAngle(Vector3.zero, direction, Vector3.up);
				return angle;
			}).ToList();

			for (var i = 0; i < orderedWickets.Count; i++)
			{
				var wicket = orderedWickets[i];

				var nextIndex = i + 1 >= orderedWickets.Count ? 0 : i + 1;
				var nextWicket = orderedWickets[nextIndex];

				AddConnectionTriangles(basePoint, floorVertex, ceilingVertex, wicket, nextWicket);
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

	private void AddFloorAndCeiling(Vector3 basePoint, int floorPoint, int ceilingPoint, Wicket wicket)
	{

	}


	private HashSet<WicketConnection> _existingConnections = new HashSet<WicketConnection>();


	private void AddConnectionTriangles(Vector3 basePoint, int floorPoint, int ceilingPoint, Wicket wicket1, Wicket wicket2)
	{

		var type = -1;
		var shortestVertexDistance = float.MaxValue;
		var distance = Vector3.Distance(_vertices[wicket1[0]], _vertices[wicket2[3]]);
		if (distance < shortestVertexDistance){
			shortestVertexDistance = distance;
			type = 0;
		}
		distance = Vector3.Distance(_vertices[wicket1[0]], _vertices[wicket2[0]]);
		if (distance < shortestVertexDistance)
		{
			shortestVertexDistance = distance;
			type = 1;
		}
		distance = Vector3.Distance(_vertices[wicket1[3]], _vertices[wicket2[0]]);
		if (distance < shortestVertexDistance)
		{
			shortestVertexDistance = distance;
			type = 2;
		}
		distance = Vector3.Distance(_vertices[wicket1[3]], _vertices[wicket2[3]]);
		if (distance < shortestVertexDistance)
		{
			type = 3;
		}

		if(_existingConnections.Contains(new WicketConnection(wicket1, wicket2)))
		{
			switch (type)
			{
				case 0:
					type = 2;
					break;
				case 1:
					type = 3;
					break;
				case 2:
					type = 0;
					break;
				case 3:
					type = 1;
					break;
			}
		} else
		{

			_existingConnections.Add(new WicketConnection(wicket1, wicket2));

		}

		switch (type)
		{
			case 0: // 0:3
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

				break;
			case 1: // 0:0
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket1[0],
					wicket2[0],
					wicket1[1]
				}));
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket2[1],
					wicket1[1],
					wicket2[0],
				}));
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket1[1],
					wicket2[1],
					ceilingPoint
				}));
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket1[0],
					wicket2[0],
					floorPoint
				}));

				break;
			case 2: // 3:0
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket1[3],
					wicket2[0],
					wicket1[2]
				}));
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket2[1],
					wicket1[2],
					wicket2[0],
				}));

				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket1[2],
					wicket2[1],
					ceilingPoint
				}));
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket1[3],
					wicket2[0],
					floorPoint
				}));

				break;
			case 3: // 3:3
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket1[3],
					wicket2[3],
					wicket1[2]
				}));
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket2[2],
					wicket1[2],
					wicket2[3],
				}));
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket1[2],
					wicket2[2],
					ceilingPoint
				}));
				_triangles.AddRange(OrientedTriangle(basePoint, new int[]
				{
					wicket1[3],
					wicket2[3],
					floorPoint
				}));

				break;
		}

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
	/// Returns the closest wicket to a point
	/// </summary>
	private Wicket ClosestWicket(Vector3 position, Wicket[] wickets)
	{
		if(wickets.Length == 1)
		{
			return wickets[0];
		}
		var closest = wickets[0];
		var distanceToClosest = Vector3.Distance(position, AverageWicketLocation(closest));

		foreach(var wicket in wickets)
		{
			var distance = Vector3.Distance(position, AverageWicketLocation(wicket));
			if(distance < distanceToClosest)
			{
				distanceToClosest = distance;
				closest = wicket;
			}
		}

		return closest;


	}

	/// <summary>
	/// Returns the center of the wicket on the floor (y = 0)
	/// </summary>
	private Vector3 AverageWicketLocation(Wicket wicket)
	{
		var sum = _vertices[wicket[0]] + _vertices[wicket[^1]];

		return sum / 2f;
	}

	private void AddTriangles(Wicket wicket1, Wicket wicket2)
	{
		var triangles = new List<int>();

		for(var i = 0; i < 4; i++)
		{
			var next = i + 1;
			if (next >= 4)
			{
				next = 0;
			}

			triangles.Add(wicket2[next]);
			triangles.Add(wicket1[next]);
			triangles.Add(wicket1[i]);


			triangles.Add(wicket2[i]);
			triangles.Add(wicket2[next]);
			triangles.Add(wicket1[i]);

			if (BothSides)
			{
				triangles.Add(wicket2[next]);
				triangles.Add(wicket1[i]);
				triangles.Add(wicket1[next]);


				triangles.Add(wicket2[i]);
				triangles.Add(wicket1[i]);
				triangles.Add(wicket2[next]);
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

			_normals.Add(new Vector3(vertex.x - basePoint.x, vertex.y, vertex.z - basePoint.z));

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