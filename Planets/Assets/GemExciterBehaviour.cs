using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemExciterBehaviour : MonoBehaviour
{
    private int excitingCount;
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
                behaviour.excited = value;
        }
        if (tag == "NegatronGem")
        {
            NegatronBehaviour behaviour = body.GetComponent<NegatronBehaviour>();
            if (behaviour)
                behaviour.excited = value;
        }
        if (tag == "NeutronGem")
        {
            NeutronGemBehaviour behaviour = body.GetComponent<NeutronGemBehaviour>();
            if (behaviour)
                behaviour.excited = value;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (IsExcitable(other.attachedRigidbody.tag))
        {
            excitingCount += 1;
            SetExcitable(other.attachedRigidbody.tag, true, other.attachedRigidbody);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsExcitable(other.attachedRigidbody.tag))
        {
            Debug.Log("Grounding " + other.attachedRigidbody.gameObject.name);
            excitingCount -= 1;
            SetExcitable(other.attachedRigidbody.tag, false, other.attachedRigidbody);
        }
    }
}
