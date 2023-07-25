using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.UIElements;
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
	 */

	private List<Vector3> _dummyPositions = new();
	private List<Wicket> _dummyWickets = new();

	private class SubMesh
	{
		private List<Vector3> _vertices = new();
		private List<int> _triangles = new();
		public readonly string _name;


		public SubMesh(string name)
		{
			_name = name;
		}

		public List<Vector3> Vertices => _vertices;

		public void Add(Vector3 vertex) => _vertices.Add(vertex);
		public void Add(List<Vector3> vertices) => _vertices.AddRange(vertices);
		public void Add(List<int> indices)
		{
			_triangles.AddRange(indices);
		}

		public void FetchMeshData(out List<Vector3> vertices, out List<int> triangles)
		{
			CleanseUnusedVertices();
			vertices = _vertices;
			triangles = _triangles;
		}

		private void CleanseUnusedVertices()
		{
			var vertices = new List<Vector3>();
			var vertexMapping = new Dictionary<int, int>();
			var tris = new List<int>();

			foreach (var tri in _triangles)
			{
				if (!vertexMapping.TryGetValue(tri, out var newVertexValue))
				{
					newVertexValue = vertices.Count;
					vertices.Add(_vertices[tri]);
					vertexMapping[tri] = newVertexValue;
				}
				tris.Add(newVertexValue);
			}
			_triangles = tris;
			_vertices = vertices;
		}

	}

	private class MeshGenerator
	{
		private readonly List<Vector3> _vertices = new();
		private readonly List<int> _triangles = new();
		private readonly List<SubMesh> _subMeshes = new();
		private readonly string _name;

		public MeshGenerator(string name)
		{
			_name = name;
		}

		public void AddSubMesh(SubMesh subMesh) => _subMeshes.Add(subMesh);
		public void AddSubMeshes(List<SubMesh> subMeshes) => _subMeshes.AddRange(subMeshes);

		public void Generate(Transform parentTransform, Material material)
		{
			foreach(var subMesh in _subMeshes)
			{
				subMesh.FetchMeshData(out var subMeshVertices, out var subMeshTriangles);
				var startingVerticeCount =_vertices.Count;
				_vertices.AddRange(subMeshVertices);
				foreach(var tri in subMeshTriangles)
				{
					_triangles.Add(tri + startingVerticeCount);
				}
			}

			var meshObject = new GameObject($"{_name}_Mesh", new Type[]
			{
				//typeof(UVDebugger)
			});

			meshObject.layer = LayerMask.NameToLayer("Walls");
			meshObject.transform.parent = parentTransform;
			meshObject.transform.localScale = Vector3.one;
			meshObject.transform.localPosition = Vector3.zero;
			meshObject.transform.eulerAngles = Vector3.zero;

			BreakOutSubTris(_triangles, _vertices, out var vertices, out var triangles, out var uvs);
			SubDivide(6, triangles, out var finalTriangles, ref vertices, ref uvs);

			var mesh = new Mesh
			{
				vertices = vertices.ToArray(),
				triangles = finalTriangles.ToArray(),
				name = $"{_name}"
			};

			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();

			mesh.SetUVs(0, uvs.ToArray());
			meshObject.AddComponent<MeshFilter>().mesh = mesh;
			meshObject.AddComponent<MeshRenderer>().material = new Material(material);
			meshObject.AddComponent<MeshCollider>().sharedMesh = mesh;
		}

		private void BreakOutSubTris(List<int> triangles, List<Vector3> vertices, out List<Vector3> outVertices, out List<int> outTriangles, out List<Vector2> uvs)
		{
			outVertices = new List<Vector3>();
			uvs = new List<Vector2>();
			outTriangles = new List<int>();
			for (var i = 0; i < triangles.Count; i += 3)
			{
				var localVertices = new List<Vector3>()
				{
					vertices[triangles[i]],
					vertices[triangles[i+1]],
					vertices[triangles[i+2]],
				};
				outTriangles.AddRange(new List<int>()
				{
					outVertices.Count,
					outVertices.Count+1,
					outVertices.Count+2
				});
				outVertices.AddRange(localVertices);
				var plane = new Plane(localVertices[0], localVertices[1], localVertices[2]);
				var center = (localVertices[0] + localVertices[1] + localVertices[2]) / 3;
				plane.Translate(-center);
				var normal = plane.normal;

				var u = Vector3.Cross(normal, new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f))).normalized;
				var v = Vector3.Cross(normal, u).normalized;

				var mins = new Vector2(float.MaxValue, float.MaxValue);
				var maxes = new Vector2(float.MinValue, float.MinValue);

				var unScaledUVs = new List<Vector2>();
				foreach (var vertex in localVertices)
				{
					var vectorToCenter = vertex - center;

					var uComponent = Vector3.Dot(vectorToCenter, u);
					var vComponent = Vector3.Dot(vectorToCenter, v);

					if (uComponent < mins.x)
					{
						mins.x = uComponent;
					}
					if (uComponent > maxes.x)
					{
						maxes.x = uComponent;
					}
					if (vComponent < mins.y)
					{
						mins.y = vComponent;
					}
					if (vComponent > maxes.y)
					{
						maxes.y = vComponent;
					}
					unScaledUVs.Add(new Vector2(uComponent, vComponent));
				}

				foreach (var uv in unScaledUVs)
				{
					uvs.Add(new Vector2((uv.x - mins.x) / (maxes.x - mins.x), (uv.y - mins.y) / (maxes.y - mins.y)));
				}
			}
		}

		private void SubDivide(int divisions, List<int> inTriangles, out List<int> finalTriangles, ref List<Vector3> finalVertices, ref List<Vector2> finalUVs)
		{
			finalTriangles = new List<int>();


			Vector3[] vertices = new Vector3[3];
			Vector2[] uvs = new Vector2[3];
			float[] deltas = new float[3];
			for (var i = 0; i < inTriangles.Count; i += 3)
			{
				for (var j = 0; j < 3; j++)
				{
					vertices[j] = finalVertices[i + j];
				}
				for (var j = 0; j < 3; j++)
				{
					deltas[j] = (vertices[j] - vertices[j == 2 ? 0 : j + 1]).magnitude;
				}
				SubDivideIndividual(ref finalTriangles, ref finalVertices, ref finalUVs, new int[] { i, i+1, i+2 }, deltas, divisions - 1);
			}



		}

		private void SubDivideIndividual(ref List<int> finalTriangles, ref List<Vector3> finalVertices, ref List<Vector2> finalUVs, int[] triangle, float[] deltas, int remainingDivisions)
		{
			var maxDelta = float.MinValue;
			var firstSplitVertexLocalIndex = -1;
			for (var i = 0; i < 3; i++)
			{
				if (deltas[i] > maxDelta)
				{
					firstSplitVertexLocalIndex = i;
				}
			}

			var secondSplitVertexLocalIndex = firstSplitVertexLocalIndex == 2 ? 0 : firstSplitVertexLocalIndex + 1;

			var fistSplitIndex = triangle[firstSplitVertexLocalIndex];
			var secondSplitIndex = triangle[secondSplitVertexLocalIndex];

			var middleVertex = (finalVertices[fistSplitIndex] + finalVertices[secondSplitIndex]) / 2f;
			// noise doesn't work cause i don't share vertices across edges right now
			// var noiseVertex = new Vector3(UnityEngine.Random.Range(-0.01f, 0.1f), UnityEngine.Random.Range(-0.01f, 0.1f), UnityEngine.Random.Range(-0.01f, 0.01f));
			// var noisyVertex = middleVertex + noiseVertex;
			var middleVertexIndex = finalVertices.Count;
			finalVertices.Add(middleVertex);

			var middleUV = (finalUVs[fistSplitIndex] + finalUVs[secondSplitIndex]) / 2f;
			//var noisyUV = new Vector2(Mathf.Repeat(middleUV.x + UnityEngine.Random.Range(-0.01f, 0.01f), 1f), Mathf.Repeat(middleUV.y + UnityEngine.Random.Range(-0.01f, 0.01f), 1f));
			finalUVs.Add(middleUV);

			var nonSplitVertexLocalIndex = -1;
			for (var i = 0; i < 3; i++)
			{
				if (i == firstSplitVertexLocalIndex || i == secondSplitVertexLocalIndex)
				{
					continue;
				}

				nonSplitVertexLocalIndex = i;
				break;
			}



			var nonSplitIndex = triangle[nonSplitVertexLocalIndex];

			var triangle1 = new int[] { fistSplitIndex, middleVertexIndex, nonSplitIndex };
			var triangle2 = new int[] { middleVertexIndex , secondSplitIndex, nonSplitIndex };

			if (remainingDivisions == 0)
			{
				finalTriangles.AddRange(triangle1); 
				finalTriangles.AddRange(triangle2);
				return;
			}
			var splitDelta = maxDelta / 2f;
			var newDelta = (finalVertices[middleVertexIndex] - finalVertices[nonSplitIndex]).magnitude;
			var delta1 = new float[] { splitDelta, newDelta, deltas[nonSplitVertexLocalIndex] };
			var delta2 = new float[] { splitDelta, deltas[secondSplitVertexLocalIndex], newDelta };

			SubDivideIndividual(ref finalTriangles, ref finalVertices, ref finalUVs, triangle1, delta1, remainingDivisions - 1);
			SubDivideIndividual(ref finalTriangles, ref finalVertices, ref finalUVs, triangle2, delta2, remainingDivisions - 1);


		}
	}

	private SubMesh _primaryMesh;

	private List<SubMesh> _subMeshes = new();

	private List<Vector3> Vertices => _primaryMesh.Vertices;

	public float CeilingHeight = 6f;
	public float WicketWidth = 7f;

	public GameObject ColliderTemplate;
	public Material Material;

	public void Destroy()
	{
		foreach(Transform child in transform)
		{
			Destroy(child.gameObject);
		}
		_subMeshes.Clear();
	}

	public void Generate()
	{
		_primaryMesh = new SubMesh("Walls");


		foreach ((var currentNodeAddress, var currentNode) in Maze.NodeMap)
		{
			var adjacentWickets = new List<Wicket>();

			foreach (var neighborAddress in currentNode.Neighbors)
			{
				var pathID = new PathID(currentNodeAddress, neighborAddress);

				if (Maze.Paths.TryGetValue(pathID, out _))
				{
					// Make the closest wicket at each intersection
					var neighborNode = Maze.NodeMap[neighborAddress];
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
		var generator = new MeshGenerator("Cave");
		generator.AddSubMesh(_primaryMesh);
		generator.AddSubMeshes(_subMeshes);
		generator.Generate(transform, Material);
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

		var averagePoint = AverageWicketLocation(wicket);
		_dummyPositions.Add(averagePoint);

		_dummyWickets.Add(wicket);
		var newMesh = new SubMesh($"Wicket Seal {_dummyWickets.Count - 1}");

		foreach(var vertex in wicket.GetPoints)
		{
			newMesh.Add(Vertices[vertex]);
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
				if(vertex == Vertices[i])
				{
					localTriangles.Add(index);
					break;
				}
				index++;
			}
		}
		newMesh.Add(localTriangles);
		_subMeshes.Add(newMesh);

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
		_primaryMesh.Add(triangle);
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
		_primaryMesh.Add(vertex);
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
			basePoint + perpindicular * (WicketWidth + UnityEngine.Random.Range(-0.5f, 0.5f)),
			basePoint + perpindicular * (0.7f * WicketWidth + UnityEngine.Random.Range(-0.5f, 0.5f)) + Vector3.up*(CeilingHeight+ UnityEngine.Random.Range(-0.5f, 0.5f)),
			basePoint - perpindicular * (WicketWidth + UnityEngine.Random.Range(-0.5f, 0.5f)) + Vector3.up*(CeilingHeight+ UnityEngine.Random.Range(-0.5f, 0.5f)),
			basePoint - perpindicular * (0.7f * WicketWidth + UnityEngine.Random.Range(-0.5f, 0.5f)),
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