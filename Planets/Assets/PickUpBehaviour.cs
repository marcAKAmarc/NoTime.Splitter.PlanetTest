
using NoTime.Splitter;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public enum HoldType { OnClick, OnTrigger}
public class PickUpBehaviour : MonoBehaviour
{
    public HoldType HoldOn = HoldType.OnClick;
    public float MaxMass = 10f;
    public float MinMass = .5f;
    public Transform HolderTransform;
    private SplitterSubscriber holderSubscriber;
    private Rigidbody holderBody;
    public float pickupAngularDrag;
    public float dragThreshold;
    public float pickupRange = 3f; // Distance within which the rigidbody can be picked up
    public Transform hoverTarget;
    public Vector3 rayOffset;
    private Rigidbody pickedRigidbody;
    private Ray ray;
    private RaycastHit hit;
    private SplitterSubscriber pickedSubscriber;
    private bool doIt;
    private bool press;
    private float previousAngularDrag;
    void Start()
    {
        ray.origin = transform.position;
        ray.direction = transform.forward;
        mask = LayerMask.GetMask("Default");
        holderSubscriber = HolderTransform.GetComponent<SplitterSubscriber>();
        holderBody = HolderTransform.GetComponent<Rigidbody>();
        
    }

    void Update()
    {
        if (HoldOn == HoldType.OnClick)
        {
            if (Input.GetMouseButtonDown(0)) // Check for left mouse button click
            {
                doIt = true;
                press = true;
            }

            if (Input.GetMouseButtonUp(0) && pickedRigidbody != null)
            {

                pickedSubscriber.AppliedPhysics.angularDrag = previousAngularDrag;

                pickedRigidbody = null;
                pickedSubscriber = null;
                doIt = false;
            }

            if (Input.GetMouseButton(1) && pickedSubscriber != null)
            {
                pickedSubscriber.AppliedPhysics.angularDrag = pickupAngularDrag;
            }
            else if(pickedSubscriber != null)
            {
                pickedSubscriber.AppliedPhysics.angularDrag = previousAngularDrag;
            }
        }
    }
    private SplitterSubscriber _colCheckSub;
    private void OnTriggerEnter(Collider other)
    {
        if (pickedRigidbody != null)
            return;
        if (HoldOn != HoldType.OnTrigger)
            return;
        if (other.attachedRigidbody == null)
            return;
        _colCheckSub = other.attachedRigidbody.GetComponent<SplitterSubscriber>();
        if (_colCheckSub != null)
        {
            pickedRigidbody = other.attachedRigidbody;
            pickedSubscriber = _colCheckSub;
            doIt = true;
            Debug.Log("trigger enter - picked up rigid: " + pickedRigidbody.gameObject.name + "; sub: " + pickedSubscriber.gameObject.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (pickedRigidbody == null)
            return;
        if (HoldOn != HoldType.OnTrigger)
            return;
        if (other.attachedRigidbody == null)
            return;
        if (other.attachedRigidbody == pickedRigidbody)
        {
            doIt = false;
            pickedRigidbody = null;
        }
    }

    private int mask;
    public float gravFudge;
    private Vector3 localHitPoint = Vector3.zero;
    void FixedUpdate()
    {
        if (!doIt)
            return;

        ray.origin = transform.position + rayOffset;
        ray.direction = transform.forward;

        if (press)
        {
            if (Physics.Raycast(ray, out hit, pickupRange, mask, QueryTriggerInteraction.Ignore))
            {
                Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();

                if (rb != null && rb.mass <= MaxMass && rb.mass >= MinMass)
                {
                    
                    pickedRigidbody = rb;
                    pickedSubscriber = pickedRigidbody.transform.GetComponent<SplitterSubscriber>();
                    localHitPoint = Quaternion.Inverse(pickedSubscriber.AppliedPhysics.rotation) 
                        * (hit.point - pickedSubscriber.AppliedPhysics.position);
                    previousAngularDrag = pickedSubscriber.AppliedPhysics.angularDrag;
                }
                else
                {
                    localHitPoint = Vector3.zero;
                    pickedRigidbody = null;
                }
            }
        }

        press = false;
        if (pickedRigidbody != null)
        {
            Vector3 targetPosition = hoverTarget.position;
            Vector3 forcePos = 
                (pickedSubscriber.AppliedPhysics.rotation * localHitPoint) 
                + pickedSubscriber.AppliedPhysics.position;
            Vector3 pidResult =
                PIDToPosition(
                    targetPosition - forcePos,
                    pickedSubscriber.AppliedPhysics.velocity - HolderVelocity()
                    , p, d, i, iMax
                );
            pickedSubscriber.AppliedPhysics.AddForceAtPosition(
                pidResult/3f,
                forcePos,
                ForceMode.Impulse
            );
            //the reciprocal version needs to be done in rigidbodyfps in the 
            //grounded section.  when moving up from the ground, add force
            //to other if it is not your anchor

            //drag
            /*if(pidResult.sqrMagnitude < Mathf.Pow(dragThreshold, 2f))
            {   
                pickedSubscriber.AppliedPhysics.angularDrag = pickupAngularDrag;
            }
            else
            {
                pickedSubscriber.AppliedPhysics.angularDrag = previousAngularDrag;
            }*/
        }
    }

    private Vector3 HolderVelocity()
    {
        if (holderSubscriber == null && holderBody == null)
            return Vector3.zero;
        else if (holderSubscriber == null && holderBody != null)
            return holderBody.velocity;
        else
            return holderSubscriber.AppliedPhysics.velocity;
    }

    public float p;
    public float i;
    public float d;
    public float iMax;
    private List<Vector3> errors = new List<Vector3>();
    private List<Vector3> velocities = new List<Vector3>();
    private List<Vector3> errorInts = new List<Vector3>();
    public int History;
    private Vector3 PIDToPosition(Vector3 error, Vector3 velocity, float pGain, float dGain, float iGain, float iMaximum)
    {
        errors.Insert(0, error);
        if (errors.Count == 1)
            velocities.Insert(0, Vector3.zero);
        else
            velocities.Insert(0, velocity/*Vector3.Project(velocity, errors[0])*/);
        errorInts.Insert(0, errors[0] * Time.fixedDeltaTime);
        //create static history
        while(errors.Count < History)
        {
            errors.Insert(0, errors[0]);
            velocities.Insert(0,velocities[0]);
            errorInts.Insert(0,errors[0] * Time.fixedDeltaTime);
        }

        if (errors.Count > History) {
            errors.RemoveAt(errors.Count - 1);
            velocities.RemoveAt(velocities.Count - 1);
            errorInts.RemoveAt(errorInts.Count - 1);
        }

        Vector3 errorSum = Vector3.zero;
        for(int i = 0; i < errorInts.Count; i++)
        {
            errorSum += errorInts[i];
        }

        return
            (errors[0] * pGain)
            + (
                velocities[0]
                *
                dGain
            )
            +
            //do we need this?
            Vector3.ClampMagnitude(errorSum, iMaximum) * iGain;

    }

    private static Vector3 Vector3Min(Vector3 v1, Vector3 v2)
    {
        if(v1.sqrMagnitude < v2.sqrMagnitude)
        {
            return v1;
        }
        else
        {
            return v2;
        }
    }
}

