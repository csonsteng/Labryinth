using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AI;

public class PathRenderer : Singleton<PathRenderer>
{
	private CaveMeshGenerator _caveMeshGenerator;

	private List<Vector3> Vertices => _caveMeshGenerator.Vertices;

	public float CeilingHeight = 6f;
	public float WicketWidth = 7f;

	public GameObject ColliderTemplate;
	public Material CaveMaterial;
	public Material FinishMaterial;

	public Transform FinishFog;
	public void Destroy()
	{
		foreach(Transform child in transform)
		{
			Destroy(child.gameObject);
		}
	}

	public void Generate()
	{
		_caveMeshGenerator = new CaveMeshGenerator("Cave", CeilingHeight);


		foreach ((var currentNodeAddress, var currentNode) in Maze.NodeMap)
		{
			var adjacentWickets = new List<Wicket>();

			if(currentNode.AccessibleNeighbors.Count == 0)
			{
				continue;
			}

			var neighborList = currentNode.AllNeighbors;
			if (currentNodeAddress == Maze.EndNodeAddress)
			{
				neighborList = currentNode.AccessibleNeighbors;
				var singleNeighbor = currentNode.AccessibleNeighbors[0];
				int endBranchRadius;
				float endBranchTheta;
				if(singleNeighbor.Theta == currentNodeAddress.Theta)
				{
					endBranchRadius = currentNodeAddress.Radius + (currentNodeAddress.Radius - singleNeighbor.Radius);
					endBranchTheta = singleNeighbor.Theta;
				} else
				{
					endBranchRadius = currentNodeAddress.Radius;
					endBranchTheta = currentNodeAddress.Theta + (currentNodeAddress.Theta - singleNeighbor.Theta);
				}
				var endBranchNodeAddress = new NodeAddress(endBranchRadius, endBranchTheta);
				if(!Maze.NodeMap.TryGetValue(endBranchNodeAddress, out var endBranchNode))
				{
					endBranchNode = new Node(endBranchNodeAddress);
					endBranchNode.SetWorldPosition(MazeGenerator.Instance.Scale);
				}
				var wicket = MakeWicket(currentNode.Position, endBranchNode.Position, 0.60f);
				currentNode.Wickets[endBranchNodeAddress] = wicket;

				adjacentWickets.Add(wicket);
				MakeEnd(currentNode.Position, wicket);
			}


			foreach (var neighborAddress in neighborList)
			{
				// Make the closest wicket at each intersection
				var neighborNode = Maze.NodeMap[neighborAddress];
				var wicket = MakeWicket(currentNode.Position, neighborNode.Position, 0.22f);
				currentNode.Wickets[neighborAddress] = wicket;
				adjacentWickets.Add(wicket);		

				var path = new PathID(currentNodeAddress, neighborAddress);
				if(!Maze.Paths.TryGetValue(path, out _))
				{
					SealWicket(currentNode.Position, wicket);
				}
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

			if (adjacentWickets.Count == 3 && currentNodeAddress.Radius == MazeGenerator.Instance.Size) // for outer edge 3 way intersections add a sealed outer facing wicket
			{
				var outerAddress = new NodeAddress(currentNodeAddress.Radius + 1, currentNodeAddress.Theta);
				var outerNode = new Node(outerAddress);
				outerNode.SetWorldPosition(MazeGenerator.Instance.Scale);

				var wicket = MakeWicket(currentNode.Position, outerNode.Position, 0.22f);
				currentNode.Wickets[outerAddress] = wicket;

				adjacentWickets.Add(wicket);
				SealWicket(basePoint, wicket);
			}

			var intersectionCenter = Vector3.zero;

			foreach (var wicket in adjacentWickets)
			{
				intersectionCenter += AverageWicketLocation(wicket);
			}

			intersectionCenter /= adjacentWickets.Count;


			if (adjacentWickets.Count == 2)
			{
				foreach (var wicket in adjacentWickets)
				{
					wicket.Vector = AverageWicketLocation(wicket) - basePoint;
				}

				var twoWicketAngle = Vector3.Angle(adjacentWickets[0].Vector, adjacentWickets[1].Vector);

				// add a dummy wicket to help with spacing on sharp turn intersections
				if (twoWicketAngle <= 150)
				{
					var distanceToCenter = basePoint - intersectionCenter;
					var newWicket = MakeWicket(basePoint, basePoint + distanceToCenter, 1f);

					intersectionCenter *= adjacentWickets.Count;
					intersectionCenter += AverageWicketLocation(newWicket);

					adjacentWickets.Add(newWicket);
					intersectionCenter /= adjacentWickets.Count;

					SealWicket(basePoint, newWicket);
				}

			}
			
			var floorVertex = Vertices.Count;
			AddVertex(intersectionCenter);
			var ceilingVertex = Vertices.Count;
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

			foreach (var neighborAddress in currentNode.AccessibleNeighbors)
			{
				var neighborNode = Maze.NodeMap[neighborAddress];
				var pathID = new PathID(currentNodeAddress, neighborAddress);

				if (checkedPaths.Contains(pathID))
				{
					continue;
				}

				var wicket1 = currentNode.Wickets[neighborAddress];
				var wicket2 = MakeWicket(currentNode.Position, neighborNode.Position, 0.5f);
				var wicket3 = neighborNode.Wickets[currentNodeAddress];

				AddTriangles(wicket1, wicket2);
				AddTriangles(wicket2, wicket3, true);
				
				checkedPaths.Add(pathID);
			}

		}
		_caveMeshGenerator.Generate(transform, CaveMaterial);
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
		var point1 = Vertices[vertex1];
		var point2 = Vertices[vertex2];
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
		colliderObject.transform.localScale = new Vector3(1f, distance, 1f);

		colliderObject.transform.localEulerAngles = new Vector3(90f, angle, 0f);
		colliderObject.SetActive(true);	
		
	}

	private void SealWicket(Vector3 basePoint, Wicket wicket)
	{
		AddTriangle(OrientedTriangle(basePoint, new int[]
		{
			wicket[0],
			wicket[1],
			wicket[3]
		}));
		AddTriangle(OrientedTriangle(basePoint, new int[]
		{
			wicket[1],
			wicket[2],
			wicket[3]
		}));
		AddCollider(wicket[0], wicket[3]);
	}

	private void MakeEnd(Vector3 basePoint, Wicket wicket)
	{
		var center = Vector3.zero;

		var vertices = new List<Vector3>();
		foreach (var vertexIndex in wicket.GetPoints)
		{
			var vertex = Vertices[vertexIndex];
			vertices.Add(vertex);
			center += Vertices[vertexIndex];
		}
		center /= 4f;

		FinishFog.position = new Vector3(center.x, 0f, center.z);

		var plane = new Plane(vertices[0], vertices[1], vertices[3]);
		var normalToPlane = plane.normal;
		if (plane.GetSide(basePoint + Vector3.up))
		{
			normalToPlane = -normalToPlane;
		}
		vertices.Clear();
		normalToPlane = new Vector3(normalToPlane.x, 0f, normalToPlane.z);

		foreach (var vertexIndex in wicket.GetPoints)
		{
			var vertex = Vertices[vertexIndex];
			var directionFromCenter = (vertex - center).normalized;
			vertices.Add(vertex + 1f * directionFromCenter - normalToPlane * 0.05f);    // stretch it out
		}
		foreach (var vertex in vertices.ToArray())
		{
			vertices.Add(vertex + normalToPlane * 0.05f);
		}

		var mesh = new Mesh()
		{
			name = "Finish",
			vertices = vertices.ToArray(),
			triangles = new int[]
			{
				0, 1, 3,
				1, 2, 3,
				7, 5, 4,
				7, 6, 5,
				3, 2, 7,
				7, 2, 6,
				4, 5, 0,
				0, 5, 1,
				1, 5, 2,
				2, 5, 6,
				0, 3, 4,
				3, 7, 4
			},
			uv = new Vector2[]
			{
				Vector2.zero, Vector2.up, Vector2.one, Vector2.right,
				Vector2.right, Vector2.one, Vector2.up, Vector2.zero
			},
			normals = new Vector3[]
			{
				-normalToPlane, -normalToPlane, -normalToPlane, -normalToPlane,
				normalToPlane, normalToPlane, normalToPlane, normalToPlane,
			}
			
		};

		var endObject = new GameObject("Finish", new Type[] { typeof(Finish) })
		{
			layer = LayerMask.NameToLayer("Walls")
		};
		endObject.transform.parent = transform;
		endObject.transform.localScale = Vector3.one;
		endObject.transform.position = Vector3.zero;

		var collider = endObject.AddComponent<MeshCollider>();
		collider.sharedMesh = mesh;
		collider.convex = true;
		collider.isTrigger = true;
		var filter = endObject.AddComponent<MeshFilter>();
		filter.sharedMesh = mesh;
		var renderer = endObject.AddComponent<MeshRenderer>();
		renderer.material = FinishMaterial;
		
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
		var plane = new Plane(Vertices[triangle[0]], Vertices[triangle[1]], Vertices[triangle[2]]);
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
		var sum = Vertices[wicket[0]] + Vertices[wicket[^1]];

		return sum / 2f;
	}

	private void AddTriangle(List<int> triangle) 
	{
		if(triangle.Count == 0)
		{
			return;
		}
		_caveMeshGenerator.Add(triangle);
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
			indices.Add(Vertices.Count);
			AddVertex(vertex);
		}
		return new Wicket(indices.ToArray());

	}

