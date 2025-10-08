using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PilotNotifier : MonoBehaviour
{
    public FlightController FlightController;
    private void OnTriggerEnter(Collider other)
    {
        FlightController.RegisterPotentialPilot(other);
    }
    private void OnTriggerExit(Collider other)
    {
        FlightController.UnregisterPotentialPilot(other);
    }
}
