using NoTime.Splitter;
using System;
using System.Collections;
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

    public void Awake()
    {
        if (fieldColliders == null)
            fieldColliders = new List<FieldCollider>();
    }
    private void OnTriggerEnter(Collider other)
    {
        GravityField otherField = other.GetComponentInParent<GravityField>();
        if(otherField == null)
            return;
        if (otherField.PriorityLayer > MaximumGravityPriorityLayer)
            return;

        fieldColliders.Add(new FieldCollider{collider = other, Field = otherField});
        fieldColliders = fieldColliders.OrderByDescending(x => x.Field.PriorityLayer).ToList();
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

    private void UpdateFieldFromFields() {
        //quick clean
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
            //GravityDirection = ((field.transform.position + (field.transform.rotation * field.transform.localPosition)) - transform.GetComponent<Rigidbody>().position).normalized;
            //Debug.Log("Gforce: " + GravityForce.ToString());
            if (ApplyGravity)
            {
                if (transform.GetComponentInParent<SplitterSubscriber>() != null)
                    transform.GetComponentInParent<SplitterSubscriber>().AppliedPhysics.AddForce(GravityDirection * GravityAcceleration, ForceMode.Acceleration);
                else
                    transform.GetComponentInParent<Rigidbody>().AddForce(GravityDirection * GravityAcceleration, ForceMode.Acceleration);
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

        if(field != null)
            Gizmos.DrawLine(transform.position, field.transform.position);
    }

    public override void OnEnterAnchor(SplitterEvent evt)
    {
        if(this.GetComponentInParent<SplitterSubscriber>()!=null && evt.Subscriber.gameObject.GetInstanceID() == this.GetComponentInParent<SplitterSubscriber>().gameObject.GetInstanceID())
        {
            this.enabled = false;
        }
        if (evt.SimulatedAnchor.GetComponentInChildren<GravityField>() != null)
        {
            evt.SimulatedSubscriber.GetComponent<GravityObject>().field = evt.SimulatedAnchor.GetComponentInChildren<GravityField>();
            evt.SimulatedSubscriber.GetComponent<GravityObject>().fieldColliders = 
                fieldColliders.Where(x=>
                    x.Field.transform.GetInstanceID() == evt.SimulatedSubscriber.GetComponent<GravityObject>().field.transform.GetInstanceID()
                ).ToList();
            /*evt.SimulatedSubscriber.GetComponent<GravityObject>().fieldColliders.Add(
                new FieldCollider { 
                    collider = evt.SimulatedAnchor.GetComponentInChildren<GravityField>().transform.GetComponent<Collider>(), 
                    Field = evt.SimulatedAnchor.GetComponentInChildren<GravityField>() 
                }
            );*/
        }
    }

    /*public override void OnExitAnchor(SplitterEvent evt)
    {
        if (evt.Subscriber.gameObject.GetInstanceID() == this.transform.GetInstanceID())
            this.enabled = true;
    }*/
}
