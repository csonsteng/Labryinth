public struct PathID
{
	public NodeAddress Address1;
	public NodeAddress Address2;

	public PathID(NodeAddress address1, NodeAddress address2)
	{
		if (address1.IsGreaterThan(address2))
		{
			Address1 = address1;
			Address2 = address2;
		} else
		{
			Address1 = address2;
			Address2 = address1;
		}
	}

	public override string ToString()
	{
		return $"Path from {Address1} to {Address2}";
	}
	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
	public override bool Equals(object obj)
	{
		if (obj is PathID otherPath)
		{
			return Address1.Equals(otherPath.Address1) && Address2.Equals(otherPath.Address2);
		}
		return false;
	}
}