	private void AddVertex(Vector3 vertex)
	{
		_caveMeshGenerator.Add(vertex);
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
		var perpindicular = basePoint.PerpindicularTo(new Vector3(normal.x, 0f, normal.z)).normalized;
		var ceilingType = UnityEngine.Random.Range(0, 3);
		var ceilingOffset = UnityEngine.Random.Range(0f, 1f);
		switch (ceilingType)
		{
			case 1:
				ceilingOffset = UnityEngine.Random.Range(1f, 2f);
				break;
			case 2:
				ceilingOffset = UnityEngine.Random.Range(2f, 5f);
				break;
		}
		var vertices = new List<Vector3>()
		{
			basePoint + perpindicular * (WicketWidth + UnityEngine.Random.Range(-0.5f, 0.5f)),
			basePoint + perpindicular * (0.7f * WicketWidth + UnityEngine.Random.Range(-1f, 0.5f)) + Vector3.up*(CeilingHeight + ceilingOffset + UnityEngine.Random.Range(-0.5f, 0.5f)),
			basePoint - perpindicular * (0.7f * WicketWidth + UnityEngine.Random.Range(-1f, 0.5f)) + Vector3.up*(CeilingHeight + ceilingOffset + UnityEngine.Random.Range(-0.5f, 0.5f)),
			basePoint - perpindicular * ( WicketWidth + UnityEngine.Random.Range(-0.5f, 0.5f)),
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