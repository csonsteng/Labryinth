using System.Collections.Generic;
using System.Net;
using UnityEngine;
using CaveCreator;

public class Node
{
	public readonly NodeAddress Address;

	private float? x;
	private float? y;

	public List<NodeAddress> AllNeighbors = new();
	public List<NodeAddress> AccessibleNeighbors = new();

	public GameObject GameObject;
	public Vector3 Position => _position;
	private Vector3 _position;

	public Node(int radius, float theta)
	{
		Address = new NodeAddress(radius, theta);
	}
	public Node(NodeAddress address)
	{
		Address = address;
	}

	public void SetWorldPosition(float scale)
	{
		_position = new Vector3(X * scale, 0f, Y * scale);
		if(GameObject != null)
		{
			GameObject.transform.position = _position;
		}
	}



	public bool TryGetRandomTraversableNeighbor(out NodeAddress address, List<NodeAddress> exclusionList = null)
	{
		var neighbors = new List<NodeAddress>();
		foreach (var neighbor in AccessibleNeighbors)
		{
			if (exclusionList == null || !exclusionList.Contains(neighbor))
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

	public float X => CalculateX();

	private float CalculateX()
	{
		if (x == null)
		{
			x = Address.Radius * Mathf.Cos(Mathf.Deg2Rad * Address.Theta);
		}
		return (float)x;
	}

	public float Y => CalculateY();

	private float CalculateY()
	{
		if (y == null)
		{
			y = Address.Radius * Mathf.Sin(Mathf.Deg2Rad * Address.Theta);
		}
		return (float)y;
	}
}
