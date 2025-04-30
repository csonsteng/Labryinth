using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DebugString : MonoBehaviour, IPointerClickHandler
{
	[SerializeField] private string _message = "Test";

	public void OnClick()
	{
		Debug.Log($"OnClick: {_message}");
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		Debug.Log($"OnPointerClick: {_message}");
	}
}
