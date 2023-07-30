using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class ImageExtensions
{
	public static void SetAlpha(this Image image, float alpha)
	{
		var color = image.color;
		image.color = new Color(color.r, color.g, color.b, alpha);
	}
}
