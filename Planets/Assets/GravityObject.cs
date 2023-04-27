using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GravityObject : SplitterEventListener
{
    public GravityField field = null;
    public Vector3 GravityDirection = Vector3.zero;
    public float GravityForce = 0f;
    public float GravityDistance = 0f;
    public bool ApplyGravity = true;

    [HideInInspector]
    public List<GravityField> fields;

    public void Awake()
    {
        if (fields == null)
            fields = new List<GravityField>();
    }
    private void OnTriggerEnter(Collider other)
    {
        GravityField otherField = other.GetComponent<GravityField>();
        if(otherField == null)
            return;
        if (!fields.Any(x => x.gameObject.GetInstanceID() == otherField.gameObject.GetInstanceID()))
            fields.Add(otherField);
        fields = fields.OrderByDescending(x => x.PriorityLayer).ToList();
        UpdateFieldFromFields();
    }
    private void OnTriggerExit(Collider other)
    {
        GravityField otherField = other.GetComponent<GravityField>();
        if (otherField == null)
            return;
        fields = fields.Where(x => x != null && x.gameObject.GetInstanceID() != otherField.gameObject.GetInstanceID()).ToList();
        fields = fields.OrderByDescending(x => x.PriorityLayer).ToList();
        UpdateFieldFromFields();
    }

    private void UpdateFieldFromFields() {
        //quick clean
        fields = fields.Where(x => x != null).ToList();

        if (fields.Count == 0)
        {
            field = null;
            return;
        }
        if (field == null || field.gameObject.GetInstanceID() != fields[0].gameObject.GetInstanceID())
        {
            field = fields[0];
        }
    }
    

    private void FixedUpdate()
    {
        if (field != null)
        {
            GravityDistance = (field.transform.position - transform.position).magnitude;
            GravityForce = field.GetGravityForce(GravityDistance);
            GravityDirection = (field.transform.position - transform.position).normalized;
            //GravityDirection = ((field.transform.position + (field.transform.rotation * field.transform.localPosition)) - transform.GetComponent<Rigidbody>().position).normalized;
            //Debug.Log("Gforce: " + GravityForce.ToString());
            if(ApplyGravity)
                transform.GetComponent<SplitterSubscriber>().AppliedPhysics.AddForce(GravityDirection * GravityForce, ForceMode.Acceleration);
        }
        else
        {
            GravityForce = 0;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if(field != null)
            Gizmos.DrawLine(transform.position, field.transform.position);
    }

    public override void OnEnterAnchor(SplitterEvent evt)
    {
        if(evt.Subscriber.gameObject.GetInstanceID() == this.transform.GetInstanceID())
        {
            this.enabled = false;
        }
        if (evt.SimulatedAnchor.GetComponent<GravityField>()!=null)
        {
            evt.SimulatedSubscriber.GetComponent<GravityObject>().field = evt.SimulatedAnchor.GetComponent<GravityField>();
        }
    }

    /*public override void OnExitAnchor(SplitterEvent evt)
    {
        if (evt.Subscriber.gameObject.GetInstanceID() == this.transform.GetInstanceID())
            this.enabled = true;
    }*/
}
