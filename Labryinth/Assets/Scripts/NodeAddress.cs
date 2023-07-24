using System;
using UnityEngine;

public struct NodeAddress
{
	public int Radius;
	public float Theta;



	public NodeAddress(int radius, float theta)
	{
		Radius = radius;
		Theta = theta;
	}

	public override string ToString()
	{
		return $"r: {Radius} theta: {Theta}";
	}

	public bool IsGreaterThan(NodeAddress otherAddress)
	{
		if (Radius > otherAddress.Radius)
		{
			return true;
		}

		if (otherAddress.Radius > Radius)
		{
			return false;
		}

		return Theta > otherAddress.Theta;
	}

	public float DistanceTo(NodeAddress otherAddress)
	{
		return Mathf.Sqrt(Mathf.Pow(Radius, 2) + Mathf.Pow(otherAddress.Radius, 2) - 2 * Radius * otherAddress.Radius * Mathf.Cos((otherAddress.Theta - Theta) * Mathf.Deg2Rad));
	}

}
