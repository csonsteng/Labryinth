using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableManager : Singleton<InteractableManager>
{
	private readonly Dictionary<GameObject, Interactable> _interactables = new();

	private readonly List<Interactable> _availableInteractables = new();

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
			EnableInteractable(interactable);
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (_interactables.TryGetValue(collider.gameObject, out var interactable))
		{
			DisableInteractable(interactable);
		}
	}

	public void EnableInteractable(Interactable interactable)
	{
		interactable.ShowCanvas();
		_availableInteractables.Add(interactable);
	}
	public void DisableInteractable(Interactable interactable)
	{
		interactable.HideCanvas();
		_availableInteractables.Remove(interactable);
	}

	public void OnInteractButtonPressed()
	{
		if(_availableInteractables.Count == 0)
		{
			return;
		}
		/*
		if (_availableInteractables.Count == 1)
		{
			_availableInteractables[0].Interact();
			return;
		}*/
		// todo: Determine which interactable we are facing
		// crossproduct of our rotation and canvas rotation = 0??

		_availableInteractables[0].Interact();

		// blahhhhhhh
		var interable = _availableInteractables[0];
		var canvasRotation = interable.CanvasRotation();
		var playerRotation = Player.Instance.transform.rotation;

		var angleCtoP = Quaternion.Angle(canvasRotation, playerRotation);
		var anglePtoC = Quaternion.Angle(playerRotation, canvasRotation);
		var dotCtoP = Quaternion.Dot(canvasRotation, playerRotation);
		var dotPtoC = Quaternion.Dot(playerRotation, canvasRotation);
		var xCtoP = canvasRotation * playerRotation;
		var xPtoC =playerRotation * canvasRotation;
		var vxCtoP = Vector3.Cross(canvasRotation.eulerAngles, playerRotation.eulerAngles);
		var vxPtoC = Vector3.Cross(playerRotation.eulerAngles, canvasRotation.eulerAngles);

		var all = "";
		AddToString(nameof(angleCtoP), angleCtoP.ToString());
		AddToString(nameof(anglePtoC), anglePtoC.ToString());
		AddToString(nameof(dotCtoP), dotCtoP.ToString());
		AddToString(nameof(dotPtoC), dotPtoC.ToString());
		AddToString(nameof(xCtoP), xCtoP.ToString());
		AddToString(nameof(xPtoC), xPtoC.ToString());
		AddToString(nameof(vxCtoP), vxCtoP.ToString());
		AddToString(nameof(vxPtoC), vxPtoC.ToString());



		Debug.Log(all);

		void AddToString(string variableName, string value)
		{
			all += $"{variableName} : {value}\n";
		}

	}



}
