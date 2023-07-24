using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

public class Pathfinder
{
	public class PathfindingNode
	{
		public PathfindingNode Parent;
		public HashSet<NodeAddress> Neighbors = new();
		public NodeAddress Address => _node.Address;

		public float H;
		public float G;
		public float F => H + G;

		private Node _node;

		public PathfindingNode(Node node)
		{
			_node = node;
		}

	}

	public Dictionary<NodeAddress, PathfindingNode> _nodes = new();

	public Pathfinder()
	{
		foreach(var path in Maze.Paths.Keys)
		{
			AddNode(path.Address1, path.Address2);
			AddNode(path.Address2, path.Address1);
		}

		void AddNode(NodeAddress address, NodeAddress neighbor)
		{
			if (!_nodes.TryGetValue(address, out var node1))
			{
				node1 = new PathfindingNode(Maze.NodeMap[address]);
				_nodes[address] = node1;
			}
			node1.Neighbors.Add(neighbor);
		}
	}

	public bool TryFindPath(NodeAddress startAddress, NodeAddress endAddress, out List<NodeAddress> result)
	{
		Debug.Log($"trying to path from {startAddress} to {endAddress}");

		result = new(); 
		if (startAddress.Equals(endAddress))
		{
			result.Add(endAddress);
			return true;
		}
		var lastNode = _nodes[endAddress];
		lastNode.H = endAddress.DistanceTo(startAddress);

		List<PathfindingNode> openSet = new();
		List<NodeAddress> closedSet = new();
		openSet.Add(lastNode);
		while (openSet.Count > 0)
		{
			var currentNode = openSet.OrderBy(node => node.F).First();

			if (currentNode.Address.Equals(startAddress))
			{
				closedSet.Add(startAddress);
				break;
			}
			foreach (var neighborAddress in currentNode.Neighbors)
			{
				if(!_nodes.ContainsKey(neighborAddress))
				{
					continue;
				}
				var neighborNode = _nodes[neighborAddress];
				float newG = currentNode.G + 1;
				if (closedSet.Contains(neighborAddress))
				{
					if (neighborNode.G > newG)
					{
						closedSet.Remove(neighborAddress);
						openSet.Add(neighborNode);
					}
				} else if (!openSet.Contains(neighborNode))
				{
					openSet.Add(neighborNode);
					neighborNode.H = neighborAddress.DistanceTo(startAddress);
					neighborNode.G = newG;
					neighborNode.Parent = currentNode;
				}

				if (neighborNode.G > newG)
				{
					neighborNode.G = newG;
					neighborNode.Parent = currentNode;
				}
				
			}
			closedSet.Add(currentNode.Address);
			openSet.Remove(currentNode);
		}


		if (!closedSet.Contains(startAddress) || !closedSet.Contains(endAddress))
		{
			Debug.LogWarning($"could not find path");
			return false;

		}
		var pathNode = _nodes[startAddress];
		while (!pathNode.Address.Equals(endAddress))
		{
			pathNode = pathNode.Parent;
			result.Add(pathNode.Address);
		}
		Debug.Log(new StringBuilder().AppendJoin(",", result).ToString());
		return true;
	}
}
