using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class ActivatorBehaviour : MonoBehaviour
{
    public Transform PassThroughTransform;
    public float reachDistance;
    public int iconGrow = 2;
    private Ray ray;
    public List<Text> texts;

    private int originalSize;

    private void Awake()
    {
        //UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        mask = LayerMask.GetMask("Activator");
        ray = new Ray();
        originalSize = texts[0].fontSize;
    }

    private bool press;
    private RaycastHit hit;
    private ActivateBehaviour ab;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Check for left mouse button click
        {
            press = true;
        }
        

        if (press && ab != null)
        {
            ab.Activate();
        }
        press = false;

        if (ab)
        {
            foreach(Text text in texts)
            {
                text.fontStyle = FontStyle.Bold;
                text.fontSize = originalSize + iconGrow;
            }
        }
        else
        {
            foreach (Text text in texts)
            {
                text.fontStyle = FontStyle.Normal;
                text.fontSize = originalSize;
            }
        }

        
    }
    private void FixedUpdate()
    {
        ab = RaycastToActivateBehaviour();
    }

    int mask;
    private ActivateBehaviour RaycastToActivateBehaviour()
    {
        ray.origin = transform.position;
        ray.direction = transform.forward;
        
        Physics.Raycast(ray, out hit, reachDistance, mask, QueryTriggerInteraction.Collide);
        if(hit.collider != null)
        {
            //Debug.Log("hit");
            return hit.collider.GetComponentInParent<ActivateBehaviour>();
        }
        return null;
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(ray.origin, ray.origin + (ray.direction * reachDistance));
        Gizmos.DrawSphere(ray.origin + (ray.direction * reachDistance), .25f);
        if (hit.collider != null)
            Gizmos.DrawSphere(hit.point, .25f);
    }
}
