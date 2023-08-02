using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AI;

namespace CaveCreator
{
	public class PathRenderer : Singleton<PathRenderer>
	{
		private CaveMeshGenerator _currentMesh;
		private CaveBounds _bounds = new();

		private List<Vector3> Vertices => _currentMesh.Vertices;

		[SerializeField] private float _ceilingHeight = 8f;
		[SerializeField] private float _wicketWidth = 7f;

		[SerializeField] private Material _caveMaterial;
		[SerializeField] private Material _finishMaterial;

		[SerializeField] private Transform _finishFog;

		private Dictionary<NodeAddress, Dictionary<NodeAddress, Wicket>> _wickets;

		public void Destroy()
		{
			foreach (Transform child in transform)
			{
				Destroy(child.gameObject);
			}
		}

		public void Generate()
		{
			_bounds = new CaveBounds();
			_wickets = new Dictionary<NodeAddress, Dictionary<NodeAddress, Wicket>>();

			var checkedPaths = new Dictionary<PathID, Wicket>();
			var generatedMeshes = new Dictionary<NodeAddress, CaveMeshGenerator>();
			foreach ((var currentNodeAddress, var currentNode) in Maze.NodeMap)
			{
				var adjacentWickets = new List<Wicket>();
				var normalMergers = new List<MeshEdgeMerger>();

				if (currentNode.AccessibleNeighbors.Count == 0)
				{
					// is an unused node
					continue;
				}
				_wickets.Add(currentNodeAddress, new Dictionary<NodeAddress, Wicket>());
				_currentMesh = new CaveMeshGenerator(currentNodeAddress.ToString(), _ceilingHeight);

				var neighborList = currentNode.AllNeighbors;
				if (currentNodeAddress == Maze.EndNodeAddress)
				{
					neighborList = currentNode.AccessibleNeighbors;
					var wicket = MakeDummyEndNode(currentNodeAddress, currentNode, adjacentWickets);
					adjacentWickets.Add(wicket);
				}


				foreach (var neighborAddress in neighborList)
				{
					// Make the closest wicket at each intersection
					var neighborNode = Maze.NodeMap[neighborAddress];
					var wicket = MakeWicket(currentNode.Position, neighborNode.Position, 0.22f);
					_wickets[currentNodeAddress][neighborAddress] = wicket;

					adjacentWickets.Add(wicket);

					var path = new PathID(currentNodeAddress, neighborAddress);
					if (!Maze.Paths.TryGetValue(path, out _))
					{
						SealWicket(currentNode.Position, wicket);
					}
				}

				var basePoint = currentNode.Position;

				// add a sealed outer facing wicket for for outer edge 3 way intersections 
				if (adjacentWickets.Count == 3 && currentNodeAddress.Radius == MazeGenerator.Instance.Size) 
				{
					var outerAddress = new NodeAddress(currentNodeAddress.Radius + 1, currentNodeAddress.Theta);
					var outerNode = new Node(outerAddress);
					outerNode.SetWorldPosition(MazeGenerator.Instance.Scale);

					var wicket = MakeWicket(basePoint, outerNode.Position, 0.22f);
					_wickets[currentNodeAddress][outerAddress] = wicket;

					adjacentWickets.Add(wicket);
					SealWicket(basePoint, wicket);
				}

				var floorVertex = Vertices.Count;
				AddVertex(basePoint, Vector3.up);
				var ceilingVertex = Vertices.Count;
				AddVertex(basePoint + Vector3.up * _ceilingHeight, Vector3.down);

				var orderedWickets = adjacentWickets.OrderBy(wicket =>
				{
					return Vector3.SignedAngle(Vector3.forward, AverageWicketLocation(wicket) - basePoint, Vector3.up);
				}).ToList();

				for (var i = 0; i < orderedWickets.Count; i++)
				{
					var wicket = orderedWickets[i];

					var nextIndex = i + 1 >= orderedWickets.Count ? 0 : i + 1;
					var nextWicket = orderedWickets[nextIndex];

					AddNodeTriangles(basePoint, floorVertex, ceilingVertex, wicket, nextWicket);
				}

				// connect neighboring meshing
				foreach (var neighborAddress in currentNode.AccessibleNeighbors)
				{
					var neighborNode = Maze.NodeMap[neighborAddress];
					var pathID = new PathID(currentNodeAddress, neighborAddress);

					var wicket1 = _wickets[currentNodeAddress][neighborAddress];

					if (checkedPaths.TryGetValue(pathID, out var wicket2))
					{
						var copiedWicket = CopyWicket(wicket2);
						AddNodeConnectionTriangles(copiedWicket, wicket1, true);
						normalMergers.Add(new MeshEdgeMerger()
						{
							MeshGenerator1 = generatedMeshes[neighborAddress],
							Mesh1Indices = wicket2.GetPoints.ToArray(),
							Mesh2Indices = copiedWicket.GetPoints.ToArray(),
						});
						continue;
					}
					wicket2 = MakeWicket(basePoint, neighborNode.Position, 0.5f);

					AddNodeConnectionTriangles(wicket1, wicket2);
					checkedPaths.Add(pathID, wicket2);
				}

				_currentMesh.Generate(transform, _caveMaterial);

				// merge edges if neighbor meshes are done.
				foreach (var merger in normalMergers)
				{
					merger.MeshGenerator2 = _currentMesh;
					merger.Merge();
				}

				generatedMeshes.Add(currentNodeAddress, _currentMesh);
			}

			OverheadCameraView.Instance.SetCameraBounds(_bounds.GetBounds());
		}
		private void AddVertex(Vector3 vertex, Vector3 normal)
		{
			_currentMesh.Add(vertex, normal);
			_bounds.CheckVertex(vertex);
		}

