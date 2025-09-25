using NoTime.Splitter;
using UnityEngine;


public class ActivatorBehaviour : MonoBehaviour
{
    public Transform PassThroughTransform;
    public SplitterSubscriber subscriber;
    public float reachDistance;
    public int iconGrow = 2;
    private Ray ray;

    public IconHighlighter iconHighlighter;

    public Vector3 localOffset;

    // Start is called before the first frame update
    void Start()
    {
        mask = LayerMask.GetMask("Activator");
        ray = new Ray();
    }

    private bool press;
    private RaycastHit hit;
    private ActivateBehaviour ab;
    // Update is called once per frame
    void Update()
    {
        press = Input.GetMouseButtonDown(0);

        if (ab)
            iconHighlighter.CanActivate = true;
        else
            iconHighlighter.CanActivate = false;
        
        if (press && ab != null)
        {
            if (PassThroughTransform == null)
                ab.Activate();
            else
                ab.Activate(PassThroughTransform);

            press = false;
        }
    }
    private void FixedUpdate()
    {
        ab = RaycastToActivateBehaviour();  
    }

    int mask;
    private ActivateBehaviour RaycastToActivateBehaviour()
    {
        ray.origin = subscriber.AppliedPhysics.position + (subscriber.AppliedPhysics.rotation * localOffset);
        ray.direction = transform.forward;
        Physics.Raycast(ray, out hit, reachDistance, mask, QueryTriggerInteraction.Collide);
        /*this absolutely works if we want to do it from inside but meh*/
        /*
        ray.origin = subscriber.Anchor.WorldPointToAnchorPoint(
            subscriber.AppliedPhysics.position +
                (subscriber.AppliedPhysics.rotation * localOffset)

        );
        ray.direction = subscriber.Anchor.WorldDirectionToAnchorDirection(
            subscriber.AppliedPhysics.rotation * (Quaternion.Inverse(subscriber.transform.rotation) * transform.forward)
        );
        subscriber.Anchor.Scene.Value.GetPhysicsScene().Raycast(ray.origin, ray.direction, out hit, reachDistance, mask, QueryTriggerInteraction.Collide);
        */
        

        if(hit.collider != null)
        {
            //Debug.Log("hit");
            return hit.collider.GetComponentInParent<ActivateBehaviour>();
        }
        return null;
        
    }

    Vector3 drawPos1, drawPos2;
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(ray.origin, ray.origin + (ray.direction * reachDistance));
        Gizmos.DrawSphere(ray.origin + (ray.direction * reachDistance), .25f);
        if (hit.collider != null)
            Gizmos.DrawSphere(hit.point, .25f);
    }
}
