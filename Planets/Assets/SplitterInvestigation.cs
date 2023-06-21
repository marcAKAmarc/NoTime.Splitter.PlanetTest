using NoTime.Splitter.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitterInvestigation : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnDisable()
    {
        SplitterSystem.InvestigatoryEvents -= Investigate;
    }

    private void OnEnable()
    {
        SplitterSystem.InvestigatoryEvents += Investigate;
    }
    private void Investigate(string eventName)
    {
        if (transform.name.Contains("TEST"))
        {
            Debug.Log(eventName + " finished.");
            Debug.Log("    position = " + transform.position);
        }
    }
}
