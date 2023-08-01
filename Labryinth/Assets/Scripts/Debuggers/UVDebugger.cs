using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
public class UVDebugger : MonoBehaviour
{
	private void OnDrawGizmos()
	{
		var meshFilter = GetComponent<MeshFilter>();
		if (meshFilter == null) return;

		var mesh = meshFilter.sharedMesh;
		if (mesh == null) return;

		var vertices = mesh.vertices;
		for(var i = 0;  i < vertices.Length; i++)
		{
			Debug.DrawLine(vertices[i], vertices[i] + mesh.normals[i] * 3f, Color.cyan);
			var tangentV4 = mesh.tangents[i];
			var tangent = mesh.tangents[i].w * new Vector3(tangentV4.x, tangentV4.y, tangentV4.z);
			Debug.DrawLine(vertices[i], vertices[i] + tangent * 3f, Color.magenta);

			var vertex = transform.TransformPoint(vertices[i]);
			var uv = mesh.uv[i];

			var xUV = new Vector3(3f * uv.x, 0f, 0f);
			Gizmos.color = Color.red;
			Gizmos.DrawLine(vertex, vertex + xUV);

			var yUV = new Vector3(0f, 3f * uv.y, 0f);
			Gizmos.color = Color.green;
			Gizmos.DrawLine(vertex, vertex + yUV);
		}
		return;
	}
}
