using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FastenerBehaviour : MonoBehaviour
{
    public Transform RayOriginDirection;
    public float Range;
    public Rigidbody body;
    public SplitterSubscriber subscriber;
    public Transform fastenedPrefab;
    public GemExciterBehaviour gemExciter;

    private bool fastened = false;
    private void Awake()
    {
        ray = new Ray();
        mask = LayerMask.GetMask("Default");
    }

    private bool pressed;
    /*private void Update()
    {
        pressed = pressed || Input.GetKeyDown(KeyCode.T); 
    }*/

    public void OnActivate()
    {
        pressed = !pressed;

        if (fastened)
        {
            Unfasten();
        }
    }
    private RaycastHit _hit;
    private Ray ray;
    private int mask;
    Rigidbody rb;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (pressed && !fastened)
        {
            ray.origin = RayOriginDirection.position;
            ray.direction = RayOriginDirection.forward;

            if (Physics.Raycast(ray, out _hit, Range, mask, QueryTriggerInteraction.Ignore))
            {
                rb = _hit.collider.GetComponentInParent<Rigidbody>();
                if (rb)
                {
                    Fasten(rb);
                }

            }
        }
        pressed = false;
    }


    private SplitterSubscriber subFastened;
    private SplitterAnchor anchorFastened;
    private GameObject tempParent;
    private Transform created;
    private GemExciterBehaviour newGemExciter;

    private void Fasten(Rigidbody body)
    {
        Debug.Log("Fastening against " + body.gameObject.name);
        anchorFastened = body.GetComponent<SplitterAnchor>();
        subFastened = body.GetComponent<SplitterSubscriber>();
        
        //enter into hitbodySub.Anchor
        if(subFastened != null 
            && subFastened.Anchor != null
        )
        {
            Debug.Log("Fastening with sub and anchor");
            created = subFastened.Anchor.AddSimulatedSubscriberTransform(this.transform, subFastened.transform, subFastened);
            newGemExciter = created.GetComponentInChildren<GemExciterBehaviour>();
            newGemExciter.enabled = gemExciter.enabled;
        }
        //enter into hitbodyAnchor
        if(anchorFastened != null)
        {
            Debug.Log("Fastening with anchor");
            created = anchorFastened.AddSimulatedAnchorTransform(this.transform, anchorFastened.transform);
            newGemExciter = created.GetComponentInChildren<GemExciterBehaviour>();
            newGemExciter.enabled = gemExciter.enabled;
        }

        //embed into surface reality
        //remove subscriber, remove rigidbody, remove grav object
        Destroy(transform.GetComponent<GravityObject>());
        Destroy(transform.GetComponent<SplitterSubscriber>());
        Destroy(transform.GetComponent<Rigidbody>());
        transform.GetComponent<SuctionBehaviour>().enabled = false;
        transform.parent = body.transform;
        /*created = Instantiate(fastenedPrefab,
            subscriber.AppliedPhysics.position,
            subscriber.AppliedPhysics.rotation
        ) as Transform;
        created.parent = tempParent.transform;
        newGemExciter = created.GetComponentInChildren<GemExciterBehaviour>();
        newGemExciter.enabled = gemExciter.enabled;*/

        fastened = true;

        //gameObject.SetActive(false);
    }

    private Rigidbody ufrigid;
    private void Unfasten()
    {
         
        if (anchorFastened != null)
        {
            Debug.Log("Unfastening anch: " + anchorFastened.gameObject.name);
            anchorFastened.RemoveSimulatedAnchorTransform(transform);
            anchorFastened = null;
        }
        if (subFastened != null  && subFastened.Anchor != null)
        {
            Debug.Log("unfastening sub: " + subFastened.gameObject.name);
            subFastened.Anchor.RemoveSimulatedSubscriberTransform(transform, subFastened);
            subFastened = null;
        }

        transform.parent = null;
        body = gameObject.AddComponent<Rigidbody>();
        body.angularDrag = 0f;
        body.useGravity = false;
        body.drag = 0f;

        subscriber = gameObject.AddComponent<SplitterSubscriber>();
        gameObject.AddComponent<GravityObject>();

        fastened = false;
        pressed = false;
    }
}
