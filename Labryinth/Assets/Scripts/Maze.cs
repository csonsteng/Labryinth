using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Maze: Singleton<Maze>
{
	public static Dictionary<NodeAddress, Node> NodeMap => Instance._nodeMap;
	public static Dictionary<PathID, Path> Paths => Instance._paths;
	public static Node StartNode => NodeMap[StartNodeAddress];
	public static Node EndNode => NodeMap[EndNodeAddress];

	public static NodeAddress StartNodeAddress
	{
		get
		{
			return Instance._startNode;
		}
		set
		{
			Instance._startNode = value;
		}
	}
	public static NodeAddress EndNodeAddress
	{
		get
		{
			return Instance._endNode;
		}
		set
		{
			Instance._endNode = value;
		}
	}


	private NodeAddress _startNode;
	private NodeAddress _endNode;

	private readonly Dictionary<NodeAddress, Node> _nodeMap = new();
	private readonly Dictionary<PathID, Path> _paths = new();

	private Pathfinder _pathFinder;

	public float Scale;

	public void CreatePathfinder()
	{
		_pathFinder = new Pathfinder();
	}

	public bool TryFindPath(NodeAddress startAddress, NodeAddress endAddress, out List<NodeAddress> result) => _pathFinder.TryFindPath(startAddress, endAddress, out result);

	public static NodeAddress RandomNodeAddress()
	{
		var allNodes = NodeMap.Keys.ToArray();
		var index = Random.Range(0, allNodes.Length);
		return allNodes[index];
	}

	public bool TryGetNearestNode(Vector3 position, out Node node)
	{
		var x = position.x / Scale;
		var y = position.z / Scale;

		var r = Mathf.RoundToInt(Mathf.Sqrt(x * x + y * y));
		var theta = Mathf.Atan2(y, x) * Mathf.Rad2Deg;

		if (theta < 0)
		{
			theta += 360f;
		}


		var layerDivisions = Mathf.Pow(2, (2 + Mathf.FloorToInt(r / 2f)));
		var step = 360f / layerDivisions;

		var steps = Mathf.RoundToInt(theta / step);

		var closestTheta = steps * step;

		if (Mathf.Approximately(closestTheta, 360f))
		{
			closestTheta = 0f;
		}

		var nodeAddress = new NodeAddress(r, closestTheta);

		if(!NodeMap.TryGetValue(nodeAddress, out node))
		{
			node = new Node(nodeAddress);
			return false;
		}
		return true;

	}
}
