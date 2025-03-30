using NoTime.Splitter.Demo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HarpoonBehaviour : MonoBehaviour
{

    
    void OnActivate(Transform t)
    {
        t.GetComponent<RigidbodyFpsController>().inControllerPosition = true;
    }
}
