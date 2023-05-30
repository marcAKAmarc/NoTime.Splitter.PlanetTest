using NoTime.Splitter;
using NoTime.Splitter.Demo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightController : MonoBehaviour
{
    public Transform potentialController;
    private Transform controllerLookTransform;
    public bool controllable = false;
    public bool controlled = false;
    public SplitterSubscriber body;
    public float MoveForce;
    public float RollSensitivity;
    public float rotateFactor;
    public float maxRotateSpeed;
    public float dampenFactor;
    private float StabilizationCapability = 1f;
    public float RotationDeadzone;
    private float rotationX = 0F;
    private float rotationY = 0F;

    

    private Color _initialThrustDisplayColor;
    public Transform Fwd1;
    public Transform Fwd2, Fwd3, Back1, Back2, Back3, Left1, Left2, Left3, Right1, Right2, Right3, Up1, Up2, Up3, Down1, Down2, Down3;
    private Material mFwd1, mFwd2, mFwd3, mBack1, mBack2, mBack3, mLeft1, mLeft2, mLeft3, mRight1, mRight2, mRight3, mUp1, mUp2, mUp3, mDown1, mDown2, mDown3;
    private Vector3 thrustDisplay;

    private Quaternion FlightRotation;

    private void Awake()
    {
        _initialThrustDisplayColor = Fwd1.transform.GetComponent<Renderer>().sharedMaterial.GetColor("_EmissionColor");
    }
    private void Start()
    {
        FlightRotation = transform.rotation;

        mFwd1 = Fwd1.transform.GetComponent<Renderer>().material;//material;
        mFwd2 = Fwd2.transform.GetComponent<Renderer>().material;
        mFwd3 = Fwd3.transform.GetComponent<Renderer>().material;
        mBack1 = Back1.transform.GetComponent<Renderer>().material;
        mBack2 = Back2.transform.GetComponent<Renderer>().material;
        mBack3 = Back3.transform.GetComponent<Renderer>().material;
        mLeft1 = Left1.transform.GetComponent<Renderer>().material;
        mLeft2 = Left2.transform.GetComponent<Renderer>().material;
        mLeft3 = Left3.transform.GetComponent<Renderer>().material;
        mRight1 = Right1.transform.GetComponent<Renderer>().material;
        mRight2 = Right2.transform.GetComponent<Renderer>().material;
        mRight3 = Right3.transform.GetComponent<Renderer>().material;
        mUp1 = Up1.transform.GetComponent<Renderer>().material;
        mUp2 = Up2.transform.GetComponent<Renderer>().material;
        mUp3 = Up3.transform.GetComponent<Renderer>().material;
        mDown1 = Down1.transform.GetComponent<Renderer>().material;
        mDown2 = Down2.transform.GetComponent<Renderer>().material;
        mDown3 = Down3.transform.GetComponent<Renderer>().material;

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<RigidbodyFpsController>())
        {
            controllable = true;
            potentialController = other.transform;
            controllerLookTransform = other.GetComponent<RigidbodyFpsController>().VerticalLook;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<RigidbodyFpsController>())
        {
            potentialController = null;
            controllable = false;
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        MaybeTakeHitToStabilization(other);
    }
    private void OnCollisionStay(Collision other)
    {
        MaybeTakeHitToStabilization(other);
    }

    private void MaybeTakeHitToStabilization(Collision other)
    {
        if (
            potentialController == null
            ||
            other.gameObject.GetComponentInParent<RigidbodyFpsController>() == null
            ||
            other.gameObject.GetComponentInParent<RigidbodyFpsController>().transform.GetInstanceID() != potentialController.GetInstanceID()
        )
        {
            FlightRotation = body.AppliedPhysics.rotation;
            _target = body.AppliedPhysics.rotation;
            StabilizationCapability = 0f;
        }
    }
    void Update()
    {
        if (controllable == true && Input.GetKeyDown(KeyCode.CapsLock))
        {
            controlled = !controlled;
            if (controlled)
            {
                potentialController.GetComponent<RigidbodyFpsController>().inControllerPosition = controlled;
                _target = transform.rotation;
            }
            else
            {
                controllerLookTransform = null;
                if (potentialController != null)
                    potentialController.GetComponent<RigidbodyFpsController>().inControllerPosition = false;
                
            }
        }


        ThrustDisplayUpdate();

    }
    private void LateUpdate()
    {
        //if(controlled)
            
    }
    Quaternion _target;
    
    void ThrustDisplayUpdate() {
        
        thrustDisplay += Vector3.ClampMagnitude(_thrustInput - thrustDisplay, Time.fixedDeltaTime);

        mFwd1.SetColor(
            "_EmissionColor", 
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                -thrustDisplay.z*3f
            )
        );
        mFwd2.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (-thrustDisplay.z - .333f) * 1.5f
            )
        );
        mFwd3.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (-thrustDisplay.z - .666f) * 3f
            )
        );
        mBack1.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                thrustDisplay.z * 3f
            )
        );
        mBack2.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (thrustDisplay.z - .333f) * 1.5f
            )
        );
        mBack3.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (thrustDisplay.z - .666f) * 3f
            )
        );

        mRight1.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                -thrustDisplay.x * 3f
            )
        );
        mRight2.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (-thrustDisplay.x - .333f) * 1.5f
            )
        );
        mRight3.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (-thrustDisplay.x - .666f) * 3f
            )
        );
        mLeft1.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                thrustDisplay.x * 3f
            )
        );
        mLeft2.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (thrustDisplay.x - .333f) * 1.5f
            )
        );
        mLeft3.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (thrustDisplay.x - .666f) * 3f
            )
        );

        mUp1.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                -thrustDisplay.y * 3f
            )
        );
        mUp2.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (-thrustDisplay.y - .333f) * 1.5f
            )
        );
        mUp3.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (-thrustDisplay.y - .666f) * 3f
            )
        );
        mDown1.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                thrustDisplay.y * 3f
            )
        );
        mDown2.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (thrustDisplay.y - .333f) * 1.5f
            )
        );
        mDown3.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                (thrustDisplay.y - .666f) * 3f
            )
        );

    }
    private void FixedUpdate()
    {
        if (controlled)
        {
            Move();
            
        }
        
        SetDirection();
        Rotate();
    }

    private Vector3 _thrust;
    private Vector3 _thrustInput;
    private void Move()
    { 


        _thrustInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            _thrustInput += Vector3.forward;
        if (Input.GetKey(KeyCode.A))
            _thrustInput += Vector3.left;
        if (Input.GetKey(KeyCode.D))
            _thrustInput += Vector3.right;
        if (Input.GetKey(KeyCode.S))
            _thrustInput += Vector3.back;
        if (Input.GetKey(KeyCode.Space))
            _thrustInput += Vector3.up;
        if (Input.GetKey(KeyCode.LeftShift))
            _thrustInput += Vector3.down;



        _thrust = _thrustInput.normalized;
        _thrust = body.AppliedPhysics.rotation * _thrust;

        body.AppliedPhysics.AddForce(_thrust * MoveForce * Time.fixedDeltaTime, ForceMode.Acceleration);
        
    }
    void SetDirection()
    {
        if (Input.GetKey(KeyCode.Tab) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E))
        {
            if(Input.GetKey(KeyCode.Tab))
                _target = controllerLookTransform.rotation;

            if(Input.GetKey(KeyCode.Q))
                _target = _target *Quaternion.AngleAxis(1f * RollSensitivity, Vector3.forward);
            if(Input.GetKey(KeyCode.E))
                _target = _target * Quaternion.AngleAxis(-1f * RollSensitivity, Vector3.forward);

            FlightRotation = Quaternion.Slerp(FlightRotation, _target, Mathf.Clamp01(3f * Time.fixedDeltaTime));
        }
        

    }
    private void Rotate()
    {
        if (StabilizationCapability < 1f)
            StabilizationCapability += Time.fixedDeltaTime/5f;
        if (StabilizationCapability > 1f)
            StabilizationCapability = 1f;

        body.SmoothRotate(FlightRotation, maxRotateSpeed, rotateFactor, dampenFactor, StabilizationCapability);

        /*if (Input.GetKey(KeyCode.Q))
            body.AppliedPhysics.AddRelativeTorque(Vector3.forward * RollSensitivity, ForceMode.Acceleration);
        if (Input.GetKey(KeyCode.E))
            body.AppliedPhysics.AddRelativeTorque(-Vector3.forward * RollSensitivity, ForceMode.Acceleration);*/

    }



    
    private void OnDrawGizmos()
    {
        

    }
}
