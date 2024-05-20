using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrassShipDoorBehavior : MonoBehaviour
{
    public bool Open = true;
    private HingeJoint joint;
    public Transform closedReference;
    private JointLimits openLimits, closedLimits;
    private Rigidbody body;

    private void Awake()
    {
        joint = transform.GetComponent<HingeJoint>();
        openLimits = new JointLimits()
        {
            min = -180,
            max = 180f
        };
        closedLimits = new JointLimits()
        {
            min = 0f,
            max = 0f
        };

        body = transform.GetComponent<Rigidbody>();
    }

    bool alignedWithClosure;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (!Open && !alignedWithClosure && Quaternion.Angle(body.rotation, closedReference.rotation) > 1f)
        {
            joint.useMotor = true;
            //body.AddRelativeTorque(Vector3.right * ClosingForce);
            //body.AddRelativeTorque(-transform.InverseTransformDirection(body.angularVelocity) * ClosingDamp);
        }
        else if(!Open)
        {
            joint.limits = closedLimits;
            alignedWithClosure = true;
            body.mass = .1f;
            joint.useMotor = false;
        }

        if (Open)
        {
            alignedWithClosure = false;
            body.mass = 10f;
            joint.useMotor = false;
            joint.limits = openLimits;
        }
            
    }
}
