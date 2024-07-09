using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class ActivatorBehaviour : MonoBehaviour
{
    public float reachDistance;
    private Ray ray;
    // Start is called before the first frame update
    void Start()
    {
        ray = new Ray();
    }

    private bool press;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Check for left mouse button click
        {
            press = true;
        }
    }

    private RaycastHit hit;
    void FixedUpdate()
    {

        ray.origin = transform.position;
        ray.direction = transform.forward;

        if (press)
        {
            //Debug.Log("Press");
            if (Physics.Raycast(ray, out hit, reachDistance, LayerMask.GetMask("Default"), QueryTriggerInteraction.Collide))
            {
                //Debug.Log("hit");
                ActivateBehaviour ab = hit.collider.GetComponentInParent<ActivateBehaviour>();

                if (ab != null)
                {
                    Debug.Log("activate found");
                    ab.Activate();
                }
            }
        }
        press = false;
    }
}
