using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PathRenderer : Singleton<PathRenderer>
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

	public float CeilingHeight = 5f;
	public float WicketWidth = 4f;

	public bool BothSides = true;

	public GameObject ColliderTemplate;

	private List<NormalDraws> _normalDraws = new();

	public struct NormalDraws
	{
		Vector3 Start;
		Vector3 End;
		Color Color;

		public NormalDraws(Vector3 vertex, Vector3 normal, Color color)
		{
			Start = vertex;
			End = vertex + normal;
			Color = color;
		}

		public void Draw()
		{
			Debug.DrawLine(Start, End, Color);
		}
	}

	private void DrawLastNormal(Color color)
	{
		_normalDraws.Add(new NormalDraws(_vertices[^1], _normals[^1], color));
	}

	private void OnDrawGizmos()
	{
		foreach(var normalDraw in _normalDraws)
		{
			normalDraw.Draw();
		}
	}
	public void Generate()
	{

		_vertices = new List<Vector3>();
		_normals = new List<Vector3>();
		_triangles = new List<int>();

		foreach ((var currentNodeAddress, var currentNode) in Maze.NodeMap)
		{
			var adjacentWickets = new List<Wicket>();

			foreach (var neighborAddress in currentNode.Neighbors)
			{
				var neighborNode = Maze.NodeMap[neighborAddress];
				var pathID = new PathID(currentNodeAddress, neighborAddress);



				if (Maze.Paths.TryGetValue(pathID, out var path))
				{
					// Make the closest wicket at each intersection
					var wicket = MakeWicket(currentNode.Position, neighborNode.Position, 0.22f);
					currentNode.Wickets[neighborAddress] = wicket;
					adjacentWickets.Add(wicket);
				}

			}

			if(adjacentWickets.Count == 0)
			{
				// node is not connected to the maze
				continue;
			}


			var basePoint = currentNode.Position;

			if (adjacentWickets.Count == 1)
			{
				var dummyNodePosition = 2*currentNode.Position - AverageWicketLocation(adjacentWickets[0]);
				var wicket = MakeWicket(currentNode.Position, dummyNodePosition, 1f);
				currentNode.Wickets[currentNode.Address] = wicket;
				adjacentWickets.Add(wicket);

				SealWicket(basePoint, adjacentWickets[1]);
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

					AddVertex(basePoint + distanceToCenter, basePoint);
					//_vertices.Add(basePoint + distanceToCenter);
					//_normals.Add(Vector3.up);
					DrawLastNormal(Color.red);

					AddVertex(basePoint + distanceToCenter + Vector3.up * CeilingHeight, basePoint);
					//_vertices.Add(basePoint + distanceToCenter + Vector3.up * CeilingHeight);
					//_normals.Add(Vector3.down);
					DrawLastNormal(Color.green);


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
			DrawLastNormal(Color.blue);
			var ceilingVertex = _vertices.Count;
			_vertices.Add(intersectionCenter + Vector3.up * CeilingHeight);
			_normals.Add(Vector3.down);
			DrawLastNormal(Color.magenta);

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

		foreach ((var currentNodeAddress, var currentNode) in Maze.NodeMap)
		{
			var adjacentWickets = new List<Wicket>();

			foreach (var neighborAddress in currentNode.Neighbors)
			{
				var neighborNode = Maze.NodeMap[neighborAddress];
				var pathID = new PathID(currentNodeAddress, neighborAddress);

				if (checkedPaths.Contains(pathID))
				{
					continue;
				}
				if (Maze.Paths.TryGetValue(pathID, out var path))
				{
					var wicket1 = currentNode.Wickets[neighborAddress];
					var wicket2 = MakeWicket(currentNode.Position, neighborNode.Position, 0.5f);
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

	private void AddCollider(int vertex1, int vertex2)
	{
		var point1 = _vertices[vertex1];
		var point2 = _vertices[vertex2];
		var averagePoint = (point1 + point2) / 2f;

		var normalized = (point2 - point1).normalized;
		float angle;
		if (Mathf.Approximately(normalized.z, 0f))
		{
			angle = 90f;
		} else
		{
			angle = Mathf.Rad2Deg * Mathf.Atan2(normalized.x , normalized.z);
		}
		var distance = (point2 - point1).magnitude;
		var colliderObject = Instantiate(ColliderTemplate, ColliderTemplate.transform.parent);
		colliderObject.transform.localPosition = averagePoint;
		colliderObject.transform.localScale = new Vector3(1f, 1f, distance);

		colliderObject.transform.localEulerAngles = new Vector3(0f, angle, 0f);
		colliderObject.SetActive(true);


		
		
		
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

		AddCollider(wicket[0], wicket[3]);

	}

	private void AddConnectionTriangles(Vector3 basePoint, int floorPoint, int ceilingPoint, Wicket wicket1, Wicket wicket2)
	{

		AddCollider(wicket1[0], wicket2[3]);
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

		if (flipped)
		{
			AddCollider(wicket1[0], wicket2[3]);
			AddCollider(wicket1[3], wicket2[0]);
		} else
		{
			AddCollider(wicket1[0], wicket2[0]);
			AddCollider(wicket1[3], wicket2[3]);
		}

	}

	private Wicket MakeWicket(Vector3 start, Vector3 end, float distance)
	{
		var basePoint = Vector3Lerp(start, end, distance);
		var vertices = GetPlanarVertices(basePoint, start);

		var indices = new List<int>();
		foreach (var vertex in vertices)
		{
			indices.Add(_vertices.Count);
			AddVertex(vertex, basePoint);
			DrawLastNormal(Color.white);
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

	private void AddVertex(Vector3 vertex, Vector3 basePoint)
	{
		_vertices.Add(vertex);
		_normals.Add((basePoint - vertex + Vector3.up).normalized);
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
			basePoint + perpindicular*WicketWidth,
			basePoint + perpindicular + Vector3.up*CeilingHeight,
			basePoint - perpindicular + Vector3.up*CeilingHeight,
			basePoint - perpindicular*WicketWidth,
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