using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
	[SerializeField] private CanvasBillboard _canvas;
	[SerializeField] private Action _onInteract;

	private void Start()
	{
		gameObject.layer = LayerMask.NameToLayer("Interactables");
		InteractableManager.Instance.Register(this);
		HideCanvas();
	}

	public void SetInteractAction(Action callback)
	{
		_onInteract = callback;
	}

	public void Interact()
	{
		_onInteract?.Invoke();
	}

	public void ShowCanvas()
	{
		_canvas.gameObject.SetActive(true);
	}

	public void HideCanvas()
	{
		_canvas.gameObject.SetActive(false);
	}

	private void OnDestroy()
	{
		if (GameManager.IsRunning)
		{
			InteractableManager.Instance.DeRegister(this);
		}
	}
}
