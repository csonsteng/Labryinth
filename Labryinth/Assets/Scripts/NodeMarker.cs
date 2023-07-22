using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeMarker : MonoBehaviour
{
    [HideInInspector]
    public NodeAddress Address;
	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.CompareTag("Enemy"))
		{
			Enemy.Instance.InformOfPosition(Address);
		}
	}
}
