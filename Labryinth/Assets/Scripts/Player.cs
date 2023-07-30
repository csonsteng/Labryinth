using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Singleton<Player>
{ 

    [SerializeField] private Camera _characterCamera;
    [SerializeField] private CharacterController _controller;

    private PlayerSettings _settings;
    private float HorizontalSensitivity => _settings.HorizontalSensitivity;
	private float VerticalSensitivity => _settings.VerticalSensitivity;
	private float WalkSpeed => _settings.WalkSpeed;
	private float RunMultiplier => _settings.RunMultiplier;
	private float StrafeSpeed => _settings.StrafeSpeed;

	public static Vector3 Position => Instance.gameObject.transform.position;
	public static Vector3 FacingDirection => Instance._facingDirection;


	private bool _canInteract = false;
	private Vector3 _facingDirection;

	private bool _initialized;

	private void Awake()
	{
		gameObject.SetActive(false);
		_initialized = false;
	}
	public void Initialize()
	{
        transform.position = Maze.StartNode.Position + new Vector3(0f, 1f, 0f);
        transform.localEulerAngles = Vector3.zero;
		_characterCamera.transform.localEulerAngles = Vector3.zero;
		_settings = Settings.Instance.PlayerSettings;
        gameObject.SetActive(true);
		_canInteract = true;
		_initialized = true;
	}

	public void OnGameOver()
	{
		_initialized = false;
	}

	private void Update()
	{
		if (!_initialized) return;
		if (Input.GetKeyDown(KeyCode.Escape)) {  }
		CheckInteractions();

		UpdatePosition();

		UpdateCameras();

		UpdateCachedFacingDirection();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(transform.position, 7.5f);
		Gizmos.DrawCube(transform.position + Quaternion.Euler(transform.localEulerAngles) * Vector3.forward * 10f, Vector3.one * 5f);
	}

	private void CheckInteractions()
	{
		if (Input.GetAxis("Interact") <= 0f)
		{
			_canInteract = true;	// pauper's debouncing
			return;
		}
		if(!_canInteract)
		{
			return;
		}
		_canInteract = false;
		InteractableManager.Instance.OnInteractButtonPressed();
	}

	private void UpdatePosition()
	{
		var strafeMovement = Input.GetAxis("Horizontal");
		var forwardMovement = Input.GetAxis("Vertical");

		var running = Input.GetAxis("Run") > 0f;

		var forwardSpeed = WalkSpeed * (running ? RunMultiplier : 1f);
		var strafeSpeed = StrafeSpeed * (running ? RunMultiplier : 1f);

		var localMoveVector = new Vector3(strafeMovement * strafeSpeed, 1f - Position.y, forwardMovement * forwardSpeed) * Time.deltaTime;
		var convertedMoveVector = Quaternion.Euler(transform.localEulerAngles) * localMoveVector;

		_controller.Move(convertedMoveVector);
	}
	private void UpdateCameras()
	{
		var mouseInputX = Input.GetAxis("Mouse X");
		var mouseInputY = Input.GetAxis("Mouse Y");

		var pitch = _characterCamera.transform.localEulerAngles.x;
		if (pitch > 90f)
		{
			pitch -= 360f;
		}
		pitch -= mouseInputY * VerticalSensitivity * Time.deltaTime;
		pitch = Mathf.Clamp(pitch, -25f, 25f);

		_characterCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
		transform.Rotate(Vector3.up, mouseInputX * HorizontalSensitivity * Time.deltaTime);
	}

	private void UpdateCachedFacingDirection()
	{
		var combinedEuler = new Vector3(_characterCamera.transform.localEulerAngles.x, transform.localEulerAngles.y, 0);
		_facingDirection = Quaternion.Euler(combinedEuler) * Vector3.forward;

		Debug.DrawRay(transform.position, _facingDirection, Color.green);
	}

}
