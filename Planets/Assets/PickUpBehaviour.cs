
using NoTime.Splitter;
using UnityEngine;


public class PickUpBehaviour : MonoBehaviour
{
    public Rigidbody HolderBody;
    private SplitterSubscriber holderSubscriber;
    public float pickupDistance = 3f; // Distance within which the rigidbody can be picked up
    public float hoverDistance = 2f; // Distance at which the picked up rigidbody hovers from the camera
    public float smoothSpeed = 5f; // Speed of smoothing the movement
    public float dampSpeed = 4f;
    public float maxSpeedChange = .1f;
    private Camera mainCamera;
    private Rigidbody pickedRigidbody;
    private Vector3 pickupOffset;
    private Ray ray;
    private RaycastHit hit;
    private SplitterSubscriber subscriber;
    private bool doIt;
    private bool press;

    void Start()
    {
        mainCamera = Camera.main;
        ray.origin = transform.position;
        ray.direction = transform.forward;

        holderSubscriber = HolderBody.GetComponent<SplitterSubscriber>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Check for left mouse button click
        {
            doIt = true;
            press = true;
        }

        if (Input.GetMouseButtonUp(0) && pickedRigidbody != null)
        {
            pickedRigidbody = null;
            doIt = false;
        }
    }

    void FixedUpdate()
    {
        if (!doIt)
            return;

        ray.origin = transform.position;
        ray.direction = transform.forward;

        if (press)
        {
            if (Physics.Raycast(ray, out hit, pickupDistance, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
            {
                Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();

                if (rb != null && rb.transform.name == "just a ball")
                {
                    pickedRigidbody = rb;
                    pickupOffset = pickedRigidbody.position - transform.position;
                    subscriber = pickedRigidbody.transform.GetComponent<SplitterSubscriber>();
                }
                else
                {
                    pickedRigidbody = null;
                }
            }
        }
        press = false;
        if (pickedRigidbody != null)
        {
            Vector3 targetPosition = transform.position + transform.forward * hoverDistance;
            if (subscriber != null)
            {
                subscriber.AppliedPhysics.AddForce(Vector3Min((targetPosition - subscriber.AppliedPhysics.position).normalized * maxSpeedChange, (targetPosition - subscriber.AppliedPhysics.position) * smoothSpeed), ForceMode.VelocityChange);
                subscriber.AppliedPhysics.AddForce(Vector3Min((holderSubscriber.AppliedPhysics.velocity - subscriber.AppliedPhysics.velocity).normalized * maxSpeedChange,(holderSubscriber.AppliedPhysics.velocity-subscriber.AppliedPhysics.velocity) * dampSpeed), ForceMode.VelocityChange);
            }
            //else
//pickedRigidbody.MovePosition(Vector3.Lerp(pickedRigidbody.position, targetPosition, smoothSpeed * Time.fixedDeltaTime));
        }
    }

    private static Vector3 Vector3Min(Vector3 v1, Vector3 v2)
    {
        if(v1.sqrMagnitude < v2.sqrMagnitude)
        {
            return v1;
        }
        else
        {
            return v2;
        }
    }
}
/*public class PickUpBehaviour : MonoBehaviour
{
    public float Acceleration;
    public float Drag;
    public float MassLimit;
    public List<Transform> Ignore;
    private Transform[] pickables = new Transform[4];
    
    private int insertIndex = 0;
    // Start is called before the first frame update

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Raw enter: " + other.transform.name);
        if(other.GetComponentInParent<Rigidbody>() != null
            && other.GetComponentInParent<Rigidbody>().mass <= MassLimit
            && !Ignore.Any(x=>x==other.GetComponentInParent<Rigidbody>().transform)
        )
        {
            Debug.Log("Success enter: " + other.transform.name); 
            insertIndex = 0;
            while(insertIndex < pickables.Length)
            {
                if (pickables[insertIndex] == null)
                    break;
                insertIndex += 1;
            }
            if(insertIndex < pickables.Length)
            {
                pickables[insertIndex] = other.transform;
            }
        }
            
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<Rigidbody>() != null
            && other.GetComponentInParent<Rigidbody>().mass <= MassLimit
            && !Ignore.Any(x => x == other.GetComponentInParent<Rigidbody>().transform)
        ) {
            insertIndex = 0;
            while (insertIndex < pickables.Length)
            {
                if (pickables[insertIndex]!= null && pickables[insertIndex].transform == other.GetComponentInParent<Rigidbody>().transform)
                {
                    pickables[insertIndex] = null;
                }
                insertIndex += 1;
            }
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var trans = pickables.Where(x => x != null).ToList();
        if (trans.Count == 0)
            return;

        var t = trans.OrderBy(x => (x.position - transform.position).sqrMagnitude).First();

        if (t.GetComponent<SplitterSubscriber>()!=null)
        {
            t.GetComponent<SplitterSubscriber>().AppliedPhysics.MovePosition(t.position + ((transform.position - t.position) * .1f));
        }
        else
            t.GetComponent<Rigidbody>().MovePosition(t.position + ((transform.position - t.position) * Acceleration * Time.deltaTime));
        Debug.Log("t = " + t.name);
    }
}*/
