using NoTime.Splitter;
using NoTime.Splitter.Demo;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DustAvoidanceReporter : MonoBehaviour
{
    public List<Collider> avoidances;
    private void Awake()
    {
        avoidances = new List<Collider>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponentsInParent<PlanetBehaviour>().Length == 0 && other.GetComponentInParent<RigidbodyFpsController>() == null)
            avoidances.Add(other);
    }
    private void OnTriggerExit(Collider other)
    {
        avoidances = avoidances.Where(x => x.GetInstanceID() != other.GetInstanceID()).ToList();
    }
}
