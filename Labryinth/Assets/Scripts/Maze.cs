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


	public NodeAddress _startNode;
	public NodeAddress _endNode;


	public readonly Dictionary<NodeAddress, Node> _nodeMap = new();
	public readonly Dictionary<PathID, Path> _paths = new();

	public static NodeAddress RandomNodeAddress()
	{
		var allNodes = NodeMap.Keys.ToArray();
		var index = Random.Range(0, allNodes.Length);
		return allNodes[index];
	}
}
