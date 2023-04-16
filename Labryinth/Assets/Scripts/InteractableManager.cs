using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableManager : Singleton<InteractableManager>
{
	private readonly Dictionary<GameObject, Interactable> _interactables = new();

	private readonly List<Interactable> _availableInteractables = new();

	private Interactable _targetInteractable;

	private void Update()
	{
		CheckTargetInteractable();
	}

	private void CheckTargetInteractable()
	{
		foreach (var interactable in _availableInteractables)
		{
			var direction = Player.FacingDirection;
			var ray = new Ray(transform.position, direction);
			if (!Physics.Raycast(ray, out var hitInfo, 5f))
			{
				continue;
			}
			if (hitInfo.collider.gameObject != interactable.gameObject)
			{
				continue;
			}
			if(interactable == _targetInteractable)
			{
				return;
			}
			UpdateTargetInteractable(interactable);
			interactable.ShowCanvas();
			return;
		}
		UpdateTargetInteractable(null);
	}

	private void UpdateTargetInteractable(Interactable interactable)
	{
		if (_targetInteractable != null)
		{
			_targetInteractable.HideCanvas();
		}
		_targetInteractable = interactable;
	}

	public void Register(Interactable interactable)
	{
		if (_interactables.ContainsKey(interactable.gameObject))
		{
			throw new System.ArgumentException($"{interactable} already in list");
		}

		_interactables[interactable.gameObject] = interactable;
	}

	public void DeRegister(Interactable interactable)
	{
		_interactables.Remove(interactable.gameObject);
	}

	private void OnTriggerEnter(Collider collider)
	{ 
		if (_interactables.TryGetValue(collider.gameObject, out var interactable))
		{
			_availableInteractables.Add(interactable);
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (_interactables.TryGetValue(collider.gameObject, out var interactable))
		{
			_availableInteractables.Remove(interactable);
		}
	}

	public void OnInteractButtonPressed()
	{
		if(_targetInteractable == null)
		{
			return;
		}
		_targetInteractable.Interact();
	}



}
