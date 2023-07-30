using System.Collections.Generic;
using UnityEngine;


public class CaveMeshGenerator
{
	private readonly float _ceilingHeight;
	private readonly List<Vector3> _vertices = new();
	private readonly List<int> _triangles = new();
	private readonly string _name;

	public CaveMeshGenerator(string name, float ceilingHeight)
	{
		_name = name;
		_ceilingHeight = ceilingHeight;
	}


	public List<Vector3> Vertices => _vertices;
	public void Add(Vector3 vertex) => _vertices.Add(vertex);
	public void Add(List<int> indices)
	{
		_triangles.AddRange(indices);
	}

	public void Generate(Transform parentTransform, Material material)
	{
		var meshObject = new GameObject($"{_name}_Mesh")
		{
			layer = LayerMask.NameToLayer("Walls")
		};
		meshObject.transform.parent = parentTransform;
		meshObject.transform.localScale = Vector3.one;
		meshObject.transform.localPosition = Vector3.zero;
		meshObject.transform.eulerAngles = Vector3.zero;

		var vertices = new List<Vector3>();
		vertices.AddRange(_vertices);

		SubDivide(2, _triangles, out var finalTriangles, ref vertices);

		var mesh = new Mesh
		{
			vertices = vertices.ToArray(),
			triangles = finalTriangles.ToArray(),
			name = $"{_name}"
		};

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		meshObject.AddComponent<MeshFilter>().mesh = mesh;
		meshObject.AddComponent<MeshRenderer>().material = new Material(material);
		meshObject.AddComponent<MeshCollider>().sharedMesh = mesh;
	}

	public struct TriLines
	{
		public int Index1;
		public int Index2;

		public TriLines(int index1, int index2)
		{
			if(index2 > index1)
			{
				Index1 = index1; Index2 = index2;
			} else
			{
				Index1 = index2; Index2 = index1;
			}
		}
	}

	private Vector3 AddNoise(Vector3 baseVertex)
	{
		var isFloorOrCeiling = baseVertex.y < 1.5f || baseVertex.y > _ceilingHeight - 1f;
		var lateralOffset = isFloorOrCeiling ? 0f : 0.35f;
		var yOffset = isFloorOrCeiling ? 0.15f : 0f; 
		return baseVertex + new Vector3(UnityEngine.Random.Range(-lateralOffset, lateralOffset), UnityEngine.Random.Range(-yOffset, yOffset), UnityEngine.Random.Range(-lateralOffset, lateralOffset));
	}

	private void SubDivide(int divisions, List<int> inTriangles, out List<int> finalTriangles, ref List<Vector3> finalVertices)
	{
		if (divisions == 0)
		{
			finalTriangles = inTriangles;
			return;
		}
		var tempTriangles = new List<int>();

		var addedVertices = new Dictionary<TriLines, int>();


		for (var i = 0; i < inTriangles.Count; i += 3)
		{
			var index0 = inTriangles[i];
			var index1 = inTriangles[i+1];
			var index2 = inTriangles[i+2];


			var triLine01 = new TriLines(index0, index1);
			var triLine12 = new TriLines(index1, index2);
			var triLine20 = new TriLines(index2, index0);

			if(!addedVertices.TryGetValue(triLine01, out var index01))
			{
				var v = AddNoise((finalVertices[index0] + finalVertices[index1]) / 2f);
				index01 = finalVertices.Count;
				finalVertices.Add(v);
				addedVertices.Add(triLine01, index01);
			}
			if (!addedVertices.TryGetValue(triLine12, out var index12))
			{
				var v = AddNoise((finalVertices[index1] + finalVertices[index2]) / 2f);
				index12 = finalVertices.Count;
				finalVertices.Add(v);
				addedVertices.Add(triLine12, index12);
			}
			if (!addedVertices.TryGetValue(triLine20, out var index20))
			{
				var v = AddNoise((finalVertices[index2] + finalVertices[index0]) / 2f);
				index20 = finalVertices.Count;
				finalVertices.Add(v);
				addedVertices.Add(triLine20, index20);
			}

			var center = AddNoise((finalVertices[index0] + finalVertices[index1] + finalVertices[index2]) / 3f);
			var centerIndex = finalVertices.Count;
			finalVertices.Add(center);

			var tris = new List<int>()
			{
				index0, index01, centerIndex,
				index01, index1, centerIndex,
				index1, index12, centerIndex,
				index12, index2, centerIndex,
				index2, index20, centerIndex,
				index20, index0, centerIndex
			};
			tempTriangles.AddRange(tris);
		}
		SubDivide(divisions - 1, tempTriangles, out finalTriangles, ref finalVertices);
	}
}

