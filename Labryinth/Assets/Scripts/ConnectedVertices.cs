using UnityEngine;

namespace CaveCreator
{
	public struct ConnectedVertices
	{
		public Vector3 Vertex1;
		public Vector3 Vertex2;

		public ConnectedVertices(Vector3 vertex1, Vector3 vertex2)
		{

			if (vertex1.x > vertex2.x)
			{

				Vertex1 = vertex1; Vertex2 = vertex2;
			} else if (vertex2.x > vertex1.x)
			{
				Vertex2 = vertex1; Vertex1 = vertex2;
			} else if (vertex1.y > vertex2.y)
			{
				Vertex1 = vertex1;
				Vertex2 = vertex2;
			} else if (vertex2.y > vertex1.y)
			{
				Vertex2 = vertex1;
				Vertex1 = vertex2;
			} else if (vertex1.z > vertex2.z)
			{
				Vertex1 = vertex1;
				Vertex2 = vertex2;
			} else if (vertex2.z > vertex1.z)
			{
				Vertex2 = vertex1;
				Vertex1 = vertex2;
			} else
			{
				Vertex2 = vertex1; Vertex1 = vertex2;   // identicall???
			}
		}
	}
}