		/// <summary>
		/// Make a dummy end node, connects the path, and adds the end effect and collider
		/// todo: break out into somewhere that makes more sense
		/// </summary>
		/// <returns></returns>
		private Wicket MakeDummyEndNode(NodeAddress currentNodeAddress, Node currentNode, List<Wicket> adjacentWickets)
		{
			var singleNeighbor = currentNode.AccessibleNeighbors[0];
			int endBranchRadius;
			float endBranchTheta;
			if (singleNeighbor.Theta == currentNodeAddress.Theta)
			{
				endBranchRadius = currentNodeAddress.Radius + (currentNodeAddress.Radius - singleNeighbor.Radius);
				endBranchTheta = singleNeighbor.Theta;
			} else
			{
				endBranchRadius = currentNodeAddress.Radius;
				endBranchTheta = currentNodeAddress.Theta + (currentNodeAddress.Theta - singleNeighbor.Theta);
			}
			var endBranchNodeAddress = new NodeAddress(endBranchRadius, endBranchTheta);
			if (!Maze.NodeMap.TryGetValue(endBranchNodeAddress, out var endBranchNode))
			{
				endBranchNode = new Node(endBranchNodeAddress);
				endBranchNode.SetWorldPosition(MazeGenerator.Instance.Scale);
			}
			var wicket = MakeWicket(currentNode.Position, endBranchNode.Position, 0.60f);
			_wickets[currentNodeAddress][endBranchNodeAddress] = wicket;

			MakeEndMesh(currentNode.Position, wicket);
			return wicket;
		}


		private void MakeEndMesh(Vector3 basePoint, Wicket wicket)
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

			_finishFog.position = new Vector3(center.x, 0f, center.z);

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
			renderer.material = _finishMaterial;

		}

		private void SealWicket(Vector3 basePoint, Wicket wicket)
		{
			AddOrientedTriangles(basePoint, new int[]
			{
				wicket[0], wicket[1], wicket[3],
				wicket[1], wicket[2], wicket[3]
			});
		}

		private void AddNodeTriangles(Vector3 basePoint, int floorPoint, int ceilingPoint, Wicket wicket1, Wicket wicket2)
		{
			AddOrientedTriangles(basePoint, new int[]
			{
				wicket1[0], wicket2[3], wicket1[1],
				wicket2[2], wicket1[1], wicket2[3],
				wicket1[1], wicket2[2], ceilingPoint,
				wicket1[0], wicket2[3], floorPoint,
				wicket1[1], wicket1[2], ceilingPoint,
				wicket1[0], wicket1[3], floorPoint
			});
		}

