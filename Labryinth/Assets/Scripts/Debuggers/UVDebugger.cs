using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
public class UVDebugger : MonoBehaviour
{
	private void OnDrawGizmosSelected()
	{
		var meshFilter = GetComponent<MeshFilter>();
		if (meshFilter == null) return;

		var mesh = meshFilter.sharedMesh;
		if (mesh == null) return;

		var vertices = mesh.vertices;
		for(var i = 0;  i < vertices.Length; i++)
		{
			//Debug.DrawLine(vertices[i], vertices[i] + mesh.normals[i], Color.cyan);

			var vertex = transform.TransformPoint(vertices[i]);
			var uv = mesh.uv[i];

			var xUV = new Vector3(uv.x, 0f, 0f);
			Gizmos.color = Color.red;
			Gizmos.DrawLine(vertex, vertex + xUV);

			var yUV = new Vector3(0f, uv.y, 0f);
			Gizmos.color = Color.green;
			Gizmos.DrawLine(vertex, vertex + yUV);
		}
		return;
	}
}
