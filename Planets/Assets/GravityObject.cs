using NoTime.Splitter;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct FieldCollider
{
    public Collider collider;
    public GravityField Field;
}
public class GravityObject : SplitterEventListener
{
    public GravityField field = null;
    public Vector3 GravityDirection = Vector3.zero;
    public float GravityAcceleration = 0f;
    public float GravityDistance = 0f;
    public bool ApplyGravity = true;
    public int MaximumGravityPriorityLayer = 999;
    [HideInInspector]
    public List<FieldCollider> fieldColliders;

    private SplitterSubscriber splitterSubscriber;
    private Rigidbody rigidbody;

    public void Awake()
    {
        splitterSubscriber = GetComponentInParent<SplitterSubscriber>();
        rigidbody = GetComponentInParent<Rigidbody>();

        if (fieldColliders == null)
            fieldColliders = new List<FieldCollider>();
    }
    int _insertAt;
    private void OnTriggerEnter(Collider other)
    {
        GravityField otherField = other.GetComponentInParent<GravityField>();
        if (otherField == null)
            return;
        if (otherField.PriorityLayer > MaximumGravityPriorityLayer)
            return;

        _insertAt = 0;
        while (_insertAt < fieldColliders.Count)
        {
            if (fieldColliders[_insertAt].Field.PriorityLayer <= otherField.PriorityLayer)
                break;
            _insertAt += 1;
        }
        if (_insertAt == fieldColliders.Count)
            fieldColliders.Add(new FieldCollider { collider = other, Field = otherField });
        else
            fieldColliders.Insert(_insertAt, new FieldCollider { collider = other, Field = otherField });
        //fieldColliders = fieldColliders.OrderByDescending(x => x.Field.PriorityLayer).ToList();
        UpdateFieldFromFields();
    }
    private void OnTriggerExit(Collider other)
    {
        GravityField otherField = other.GetComponentInParent<GravityField>();
        if (otherField == null)
            return;
        fieldColliders = fieldColliders.Where(x => x.Field != null && x.collider.GetInstanceID() != other.GetInstanceID()).ToList();
        fieldColliders = fieldColliders.OrderByDescending(x => x.Field.PriorityLayer).ToList();
        UpdateFieldFromFields();
    }

    private void UpdateFieldFromFields()
    {
        //quick clean
        if (fieldColliders.Any(x => x.Field == null))
            fieldColliders = fieldColliders.Where(x => x.Field != null).ToList();

        if (fieldColliders.Count == 0)
        {
            field = null;
            return;
        }
        if (field == null || field.gameObject.GetInstanceID() != fieldColliders[0].Field.gameObject.GetInstanceID())
        {
            field = fieldColliders[0].Field;
        }
    }


    private void FixedUpdate()
    {
        if (field != null)
        {
            GravityDistance = (field.transform.position - transform.position).magnitude;
            GravityAcceleration = field.GetGravityAcceleration(GravityDistance);
            GravityDirection = (field.transform.position - transform.position).normalized;

            if (ApplyGravity)
            {
                if (splitterSubscriber != null)
                    splitterSubscriber.AppliedPhysics.AddForce(GravityDirection * GravityAcceleration, ForceMode.Acceleration);
                else
                    rigidbody.AddForce(GravityDirection * GravityAcceleration, ForceMode.Acceleration);
            }
        }
        else
        {
            GravityAcceleration = 0;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        if (field != null)
            Gizmos.DrawLine(transform.position, field.transform.position);
    }

    public override void OnEnterAnchor(SplitterEvent evt)
    {
        //we want to keep this on in WorldSpace so that we can read from it
        if (splitterSubscriber != null && evt.Subscriber.gameObject.GetInstanceID() == splitterSubscriber.gameObject.GetInstanceID())
        {
            this.enabled = true;
            ApplyGravity = false;
        }

        //get existing fieldColliders and set them up
        if (evt.SimulatedAnchor.GetComponentInChildren<GravityField>() != null)
        {
            evt.SimulatedSubscriber.GetComponent<GravityObject>().field = evt.SimulatedAnchor.GetComponentInChildren<GravityField>();
            evt.SimulatedSubscriber.GetComponent<GravityObject>().fieldColliders =
                fieldColliders.Where(x =>
                    x.Field.transform.GetInstanceID() == evt.SimulatedSubscriber.GetComponent<GravityObject>().field.transform.GetInstanceID()
                ).ToList();
        }
    }

    public override void OnExitAnchor(SplitterEvent evt)
    {
        if (evt.Subscriber.gameObject.GetInstanceID() == splitterSubscriber.gameObject.GetInstanceID())
            ApplyGravity = true;
    }
    private void OnDisable()
    {
        var Breaka = "here";
    }
    private void OnEnable()
    {
        var breaka = "here";
    }
}
