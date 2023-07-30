using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extensions
{
	public static Vector2 AsVector2(this Vector3 vector3)
	{
		return new Vector2(vector3.x, vector3.z);
	}

	public static Vector3 PerpindicularTo(this Vector3 vector3, Vector3 other)
	{
		var perpindicular = Vector2.Perpendicular((other - vector3).AsVector2()).normalized;
		return new Vector3(perpindicular.x, vector3.y, perpindicular.y);
	}
}