using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arm : MonoBehaviour
{
    [SerializeField] Transform _shoulder;
	[SerializeField] Transform _elbow;
	[SerializeField] Transform _wrist;

	public List<Position> Positions = new List<Position>();

	[System.Serializable]
	public class Position
	{
		public Vector3 ShoulderRotation;
		public Vector3 ElbowRotation;
		public Vector3 WristRotation;
	}

	[Button]
	public void TPose()
	{
		_shoulder.localEulerAngles = Vector3.zero;
		_elbow.localEulerAngles = Vector3.zero;
		_wrist.localEulerAngles = Vector3.zero;
	}

	[Button]
	public void SetPosition(int index)
	{
		var position = Positions[index];
		_shoulder.localEulerAngles = position.ShoulderRotation;
		_elbow.localEulerAngles = position.ElbowRotation;
		_wrist.localEulerAngles = position.WristRotation;
	}

	[Button]
	public void AddCurrentStateAsPosition()
	{
		Positions.Add(new Position()
		{
			ShoulderRotation = _shoulder.localEulerAngles,
			ElbowRotation = _elbow.localEulerAngles,
			WristRotation = _wrist.localEulerAngles,
		});
	}
}
