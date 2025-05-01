using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectatorController : Singleton<SpectatorController>
{
	[SerializeField] private Camera _camera;
	[SerializeField] private GameObject _light;
	private PlayerSettings _settings => Settings.Instance.PlayerSettings;
	private float HorizontalSensitivity => _settings.HorizontalSensitivity;
	private float VerticalSensitivity => _settings.VerticalSensitivity;
	private float WalkSpeed => _settings.WalkSpeed;
	private float RunMultiplier => _settings.RunMultiplier;
	private float StrafeSpeed => _settings.StrafeSpeed;

	private bool _canInteract;

	private void Start()
	{
		EnableCamera(false);
	}

	public void EnableCamera(bool enable)
	{
		_camera.gameObject.SetActive(enable);
		_light.SetActive(enable);
	}

	private void Update()
    {
        if (!GameManager.Instance.SpectatorMode) 
		{
			return;
		}
		if (Input.GetMouseButtonDown(0))
		{

		}
		UpdateLookDirection();
		UpdatePosition();
		CheckKeyPresses();
	}


	private void UpdatePosition()
	{
		var strafeMovement = Input.GetAxis("Horizontal");
		var forwardMovement = Input.GetAxis("Vertical");

		var running = Input.GetAxis("Run") > 0f;

		var forwardSpeed = WalkSpeed * (running ? RunMultiplier : 1f);
		var strafeSpeed = StrafeSpeed * (running ? RunMultiplier : 1f);

		var localMoveVector = new Vector3(strafeMovement * strafeSpeed, 0f, forwardMovement * forwardSpeed) * Time.deltaTime;
		
		var convertedMoveVector = Quaternion.Euler(transform.localEulerAngles + _camera.transform.localEulerAngles) * localMoveVector;

		transform.position += convertedMoveVector;
	}
	private void UpdateLookDirection()
	{
		var mouseInputX = Input.GetAxis("Mouse X");
		var mouseInputY = Input.GetAxis("Mouse Y");
		
		var pitch = _camera.transform.localEulerAngles.x;
		if (pitch > 90f)
		{
			pitch -= 360f;
		}
		pitch -= mouseInputY * VerticalSensitivity * Time.deltaTime;

		_camera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
		transform.Rotate(Vector3.up, mouseInputX * HorizontalSensitivity * Time.deltaTime);
	}

	private void CheckKeyPresses()
	{
		if (Input.GetAxis("Interact") <= 0f && Input.GetAxis("SecondaryInteract") <= 0f)
		{
			_canInteract = true;
			return;
		}
		if (!_canInteract)
		{
			return;
		}
		_canInteract = false;
		if (Input.GetAxis("Interact") > 0f)
		{
			transform.position = Player.Instance.transform.position;
			return;
		}
		transform.position = Enemy.Instance.transform.position;
	}
}
