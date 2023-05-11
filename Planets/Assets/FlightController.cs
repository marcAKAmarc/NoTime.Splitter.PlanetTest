using NoTime.Splitter;
using NoTime.Splitter.Demo;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.TerrainTools;
using UnityEngine;

public class FlightController : MonoBehaviour
{
    public Transform controller;
    public bool controllable = false;
    public bool controlled = false;
    public SplitterSubscriber body;
    public float MoveForce;
    public float RollSensitivity;
    public float RotationSensitivity;
    public float RotationSmoothTime;
    public float maxRotateSpeed;
    public float StabilizationSensitivity;
    private Vector3 StabilizationVelocity = Vector3.zero;
    public float ThrustStabilizationSensitivity;
    public float RotationDeadzone;
    private float rotationX = 0F;
    private float rotationY = 0F;

    public float dampenFactor;

    private Color _initialThrustDisplayColor;
    public Transform Fwd1;
    public Transform Fwd2, Fwd3, Back1, Back2, Back3, Left1, Left2, Left3, Right1, Right2, Right3, Up1, Up2, Up3, Down1, Down2, Down3;
    private Material mFwd1, mFwd2, mFwd3, mBack1, mBack2, mBack3, mLeft1, mLeft2, mLeft3, mRight1, mRight2, mRight3, mUp1, mUp2, mUp3, mDown1, mDown2, mDown3;
    //private Material[18] thrustMaterials;
    private Vector3 thrustDisplay;

    public Transform GravityNormal;
    public Transform GravityLanding;
    private FlightModes FlightMode = FlightController.FlightModes.Normal;

    private Quaternion FlightRotation;
    private Vector3 FlightRotationVelocity;
    private float FlightRotationStrength;
    private enum FlightModes { Normal, Landing }

