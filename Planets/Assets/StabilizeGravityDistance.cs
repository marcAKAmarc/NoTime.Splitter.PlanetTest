using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StabilizeGravityDistance : MonoBehaviour
{
    public GravityObject gravInfo;
    public float maxForce;
    public float minForce;
    public SplitterSubscriber sub;
    public bool includeVelocityTowardGravity;
    private Vector3 agFrc;
    private Vector3 fvTo;

    public FlightController flightController;
    public void FixedUpdate()
    {
        if (!flightController.PoweredByDrainer)
            return;

        if (!gravInfo.ApplyGravity || gravInfo.GravityAcceleration == 0f)
            return;

        Vector3 gFrc = gravInfo.GravityAcceleration * gravInfo.GravityDirection.normalized * sub.Body.mass;
        if (includeVelocityTowardGravity)
        {
            fvTo = (Vector3.Project(sub.AppliedPhysics.velocity, gravInfo.GravityDirection)) * sub.Body.mass;      
        }
        //agFrc = Vector3.ClampMagnitude(-gFrc, maxForce);
        agFrc = (-gFrc) + (-fvTo);

        if (agFrc.sqrMagnitude > Mathf.Pow(maxForce * sub.Body.mass * Time.fixedDeltaTime, 2f))
            agFrc = agFrc.normalized * maxForce * sub.Body.mass * Time.fixedDeltaTime;
        /*if (agFrc.sqrMagnitude < Mathf.Pow(minForce * sub.Body.mass * Time.fixedDeltaTime, 2f))
            return;*/

        flightController.AddExternalDisplayInput(
            new Vector3(
                Mathf.Round(agFrc.normalized.x),
                Mathf.Round(agFrc.normalized.y),
                Mathf.Round(agFrc.normalized.z)
            )
        );

        sub.AppliedPhysics.AddForce(
            agFrc
        );
    }

    public void OnActivate()
    {
        this.enabled = !this.enabled;
    }
}
