using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverheadCameraView : Singleton<OverheadCameraView> 
{
	[SerializeField] private Camera _overheadCamera;
	[SerializeField] private Material _mapMaterial;
	[SerializeField] private Shader _overheadShader;
	[SerializeField] private GameObject _mapBacking;

	private void Start()
	{
		_mapBacking.SetActive(false);
	}

	public async void SetCameraBounds(Bounds bounds)
	{
		_mapBacking.SetActive(true);
		_overheadCamera.transform.position = new Vector3(bounds.center.x, 90f, bounds.center.z);
		var size = bounds.size;
		var maxDimension = Mathf.Max(size.x, size.z);
		_overheadCamera.orthographicSize = 1.05f * maxDimension / 2f;
		await UniTask.DelayFrame(1);
		var texture = new RenderTexture(1024, 1024, 1);
		_overheadCamera.targetTexture = texture;
		_mapMaterial.mainTexture = texture;
		_overheadCamera.RenderWithShader(_overheadShader, "");
		await UniTask.DelayFrame(1);
		_overheadCamera.enabled = false;
		_mapBacking.SetActive(false);
	}
}
