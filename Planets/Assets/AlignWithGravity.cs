using NoTime.Splitter;
using UnityEngine;

public class AlignWithGravity : SplitterEventListener
{
    private Vector3 GravityForce;
    public SplitterSubscriber body;
    // Start is called before the first frame update
    public void Start()
    {
        body = transform.GetComponent<SplitterSubscriber>();
    }
    private void AlignRotationWithGravity()
    {
        if (transform.GetComponent<GravityObject>().GravityDistance > 300f)
            return;
        GravityForce = transform.GetComponent<GravityObject>().GravityDirection * transform.GetComponent<GravityObject>().GravityAcceleration;
        Quaternion target = Quaternion.FromToRotation(body.AppliedPhysics.rotation * Vector3.down, GravityForce.normalized);
        body.AppliedPhysics.MoveRotation(
            Quaternion.Slerp(
                body.AppliedPhysics.rotation,
                target * body.AppliedPhysics.rotation,
                .1f * Mathf.Pow(transform.GetComponent<GravityObject>().GravityAcceleration / 9.8f, 2f)
        ));
    }

    private void FixedUpdate()
    {
        AlignRotationWithGravity();
    }
}
