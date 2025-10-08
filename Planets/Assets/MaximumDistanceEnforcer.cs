using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaximumDistanceEnforcer : MonoBehaviour
{
    public Transform origin;
    public SplitterSubscriber sub;
    public float maximumDistanceFromOrigin;
    private Vector3 dirToOrigin;
    private Vector3 velAwayFromOrigin;
    public void FixedUpdate()
    {
        dirToOrigin = origin.position - sub.AppliedPhysics.position;
        //if out of bounds and heading away
        if (dirToOrigin.sqrMagnitude > Mathf.Pow(maximumDistanceFromOrigin, 2f)
            && Vector3.Dot(sub.AppliedPhysics.velocity.normalized, dirToOrigin.normalized) < 0)
        {
            velAwayFromOrigin = Vector3.Project(sub.AppliedPhysics.velocity, dirToOrigin);
            sub.AppliedPhysics.AddForce(-velAwayFromOrigin * sub.AppliedPhysics.mass);
        }

        
    }
}
