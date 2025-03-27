
using NoTime.Splitter;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PickUpBehaviour : MonoBehaviour
{
    public Rigidbody HolderBody;
    private SplitterSubscriber holderSubscriber;
    private GravityObject myGravity;
    public float pickupDistance = 3f; // Distance within which the rigidbody can be picked up
    public float hoverDistance = 2f; // Distance at which the picked up rigidbody hovers from the camera
    public float hoverVOffset = 1f; //vertical up hover offset
    public float smoothSpeed = 5f; // Speed of smoothing the movement
    public float dampSpeed = 4f;
    public float maxVelocityChange = .1f;
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
        mask = LayerMask.GetMask("Default");
        myGravity = HolderBody.GetComponent<GravityObject>();
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

    private int mask;
    public float gravFudge;
    void FixedUpdate()
    {
        if (!doIt)
            return;

        ray.origin = transform.position;
        ray.direction = transform.forward;

        if (press)
        {
            if (Physics.Raycast(ray, out hit, pickupDistance, mask, QueryTriggerInteraction.Ignore))
            {
                Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();

                if (rb != null && rb.mass <= 10)
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
            Vector3 gravCancel = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
                gravCancel = Vector3.zero;//(myGravity.GravityAcceleration * myGravity.GravityDirection) * Time.fixedDeltaTime * Time.fixedDeltaTime;
            Vector3 targetPosition = 
                holderSubscriber.AppliedPhysics.position  
                + (holderSubscriber.AppliedPhysics.transform.up * hoverVOffset)
                + transform.forward * hoverDistance;
            subscriber.AppliedPhysics.AddForce(
                PIDToPosition(
                    targetPosition - subscriber.AppliedPhysics.position,
                    subscriber.AppliedPhysics.velocity - holderSubscriber.AppliedPhysics.velocity, p, d, i, iMax
                ),
                ForceMode.VelocityChange
            );
        }
    }
    public float p;
    public float i;
    public float d;
    public float iMax;
    //public float iMin;
    private List<Vector3> errors = new List<Vector3>();
    //private List<Vector3> velocities = new List<Vector3>();
    private List<Vector3> errorInts = new List<Vector3>();
    public int History;
    private Vector3 PIDToPosition(Vector3 error, Vector3 velocity, float pGain, float dGain, float iGain, float iMaximum)
    {
        errors.Insert(0, error);
        //velocities.Insert(0, velocity);
        errorInts.Insert(0, errors[0] * Time.fixedDeltaTime);
        //create static history
        while(errors.Count < History)
        {
            errors.Insert(0, errors[0]);
            //velocities.Insert(0,velocities[0]);
            errorInts.Insert(0,errors[0] * Time.fixedDeltaTime);
        }

        if (errors.Count > History) {
            errors.RemoveAt(errors.Count - 1);
            //velocities.RemoveAt(velocities.Count - 1);
            errorInts.RemoveAt(errorInts.Count - 1);
        }

        Vector3 errorSum = Vector3.zero;
        for(int i = 0; i < errorInts.Count; i++)
        {
            errorSum += errorInts[i];
        }

        return
            (errors[0] * pGain)
            + (
                ((errors[0] - errors[1]) / Time.fixedDeltaTime)
                *
                dGain
            )
            +
            //do we need this?
            Vector3.ClampMagnitude(errorSum, iMaximum) * iGain;

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
