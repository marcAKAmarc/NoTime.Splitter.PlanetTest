using NoTime.Splitter;
using NoTime.Splitter.Demo;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlightController : SplitterEventListener
{
    public bool StablizerInstalled;
    public bool RollInstalled;
    public bool LookRotationInstalled;
    public bool AutomaticOrbitInstalled;
    public bool TractorBeamInstalled;
    public bool TerranFlightInstalled;


    public BrassShipDoorBehavior Door;
    public Transform potentialController;
    public Transform controllerLookTransform;
    public bool passengerPresent = false;
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
    public LoopSoundCollection ShipEngine;


    private Color _initialThrustDisplayColor;
    public Transform Fwd1;
    public Transform Fwd2, Fwd3, Back1, Back2, Back3, Left1, Left2, Left3, Right1, Right2, Right3, Up1, Up2, Up3, Down1, Down2, Down3;
    private Material mFwd1, mFwd2, mFwd3, mBack1, mBack2, mBack3, mLeft1, mLeft2, mLeft3, mRight1, mRight2, mRight3, mUp1, mUp2, mUp3, mDown1, mDown2, mDown3;
    private Vector3 thrustDisplay;

    public List<EngineParticleBehavior> UpEngines, DownEngines, LeftEngines, RightEngines, ForwardEngines, BackEngines;

    private Quaternion GoalRotation;
    public Transform FlightRotationVisual;
    public List<InteriorLightBehavior> InteriorLights;

    Vector3 autopilotThrust;
    public void RegisterAutopilotThrust(Vector3 thrust)
    {
        autopilotThrust = thrust;
    }

    private void Awake()
    {
        _initialThrustDisplayColor = Fwd1.transform.GetComponent<Renderer>().sharedMaterial.GetColor("_EmissionColor");
    }
    private void Start()
    {
        transform.GetComponent<SplitterSubscriber>().AppliedPhysics.centerOfMass = -Vector3.up;
        GoalRotation = transform.rotation;

        mFwd1 = Fwd1.transform.GetComponent<Renderer>().material;
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
            passengerPresent = true;
            potentialController = other.transform;

        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<RigidbodyFpsController>())
        {
            potentialController = null;
            passengerPresent = false;
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

    SplitterSubscriber _otherSubscriber;
    private void MaybeTakeHitToStabilization(Collision other)
    {
        
        //bail if this collision is from an object occurring within your simulation
        if (
            transform.GetComponent<SplitterAnchor>() != null
            &&
            other.body.GetComponent<SplitterSubscriber>() != null
            &&
            transform.GetComponent<SplitterAnchor>().IsInMySimulation(other.body.GetComponent<SplitterSubscriber>())

        )
            return;
        
        _FlightRotationWhenHit = GoalRotation;
        GoalRotation = body.AppliedPhysics.rotation;
        _target = body.AppliedPhysics.rotation;
        StabilizationCapability = 0f;

        //camera shake
        Vector3 relVel;
        if (potentialController != null)
        {
            relVel = RelativeVelocity(transform.GetComponent<Rigidbody>(), other.body as Rigidbody, other.contacts.First().point);
            if (relVel.sqrMagnitude <= 25f)
                return;

            potentialController.GetComponent<PlayerPublicInfoServer>().camera.GetComponent<CameraShaker>().AddInput(new CameraShakeInput
            {
                Amplitude = relVel.sqrMagnitude / 5000f,
                Frequency = 10f,
                Decay = .8f,
                Asymmetry = new Vector2(.8f, .64f),
                startTime = Time.time
            });
        }
    }
    void Update()
    {

        HandlePilotSeat();

        //merge autopilot input
        _thrustInput += autopilotThrust.normalized;

        ThrustDisplayUpdate();

        HandleThrustVisuals();

        HandleCameraShake();

        HandleShipSounds();

        DebugScene();

        _thrustInput -= autopilotThrust.normalized;
        autopilotThrust = Vector3.zero;
        
    }


    public void HandleShipSounds()
    {
        if (_thrustInput.sqrMagnitude != 0f)
            ShipEngine.enabled = true;
        else
            ShipEngine.enabled = false;
    }
    void HandleCameraShake()
    {
        if (potentialController != null && _thrustInput.sqrMagnitude != 0f)
        {
            potentialController.GetComponent<PlayerPublicInfoServer>().camera.GetComponent<CameraShaker>().AddInput(new CameraShakeInput
            {
                Amplitude = .0045f,
                Frequency = 20,
                Decay = .2f,
                Asymmetry = new Vector2(.8f, .64f),
                startTime = Time.time
            });
        }
    }
    void DebugScene()
    {
        //FlightRotationVisual.rotation = GoalRotation;
    }
    private void HandlePilotSeat()
    {
        _target = transform.rotation;
        if (passengerPresent == true && Input.GetKeyDown(KeyCode.CapsLock))
        {

            controlled = !controlled;
            if (controlled)
            {
                potentialController.GetComponent<RigidbodyFpsController>().inControllerPosition = controlled;
                //Door.DoorRequestState = BrassShipDoorBehavior.DoorRequestStates.closed;
                
                controllerLookTransform = potentialController.GetComponent<RigidbodyFpsController>().VerticalLook;
                foreach (var light in InteriorLights)
                {
                    light.Switch(true);
                }
            }
            else
            {
                controllerLookTransform = null;
                //Door.DoorRequestState = BrassShipDoorBehavior.DoorRequestStates.open;
                if (potentialController != null)
                    potentialController.GetComponent<RigidbodyFpsController>().inControllerPosition = false;
                foreach (var light in InteriorLights)
                {
                    light.Switch(false);
                }
            }
        }
    }

    private void LateUpdate()
    {
        //if(controlled)

    }
    Quaternion _target;

    void HandleThrustVisuals()
    {
        foreach (var eng in ForwardEngines)
            eng.SetEngineOn(_thrustInput.z > 0);
        foreach (var eng in BackEngines)
            eng.SetEngineOn(_thrustInput.z < 0);
        foreach (var eng in RightEngines)
            eng.SetEngineOn(_thrustInput.x > 0);
        foreach (var eng in LeftEngines)
            eng.SetEngineOn(_thrustInput.x < 0);
        foreach (var eng in UpEngines)
            eng.SetEngineOn(_thrustInput.y > 0);
        foreach (var eng in DownEngines)
            eng.SetEngineOn(_thrustInput.y < 0);

    }
    void ThrustDisplayUpdate()
    {

        thrustDisplay += Vector3.ClampMagnitude(_thrustInput - thrustDisplay, Time.fixedDeltaTime);

        mFwd1.SetColor(
            "_EmissionColor",
            Color.Lerp(
                Color.black,
                _initialThrustDisplayColor,
                -thrustDisplay.z * 3f
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
        _thrustInput = Vector3.zero;
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

        //body.AppliedPhysics.AddForceAtPosition(_thrust * MoveForce * Time.fixedDeltaTime, gameObject.GetComponent<SplitterAnchor>().inclusiveWorldCenterOfMass(), ForceMode.Acceleration);
        body.AppliedPhysics.AddForce(_thrust * MoveForce * Time.fixedDeltaTime, ForceMode.Acceleration);

    }
    Quaternion _FlightRotationWhenHit = Quaternion.identity;
    Stabilizer _simSubStabilizer;
    void SetDirection()
    {
        /*if (!passengerPresent)
        {
            GoalRotation = body.AppliedPhysics.rotation;
            return;
        }*/

        if (controlled)
        {
            if (LookRotationInstalled && Input.GetKey(KeyCode.Tab))
                _target = controllerLookTransform.rotation;

            if (RollInstalled && Input.GetKey(KeyCode.Q))
                _target = _target * Quaternion.AngleAxis(1f * RollSensitivity, Vector3.forward);
            if (RollInstalled && Input.GetKey(KeyCode.E))
                _target = _target * Quaternion.AngleAxis(-1f * RollSensitivity, Vector3.forward);
        }

        if (StabilizationCapability < 1f)
            StabilizationCapability += Time.fixedDeltaTime / 4f;
        if (StabilizationCapability > 1f)
            StabilizationCapability = 1f;

        if (!StablizerInstalled && !Input.GetKey(KeyCode.Tab))
            StabilizationCapability = 0f;

        GoalRotation = Quaternion.Slerp(GoalRotation, _target, Mathf.Clamp01(Time.fixedDeltaTime / .5f));
        
        GoalRotation = Quaternion.Slerp(body.AppliedPhysics.rotation, GoalRotation, StabilizationCapability);
        _target = Quaternion.Slerp(body.AppliedPhysics.rotation, _target, StabilizationCapability);
    }
    private void Rotate()
    {
        body.SmoothRotate(GoalRotation, maxRotateSpeed, rotateFactor, dampenFactor, Mathf.Pow(StabilizationCapability, 4f));

    }

    private Vector3 RelativeVelocity(Rigidbody origin, Rigidbody measure)
    {
        SplitterSubscriber originSubscriber = origin.transform.GetComponent<SplitterSubscriber>();
        SplitterSubscriber measureSubscriber = measure.transform.GetComponent<SplitterSubscriber>();

        Vector3 originPointVel;
        if (originSubscriber != null)
            originPointVel = originSubscriber.AppliedPhysics.velocity;
        else
            originPointVel = origin.velocity;

        Vector3 measurePointVel;
        if (measureSubscriber != null)
            measurePointVel = measureSubscriber.AppliedPhysics.velocity;
        else
            measurePointVel = measure.velocity;

        return measurePointVel - originPointVel;

    }

    private Vector3 RelativeVelocity(Rigidbody origin, Rigidbody measure, Vector3 WorldPos)
    {
        SplitterSubscriber originSubscriber = origin.transform.GetComponent<SplitterSubscriber>();
        SplitterSubscriber measureSubscriber = measure.transform.GetComponent<SplitterSubscriber>();

        Vector3 originPointVel;
        if (originSubscriber != null)
            originPointVel = originSubscriber.AppliedPhysics.GetPointVelocity(WorldPos);
        else
            originPointVel = origin.GetPointVelocity(WorldPos);

        Vector3 measurePointVel;
        if (measureSubscriber != null)
            measurePointVel = measureSubscriber.AppliedPhysics.GetPointVelocity(WorldPos);
        else
            measurePointVel = measure.GetPointVelocity(WorldPos);

        return measurePointVel - originPointVel;
    }

}
