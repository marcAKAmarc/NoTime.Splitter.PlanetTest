using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testTriggerLayers : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name + " has entered.");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log(other.gameObject.name + " has exitted.");
    }
}
