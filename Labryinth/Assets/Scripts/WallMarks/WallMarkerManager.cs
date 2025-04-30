using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallMarkerManager : MonoBehaviour
{
	[SerializeField] private WallMarkConfiguration[] _configurations;

	public Sprite[] WallMarkSprites { get; private set; }

	private static readonly int[] _triangles = new int[]
	{
		0, 1, 2,
		0, 2, 3,
		0, 3, 4,
		0, 4, 5,
		0, 5, 6,
		0, 6, 7,
		0, 7, 8,
		0, 8, 1
	};

	public static readonly Vector2[] UVs = new Vector2[]
	{
		new Vector2(0.5f, 0.5f),	// center
		new Vector2(0.5f, 1f), // top
		new Vector2(1f, 1f),
		new Vector2(1f, 0.5f),	// right
		new Vector2(1f, 0f),
		new Vector2(0.5f, 0f), // bottom
		new Vector2(0f, 0f),
		new Vector2(0f, 0.5f), // left
		new Vector2(0f, 1f),
	};

	public static readonly Vector2[] UVs_old = new Vector2[]
	{
		Vector2.zero,	// center
		new Vector2(0f, 1f), // top
		new Vector2(1f, 1f),
		new Vector2(1f, 0f),	// right
		new Vector2(1f, -1f),
		new Vector2(0f, -1f), // bottom
		new Vector2(-1f, -1f),
		new Vector2(-1f, 0f), // left
		new Vector2(-1f, 1f),
	};

	private void Start()
	{
		WallMarkSprites = new Sprite[_configurations.Length];
		for (var i = 0; i < _configurations.Length; i++)
		{
			WallMarkSprites[i] = _configurations[i].Sprite;
		}
	}

	public void MarkWall(List<Vector3> vertices, int materialSelection)
    {
		var material = _configurations[materialSelection].Material;
		var mesh = new Mesh
		{
			vertices = vertices.ToArray(),
			triangles = _triangles,
			uv = UVs
		};

		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();

		var meshObject = new GameObject($"WallMark");


		meshObject.transform.parent = transform;
		meshObject.transform.localScale = Vector3.one;
		meshObject.transform.localPosition = Vector3.zero;
		meshObject.transform.eulerAngles = Vector3.zero;

		meshObject.AddComponent<MeshFilter>().mesh = mesh;
		meshObject.AddComponent<MeshRenderer>().material = new Material(material);
	}
}
