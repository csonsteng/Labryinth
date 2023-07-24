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
}
