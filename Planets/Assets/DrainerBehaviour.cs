using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public delegate void PowerEvent(bool on);
public class DrainerBehaviour : MonoBehaviour
{
    private SplitterSubscriber Held;
    private int colliderCount = 0;
    public SplitterSubscriber MyPhysics;
    public Transform Target;
    public float maxSpeedChange = 1f;
    public float smoothSpeed = 3f;
    public float dampSpeed = .5f;

    public PowerEvent PowerEvents;

    private SplitterSubscriber _sub;
    private void OnTriggerEnter(Collider other)
    {
        if( 
            other.gameObject.tag == "Drainable"
        )
        {
            _sub = other.GetComponentInParent<SplitterSubscriber>();
            if (Held == null)
            {
                Held = _sub;
                PowerEvents(true);
            }
            else if (Held == _sub)
            {
                colliderCount += 1;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(
            other.gameObject.tag == "Drainable"
            &&
            other.GetComponentInParent<SplitterSubscriber>() == Held
        )
        {
            colliderCount -= 1;
            if (colliderCount == 0)
            {
                Held = null;
                PowerEvents(false);
            }
        }
    }

    private Vector3 _target;
    private void FixedUpdate()
    {
        if (Held != null)
        {
            _target = (Target.position - MyPhysics.transform.position) + MyPhysics.AppliedPhysics.position;
            Held.AppliedPhysics.AddForce(
                Vector3Min(
                    (_target - Held.AppliedPhysics.position).normalized * maxSpeedChange, 
                    (_target - Held.AppliedPhysics.position) * smoothSpeed
                )
                , ForceMode.VelocityChange
            );
            Held.AppliedPhysics.AddForce(
                Vector3Min(
                    (MyPhysics.AppliedPhysics.velocity - Held.AppliedPhysics.velocity).normalized * maxSpeedChange, 
                    (MyPhysics.AppliedPhysics.velocity - Held.AppliedPhysics.velocity) * dampSpeed //* Time.fixedDeltaTime
                 )
                 , ForceMode.VelocityChange
            );
        }
    }

    private static Vector3 Vector3Min(Vector3 v1, Vector3 v2)
    {
        if (v1.sqrMagnitude < v2.sqrMagnitude)
        {
            return v1;
        }
        else
        {
            return v2;
        }
    }

    public bool IsDraining()
    {
        return Held != null;
    }
}
public static class SubscriberExtensions
{
    public static Vector3? GetSimulatedTransformPosition(this SplitterSubscriber sub, Transform splitterTransform)
    {
        if (sub.Anchor != null)
        {
            Transform _simTransform = null;
            _simTransform = sub.Anchor.GetMatchedTransform(sub, splitterTransform);
            if (_simTransform != null)
            {
                return sub.Anchor.AnchorPointToWorldPoint(_simTransform.position);
            }
            else
            {
                return null;
            }

        }
        else
        {
            return splitterTransform.position;
        }
    }
}