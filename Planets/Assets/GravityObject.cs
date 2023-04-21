using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityObject : SplitterEventListener
{
    public GravityField field = null;
    public Vector3 GravityDirection = Vector3.zero;
    public float GravityForce = 0f;
    public bool ApplyGravity = true;


    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<GravityField>() == null)
            return;
        field = other.GetComponent<GravityField>();
        

    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<GravityField>() == null)
            return;
        if(field != null && field.gameObject.GetInstanceID() == other.gameObject.GetInstanceID())
            field = null;
    }

    private void FixedUpdate()
    {
        if (field != null)
        {
            GravityForce = field.GetGravityForce(transform.position);
            GravityDirection = (field.transform.position - transform.position).normalized;
            //GravityDirection = ((field.transform.position + (field.transform.rotation * field.transform.localPosition)) - transform.GetComponent<Rigidbody>().position).normalized;
            //Debug.Log("Gforce: " + GravityForce.ToString());
            if(ApplyGravity)
                transform.GetComponent<SplitterSubscriber>().AddForce(GravityDirection * GravityForce, ForceMode.Acceleration);
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
