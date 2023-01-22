using UnityEngine;

public class Path
{
	public PathID PathID;
	public GameObject GameObject;

	public Wicket[] Wickets = new Wicket[3];

	public Path(PathID ID)
	{
		PathID = ID;
	}

}