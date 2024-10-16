using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BinBehaviour : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.GetComponentInChildren<LifeResourceBehaviour>() != null)
        {
            Destroy(other.gameObject);
            //Create Health Item
        }
    }
}
