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
    private float maxSpeedChange = 1f;
    private float smoothSpeed = 3f;
    private float dampSpeed = .2f;

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

    private void FixedUpdate()
    {
        
        if (Held != null)
        {
            Held.AppliedPhysics.AddForce(
                Vector3Min(
                    (Target.position - Held.AppliedPhysics.position).normalized * maxSpeedChange, 
                    (Target.position - Held.AppliedPhysics.position) * smoothSpeed
                )
                , ForceMode.VelocityChange
            );
            Held.AppliedPhysics.AddForce(
                Vector3Min(
                    (MyPhysics.AppliedPhysics.velocity - Held.AppliedPhysics.velocity).normalized * maxSpeedChange, 
                    (MyPhysics.AppliedPhysics.velocity - Held.AppliedPhysics.velocity) * dampSpeed
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
