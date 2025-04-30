using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialWheel : Singleton<RadialWheel>
{
	[SerializeField] private Transform _container;
	[SerializeField] private RadialWheelItem _template;
	[SerializeField] private float _iconRadius;
	[SerializeField] private Sprite[] _testSprites;

	private Action _onOpen;
	private Action _onClose;

	private readonly List<GameObject> _spawnedObjects = new();

	private void Start()
	{
		_container.localScale = Vector3.zero;
		_container.gameObject.SetActive(false);
		//Open(_testSprites, (option) => Debug.Log(option));
	}

	public void RegisterListeners(Action onOpen, Action onClose)
	{
		_onOpen += onOpen;
		_onClose += onClose;
	}
	private void Setup(Sprite[] options, Action<int> OnSelected)
	{
		var anglePerSegment = 360f / options.Length;
		var fillAmount = anglePerSegment / 360f;
		var angle = 0f;
		var iconAngle = 90f - anglePerSegment / 2f;
		var iconPosition = new Vector2(_iconRadius * Mathf.Cos(Mathf.Deg2Rad * iconAngle), _iconRadius * Mathf.Sin(Mathf.Deg2Rad * iconAngle));
		for (var i = 0; i < options.Length; i++)
		{
			var wheelItem = Instantiate(_template, _template.transform.parent);
			var option = i;
			wheelItem.Setup(options[i], iconPosition, fillAmount, -angle, () =>
			{
				OnSelected?.Invoke(option);
				Close();
			});
			angle += anglePerSegment;
			wheelItem.gameObject.SetActive(true);
			_spawnedObjects.Add(wheelItem.gameObject);
		}
		_template.gameObject.SetActive(false);
	}

	public void Open(Sprite[] options, Action<int> OnSelected)
	{
		_onOpen?.Invoke();
		Setup(options, OnSelected);
		_container.localScale = Vector3.zero;
		_container.gameObject.SetActive(true);
		_container.DOScale(1f, 0.2f);
	}

	private async void Close()
	{
		_onClose?.Invoke();
		await _container.DOScale(0f, 0.2f).ToUniTask();
		_container.gameObject.SetActive(false);
		foreach (var  wheelItem in _spawnedObjects)
		{
			Destroy(wheelItem);
		}
	}

}
