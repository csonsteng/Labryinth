using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

	// todo: break out to multiple meshs so materials sit better

	private class MeshGenerator
	{
		private readonly List<Vector3> _vertices = new List<Vector3>();
		private readonly List<int> _triangles = new List<int>();
		private readonly List<int> _reverseTriangles = new List<int>();
		private readonly string _name;
		public MeshGenerator(string name)
		{
			_name = name;
		}

		public IEnumerable<Vector3> Vertices => _vertices;

		public void Add(Vector3 vertex) => _vertices.Add(vertex);
		public void Add(List<int> indices)
		{
			_triangles.AddRange(indices);
			for(var i = indices.Count - 1; i >= 0; i--)
			{
				_reverseTriangles.Add(indices[i]);
			}
		}

		public void Generate(Transform parentTransform, Material material)
		{
			Generate(parentTransform, material, _triangles);
			Generate(parentTransform, material, _reverseTriangles, "Reverse_");
		}

		private void Generate(Transform parentTransform, Material material, List<int> triangles, string namePrefix = "")
		{

			var meshObject = new GameObject($"{namePrefix}{_name}_Mesh", new Type[]
			{
				typeof(MeshFilter),
				typeof(MeshRenderer),
			});
			meshObject.transform.parent = parentTransform;
			meshObject.transform.localScale = Vector3.one;
			meshObject.transform.localPosition = Vector3.zero;
			meshObject.transform.eulerAngles = Vector3.zero;

			var mesh = new Mesh
			{
				vertices = _vertices.ToArray(),
				triangles = triangles.ToArray(),
				name = "Maze"
			};

			var uvs = new List<Vector2>();

			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			var bounds = mesh.bounds;
			foreach (var vertex in _vertices)
			{
				uvs.Add((new Vector2(vertex.x / bounds.size.x, vertex.z / bounds.size.z)) * 10f);
			}
			mesh.SetUVs(0, uvs.ToArray());
			meshObject.GetComponent<MeshFilter>().mesh = mesh;
			meshObject.GetComponent<MeshRenderer>().material = material;
		}
	}

	private MeshGenerator _floorMesh;
	private MeshGenerator _wallsMesh;
	private MeshGenerator _ceilingMesh;

	private List<MeshGenerator> _additionalMeshes = new List<MeshGenerator>();

	private List<Vector3> _vertices = new List<Vector3>();

	public float CeilingHeight = 5f;
	public float WicketWidth = 4f;

	public GameObject ColliderTemplate;
	public Material Material;

	public void Generate()
	{
		_floorMesh = new MeshGenerator("Floor");
		_wallsMesh = new MeshGenerator("Walls");
		_ceilingMesh = new MeshGenerator("Ceiling");
		_vertices = new List<Vector3>();

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

					AddVertex(basePoint + distanceToCenter);

					AddVertex(basePoint + distanceToCenter + Vector3.up * CeilingHeight);


					var averagePoint = AverageWicketLocation(newWicket);
					newWicket.Vector = averagePoint - basePoint;

					intersectionCenter *= adjacentWickets.Count;
					intersectionCenter += AverageWicketLocation(newWicket);

					adjacentWickets.Add(newWicket);
					intersectionCenter /= adjacentWickets.Count;
				}

			}
			var floorVertex = _vertices.Count;
			AddVertex(intersectionCenter);
			var ceilingVertex = _vertices.Count;
			AddVertex(intersectionCenter + Vector3.up * CeilingHeight);

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

		_wallsMesh.Generate(transform, Material);
		_floorMesh.Generate(transform, Material);
		_ceilingMesh.Generate(transform, Material);
		/*
				var mesh = new Mesh
				{
					vertices = _vertices.ToArray(),
					triangles = _triangles.ToArray(),
					name = "Maze"
				};

				var uvs = new List<Vector2>();

				mesh.RecalculateBounds();
				mesh.RecalculateNormals();
				mesh.RecalculateTangents();
				var bounds = mesh.bounds;
				foreach (var vertex in _vertices)
				{
					uvs.Add((new Vector2(vertex.x / bounds.size.x, vertex.z / bounds.size.z) * 10f));
				}
				mesh.SetUVs(0, uvs.ToArray());
				GetComponent<MeshFilter>().mesh = mesh;*/

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
		var newMesh = new MeshGenerator("Wicket Seal");

		foreach(var vertex in wicket.GetPoints)
		{
			newMesh.Add(_vertices[vertex]);
		}
		var worldTriangles = new List<int>();
		worldTriangles.AddRange(OrientedTriangle(basePoint, new int[]
		{
			wicket[0],
			wicket[1],
			wicket[3]
		}));
		worldTriangles.AddRange(OrientedTriangle(basePoint, new int[]
		{
			wicket[1],
			wicket[2],
			wicket[3]
		}));

		var localTriangles = new List<int>();
		foreach(var i in worldTriangles)
		{
			var index = 0;
			foreach(var vertex in newMesh.Vertices)
			{
				if(vertex == _vertices[i])
				{
					localTriangles.Add(index);
					break;
				}
				index++;
			}
		}
		newMesh.Add(localTriangles);
		newMesh.Generate(transform, Material);

		AddCollider(wicket[0], wicket[3]);

	}

	private void AddConnectionTriangles(Vector3 basePoint, int floorPoint, int ceilingPoint, Wicket wicket1, Wicket wicket2)
	{

		AddCollider(wicket1[0], wicket2[3]);
		AddTriangle(OrientedTriangle(basePoint, new int[]
		{
			wicket1[0],
			wicket2[3],
			wicket1[1]
		}));
		AddTriangle(OrientedTriangle(basePoint, new int[]
		{
			wicket2[2],
			wicket1[1],
			wicket2[3],
		}));
		AddTriangle(OrientedTriangle(basePoint, new int[]
		{
			wicket1[1],
			wicket2[2],
			ceilingPoint
		}));
		AddTriangle(OrientedTriangle(basePoint, new int[]
		{
			wicket1[0],
			wicket2[3],
			floorPoint
		}));
		AddTriangle(OrientedTriangle(basePoint, new int[]
		{
			wicket1[1],
			wicket1[2],
			ceilingPoint
		}));
		AddTriangle(OrientedTriangle(basePoint, new int[]
		{
			wicket1[0],
			wicket1[3],
			floorPoint
		}));

	}

	private List<int> OrientedTriangle(Vector3 basePoint, int[] triangle)
	{
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

	private void AddTriangle(List<int> triangle) 
	{
		if(triangle.Count == 0)
		{
			return;
		}

		var sameHeight = _vertices[triangle[0]].y;
		for(var i = 1; i < triangle.Count; i++)
		{
			if (_vertices[triangle[i]].y != sameHeight)
			{
				_wallsMesh.Add(triangle);
				return;
			}
		}
		if(Mathf.Approximately(sameHeight, 0f))
		{
			_floorMesh.Add(triangle);
		} else
		{
			_ceilingMesh.Add(triangle);
		}
	}


	private void AddTriangles(Wicket wicket1, Wicket wicket2, bool flipped = false)
	{
		for(var i = 0; i < 4; i++)
		{
			var next = i + 1;
			if (next >= 4)
			{
				next = 0;
			}

			var i2 = flipped? 3 - i: i;
			var next2 = flipped ? 3 - next : next;

			AddTriangle(new List<int> {
				wicket2[next2],
				wicket1[next],
				wicket1[i],
			});
			AddTriangle(new List<int> {
				wicket2[i2],
				wicket2[next2],
				wicket1[i],
			});
		};

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
			AddVertex(vertex);
		}
		return new Wicket(indices.ToArray());

	}

	private void AddVertex(Vector3 vertex)
	{
		_vertices.Add(vertex);
		_wallsMesh.Add(vertex);
		_floorMesh.Add(vertex);
		_ceilingMesh.Add(vertex);
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

	public Vector3 Vector;

	public Wicket(int[] points)
	{
		Points = points;
	}


	public IEnumerable<int> GetPoints => Points;

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