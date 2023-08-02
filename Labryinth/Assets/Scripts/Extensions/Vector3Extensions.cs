using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

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

	public static Vector3 Lerp(this Vector3 vector3, Vector3 other, float distance)
	{
		var x = Mathf.Lerp(vector3.x, other.x, distance);
		var y = Mathf.Lerp(vector3.y, other.y, distance);
		var z = Mathf.Lerp(vector3.z, other.z, distance);

		return new Vector3(x, y, z);
	}
}