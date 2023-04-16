using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class CanvasBillboard : MonoBehaviour
{	
	private Canvas _canvas;

	private void Awake()
	{
		_canvas = GetComponent<Canvas>();
	}

	private void Update()
	{
		_canvas.transform.rotation = Camera.main.transform.rotation;
	}
}
