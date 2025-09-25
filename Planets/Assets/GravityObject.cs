using NoTime.Splitter;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct FieldCollider
{
    public GravityField Field;
    public int Count;
}
public class GravityObject : SplitterEventListener
{
    public GravityField field = null;
    public Vector3 GravityDirection = Vector3.zero;
    public float GravityAcceleration = 0f;
    public float GravityDistance = 0f;
    public bool ApplyGravity = true;
    public float MaximumGravityPriorityLayer = 999;
    //[HideInInspector]
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
    int _i = 0;
    private void OnTriggerEnter(Collider other)
    {
        GravityField otherField = other.GetComponentInParent<GravityField>();
        
        if (otherField == null)
            return;
        if (otherField.PriorityLayer > MaximumGravityPriorityLayer)
            return;
        for (_i = 0; _i < fieldColliders.Count; _i++)
        {
            if (fieldColliders[_i].Field == otherField)
            {
                //update
                fieldColliders[_i] = new FieldCollider() { Count = fieldColliders[_i].Count + 1, Field = otherField };
                return;
            }
        }


        _insertAt = 0;
        while (_insertAt < fieldColliders.Count)
        {
            if (fieldColliders[_insertAt].Field.PriorityLayer <= otherField.PriorityLayer)
                break;
            _insertAt += 1;
        }
        if (_insertAt == fieldColliders.Count)
            fieldColliders.Add(new FieldCollider { Field = otherField, Count = 1 });
        else
            fieldColliders.Insert(_insertAt, new FieldCollider { Field = otherField, Count = 1 });
        
        UpdateFieldFromFields();
    }
    private FieldCollider _fieldColliderToRemove;
    private int _removalIndex;
    private void OnTriggerExit(Collider other)
    {
        GravityField otherField = other.GetComponentInParent<GravityField>();
        if (otherField == null)
            return;

        for (_i = 0; _i < fieldColliders.Count; _i++)
        {
            if (fieldColliders[_i].Field == otherField)
            {
                if (fieldColliders[_i].Count == 1)
                    fieldColliders.RemoveAt(_i);
                else
                {
                    //decrement
                    fieldColliders[_i] = new FieldCollider()
                    {
                        Count = fieldColliders[_i].Count - 1,
                        Field = fieldColliders[_i].Field,
                    };
                }
                break;
            }
        }
        //fieldColliders = fieldColliders.OrderByDescending(x => x.Field.PriorityLayer).ToList();
        UpdateFieldFromFields();
    }

    int ufffI;
    private void UpdateFieldFromFields()
    {
        //quick clean
        for(ufffI = 0; ufffI < fieldColliders.Count; ufffI++)
        {
            if (fieldColliders[ufffI].Field == null)
            {
                fieldColliders.RemoveAt(ufffI);
                ufffI--;
            }
        }
        /*if (fieldColliders.Any(x => x.Field == null))
            fieldColliders = fieldColliders.Where(x => x.Field != null).ToList();*/

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
                {
                    rigidbody.AddForce(GravityDirection * GravityAcceleration, ForceMode.Acceleration);
                    Debug.Log("Applied Gravity to Rigidbody!");
                }
            }
        }
        else
        {
            GravityAcceleration = 0;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if (field != null)
            Gizmos.DrawLine(transform.position, field.transform.position);
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
