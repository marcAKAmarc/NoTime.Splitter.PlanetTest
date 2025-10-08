using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverStickBehaviour : MonoBehaviour
{
    public List<Transform> to;
    public float JourneyTime;
    private float JourneyStart;
    private float currentJourney;
    private Vector3 startPos;
    private Quaternion startRot;

    public FlightController fController;
    // Start is called before the first frame update
    private void OnEnable()
    {
        InitJourney();
    }

    private void OnDisable()
    {
        CycleTos();
    }

    // Update is called once per frame
    void Update()
    {
        currentJourney = (Time.time - JourneyStart) / JourneyTime;
        if (currentJourney > 1f)
            currentJourney = 1f;

        transform.localPosition = Vector3.Lerp(startPos, to[0].localPosition, currentJourney);
        transform.rotation = transform.parent.rotation * Quaternion.Lerp(startRot, Quaternion.Inverse(transform.parent.rotation) * to[0].rotation, currentJourney);

        if (currentJourney == 1f)
            enabled = false;
    }

    public void OnActivate()
    {        
        if (this.enabled == false)
            this.enabled = true;
        else if (this.enabled == true)
        {
            CycleTos();
            InitJourney();
        }   
    }

    private void CycleTos()
    {
        //cycle tos
        Transform oldStart = to[0];
        to.RemoveAt(0);
        to.Add(oldStart);
    }

    private void InitJourney()
    {
        JourneyStart = Time.time;
        startPos = transform.localPosition;
        startRot = Quaternion.Inverse(transform.parent.rotation) * transform.rotation;
    }
}
