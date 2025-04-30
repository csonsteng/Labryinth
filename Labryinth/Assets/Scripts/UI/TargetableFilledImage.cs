using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetableFilledImage : Image
{

	// todo: Needs to work with more than just Top Origin Clockwise

	public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
	{
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var localMousePos))
		{
			return false;
		}

		var rect = GetPixelAdjustedRect();

		var initialVector = (Origin360)fillOrigin switch
		{
			Origin360.Top => new Vector2(0, rect.height),
			Origin360.Bottom => new Vector2(0, -rect.height),
			Origin360.Right => new Vector2(rect.width, 0),
			Origin360.Left => new Vector2(-rect.width, 0),
			_ => Vector2.zero,
		};


		float angle = Vector2.Angle(initialVector, localMousePos);

		if (localMousePos.x < 0)
		{
			angle = 360 - angle;
		}

		float currentMouseFill = angle / 360f;

		if (currentMouseFill < fillAmount)
		{
			return true;
		}
		return false;
	}
}