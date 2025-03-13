using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SandRingBehaviour : MonoBehaviour
{

    public Transform localPlayer;

    private void Update()
    {
        transform.rotation = Quaternion.LookRotation(
            (localPlayer.position - transform.position).normalized,
            transform.up
        );
    }

}
