using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PathRenderer : MonoBehaviour
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
	 * 
	 * Lets make our vertices first, and then we'll figure out how we want to draw our triangles
	 */
	private List<Vector3> _vertices = new List<Vector3>();
	private List<Vector3> _normals = new List<Vector3>();

	private List<int> _triangles = new List<int>();


	public void Generate(MazeGenerator maze)
	{

		_vertices = new List<Vector3>();
		_normals = new List<Vector3>();
		_triangles = new List<int>();

		var checkedPaths = new HashSet<PathID>();

		foreach ((var currentNodeAddress, var currentNode) in maze.NodeMap){

			foreach (var neighborAddress in currentNode.Neighbors)
			{
				var neighborNode = maze.NodeMap[neighborAddress];
				var pathID = new PathID(currentNodeAddress, neighborAddress);

				if (checkedPaths.Contains(pathID)){
					continue;
				}

				if (maze.Paths.TryGetValue(pathID, out var path))
				{

					var wicket1 = MakeWicket(currentNode.GameObject.transform.position, neighborNode.GameObject.transform.position, 0.15f);
					var wicket2 = MakeWicket(currentNode.GameObject.transform.position, neighborNode.GameObject.transform.position, 0.5f);
					var wicket3 = MakeWicket(currentNode.GameObject.transform.position, neighborNode.GameObject.transform.position, 0.85f);

					AddTriangles(wicket1, wicket2);
					AddTriangles(wicket2, wicket3);

					checkedPaths.Add(pathID);
				}

			}
		}


		var mesh = new Mesh
		{
			vertices = _vertices.ToArray(),
			triangles = _triangles.ToArray(),
			normals = _normals.ToArray(),
			name = "Maze"
		};

		GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;

	}

	private void AddTriangles(Wicket wicket1, Wicket wicket2)
	{
		var triangles = new List<int>();

		for(var i = 0; i < 4; i++)
		{
			var next = i + 1;
			if (next >= 4)
			{
				next = 0;
			}

			triangles.Add(wicket2.Corners[next]);
			triangles.Add(wicket1.Corners[next]);
			triangles.Add(wicket1.Corners[i]);


			triangles.Add(wicket2.Corners[i]);
			triangles.Add(wicket2.Corners[next]);
			triangles.Add(wicket1.Corners[i]);
		};

		_triangles.AddRange(triangles);

	}

	private Wicket MakeWicket(Vector3 start, Vector3 end, float distance)
	{
		var basePoint = Vector3Lerp(start, end, distance);
		var vertices = GetPlanarVertices(basePoint, start);

		var indices = new List<int>();
		foreach (var vertex in vertices)
		{
			indices.Add(_vertices.Count);
			_vertices.Add(vertex);

			_normals.Add(new Vector3(vertex.x - basePoint.x, vertex.y, vertex.z - basePoint.z));

		}
		var wicket = new Wicket
		{
			Corners = indices.ToArray()
		};

		return wicket;

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
			basePoint + perpindicular*2f,
			basePoint + perpindicular + Vector3.up*3f,
			basePoint - perpindicular + Vector3.up*3f,
			basePoint - perpindicular*2f,
		};
		return vertices;
	}

}
public class Wicket
{
	public int[] Corners = new int[4];

}