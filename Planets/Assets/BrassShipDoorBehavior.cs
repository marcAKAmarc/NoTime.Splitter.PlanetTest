using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;
using static UnityEngine.Rendering.DebugUI;

public class BrassShipDoorBehavior : MonoBehaviour
{
    public bool Open = true;
    public HingeJoint doorJoint;
    public Transform closedReference, openedReference, GroundCheckOrigin, IntegratedDoor, IntegratedDoorCollider, DoorAtStart;
    public Transform centerOfMass;
    private JointLimits openLimits, closedLimits;
    public Rigidbody doorBody;
    private SplitterSubscriber splitterSubscriber;
    private JointMotor openMotor, closeMotor, stopMotor;

    public enum DoorRequestStates { open, stop, closed }
    public DoorRequestStates DoorRequestState {
        get => pr_doorRequestState;
        set => InputDoorRequestState(value);
    }
    [FormerlySerializedAs("DoorRequestState")]
    [SerializeField]
    private DoorRequestStates pr_doorRequestState;
    
    private void InputDoorRequestState(DoorRequestStates value)
    {
        Debug.Log("Processing? Yes");
        if (value == DoorRequestStates.open)
        {
            SwitchToIndependentDoor();
        }
        pr_doorRequestState = value;
    }
    
    private void Awake()
    {
        splitterSubscriber = this.GetComponent<SplitterSubscriber>();
        

        doorJoint = transform.GetComponent<HingeJoint>();
        openLimits = new JointLimits()
        {
            min = -135.5f,
            max = -135f
        };
        closedLimits = new JointLimits()
        {
            min = 0f,
            max = 0f
        };

        openMotor = new JointMotor()
        {
            targetVelocity = -50,
            force = 2000
        };

        closeMotor = new JointMotor()
        {
            targetVelocity = 50,
            force = 2000
        };
        stopMotor = new JointMotor()
        {
            targetVelocity = 0,
            force = 5000
        };
        doorBody = transform.GetComponent<Rigidbody>();
    }

    public void Start()
    {
        doorJoint.motor = stopMotor;
        doorJoint.useMotor = true;
        //splitterSubscriber.ManualEnterAnchor(closedReference.GetComponentInParent<SplitterAnchor>());
    }

    bool alignedWithClosure;
    bool alignedWithOpen;
    bool touchingGround;

    private Ray ray;
    private RaycastHit hit;
    // Update is called once per frame
    void FixedUpdate()
    {
        doorBody.centerOfMass = centerOfMass.position - doorBody.position;
        //if (!Open && !alignedWithClosure && Quaternion.Angle(body.rotation, closedReference.rotation) > 1f)
        if(pr_doorRequestState == DoorRequestStates.closed && Quaternion.Angle(doorBody.rotation, closedReference.rotation) > 1f)
        {
            Debug.Log("start closing");
            //start closing
            doorJoint.motor = closeMotor;
            //body.AddRelativeTorque(Vector3.right * ClosingForce);
            //body.AddRelativeTorque(-transform.InverseTransformDirection(body.angularVelocity) * ClosingDamp);
        }
        else if(pr_doorRequestState == DoorRequestStates.closed)
        {
            Debug.Log("arrived at Closed");
            //arriving to closed
            //joint.limits = closedLimits;
            doorJoint.useLimits = true;
            //alignedWithClosure = true;
            //joint.useMotor = false;
            pr_doorRequestState = DoorRequestStates.stop;
            SwitchToIntegratedDoor();

        }

        if(pr_doorRequestState == DoorRequestStates.open && !touchingGround  && Quaternion.Angle(doorBody.rotation, openedReference.rotation) > 3f)
        {
            Debug.Log("START OPEN");
            doorJoint.useLimits = false;
            //start opening
            doorJoint.motor = openMotor;
            
            //check for ground
            ray.direction = GroundCheckOrigin.forward;
            ray.origin = GroundCheckOrigin.position;
            Physics.Raycast(ray, out hit, .1f, 1 << LayerMask.NameToLayer("Default"), QueryTriggerInteraction.Ignore);
            if (hit.collider != null)
                touchingGround = true;
        }

        else if (pr_doorRequestState == DoorRequestStates.open)
        {
            if (touchingGround)
                Debug.Log("arrived at open because touching ground");
            else
                Debug.Log("arrived at open because at max open");
            //arrive at open
            pr_doorRequestState = DoorRequestStates.stop;
        }

        if (pr_doorRequestState == DoorRequestStates.stop)
        {
            doorJoint.motor = stopMotor;
            touchingGround = false;
        }
    }

    private void SwitchToIntegratedDoor()
    {
        doorBody.position = DoorAtStart.position;
        doorBody.rotation = DoorAtStart.rotation;
        doorBody.mass = .01f;
        IntegratedDoor.gameObject.SetActive(true);
        IntegratedDoorCollider.gameObject.SetActive(true);
        this.GetComponent<Collider>().enabled = false;
        //gameObject.SetActive(false);
    }

    private void SwitchToIndependentDoor()
    {
        
        doorBody.mass = 10f;
        IntegratedDoor.gameObject.SetActive(false);
        IntegratedDoorCollider.gameObject.SetActive(false);
        this.GetComponent<Collider>().enabled = true;
        //gameObject.SetActive(true);
    }

    private WaitForFixedUpdate fixWait;
    public IEnumerator DelaySwitchToIndependentDoor()
    {
        yield return fixWait;
        yield return fixWait;
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(ray.origin, ray.origin + (ray.direction * .1f)); 
    }
}
