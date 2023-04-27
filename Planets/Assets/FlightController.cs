using NoTime.Splitter;
using NoTime.Splitter.Demo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightController : MonoBehaviour
{
    public Transform controller;
    public bool controllable = false;
    public bool controlled = false;
    public SplitterSubscriber body;
    public float MoveForce;
    public float RotateForce;
    public float RotationSensitivity;
    private float rotationX = 0F;
    private float rotationY = 0F;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<RigidbodyFpsController>())
        {
            controllable = true;
            controller = other.transform;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<RigidbodyFpsController>())
        {
            controllable = false;
            if(controller != null)
                controller.GetComponent<RigidbodyFpsController>().inControllerPosition = false;
            controller = null;
        }
    }
    void Update()
    {
        rotationX += Input.GetAxis("Mouse X") * RotationSensitivity;
        rotationY += Input.GetAxis("Mouse Y") * RotationSensitivity;
        if (controllable == true && Input.GetKeyDown(KeyCode.CapsLock))
        {
            controlled = !controlled;
            controller.GetComponent<RigidbodyFpsController>().inControllerPosition = controlled;
        }
    }
    private void FixedUpdate()
    {
        if (controlled)
        {
            Move();
            Rotate();
        }
    }
    private void Move()
    {
        
            Vector3 direction = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
                direction += Vector3.forward;
            if (Input.GetKey(KeyCode.A))
                direction += Vector3.left;
            if (Input.GetKey(KeyCode.D))
                direction += Vector3.right;
            if (Input.GetKey(KeyCode.S))
                direction += Vector3.back;
            if (Input.GetKey(KeyCode.Space))
                direction += Vector3.up;
            if (Input.GetKey(KeyCode.LeftShift))
                direction += Vector3.down;
            direction = direction.normalized;

            direction = body.AppliedPhysics.rotation * direction;


            body.AppliedPhysics.AddForce(direction * MoveForce * Time.fixedDeltaTime, ForceMode.Acceleration);
        
    }

    private void Rotate()
    {
        
        
        Vector3 direction = transform.InverseTransformDirection(transform.forward-controller.GetComponentInChildren<Camera>().transform.forward);
        direction = (new Vector3(direction.y, -direction.x, 0f)) * RotationSensitivity;
        if (direction.sqrMagnitude < Mathf.Pow(.3f, 2f))
        {
            direction = Vector3.zero;
        }

        if (Input.GetKey(KeyCode.Q))
            direction += Vector3.forward;
        if (Input.GetKey(KeyCode.E))
            direction += -Vector3.forward;
        
        //add force
        body.AppliedPhysics.AddRelativeTorque(direction * RotateForce * Time.fixedDeltaTime, ForceMode.Acceleration);
        

        
    }
}
