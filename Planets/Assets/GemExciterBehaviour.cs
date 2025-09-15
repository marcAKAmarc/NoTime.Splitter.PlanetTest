using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemExciterBehaviour : MonoBehaviour
{
    private int excitingCount;
    private List<Rigidbody> collidingExcitables;
    private Collider collider;
    private void Awake()
    {
        collidingExcitables = new List<Rigidbody>();
        
    }
    private void Start()
    {
        TryGetComponent(out collider);
    }
    public int GetExcitingCount()
    {
        return excitingCount;
    } 
    private bool IsExcitable(string tag)
    {
        if (tag == "NegatronGem" || tag == "PositronGem" || tag == "NeutronGem")
            return true;
        else
            return false;
    }
    private void SetExcitable(string tag, bool value, Rigidbody body)
    {
        if (tag == "PositronGem")
        {
            PositronGemBehaviour behaviour = body.GetComponent<PositronGemBehaviour>();
            if (behaviour)
                behaviour.AddExcitement(value);
        }
        if (tag == "NegatronGem")
        {
            NegatronBehaviour behaviour = body.GetComponent<NegatronBehaviour>();
            if (behaviour)
                behaviour.AddExcitement(value);
        }
        if (tag == "NeutronGem")
        {
            NeutronGemBehaviour behaviour = body.GetComponent<NeutronGemBehaviour>();
            if (behaviour)
                behaviour.AddExcitement(value);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log(other.name + " has entered.");
        if (IsExcitable(other.attachedRigidbody.tag))
        {
            excitingCount += 1;
            SetExcitable(other.attachedRigidbody.tag, true, other.attachedRigidbody);
            collidingExcitables.Add(other.attachedRigidbody);
        }
    }

    private int oteI;
    private void OnTriggerExit(Collider other)
    {
        //Debug.Log(other.gameObject.name + " has exitted.");
        if (IsExcitable(other.attachedRigidbody.tag))
        {
            //Debug.Log("Grounding " + other.attachedRigidbody.gameObject.name);
            excitingCount -= 1;
            SetExcitable(other.attachedRigidbody.tag, false, other.attachedRigidbody);

            //remove from colliding excitables
            for (oteI = 0; oteI < collidingExcitables.Count; oteI++)
            {
                if (collidingExcitables[oteI] == other.attachedRigidbody)
                {
                    collidingExcitables.RemoveAt(oteI);
                    break;
                }
            }
        }
    }

    int odI;
    private void OnDisable()
    {
        for(odI = 0; odI < collidingExcitables.Count; odI++)
        {
            if (collidingExcitables[odI] != null)
            {
                //Debug.Log("Grounding " + collidingExcitables[odI].gameObject.name);
                excitingCount -= 1;
                SetExcitable(collidingExcitables[odI].tag, false, collidingExcitables[odI]);
            }
        }

        collidingExcitables.Clear();
        excitingCount = 0;
        if(collider)
            collider.enabled = false;
    }

    private void OnEnable()
    {
        if (collider == null)
            TryGetComponent(out collider);     
        if(collider)
            collider.enabled = true;
    }
}
