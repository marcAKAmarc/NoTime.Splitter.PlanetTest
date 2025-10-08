using NoTime.Splitter;
using UnityEngine;

public class AlignWithGravity : SplitterEventListener
{
    private Vector3 GravityForce;
    public SplitterSubscriber body;
    public float rotationFactor = .05f;
    public float maxAngularVelocityChange = 0f;
    public float dampFactor = .005f;
    public float activeDistance = 260f;

    public bool AlignBasedOnGravityStrength = true;
    public bool RequireInAnchor = true;

    public FlightController optionalFlightController;

    public GravityObject gravObj;
    // Start is called before the first frame update
    public void Start()
    {
        if(gravObj == null)
            gravObj = transform.GetComponent<GravityObject>();
        body = transform.GetComponent<SplitterSubscriber>();
    }
    private void AlignRotationWithGravity()
    {
        if (optionalFlightController && (!optionalFlightController.isActiveAndEnabled || !optionalFlightController.PoweredByDrainer))
            return;
        /*if (gravObj.GravityDistance > activeDistance)
            return;*/
        if (RequireInAnchor && body.Anchor == null)
            return;

        //bail if flightcontroller and not stabilizable
        if (optionalFlightController != null && optionalFlightController.GetStabilization() != 1f)
            return;

        GravityForce = gravObj.GravityDirection * transform.GetComponent<GravityObject>().GravityAcceleration;
        Quaternion target = Quaternion.FromToRotation(body.AppliedPhysics.rotation * Vector3.down, GravityForce.normalized);
        
        float slerpT;
        if (AlignBasedOnGravityStrength)
            slerpT = rotationFactor * gravObj.GravityAcceleration / 9.8f;
        else
            slerpT = rotationFactor;

        float angle;
        Vector3 axis;
        (target).ToAngleAxis(out angle, out axis);

        Vector3 angularDisplacement = angle * axis * Mathf.Deg2Rad * slerpT;
        if (maxAngularVelocityChange != 0f)
            angularDisplacement = Vector3.ClampMagnitude(angularDisplacement, maxAngularVelocityChange);

        Vector3 damping = -Vector3.Project(body.AppliedPhysics.angularVelocity, angularDisplacement.normalized) * dampFactor;

        body.AppliedPhysics.AddTorque(angularDisplacement + damping, ForceMode.VelocityChange);
        /*body.AppliedPhysics.MoveRotation(
            Quaternion.Slerp(
                body.AppliedPhysics.rotation,
                target * body.AppliedPhysics.rotation,
                slerpT
        ));*/

        
    }

    private void FixedUpdate()
    {
        if(!optionalFlightController || (optionalFlightController.isActiveAndEnabled && optionalFlightController.PoweredByDrainer))
            AlignRotationWithGravity();
    }

    public void OnActivate()
    {
        this.enabled = !this.enabled;
    }
}
