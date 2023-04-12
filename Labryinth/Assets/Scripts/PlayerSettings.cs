using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class PlayerSettings : ScriptableObject
{
	public float HorizontalSensitivity = 350f;
	public float VerticalSensitivity = 350f;

	public float WalkSpeed = 7f;
	public float RunMultiplier = 1.8f;
	public float StrafeSpeed = 3f;

}