    private Vector3 centerOfGravity;
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
            controller = other.transform;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<RigidbodyFpsController>())
        {
            controllable = false;
            if(controller != null)
                controller.GetComponent<RigidbodyFpsController>().inControllerPosition = false;
            controller = null;
        }
    }
    
    private void FlightModeUpdate()
    {
        if (controlled == true && Input.GetKeyDown(KeyCode.Tab))
        {
            if (FlightMode == FlightModes.Normal)
            {
                FlightMode = FlightModes.Landing;
                GravityLanding.gameObject.SetActive(true);
                GravityNormal.gameObject.SetActive(false);
            }
            else
            {
                FlightMode = FlightModes.Normal;
                GravityLanding.gameObject.SetActive(false);
                GravityNormal.gameObject.SetActive(true);
            }
        }


    }
    
    void Update()
    {
        rotationX += Input.GetAxis("Mouse X") * RotationSensitivity;
        rotationY += Input.GetAxis("Mouse Y") * RotationSensitivity;
        if (controllable == true && Input.GetKeyDown(KeyCode.CapsLock))
        {
            controlled = !controlled;
            controller.GetComponent<RigidbodyFpsController>().inControllerPosition = controlled;
        }

        //FlightModeUpdate();
        ThrustDisplayUpdate();

    }
    private void LateUpdate()
    {
        if(controlled)
            SetDirection();
    }
    Quaternion _target;
    void SetDirection()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            _target = controller.GetComponentInChildren<Camera>().transform.rotation;
        }
        else
        {
            _target = body.AppliedPhysics.rotation;
        }
        //if (Quaternion.Dot(body.AppliedPhysics.rotation, controller.GetComponentInChildren<Camera>().transform.rotation) < 1f - RotationDeadzone)
        //{
            FlightRotation = SmoothDampQuaternion(FlightRotation, _target, ref FlightRotationVelocity, 4f);
        //}
        
        if (Input.GetKey(KeyCode.Q))
            FlightRotation = Quaternion.AngleAxis(1f * RollSensitivity, transform.forward) * FlightRotation;
        if (Input.GetKey(KeyCode.E))
            FlightRotation = Quaternion.AngleAxis(-1f * RollSensitivity, transform.forward) * FlightRotation;
        /*FlightDirection = transform.InverseTransformDirection(transform.forward - controller.GetComponentInChildren<Camera>().transform.forward);
        FlightDirection = (new Vector3(FlightDirection.y, -FlightDirection.x, 0f));
        
        //should do deadzones individually per axis
        if (Mathf.Abs(FlightDirection.y) < RotationDeadzone)
        {
            FlightDirection = FlightDirection - (FlightDirection.y * Vector3.up);
        }
        if (Mathf.Abs(FlightDirection.x) < RotationDeadzone)
        {
            FlightDirection = FlightDirection - (FlightDirection.x * Vector3.right);
        }

        FlightDirection = FlightDirection * RotationSensitivity;

        if (Input.GetKey(KeyCode.Q))
            FlightDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.E))
            FlightDirection += -Vector3.forward;*/
    }
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
        AngularDrag();
        Rotate();
    }
    private Vector3 _thrust;
    private Vector3 _thrustInput;
    private void Move()
    {
        centerOfGravity =
            (
                (
                    (transform.GetComponent<Rigidbody>().centerOfMass + transform.GetComponent<Rigidbody>().position)
                    *
                    transform.GetComponent<Rigidbody>().mass
                )
                +
                (
                    (controller.GetComponent<Rigidbody>().centerOfMass + controller.GetComponent<Rigidbody>().position)
                    *
                    controller.GetComponent<Rigidbody>().mass
                )
            ) / (transform.GetComponent<Rigidbody>().mass + controller.GetComponent<Rigidbody>().mass);

        //Debug.Log("Center of Grav Calc: " + centerOfGravity.ToString("G6"));
        //Debug.Log("Center of Grav Normal: " + (transform.GetComponent<Rigidbody>().centerOfMass + transform.GetComponent<Rigidbody>().position).ToString("G6"));

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

    private void Rotate()
    {
        float usedSensitivity = StabilizationSensitivity;
        if(controlled && Input.GetKey(KeyCode.Space))
        {
            usedSensitivity = ThrustStabilizationSensitivity;
        }
        //Quaternion GoalRotation = Quaternion.Slerp(body.AppliedPhysics.rotation, FlightRotation, usedSensitivity);
        Quaternion GoalRotation = SmoothDampQuaternion(body.AppliedPhysics.rotation, FlightRotation, ref StabilizationVelocity, .1f);
        var towardDiff = makeNegativable((Quaternion.Inverse(body.AppliedPhysics.rotation) * GoalRotation).eulerAngles);
        if (towardDiff.sqrMagnitude > Mathf.Pow(maxRotateSpeed,2f))
            towardDiff = towardDiff.normalized * maxRotateSpeed;
        body.AppliedPhysics.AddRelativeTorque(
            towardDiff-body.AppliedPhysics.angularVelocity,
            ForceMode.Acceleration
        );

    }
    private static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime)
    {
        Vector3 c = current.eulerAngles;
        Vector3 t = target.eulerAngles;
        return Quaternion.Euler(
          Mathf.SmoothDampAngle(c.x, t.x, ref currentVelocity.x, smoothTime),
          Mathf.SmoothDampAngle(c.y, t.y, ref currentVelocity.y, smoothTime),
          Mathf.SmoothDampAngle(c.z, t.z, ref currentVelocity.z, smoothTime)
        );
    }
    private void AngularDrag()
    {
        body.AppliedPhysics.AddRelativeTorque(
            -body.AppliedPhysics.angularVelocity.x * dampenFactor,
            -body.AppliedPhysics.angularVelocity.y * dampenFactor,
            -body.AppliedPhysics.angularVelocity.z * dampenFactor,
            ForceMode.VelocityChange
        );
    }
    private static Vector3 makeNegativable(Vector3 val)
    {
        while (val.x < -180f)
            val += Vector3.right * 360f;
        while (val.x > 180f)
            val -= Vector3.right * 360f;
        while (val.y < -180f)
            val += Vector3.up * 360f;
        while (val.y > 180f)
            val -= Vector3.up * 360f;
        while (val.z < -180f)
            val += Vector3.forward * 360f;
        while (val.z > 180f)
            val -= Vector3.forward * 360f;

        return val;
    }
    
    private void OnDrawGizmos()
    {
        

    }
}
