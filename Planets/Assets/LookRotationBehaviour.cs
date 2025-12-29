using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookRotationBehaviour : MonoBehaviour
{
    private FlightController fController;
    private SplitterSubscriber sub;
    [HideInInspector]
    public Transform LookTransform;

    public float rotationFactor;
    public float maxAngularVelocityChange;
    public float dampFactor;

    // Start is called before the first frame update
    void Start()
    {
        transform.TryGetComponent(out sub);
        transform.TryGetComponent(out fController);
    }

    public void SetLookTransform(Transform t)
    {
        LookTransform = t;
    }

    private void FixedUpdate()
    {
        if(fController.isActiveAndEnabled && fController.PoweredByDrainer)
            AlignRotationWithLook();
    }
    private void AlignRotationWithLook()
    {

        //bail if not stabilizable
        if (fController.GetStabilization() == 0f)
            return;

        Quaternion target = Quaternion.FromToRotation(sub.AppliedPhysics.rotation * Vector3.forward, LookTransform.forward);

        float slerpT = rotationFactor;


        float angle;
        Vector3 axis;
        (target).ToAngleAxis(out angle, out axis);

        Vector3 angularDisplacement = angle * axis * Mathf.Deg2Rad * slerpT;
        if (maxAngularVelocityChange != 0f)
            angularDisplacement = Vector3.ClampMagnitude(angularDisplacement, maxAngularVelocityChange);

        Vector3 damping = -Vector3.Project(sub.AppliedPhysics.angularVelocity, angularDisplacement.normalized) * dampFactor;

        sub.AppliedPhysics.AddTorque(angularDisplacement + damping, ForceMode.VelocityChange);
    }
}
