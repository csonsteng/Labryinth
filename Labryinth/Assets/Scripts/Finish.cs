using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Finish : MonoBehaviour
{
	private void Awake()
	{
		GetComponentInChildren<Interactable>().SetInteractAction(EndReached);
	}

	private void EndReached()
	{
		Debug.Log("you have escaped!");
	}
}
