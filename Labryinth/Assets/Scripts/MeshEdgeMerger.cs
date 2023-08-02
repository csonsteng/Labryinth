using System.Collections.Generic;
namespace CaveCreator
{
	public class MeshEdgeMerger
	{
		public CaveMeshGenerator MeshGenerator1;
		public CaveMeshGenerator MeshGenerator2;

		public int[] Mesh1Indices;
		public int[] Mesh2Indices;

		public void Merge()
		{
			var Mesh1 = MeshGenerator1.Mesh;
			var Mesh2 = MeshGenerator2.Mesh;

			var mesh1Normals = Mesh1.normals;
			var mesh2Normals = Mesh2.normals;
			var mesh1Tangents = Mesh1.tangents;
			var mesh2Tangents = Mesh2.tangents;
			var mesh1Vertices = Mesh1.vertices;
			var mesh2Vertices = Mesh2.vertices;

			for (var i = 0; i < Mesh1Indices.Length; i++)
			{
				var normal1 = mesh1Normals[Mesh1Indices[i]];
				var normal2 = mesh2Normals[Mesh2Indices[i]];
				var averageNormal = (normal1 + normal2) / 2f;
				mesh1Normals[Mesh1Indices[i]] = averageNormal;
				mesh2Normals[Mesh2Indices[i]] = averageNormal;

				var tangent1 = mesh1Tangents[Mesh1Indices[i]];
				var tangent2 = mesh2Tangents[Mesh2Indices[i]];
				var averageTangent = (tangent1 + tangent2) / 2f;
				mesh1Tangents[Mesh1Indices[i]] = averageTangent;
				mesh2Tangents[Mesh2Indices[i]] = averageTangent;


				var vertex1 = mesh1Vertices[Mesh1Indices[i]];
				var vertex2 = mesh1Vertices[Mesh1Indices[i == Mesh1Indices.Length - 1 ? 0 : i + 1]];
				var vertexInfo = new ConnectedVertices(vertex1, vertex2); // these vertices should be identical in both meshes

				var vertexPairings = new List<VertexPairing>()
				{
					new VertexPairing(vertexInfo, vertexInfo)
				};

				while (vertexPairings.Count > 0)
				{
					var currentVertex = vertexPairings[0];
					vertexPairings.RemoveAt(0);

					var info1 = currentVertex.VertexInfo1;
					var info2 = currentVertex.VertexInfo2;

					if (!MeshGenerator1.NewVertices.TryGetValue(info1, out var centerIndex1))
					{
						continue;
					}
					if (!MeshGenerator2.NewVertices.TryGetValue(info2, out var centerIndex2))
					{
						continue;
					}

					var centerVertex1 = mesh1Vertices[centerIndex1];
					var centerVertex2 = mesh2Vertices[centerIndex2];

					vertexPairings.Add(new VertexPairing(new ConnectedVertices(info1.Vertex1, centerVertex1), new ConnectedVertices(info2.Vertex1, centerVertex2)));
					vertexPairings.Add(new VertexPairing(new ConnectedVertices(info1.Vertex2, centerVertex1), new ConnectedVertices(info2.Vertex2, centerVertex2)));



					var averageVertex = (centerVertex1 + centerVertex2) / 2f;
					mesh1Vertices[centerIndex1] = averageVertex;
					mesh2Vertices[centerIndex2] = averageVertex;

					normal1 = mesh1Normals[centerIndex1];
					normal2 = mesh2Normals[centerIndex2];
					averageNormal = (normal1 + normal2) / 2f;
					mesh1Normals[centerIndex1] = averageNormal;
					mesh2Normals[centerIndex2] = averageNormal;

					tangent1 = mesh1Tangents[centerIndex1];
					tangent2 = mesh2Tangents[centerIndex2];
					averageTangent = (tangent1 + tangent2) / 2f;
					mesh1Tangents[centerIndex1] = averageTangent;
					mesh2Tangents[centerIndex2] = averageTangent;
				}
			}
			Mesh1.normals = mesh1Normals;
			Mesh2.normals = mesh2Normals;
			Mesh1.tangents = mesh1Tangents;
			Mesh2.tangents = mesh2Tangents;
			Mesh1.vertices = mesh1Vertices;
			Mesh2.vertices = mesh2Vertices;
		}

		private class VertexPairing
		{
			public ConnectedVertices VertexInfo1;
			public ConnectedVertices VertexInfo2;

			public VertexPairing(ConnectedVertices vertexInfo1, ConnectedVertices vertexInfo2)
			{
				VertexInfo1 = vertexInfo1;
				VertexInfo2 = vertexInfo2;
			}
		}


	}
}