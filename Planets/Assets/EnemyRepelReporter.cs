using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRepelReporter : MonoBehaviour
{
    public BasicEnemyController controller;
    private void OnTriggerEnter(Collider other)
    {
        controller.OnRepelEnter(other);
    }
    private void OnTriggerExit(Collider other)
    {
        controller.OnRepelExit(other);
    }
}