		private void AddOrientedTriangles(Vector3 basePoint, int[] triangle)
		{
			for(var i = 0; i < triangle.Length; i += 3)
			{
				AddOrientedTriangle(basePoint, new int[]
				{
					triangle[i], triangle[i + 1], triangle[i + 2]
				});
			}
		}

		private void AddOrientedTriangle(Vector3 basePoint, int[] triangle)
		{
			var plane = new Plane(Vertices[triangle[0]], Vertices[triangle[1]], Vertices[triangle[2]]);
			if (plane.GetSide(basePoint + Vector3.up))
			{
				_currentMesh.Add(triangle.ToList());
				return;
			}

			_currentMesh.Add(new List<int>()
			{
				triangle[0],
				triangle[2],
				triangle[1],
			});
		}

		/// <summary>
		/// Returns the average of the bottom two vertices
		/// </summary>
		private Vector3 AverageWicketLocation(Wicket wicket)
		{
			var sum = Vertices[wicket[0]] + Vertices[wicket[^1]];

			return sum / 2f;
		}

		private void AddNodeConnectionTriangles(Wicket wicket1, Wicket wicket2, bool flipped = false)
		{
			for (var i = 0; i < 4; i++)
			{
				var next = i + 1;
				if (next >= 4)
				{
					next = 0;
				}

				var i2 = flipped ? 3 - i : i;
				var next2 = flipped ? 3 - next : next;

				_currentMesh.Add(new List<int> {
				wicket2[next2],
				wicket1[next],
				wicket1[i],
			});
				_currentMesh.Add(new List<int> {
				wicket2[i2],
				wicket2[next2],
				wicket1[i],
			});
			};
		}

		private Wicket MakeWicket(Vector3 start, Vector3 end, float distance, float widthFactor = 1f)
		{
			var basePoint = start.Lerp(end, distance);
			GetWicketVertices(basePoint, start, widthFactor, out var vertices, out var normals);

			var indices = new List<int>();
			for (var i = 0; i < vertices.Count; i++)
			{
				indices.Add(Vertices.Count);
				AddVertex(vertices[i], normals[i]);
			}
			return new Wicket(indices.ToArray(), vertices.ToArray(), normals.ToArray());

		}

		private Wicket CopyWicket(Wicket wicket)
		{
			var vertices = wicket.Vertices;
			var normals = wicket.Normals;

			var indices = new List<int>();
			for (var i = 0; i < vertices.Length; i++)
			{
				indices.Add(Vertices.Count);
				AddVertex(vertices[i], normals[i]);
			}
			return new Wicket(indices.ToArray(), vertices, normals);
		}



		private float BaseNoise => UnityEngine.Random.Range(-0.5f, 0.5f);
		private float HorizontalCeilingNoise => UnityEngine.Random.Range(-1f, 0.5f);
		private float VerticalCeilingNoise => UnityEngine.Random.Range(-0.5f, 0.5f);

		private void GetWicketVertices(Vector3 basePoint, Vector3 nodePoint, float widthFactor, out List<Vector3> vertices, out List<Vector3> normals)
		{
			var perpindicular = basePoint.PerpindicularTo(nodePoint).normalized;
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

			vertices = new List<Vector3>()
		{
			basePoint + perpindicular * (_wicketWidth * widthFactor + BaseNoise),
			basePoint + perpindicular * (0.7f * _wicketWidth * widthFactor + HorizontalCeilingNoise) + Vector3.up*(_ceilingHeight + ceilingOffset + VerticalCeilingNoise),
			basePoint - perpindicular * (0.7f * _wicketWidth * widthFactor + HorizontalCeilingNoise) + Vector3.up*(_ceilingHeight + ceilingOffset + VerticalCeilingNoise),
			basePoint - perpindicular * ( _wicketWidth * widthFactor + BaseNoise),
		};

			normals = new List<Vector3>();

			foreach (var vertex in vertices)
			{
				normals.Add((nodePoint - vertex + Vector3.up * 2f).normalized);
			}
		}

	}
}