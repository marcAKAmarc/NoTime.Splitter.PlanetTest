using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeResourceBehaviour : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {

        if(other.gameObject.GetComponent<PlayerInventory>() != null)
        {
            Debug.Log("We entered the trigger.");
            other.gameObject.GetComponent<PlayerInventory>().PutLifeResource();
            Destroy(transform.parent.gameObject);
        }
    }
}
