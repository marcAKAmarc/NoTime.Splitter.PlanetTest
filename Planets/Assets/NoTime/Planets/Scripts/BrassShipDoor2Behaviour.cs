using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrassShipDoor2Behaviour : MonoBehaviour
{
    public float HingeAttachmentForce;
    public float HingeAttachmentSlowForce;
    public SplitterAnchor ShipAnchor;
    public SplitterSubscriber ShipSubscriber;
    public Transform HingePosition;
    public Transform HingePositionSelf;
    private Rigidbody body;
    private SplitterSubscriber subscriber;
    // Start is called before the first frame update
    void Start()
    {
        body = transform.GetComponent<Rigidbody>();
        subscriber = transform.GetComponent<SplitterSubscriber>();

        //subscriber.ManualEnterAnchor(ShipAnchor);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        /*subscriber.AppliedPhysics.AddForceAtPosition(
            (HingePosition.position - HingePositionSelf.position) * HingeAttachmentForce,
            HingePositionSelf.position
        );
        subscriber.AppliedPhysics.AddForce(
            -(
                Vector3.Project(
                    subscriber.AppliedPhysics.velocity - ShipSubscriber.AppliedPhysics.velocity,
                    HingePosition.position - HingePositionSelf.position
                ) * HingeAttachmentSlowForce
            )
        );*/
        //keep on hinge point
        subscriber.AppliedPhysics.MovePosition(subscriber.AppliedPhysics.position + (HingePosition.position - HingePositionSelf.position));
        subscriber.AppliedPhysics.MoveRotation(subscriber.AppliedPhysics.rotation * Quaternion.FromToRotation(HingePositionSelf.right, HingePosition.right));

        subscriber.AppliedPhysics.MoveRotation(subscriber.AppliedPhysics.rotation * Quaternion.AngleAxis(1f, Vector3.right));
    }
}
