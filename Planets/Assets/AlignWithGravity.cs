using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignWithGravity : MonoBehaviour
{
    private Vector3 GravityForce;
    public SplitterSubscriber body;
    // Start is called before the first frame update
    private void AlignRotationWithGravity()
    {
        GravityForce = transform.GetComponent<GravityObject>().GravityDirection * transform.GetComponent<GravityObject>().GravityForce;
        
            Quaternion target = Quaternion.FromToRotation(-body.transform.up, GravityForce.normalized) * body.AppliedPhysics.rotation;
            body.AppliedPhysics.MoveRotation(Quaternion.Slerp(body.AppliedPhysics.rotation, target, .1f));
        
    }

    private void FixedUpdate()
    {
        AlignRotationWithGravity();
    }
}
