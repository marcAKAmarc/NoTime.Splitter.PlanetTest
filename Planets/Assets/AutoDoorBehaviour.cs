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
    public float JourneyTime;
    public int colCount = 0;
    private int prevColCount = 0;
    public float JourneyStart;
    public AudioSource audio;
    public void Start()
    {
        JourneyStart = Time.time;
        totalDistance = (Door1Closed.position - Door1Open.position).magnitude;
    }

    private RigidbodyFpsController otherRigidFPS;
    private void OnTriggerEnter(Collider other)
    {
        prevColCount = colCount;
        if (other.TryGetComponent(out otherRigidFPS))
        {
            colCount += 1;
            if (colCount >= 1 && prevColCount == 0)
            {
                audio.pitch = 1f;
                audio.Play();
                JourneyStart = Time.time;
            }
        }
        
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out otherRigidFPS))
        {
            colCount -= 1;
            if (colCount == 0)
            {
                JourneyStart = Time.time;
                audio.pitch = .9f;
                audio.Play();
            }
        }
    }

    private float totalDistance;
    void Update()
    {

        //if journey over
        if ((Time.time - JourneyStart) / JourneyTime >= 1f)
        {
            audio.Stop();
            if (colCount >= 1)
            {
                Door1.position = Door1Open.position;
                Door2.position = Door2Open.position;
            }
            else
            {
                Door1.position = Door1Closed.position;
                Door2.position = Door2Closed.position;
            }
        }
        //else journey happening now
        else
        {
            if (colCount >= 1)
            {
                Door1.position = Door1Closed.position + (
                        (Door1Open.position - Door1Closed.position) * ((Time.time - JourneyStart) / JourneyTime)
                    );
                Door2.position = Door2Closed.position + (
                        (Door2Open.position - Door2Closed.position) * ((Time.time - JourneyStart) / JourneyTime)
                    );
            }
            else
            {
                Door1.position = Door1Open.position + (
                        (Door1Closed.position - Door1Open.position) * ((Time.time - JourneyStart) / JourneyTime)
                    );
                Door2.position = Door2Open.position + (
                    (Door2Closed.position - Door2Open.position) * ((Time.time - JourneyStart) / JourneyTime)
                );
            }
        }
    }
}
