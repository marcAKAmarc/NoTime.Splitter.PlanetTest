using NoTime.Splitter;
using UnityEngine;


public static class RigidbodyRotationExtensions
{
    public static void SmoothRotate(this SplitterSubscriber body, Quaternion targetRotation, float maxTowardSpeed, float TowardFactor, float dampenFactor, float stabilizationCapability = 1f)
    {
        // Compute the change in orientation we need to impart.
        Quaternion rotationChange = targetRotation * Quaternion.Inverse(body.AppliedPhysics.rotation);

        // Convert to an angle-axis representation, with angle in range -180...180
        rotationChange.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
            angle -= 360f;

        // If we're already facing the right way, just stop.
        // This avoids problems with the infinite axes ToAngleAxis gives us in this case.
        /*if (Mathf.Approximately(angle, 0))
        {
            body.AppliedPhysics.AddTorque(-body.AppliedPhysics.angularVelocity * stabilizationCapability);
            return;
        }*/

        // If you need to, you can enforce a cap here on the maximum rotation you'll
        // allow in a single step, to prevent overly jerky movement from upsetting your sim.
        // angle = Mathf.Clamp(angle, -90f, 90f);

        // Convert to radians.
        angle *= Mathf.Deg2Rad;

        // Compute an angular velocity that will bring us to the target orientation
        // in a single time step.
        var targetAngularVelocity = axis * angle;

        // You can reduce this parameter to smooth the movement over multiple time steps,
        // to help reduce the effect of sudden jerks.
        float catchUp = TowardFactor;
        targetAngularVelocity *= catchUp;

        //Apply a speed limit
        if (targetAngularVelocity.sqrMagnitude > Mathf.Pow(maxTowardSpeed, 2f))
            targetAngularVelocity = targetAngularVelocity.normalized * maxTowardSpeed;

        // Apply a torque to finish the job.
        body.AppliedPhysics.AddTorque((targetAngularVelocity - body.AppliedPhysics.angularVelocity) * stabilizationCapability, ForceMode.VelocityChange);

        //apply damp
        float dotGoal = Mathf.Clamp01(Quaternion.Dot(rotationChange, body.AppliedPhysics.rotation));
        //body.AppliedPhysics.AddTorque(Vector3.zero, ForceMode.VelocityChange);
        body.AppliedPhysics.AddTorque(
            (-body.AppliedPhysics.angularVelocity) * dotGoal/*(2*dotGoal-Mathf.Pow(dotGoal,2))*/ * dampenFactor * stabilizationCapability,
            ForceMode.VelocityChange
        );
    }

