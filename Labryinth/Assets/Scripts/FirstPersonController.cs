using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public static FirstPersonController Instance => GetInstance();
    private static FirstPersonController _instance;
    public static FirstPersonController GetInstance()
	{
        if (_instance == null)
		{
            _instance = FindObjectOfType<FirstPersonController>();
		}
        return _instance;
	}

    private bool _initialized;
    [SerializeField] private float _forwardSpeed = 100f;
    [SerializeField] private float _sideSpeed = 50f;
    [SerializeField] private float _cameraSpeed = 500f;

    [SerializeField] private Camera _characterCamera;

    public void Initialize(Vector3 location)
	{
        transform.localPosition = new Vector3(location.x, 0.5f, location.z);
        
        Cursor.lockState = CursorLockMode.Locked;
        _initialized = true;
	}
    // Update is called once per frame
    void Update()
    {
		if (!_initialized){
            return;
		}

        var strafeMovement = Input.GetAxis("Horizontal");
        var forwardMovement = Input.GetAxis("Vertical");

        var localMoveVector = new Vector3(strafeMovement * _forwardSpeed, 0f, forwardMovement * _sideSpeed) * Time.deltaTime;
        var convertedMoveVector = Quaternion.Euler(transform.localEulerAngles) * localMoveVector;

        transform.localPosition += convertedMoveVector;

        var mouseInputX = Input.GetAxis("Mouse X");
        var mouseInputY = Input.GetAxis("Mouse Y");

        //_characterCamera.transform.Rotate(Quaternion.Euler(transform.localEulerAngles) * Vector3.left, mouseInputY * _cameraSpeed * Time.deltaTime);
        //_characterCamera.transform.localEulerAngles = new Vector3(_characterCamera.transform.localEulerAngles.x, _characterCamera.transform.localEulerAngles.y, 0f);
        transform.Rotate(Vector3.up, mouseInputX * _cameraSpeed * Time.deltaTime);
    }
}
