using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowBehaviour : MonoBehaviour
{
    public SplitterSubscriber target;

    private SplitterSubscriber sub;
    public float targetPForce;
    public float minPForce;
    public float maxPForce;
    public float targetDForce;
    public float targetFForce;
    public float RememberTime = .1f;
    public float ForgetTime = .1f;
    public float AlertRadius = 3f;
    public float SneakRadius = 10f;
    public float WanderRetargetTime = 10f;
    public GravityObject gravityObject;

    private Vector3 wanderDirection = Vector3.zero;

    public Vector3 RepelDir;
    // Start is called before the first frame update
    void Start()
    {
        TryGetComponent(out sub);
        wanderDirection = Vector3.zero;
    }

    Vector3 thisPForce, thisDForce, thisFForce;
    // Update is called once per frame
    void FixedUpdate()
    {
        bool remembered = UnityEngine.Random.value < Time.fixedDeltaTime/RememberTime;
        if (remembered && target != null)
        {
            thisPForce =
                targetPForce *
                Vector3.ProjectOnPlane(
                    (RepelDir == Vector3.zero)?
                        target.AppliedPhysics.position - sub.AppliedPhysics.position
                        : RepelDir,
                    -gravityObject.GravityDirection
                ).normalized;
            thisPForce = Vector3.ClampMagnitude(thisPForce, maxPForce);
            if (thisPForce.sqrMagnitude < Mathf.Pow(minPForce, 2f))
                thisPForce = thisPForce.normalized * minPForce;          
        }

        if(target != null)
        {
            //strafish 
            thisDForce =
                targetDForce *
                Vector3.Project(
                    target.AppliedPhysics.velocity - sub.AppliedPhysics.velocity,
                    Vector3.Cross(
                        (target.AppliedPhysics.position - sub.AppliedPhysics.position).normalized,
                        -gravityObject.GravityDirection
                    )
                );
        }

        //wander retargetting
        if(target == null && UnityEngine.Random.value < Time.fixedDeltaTime / WanderRetargetTime)
        {
            wanderDirection = 
                Vector3.ProjectOnPlane(
                    new Vector3(Random.value-.5f, Random.value-.5f, Random.value-.5f),
                    -gravityObject.GravityDirection
                ).normalized;

        }
        //wandering
        if(target == null && remembered)
        {
            //wander
            thisPForce =
                targetPForce *
                wanderDirection;

            thisPForce = Vector3.ClampMagnitude(thisPForce, maxPForce);
            if (thisPForce.sqrMagnitude < Mathf.Pow(minPForce, 2f))
                thisPForce = thisPForce.normalized * minPForce;
        }

        //friction
        thisFForce =
            -targetFForce *
            sub.AppliedPhysics.velocity - (sub.Anchor ? sub.Anchor.GetUltimatePointVelocity(sub.AppliedPhysics.position) : Vector3.zero);

        sub.AppliedPhysics.AddForce(thisDForce + thisPForce + thisFForce);

        if (UnityEngine.Random.value < Time.fixedDeltaTime/ForgetTime)
        {
            thisPForce = Vector3.zero;
            thisDForce = Vector3.zero;
        }
    }
}
