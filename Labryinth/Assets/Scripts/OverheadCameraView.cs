using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverheadCameraView : MonoBehaviour
{
    public Camera OverheadCamera;
    public RawImage Image;

	private void Start()
	{
		var texture = new RenderTexture(Screen.width, Screen.height, 1);
		OverheadCamera.targetTexture = texture;
		Image.texture = texture;

		Image.SetNativeSize();
	}
}
