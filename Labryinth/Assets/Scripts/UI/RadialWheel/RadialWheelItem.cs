using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadialWheelItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private Image _backing;
	[SerializeField] private Image _icon;

	private Action _callback;
	private Tween _scaleTween;

	public void Setup(Sprite sprite, Vector2 iconPosition, float fillAmount, float angle, Action callback)
	{
		_callback = callback;
		_icon.sprite = sprite;
		transform.localEulerAngles = new Vector3(0f, 0f, angle);
		_icon.transform.localPosition = iconPosition;
		_icon.transform.localEulerAngles = new Vector3(0f, 0f, -angle);
		_backing.fillAmount = fillAmount;
	}
    /// <summary>
    /// Exposed for Inspector
    /// </summary>
    public void OnClick()
    {
		_callback?.Invoke();
    }

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (_scaleTween.IsActive())
		{
			_scaleTween.Kill(false);
		}
		_scaleTween = transform.DOScale(1.05f, 0.1f);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (_scaleTween.IsActive())
		{
			_scaleTween.Kill(false);
		}
		_scaleTween = transform.DOScale(1f, 0.1f);
	}
}
