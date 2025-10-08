using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StabilizeRotationBehaviour : MonoBehaviour
{
    private SplitterSubscriber sub;
    private FlightController fController;
    public float DampAmt;

    public void Start()
    {
        transform.TryGetComponent(out sub);
        transform.TryGetComponent(out fController);
    }
    private void FixedUpdate()
    {
        //I think we always want this one to run when we are able
        //if(fController.PoweredByDrainer)
        if(fController.GetStabilization() > 0f)
            sub.AppliedPhysics.AddTorque(-sub.AppliedPhysics.angularVelocity * sub.AppliedPhysics.mass * DampAmt * fController.GetStabilization());
    }
}
