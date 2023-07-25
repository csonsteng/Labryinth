using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableManager : Singleton<InteractableManager>
{
	private readonly Dictionary<GameObject, Interactable> _interactables = new();

	private readonly HashSet<GameObject> _availableInteractables = new();

	private Interactable _targetInteractable;

	private int _layerMask;
	private int _wallOnlyMask;

	private bool _hasTarget;


	private void Awake()
	{
		_layerMask = LayerMask.GetMask(new string[] { "Interactables", "Default" });
		_wallOnlyMask = LayerMask.GetMask(new string[] { "Default" });
	}

	private void Update()
	{
		var direction = Player.FacingDirection;
		var ray = new Ray(transform.position, direction);
		_hasTarget = Physics.Raycast(ray, out var hitInfo, 7.5f, _layerMask);
		if (!_hasTarget)
		{
			UpdateTargetInteractable(null);
			return;
		}
		Debug.Log(hitInfo.collider.gameObject.name);
		if (CheckTargetInteractable(hitInfo))
		{
			return;
		}
		// we have a wall 8)
	}

	private bool CheckTargetInteractable(RaycastHit hitInfo)
	{
		var targetGameObject = hitInfo.collider.gameObject;
		if (!_availableInteractables.Contains(targetGameObject))
		{
			UpdateTargetInteractable(null);
			return false;
		}
		UpdateTargetInteractable(_interactables[targetGameObject]);
		return true;
		
	}

	private void UpdateTargetInteractable(Interactable interactable)
	{
		if(interactable == _targetInteractable)
		{
			return;
		}
		if (_targetInteractable != null)
		{
			_targetInteractable.HideCanvas();
		}
		_targetInteractable = interactable;
		if (_targetInteractable != null)
		{
			_targetInteractable.ShowCanvas();
		}
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
		if (_interactables.ContainsKey(collider.gameObject))
		{
			_availableInteractables.Add(collider.gameObject);
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (_interactables.ContainsKey(collider.gameObject))
		{
			_availableInteractables.Remove(collider.gameObject);
		}
	}

	public void OnInteractButtonPressed()
	{
		if (!_hasTarget)
		{
			return;
		}
		if (_targetInteractable != null)
		{
			_targetInteractable.Interact();
			return;
		}
		SprayWall();
	}

	private Vector3[] _faceSprayOffsets = new Vector3[]
	{
		Vector3.zero,
		Vector3.up,
		Vector3.down,
		Vector3.right,
		Vector3.left
	};

	private void SprayWall()
	{
		var pts = new List<Vector3>();
		foreach (var offset in _faceSprayOffsets)
		{
			var direction = Player.FacingDirection;
			var rotation = Quaternion.LookRotation(direction);

			var origin = transform.position + rotation * offset;
			var ray = new Ray(origin, direction);
			if(!Physics.Raycast(ray, out var hitInfo, 100f, _wallOnlyMask))
			{
				continue;
			}
			pts.Add(hitInfo.point);
		}

		foreach(var pt in pts)
		{
			Debug.DrawLine(transform.position, pt, Color.red, 3f);
		}
	}



}
