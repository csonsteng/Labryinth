using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableManager : Singleton<InteractableManager>
{
	[SerializeField] private WallMarkerManager _wallMarkerManager;

	private readonly Dictionary<GameObject, Interactable> _interactables = new();

	private readonly HashSet<GameObject> _availableInteractables = new();

	private Interactable _targetInteractable;

	private int _layerMask;
	private int _wallOnlyMask;

	private bool _hasTarget;


	private void Awake()
	{
		_layerMask = LayerMask.GetMask(new string[] { "Interactables", "Walls" });
		_wallOnlyMask = LayerMask.GetMask(new string[] { "Walls" });
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
		DrawOnWall();
	}

	private Vector3[] _faceSprayPlanePoints = new Vector3[]
	{
		new Vector3(0f, 0.707f, 0f),
		new Vector3(0.5f, -0.5f, 0f),
		new Vector3(-0.5f, -0.5f, 0f),
	};

	private void DrawOnWall()
	{
		var pts = new List<Vector3>();
		var direction = Player.FacingDirection;
		var rotation = Quaternion.LookRotation(direction);
		var averagePoint = Vector3.zero;
		foreach (var offset in _faceSprayPlanePoints)
		{
			var origin = transform.position + rotation * offset;
			var ray = new Ray(origin, direction);
			if (!Physics.Raycast(ray, out var hitInfo, 8f, _wallOnlyMask))
			{
				Debug.Log("Cannot draw here");
				return;
			}
			pts.Add(hitInfo.point);
			averagePoint += hitInfo.point;
		}

		averagePoint /= 3f;


		var plane = new Plane(pts[0], pts[1], pts[2]);
		direction = -plane.normal;
		rotation = Quaternion.LookRotation(direction);



		pts.Clear();
		foreach (var offset in WallMarkerManager.UVs)
		{
			var origin = averagePoint - direction + rotation * offset / 2f;
			var ray = new Ray(origin, direction);
			if (!Physics.Raycast(ray, out var hitInfo, 2f, _wallOnlyMask))
			{
				Debug.Log("Cannot draw here");
				return;
			}
			pts.Add(hitInfo.point - direction * 0.05f);
		}

		foreach(var pt in pts)
		{
			foreach(var pt2 in pts)
			{
				Debug.DrawLine(pt, pt2, Color.red, 3f);
			}
		}
		_wallMarkerManager.MarkWall(pts);
	}
}
