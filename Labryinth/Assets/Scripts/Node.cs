using System.Collections.Generic;
using UnityEngine;

public class Node
{
	public readonly NodeAddress Address;

	private float? x;
	private float? y;

	public List<NodeAddress> Neighbors = new();

	public GameObject GameObject;

	public Node(int radius, float theta)
	{
		Address = new NodeAddress(radius, theta);
	}

	public Vector3 Position(float scale)
	{
		return new Vector3(scale * X, 0f, scale * Y);
	}


	public bool TryGetRandomNeighbor(List<NodeAddress> exclusionList, out NodeAddress address)
	{
		var neighbors = new List<NodeAddress>();
		foreach(var neighbor in Neighbors)
		{
			if (!exclusionList.Contains(neighbor))
			{
				neighbors.Add(neighbor);
			}
		}
		if (neighbors.Count == 0)
		{
			address = new NodeAddress(0, 0f);
			return false;
		}
		var index = Random.Range(0, neighbors.Count);
		address = neighbors[index];
		return true;
	}
	private float X => CalculateX();

	private float CalculateX()
	{
		if (x == null)
		{
			x = Address.Radius * Mathf.Cos(Mathf.Deg2Rad * Address.Theta);
		}
		return (float)x;
	}

	private float Y => CalculateY();

	private float CalculateY()
	{
		if (y == null)
		{
			y = Address.Radius * Mathf.Sin(Mathf.Deg2Rad * Address.Theta);
		}
		return (float)y;
	}
}
