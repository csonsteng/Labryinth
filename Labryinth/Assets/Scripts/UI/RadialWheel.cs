using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialWheel : MonoBehaviour
{
	[SerializeField] private RadialWheelItem _template;
	[SerializeField] private float _iconRadius;
	[SerializeField] private Sprite[] _testSprites;

	private void Start()
	{
		Setup();
	}
	public void Setup()
	{
		var anglePerSegment = 360f / _testSprites.Length;
		var fillAmount = anglePerSegment / 360f;
		var angle = 0f;
		var iconAngle = 90f - anglePerSegment / 2f;
		var iconPosition = new Vector2(_iconRadius * Mathf.Cos(Mathf.Deg2Rad * iconAngle), _iconRadius * Mathf.Sin(Mathf.Deg2Rad * iconAngle));
		foreach (var sprite in _testSprites)
		{
			var wheelItem = Instantiate(_template, _template.transform.parent);
			wheelItem.Setup(sprite, iconPosition, fillAmount, -angle, () =>
			{
				Debug.Log("clicked");
			});
			angle += anglePerSegment;
			wheelItem.gameObject.SetActive(true);
		}
		_template.gameObject.SetActive(false);
	}

}
