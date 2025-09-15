using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuctionBehaviour : MonoBehaviour
{
    public SplitterSubscriber Subscriber;
    public Transform RayOriginDirection;
    public float RayLength;
    public float RayOffDistance;
    public float SuctionForce;
    public float FrictionForce;
    private int mask;
    private Ray ray;
    private RaycastHit hit;
    private bool raycastResult;
    // Start is called before the first frame update
    void Start()
    {
        mask = LayerMask.GetMask("Default");
    }


    private SplitterSubscriber otherSub;
    private bool foundOtherSub;
    // Update is called once per frame
    void FixedUpdate()
    {
        ray.origin = RayOriginDirection.position;
        ray.direction = RayOriginDirection.forward;
        raycastResult = Physics.Raycast(ray, out hit, RayLength, mask, QueryTriggerInteraction.Ignore);
        if (raycastResult
            && (hit.point - ray.origin).sqrMagnitude > Mathf.Pow(RayOffDistance, 2f)
        )
        {
            Vector3 fullPositionalForce = SuctionForce *
                (hit.point - ray.origin);
            Vector3 fullFrictionForce = Vector3.zero;
            foundOtherSub = hit.rigidbody.TryGetComponent(out otherSub);
            if(foundOtherSub)
            {
                fullFrictionForce = FrictionForce
                    * (
                        otherSub.AppliedPhysics.velocity
                        - Subscriber.AppliedPhysics.velocity
                    );
            }
            else
            {
                fullFrictionForce = FrictionForce
                    * (
                        hit.rigidbody.velocity
                        - Subscriber.AppliedPhysics.velocity
                    );
            }
            Subscriber.AppliedPhysics.AddForceAtPosition(
                fullFrictionForce + fullPositionalForce,
                ray.origin
            );
            if (foundOtherSub)
            {
                otherSub.AppliedPhysics.AddForceAtPosition(
                    -(fullFrictionForce + fullPositionalForce),
                    hit.point
                );
            }
            else
            {
                hit.rigidbody.AddForceAtPosition(
                    -(fullFrictionForce + fullPositionalForce),
                    hit.point
                );
            }
        }
    }
}
