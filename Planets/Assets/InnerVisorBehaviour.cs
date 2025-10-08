using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InnerVisorBehaviour : MonoBehaviour
{
    public SplitterSubscriber subscriber;
    public Camera camera;
    public Transform Player;
    public Vector3 initialOffset;
    public Quaternion initialRotation;

    private void Awake()
    {
        //Camera.onPreRender += setPosition;
        initialOffset = camera.transform.InverseTransformDirection(transform.position - camera.transform.position);
        initialRotation = transform.rotation;
    }
    void LateUpdate()
    {
        //if (cam == camera && false)
        //{
            /*transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(camera.transform.forward, Player.transform.up).normalized, Player.transform.up);
            transform.position = camera.transform.position + camera.transform.TransformDirection(initialOffset);*/
            transform.position = subscriber.AppliedPhysics.position + (subscriber.AppliedPhysics.rotation * initialOffset);
            transform.rotation = subscriber.AppliedPhysics.rotation * initialRotation;
        //}
    }

    private void OnDestroy()
    {
        //Camera.onPreRender -= setPosition;
    }
}
