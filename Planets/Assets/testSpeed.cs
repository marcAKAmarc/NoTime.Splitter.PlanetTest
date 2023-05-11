using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testSpeed : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Alpha1))
        {
            transform.GetComponent<Rigidbody>().AddForce(Vector3.forward * 10f, ForceMode.Acceleration);
        }
    }
}
