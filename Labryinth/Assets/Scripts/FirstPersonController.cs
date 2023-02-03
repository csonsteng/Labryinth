using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{ 
    private bool _initialized;
    [SerializeField] private float _forwardSpeed = 100f;
    [SerializeField] private float _sideSpeed = 50f;
    [SerializeField] private float _cameraSpeed = 500f;

    [SerializeField] private Camera _characterCamera;
    [SerializeField] private CharacterController _controller;

    private bool _skipFrame = true;

    public void Initialize()
	{

        transform.position = Maze.StartNode.GameObject.transform.position + new Vector3(0f, 0.5f, 0f);
        
        Cursor.lockState = CursorLockMode.Locked;
        _initialized = true;
        _skipFrame = true;
	}
    // Update is called once per frame
    void Update()
    {
		if (!_initialized){
            return;
		}

		if (_skipFrame)
		{
            _skipFrame = false;
            return;
		}

        var strafeMovement = Input.GetAxis("Horizontal");
        var forwardMovement = Input.GetAxis("Vertical");

        var localMoveVector = new Vector3(strafeMovement * _forwardSpeed, 0f, forwardMovement * _sideSpeed) * Time.deltaTime;
        var convertedMoveVector = Quaternion.Euler(transform.localEulerAngles) * localMoveVector;

        _controller.Move(convertedMoveVector);

        //transform.localPosition += convertedMoveVector;

        var mouseInputX = Input.GetAxis("Mouse X");
        var mouseInputY = Input.GetAxis("Mouse Y");

        _characterCamera.transform.Rotate(Vector3.left, mouseInputY * _cameraSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up, mouseInputX * _cameraSpeed * Time.deltaTime);
    }

	private void OnCollisionEnter(Collision collision)
	{
        Debug.Log(collision.collider.gameObject.tag);
		if (collision.collider.gameObject.CompareTag("Finish"))
		{
            Debug.Log("YOU WIN!");
		}
	}
}