    public static void SmoothRotate(this Rigidbody body, Quaternion targetRotation, float maxTowardSpeed, float TowardFactor, float dampenFactor, float stabilizationCapability = 1f)
    {
        // Compute the change in orientation we need to impart.
        Quaternion rotationChange = targetRotation * Quaternion.Inverse(body.rotation);

        // Convert to an angle-axis representation, with angle in range -180...180
        rotationChange.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
            angle -= 360f;

        // If we're already facing the right way, just stop.
        // This avoids problems with the infinite axes ToAngleAxis gives us in this case.
        /*if (Mathf.Approximately(angle, 0))
        {
            body.AppliedPhysics.AddTorque(-body.AppliedPhysics.angularVelocity * stabilizationCapability);
            return;
        }*/

        // If you need to, you can enforce a cap here on the maximum rotation you'll
        // allow in a single step, to prevent overly jerky movement from upsetting your sim.
        // angle = Mathf.Clamp(angle, -90f, 90f);

        // Convert to radians.
        angle *= Mathf.Deg2Rad;

        // Compute an angular velocity that will bring us to the target orientation
        // in a single time step.
        var targetAngularVelocity = axis * angle;

        // You can reduce this parameter to smooth the movement over multiple time steps,
        // to help reduce the effect of sudden jerks.
        float catchUp = TowardFactor;
        targetAngularVelocity *= catchUp;

        //Apply a speed limit
        if (targetAngularVelocity.sqrMagnitude > Mathf.Pow(maxTowardSpeed, 2f))
            targetAngularVelocity = targetAngularVelocity.normalized * maxTowardSpeed;

        // Apply a torque to finish the job.
        body.AddTorque((targetAngularVelocity - body.angularVelocity) * stabilizationCapability, ForceMode.VelocityChange);

        //apply damp
        float dotGoal = Mathf.Clamp01(Quaternion.Dot(rotationChange, body.rotation));
        //body.AppliedPhysics.AddTorque(Vector3.zero, ForceMode.VelocityChange);
        body.AddTorque(
            (-body.angularVelocity) * dotGoal/*(2*dotGoal-Mathf.Pow(dotGoal,2))*/ * dampenFactor * stabilizationCapability,
            ForceMode.VelocityChange
        );
    }
}
public class TestDampRotate : MonoBehaviour
{
    public Transform Target;
    public float maxTowardSpeed;
    public float TowardFactor;
    public float dampenFactor;
    private Rigidbody body;
    private SplitterSubscriber subscriberBody;
    private void Start()
    {
        body = transform.GetComponent<Rigidbody>();
        subscriberBody = transform.GetComponent<SplitterSubscriber>();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (subscriberBody == null)
            body.SmoothRotate(Target.rotation, maxTowardSpeed, TowardFactor, dampenFactor);
        else
            subscriberBody.SmoothRotate(Target.rotation, maxTowardSpeed, TowardFactor, dampenFactor);
        //ApplySmoothRotate(Target, maxTowardSpeed, TowardFactor, dampenFactor, body);
    }
    private void ApplySmoothRotate(Transform targetRotation, float maxTowardSpeed, float TowardFactor, float dampenFactor, Rigidbody body)
    {
        // Compute the change in orientation we need to impart.
        Quaternion rotationChange = targetRotation.rotation * Quaternion.Inverse(body.rotation);

        // Convert to an angle-axis representation, with angle in range -180...180
        rotationChange.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
            angle -= 360f;

        // If we're already facing the right way, just stop.
        // This avoids problems with the infinite axes ToAngleAxis gives us in this case.
        if (Mathf.Approximately(angle, 0))
        {
            body.angularVelocity = Vector3.zero;
            return;
        }

        // If you need to, you can enforce a cap here on the maximum rotation you'll
        // allow in a single step, to prevent overly jerky movement from upsetting your sim.
        // angle = Mathf.Clamp(angle, -90f, 90f);

        // Convert to radians.
        angle *= Mathf.Deg2Rad;

        // Compute an angular velocity that will bring us to the target orientation
        // in a single time step.
        var targetAngularVelocity = axis * angle / Time.deltaTime;

        // You can reduce this parameter to smooth the movement over multiple time steps,
        // to help reduce the effect of sudden jerks.
        float catchUp = TowardFactor;
        targetAngularVelocity *= catchUp;

        //Apply a speed limit
        if (targetAngularVelocity.sqrMagnitude > Mathf.Pow(maxTowardSpeed, 2f))
            targetAngularVelocity = targetAngularVelocity.normalized * maxTowardSpeed;

        // Apply a torque to finish the job.
        body.AddTorque(targetAngularVelocity - body.angularVelocity, ForceMode.VelocityChange);

        //apply damp
        float dot = Mathf.Clamp01(Quaternion.Dot(rotationChange, body.rotation));
        body.AddRelativeTorque(
            -(body.angularVelocity + targetAngularVelocity) * dot * dampenFactor,
            ForceMode.Acceleration
        );
    }



    private void Attempt1()
    {
        Quaternion GoalRotation = Target.rotation;
        Quaternion.Slerp(GoalRotation, body.rotation, .05f);
        var towardDiff = makeNegativable((Quaternion.Inverse(body.rotation) * GoalRotation).eulerAngles);
        /*if (towardDiff.sqrMagnitude > Mathf.Pow(maxTowardSpeed, 2f))
            towardDiff = towardDiff.normalized * maxTowardSpeed;*/
        body.AddRelativeTorque(
            (towardDiff - body.angularVelocity),
            ForceMode.Acceleration
        );

        float dot = Mathf.Clamp01(Quaternion.Dot(GoalRotation, body.rotation));
        body.AddRelativeTorque(
            -(body.angularVelocity + towardDiff) * dot * dampenFactor,
            ForceMode.Acceleration
        );
        Debug.Log("Dot: " + dot.ToString());
    }

    private static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime)
    {
        Vector3 c = current.eulerAngles;
        Vector3 t = target.eulerAngles;
        return Quaternion.Euler(
          Mathf.SmoothDampAngle(c.x, t.x, ref currentVelocity.x, smoothTime),
          Mathf.SmoothDampAngle(c.y, t.y, ref currentVelocity.y, smoothTime),
          Mathf.SmoothDampAngle(c.z, t.z, ref currentVelocity.z, smoothTime)
        );
    }
    private static Vector3 makeNegativable(Vector3 val)
    {
        while (val.x < -180f)
            val += Vector3.right * 360f;
        while (val.x > 180f)
            val -= Vector3.right * 360f;
        while (val.y < -180f)
            val += Vector3.up * 360f;
        while (val.y > 180f)
            val -= Vector3.up * 360f;
        while (val.z < -180f)
            val += Vector3.forward * 360f;
        while (val.z > 180f)
            val -= Vector3.forward * 360f;

        return val;
    }
}
