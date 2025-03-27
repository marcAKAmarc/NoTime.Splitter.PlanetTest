using NoTime.Splitter.Demo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDoorBehaviour : MonoBehaviour
{
    public Collider OpenTrigger;
    public Transform Door1;
    public Transform Door2;
    public Transform Door1Closed, Door1Open, Door2Closed, Door2Open;
    public int colCount = 0;

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<RigidbodyFpsController>()!=null)
            colCount += 1;
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<RigidbodyFpsController>() != null)
            colCount -= 1;
    }

    // Update is called once per frame
    Vector3 d1error = Vector3.zero;
    Vector3 d2error = Vector3.zero;
    Vector3 d1errorPrev = Vector3.zero;
    Vector3 d2errorPrev = Vector3.zero;
    public float movementPGain = 0f;
    public float movementDGain = 0f;
    void Update()
    {
        if (colCount > 0)
        {
            d1errorPrev = d1error;
            d1error = (Door1Open.position - Door1.position);
            d2errorPrev = d2error;
            d2error = (Door2Open.position - Door2.position);

        }
        else
        {
            d1errorPrev = d1error;
            d1error = (Door1Closed.position - Door1.position);
            d2errorPrev = d2error;
            d2error = (Door2Closed.position - Door2.position);
        }
        
        //Door1.position = d1error.normalized * Mathf.Min()
        Door1.position += d1error * Mathf.Min(Time.deltaTime * movementPGain, 1f);
        //Door1.position += (d1error - d1errorPrev) * Time.deltaTime * movementDGain;
        Door2.position += d2error * Mathf.Min(Time.deltaTime * movementPGain, 1f);
        //Door2.position += (d2error - d2errorPrev) * Time.deltaTime * movementDGain;
    }
}
