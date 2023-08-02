using UnityEngine;

public partial class PathRenderer
{
	private class CaveBounds
	{
		float minX = float.MaxValue;
		float minZ = float.MaxValue;
		float maxX = float.MinValue;
		float maxZ = float.MinValue;

		public void CheckVertex(Vector3 vertex)
		{
			if(vertex.x < minX) minX = vertex.x;
			if(vertex.x > maxX) maxX = vertex.x;
			if (vertex.z < minZ) minZ = vertex.z;
			if (vertex.z > maxZ) maxZ = vertex.z;
		}

		public Bounds GetBounds()
		{
			var center = new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
			var size = new Vector3(maxX - minX, 0f, maxZ - minZ);
			return new Bounds(center, size);
		}
	}

}
