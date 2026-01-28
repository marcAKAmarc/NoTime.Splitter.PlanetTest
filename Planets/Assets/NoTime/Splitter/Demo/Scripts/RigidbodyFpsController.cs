using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using static NoTime.Splitter.Demo.hitHelpers;
using static UnityEngine.GraphicsBuffer;

namespace NoTime.Splitter.Demo
{
    public enum FPSPhysicalState { steady, unsteady, collapsed}
    public class RigidbodyFpsController : SplitterEventListener
    {

        public bool Grounded = true;
        public FPSPhysicalState physicalState; 
        public float GroundedOriginOffset = .67f;
        public float GroundedOriginCastDistance = .67f;
        public float GroundedRiseAmt = .01f;
        public float GroundedRadius = 0.45f;
        public LayerMask GroundLayers;
        public float WalkForce = 500f;
        public float StopForce = 5f;
        public float MaxWalkSpeed = 5f;
        public float MinimumMovementDistance = .01f;
        public float sensitivityX = 8F;
        public float sensitivityY = 6F;
        public float VerticalLookMaxAngle = 60F;
        public Transform VerticalLook;
        private Transform _verticalLook;
        private Quaternion VerticalLookStart;
        public float JumpForce;
        public bool MarioJumpEnabled;
        public float JetpackForce;
        private bool JetpackUp = false;
        private bool JetpackDown = false;
        private bool JetpackFwd, JetpackBack, JetpackLeft, JetpackRight;
        public LoopSoundCollection jetpackSound;
        public CameraShaker CameraShaker;
        public AudioMixer ExternalsMixer;
        public string CutoffParameter;
        public FootstepSoundsBehaviour footstepSounds;
        public PhysicalSounds physicalSounds;
        public VisorBehaviour visorBehaviour;
        [HideInInspector]
        public bool inControllerPosition = false;

        private SplitterSubscriber body;
        public HintController hintController;
        CameraShakeInput jetpackShake;
        public CameraController camController;
        public AlignWithGravity gravityAligner;

        public float CollapseImpulse;
        public float UnsteadyImpulse;
        public float CollapsedTime;
        public float UnsteadyTime;
        private WaitForSeconds CollapsedTimeWait, UnsteadyTimeWait;
        private void Awake()
        {
            jetpackShake = new CameraShakeInput
            {
                Attack = .2f,
                Amplitude = 0f,
                AngularAmplitude = .2f,
                Frequency = 15f,
                Decay = .2f,
                Asymmetry = new Vector2(.8f, .64f),
                startTime = 0f
            };
            _spearRotationTarget = transform.rotation;
            body = transform.GetComponent<SplitterSubscriber>();

            CollapsedTimeWait = new WaitForSeconds(CollapsedTime);
            UnsteadyTimeWait = new WaitForSeconds(UnsteadyTime);
        }

        private void Start()
        {
            _gravityObject = transform.GetComponent<GravityObject>();
            _verticalLook = VerticalLook;
            VerticalLookStart = Quaternion.identity;
        }


        bool prevInspace;
        bool prevInControllerPosition;
        bool freezeLook;
        bool lowAtmosphere;
        void Update()
        {
            if (_gravityObject)
                lowAtmosphere = _gravityObject.GravityAcceleration < 3f;

            rotationX += Input.GetAxis("Mouse X") * sensitivityX;
            _rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            ShouldJump = ShouldJump || Input.GetKeyDown(KeyCode.Space);
            
            JetpackUp = (!Grounded && Input.GetKeyDown(KeyCode.Space)) || (JetpackUp && Input.GetKey(KeyCode.Space));
            JetpackDown = (!Grounded && Input.GetKeyDown(KeyCode.LeftShift)) || (JetpackDown && Input.GetKey(KeyCode.LeftShift));
            JetpackFwd =  (InSpace && Input.GetKeyDown(KeyCode.W)) || (JetpackFwd && Input.GetKey(KeyCode.W));
            JetpackBack = (InSpace && Input.GetKeyDown(KeyCode.S)) || (JetpackBack && Input.GetKey(KeyCode.S));
            JetpackLeft = (InSpace && Input.GetKeyDown(KeyCode.A)) || (JetpackLeft && Input.GetKey(KeyCode.A));
            JetpackRight =(InSpace && Input.GetKeyDown(KeyCode.D)) || (JetpackRight && Input.GetKey(KeyCode.D));

            ShouldMarioJump = MarioJumpEnabled && !InSpace && !JetpackUp && (ShouldMarioJump || ShouldJump) && (Input.GetKeyDown(KeyCode.Space) || Input.GetKey(KeyCode.Space));
            freezeLook = Input.GetKey(KeyCode.F);


            //jetpack fx
            if(AnyJetpack())
                CameraShaker.AddInput(jetpackShake);

            if (AnyJetpack() && jetpackSound.enabled == false)
            {
                jetpackSound.enabled = true;
            }
            if(!AnyJetpack() && jetpackSound.enabled == true)
            {
                jetpackSound.enabled = false;
            }

            if (InSpace || lowAtmosphere)
            {
                ExternalsMixer.SetFloat(CutoffParameter, 150f);
            }
            else
            {
                ExternalsMixer.SetFloat(CutoffParameter, 22000f);
            }

            //footstep fx
            if (!footstepSounds.enabled && Walking())
            {
                footstepSounds.enabled = true;
            }
            else if (footstepSounds.enabled && !Walking())
                footstepSounds.enabled = false;

            if (footstepSounds.enabled)
            {
                if (body.Simulating())
                    footstepSounds.CurrentCharacterSpeed = body.GetSimulationBody().velocity.magnitude;
                else
                    footstepSounds.CurrentCharacterSpeed = body.AppliedPhysics.velocity.magnitude;
            }
            if(prevInspace != InSpace)
            {
                visorBehaviour.SetVisor(InSpace);
                if (InSpace)
                    hintController.ActivateHint(hintType.JetpackInSpace);
                else
                    hintController.DeactivateHint(hintType.JetpackInSpace);
            }

            //camera stuff
            if (inControllerPosition && !prevInControllerPosition)
                camController.AddState(CameraController.CameraStateType.Pilot);
            if (!inControllerPosition && prevInControllerPosition)
                camController.RemoveState(CameraController.CameraStateType.Pilot);

            prevInspace = InSpace;
            prevInControllerPosition = inControllerPosition;
        }
        private bool Walking()
        {
            return !InSpace && Grounded && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D));
        }
        private bool AnyJetpack()
        {
            return JetpackUp || JetpackDown || JetpackFwd || JetpackBack || JetpackLeft || JetpackRight;
        }
        Vector3 previousPosition;
        bool Moved = false;
        bool ShouldJump = false;
        bool ShouldMarioJump = false;

        private void OnCollisionEnter(Collision collision)
        {
            HandleBlowToPhysicalState(collision.impulse.sqrMagnitude);
        }

        private void HandleBlowToPhysicalState(float impulse)
        {
            Debug.Log("Impulse sqr: " + impulse);
            if(impulse > Mathf.Pow(CollapseImpulse,2f))
            {
                if (currentPhysicalStateRoutine != null)
                    StopCoroutine(currentPhysicalStateRoutine);
                currentPhysicalStateRoutine = StartCoroutine(PhysicalStateRecovery(FPSPhysicalState.collapsed));
            }
            else if(impulse > Mathf.Pow(UnsteadyImpulse,2f))
            { 
                if (currentPhysicalStateRoutine != null)
                    StopCoroutine(currentPhysicalStateRoutine);
                currentPhysicalStateRoutine = StartCoroutine(PhysicalStateRecovery(FPSPhysicalState.unsteady));
            }
        }
        private Coroutine currentPhysicalStateRoutine;
        IEnumerator PhysicalStateRecovery(FPSPhysicalState startState)
        {
            if(startState == FPSPhysicalState.collapsed)
            {
                physicalState = FPSPhysicalState.collapsed;
                yield return CollapsedTimeWait;
                physicalState = FPSPhysicalState.unsteady;
                yield return UnsteadyTimeWait;
                physicalState = FPSPhysicalState.steady;
            }
            if(startState == FPSPhysicalState.unsteady)
            {
                physicalState = FPSPhysicalState.unsteady;
                yield return UnsteadyTimeWait;
                physicalState = FPSPhysicalState.steady;
            }
            yield return null;
        }

        bool InSpace = false;
        private void FixedUpdate()
        {
            if (body.Anchor == null)
                InSpace = true;

            RotationalStuff();

            if (physicalState == FPSPhysicalState.collapsed)
            {
                Grounded = false;
                _gravityObject.ApplyGravity = true;
            }
            if (physicalState != FPSPhysicalState.collapsed)
            {
                GroundCheck();
            }

                GravityLook();

                /*if (_rotateToGravity)
                    AlignRotationWithGravity();*/

                Move();


                Jump();

                Jetpack();

                //sticky
                //EnforceMinimumMovementDistance();

                //friction
                if (!TempDisableFriction)
                    FrictionAndSlowdown();
            
            
        }
        
        private void RotationalStuff()
        {
            if (
                physicalState == FPSPhysicalState.unsteady || physicalState == FPSPhysicalState.collapsed
            )
                body.AppliedPhysics.constraints = RigidbodyConstraints.None;
            else
            {
                if (InSpace)
                    body.AppliedPhysics.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
                else
                    body.AppliedPhysics.constraints = RigidbodyConstraints.FreezeRotation;
            }

            if(physicalState == FPSPhysicalState.collapsed)
            {
                if (gravityAligner.enabled)
                    gravityAligner.enabled = false;
            }
            else
            {
                if (!gravityAligner.enabled)
                    gravityAligner.enabled = true;
            }
        }

        private Quaternion _spearRotationTarget;
        private void Move()
        {
            if (
                !inControllerPosition &&
                (
                    Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E)
                )

            )
            {
                Vector3 direction = Vector3.zero;
                if (Input.GetKey(KeyCode.W))
                    direction += Vector3.forward;
                if (Input.GetKey(KeyCode.A))
                    direction += Vector3.left;
                if (Input.GetKey(KeyCode.D))
                    direction += Vector3.right;
                if (Input.GetKey(KeyCode.S))
                    direction += Vector3.back;
                direction = direction.normalized;

                direction = body.AppliedPhysics.rotation * direction;

                if (Grounded)
                {
                    direction = Vector3.ProjectOnPlane(direction, worldHit.normal).normalized;
                    

                    //body.AppliedPhysics.AddForce((body.AppliedPhysics.rotation * Vector3.up * GroundedRiseAmt / Time.fixedDeltaTime) * body.AppliedPhysics.mass);
                }
                    

                float MoveForce = WalkForce;
                if (!Grounded)
                    MoveForce = WalkForce / 4f;


                body.AppliedPhysics.AddForce(direction * MoveForce, ForceMode.Acceleration);
                Moved = true;

                if (InSpace)
                {
                    if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E))
                    {
                        Vector3 torque = Vector3.zero;
                        if (Input.GetKey(KeyCode.Q))
                            torque += body.AppliedPhysics.transform.forward * 1f * RollSensitivity;
                        if (Input.GetKey(KeyCode.E))
                            torque += body.AppliedPhysics.transform.forward * -1f * RollSensitivity;
                        body.AppliedPhysics.AddTorque(torque, ForceMode.Force);
                    }
                    
                }
            }
            else
            {
                Moved = false;
            }
        }

        public bool _rotateToGravity = true;

        private void EnforceMinimumMovementDistance()
        {
            if (Grounded && !Moved)
            {
                if (body.GetSimulationBody() != null
                    &&
                    body.GetSimulationBody().velocity.sqrMagnitude < Mathf.Pow(MinimumMovementDistance, 2f)
                )
                {
                    body.AppliedPhysics.AddForce(-(body.GetSimulationBody().velocity.magnitude * body.AppliedPhysics.velocity.normalized), ForceMode.VelocityChange);
                }

                else if (
                    previousPosition != null
                    && (previousPosition - body.AppliedPhysics.position).sqrMagnitude < MinimumMovementDistance * MinimumMovementDistance
                )
                    body.AppliedPhysics.MovePosition(previousPosition);

                previousPosition = body.AppliedPhysics.position;
            }
        }

        Vector3 fricVel;
        Vector3 normalDir;
        private GravityObject _gravityObject;
        private SplitterAnchor _hitAnchor;

        private void FrictionAndSlowdown()
        {
            
            if (InSpace || !Grounded || TempDisableFriction || worldHit.rigidbody == null)
            {
                /*if (_hit.rigidbody == null)
                    Debug.Log("No rigidbody on _hit.");
                if (InSpace)
                    Debug.Log("In Space");
                if (!Grounded)
                    Debug.Log("Not Grounded");*/
                
                return;

            }

            if (worldHit.rigidbody != null)
            {
                worldHit.rigidbody.TryGetComponent(out _hitAnchor);
            }
            fricVel = body.AppliedPhysics.velocity;

            
            if(body.Anchor != null && _hitAnchor != null && body.Anchor.gameObject == _hitAnchor.gameObject)
            {
                //Debug.Log("working from same anchor");
                fricVel -= body.Anchor.AnchorVelocityToWorldVelocity(Vector3.zero, body.AppliedPhysics.position);
            }
            else if (worldHit.sub != null)
            {
                //Debug.Log(" working from hitsub");
                fricVel -= worldHit.sub.AppliedPhysics.GetPointVelocity(body.AppliedPhysics.position);
            }
            //what about just rigids?
            else if (body.Anchor != null)
            {
                fricVel -= body.Anchor.AnchorVelocityToWorldVelocity(Vector3.zero, body.AppliedPhysics.position);
            }
            else
            {
                fricVel -= worldHit.rigidbody.GetPointVelocity(body.AppliedPhysics.position);
            }

            if (
                !Moved
                ||
                //going faster than maxwalkspeed
                fricVel.sqrMagnitude > Mathf.Pow(MaxWalkSpeed, 2f)                     
            )
            {      
                body.AppliedPhysics.AddForce(StopForce * -fricVel, ForceMode.Acceleration);
            }

        }

        private void Jump()
        {
            if (ShouldJump && Grounded && !inControllerPosition)
            {
                body.AppliedPhysics.AddForce(JumpForce * body.transform.up, ForceMode.Impulse);
                if (_preventGroundChecker != null)
                    StopCoroutine(_preventGroundChecker);
                _preventGroundChecker = PreventGroundCheck();
                StartCoroutine(_preventGroundChecker);
                Grounded = false;

            }
            

            if(ShouldMarioJump && 
                (
                    (ShouldJump) 
                    || (body.Anchor != null && Vector3.Dot(body.AppliedPhysics.velocity.normalized, -_gravityObject.GravityDirection.normalized) > 0f)
                )
            )
            {
                body.AppliedPhysics.AddForce(transform.up  * _gravityObject.GravityAcceleration/2f, ForceMode.Acceleration);
            }
            else
            {
                ShouldMarioJump = false;
            }

            ShouldJump = false;
        }

        private void Jetpack()
        {
            if (JetpackUp)
            {
                body.AppliedPhysics.AddForce(JetpackForce * body.transform.up, ForceMode.Force);
            }
            if (JetpackDown)
            {
                body.AppliedPhysics.AddForce(JetpackForce * -body.transform.up, ForceMode.Force);
            }
        }

        private RaycastHit _hit = new RaycastHit();
        private Vector3 _spherePos;
        private int _oldLayer;
        private Ray _ray;
        private float _dotResult;
        private Vector3 _projResult;
        private Vector3 _anchorVelocity;
        public bool TempDisableGroundCheck;
        public bool TempDisableFriction;
        private Vector3 gravDir;
        private bool wasGrounded;
        private SplitterSubscriber GroundCheckHitSub;

        private WorldHit worldHit;
        private void GroundCheck()
        {   
            wasGrounded = Grounded;

            
            if (InSpace)
            {
                Grounded = false;
                if(_gravityObject != null)
                    _gravityObject.ApplyGravity = true;
                return;
            }
            if (preventGroundCheck || TempDisableGroundCheck)
                return;

            

            Vector3 pointUnderneath;
            Vector3 worldPoint = Vector3.zero;
            //ugh do this in Anchor? //this fixes it but it fucks up so much shit cause we depend on _hit elsewhere... see below...

            //in anchor space, do the sphere cast and placement of player
            if (body.Anchor && body.Anchor.Scene.HasValue) {
                
                Rigidbody simBody = body.Anchor.GetSimulationBody(body);
                
                _oldLayer = simBody.gameObject.layer;
                simBody.gameObject.layer = 31;

                _spherePos = simBody.position + (simBody.rotation * -Vector3.up * GroundedOriginOffset);
                Grounded = body.Anchor.Scene.Value.GetPhysicsScene().SphereCast(simBody.position, GroundedRadius, (_spherePos - simBody.position).normalized, out _hit, GroundedOriginCastDistance, GroundLayers, QueryTriggerInteraction.Ignore);

                simBody.gameObject.layer = _oldLayer;

                if (Grounded) {
                    pointUnderneath = Vector3.Project(_hit.point - simBody.position, simBody.rotation * Vector3.down) + simBody.position;
                    simBody.position = pointUnderneath + simBody.rotation * Vector3.up * GroundedRiseAmt;
                }
                _hit.toWorldHit(body.Anchor, out worldHit);
            }
            else
            //in real space, do raycast and placement of player
            {
                //set collider layer to tmpExclue
                _oldLayer = gameObject.layer;
                gameObject.layer = 31;

                _spherePos = body.AppliedPhysics.position + (body.AppliedPhysics.rotation * -Vector3.up * GroundedOriginOffset);
                Grounded = gameObject.scene.GetPhysicsScene().SphereCast(body.AppliedPhysics.position, GroundedRadius, (_spherePos - body.AppliedPhysics.position).normalized, out _hit, GroundedOriginCastDistance, GroundLayers, QueryTriggerInteraction.Ignore);

                gameObject.layer = _oldLayer;
                if (Grounded)
                {
                    pointUnderneath = Vector3.Project(_hit.point - body.AppliedPhysics.position, body.AppliedPhysics.rotation * Vector3.down) + body.AppliedPhysics.position;


                    body.AppliedPhysics.position =
                        pointUnderneath +
                        (body.AppliedPhysics.rotation * Vector3.up //body up
                            *
                            GroundedRiseAmt
                        );
                    _hit.toWorldHit(out worldHit);
                }
            }


            if (Grounded)
            {



                /*if (_gravityObject.ApplyGravity)
                {
                    //undo gravity
                    Vector3 downAmt = Vector3.Project(body.AppliedPhysics.velocity
                        - body.Anchor.GetComponent<Rigidbody>().GetPointVelocity(pointUnderneath)
                        , (_gravityObject.GravityDirection * _gravityObject.GravityAcceleration).normalized);

                    if (Vector3.Dot(downAmt.normalized, body.AppliedPhysics.rotation * Vector3.down) > 0f)
                    {
                        body.AppliedPhysics.velocity -= downAmt;
                    }
                }*/
                _gravityObject.ApplyGravity = false;
                /*body.AppliedPhysics.MovePosition(
                    pointUnderneath +
                    (body.AppliedPhysics.rotation * Vector3.up //body up
                        *
                        GroundedRiseAmt
                    )
                );*/
                /*body.AppliedPhysics.AddForce(
                    (
                        (pointUnderneath +
                            (body.AppliedPhysics.rotation * Vector3.up //body up
                                *
                                GroundedRiseAmt
                            )
                        )
                        - body.AppliedPhysics.position
                    )
                    ,ForceMode.VelocityChange
                );*/
            }
            else
            {
                if (_gravityObject != null)
                    _gravityObject.ApplyGravity = true;
            }

            if(!wasGrounded && Grounded)
            {
                
                if(worldHit.sub)
                {
                    physicalSounds.SimulateCollision(
                        Mathf.Min(worldHit.sub.AppliedPhysics.mass, body.AppliedPhysics.mass)
                            * (worldHit.sub.AppliedPhysics.GetPointVelocity(worldHit.point) - body.AppliedPhysics.velocity).sqrMagnitude,
                        worldHit.point
                    );
                }
                else
                {
                    physicalSounds.SimulateCollision(
                        Mathf.Min(worldHit.rigidbody.mass, body.AppliedPhysics.mass)
                        * (worldHit.rigidbody.velocity - body.AppliedPhysics.velocity).sqrMagnitude,
                        worldHit.point
                     );
                }
            }

            /*//should we just redo the cast to get the hit?! gross!
            if (body.Anchor && body.Anchor.Scene.HasValue)
            {
                //set collider layer to tmpExclue
                _oldLayer = gameObject.layer;
                gameObject.layer = 31;

                _spherePos = body.AppliedPhysics.position + (body.AppliedPhysics.rotation * -Vector3.up * GroundedOriginOffset);
                Grounded = gameObject.scene.GetPhysicsScene().SphereCast(body.AppliedPhysics.position, GroundedRadius, (_spherePos - body.AppliedPhysics.position).normalized, out _hit, GroundedOriginCastDistance, GroundLayers, QueryTriggerInteraction.Ignore);

                gameObject.layer = _oldLayer;
                if (Grounded)
                {
                    pointUnderneath = Vector3.Project(_hit.point - body.AppliedPhysics.position, body.AppliedPhysics.rotation * Vector3.down) + body.AppliedPhysics.position;


                    body.AppliedPhysics.position =
                        pointUnderneath +
                        (body.AppliedPhysics.rotation * Vector3.up //body up
                            *
                            GroundedRiseAmt
                        );
                    worldPoint = _hit.point;
                }
            }*/
        }
        private WaitForSeconds PreventGroundCheckTime = new WaitForSeconds(.2f);
        private bool preventGroundCheck = false;
        private IEnumerator _preventGroundChecker;
        IEnumerator PreventGroundCheck()
        {
            preventGroundCheck = true;
            yield return PreventGroundCheckTime;
            preventGroundCheck = false;
        }



        private float rotationX = 0F;
        private float _rotationY = 0F;
        public float RollSensitivity;
        public float rotateFactor;
        public float maxRotateSpeed;
        public float dampenFactor;

        void GravityLook()
        {

            Quaternion xQuaternion =
                Quaternion.AngleAxis(rotationX, Vector3.up);
            body.AppliedPhysics.MoveRotation(body.AppliedPhysics.rotation * xQuaternion);
            rotationX = 0;   

            Quaternion yQuaternionAddition = Quaternion.AngleAxis(-_rotationY, Vector3.right);
            //YOOO THIS IF IS TEMP TODO
            if (VerticalLook != null)
            {
                Quaternion potentialNewLocal = (yQuaternionAddition * _verticalLook.localRotation);
                if (Mathf.Abs(Quaternion.Angle(VerticalLookStart, potentialNewLocal)) < this.VerticalLookMaxAngle)
                    _verticalLook.localRotation = potentialNewLocal;
            }
            _rotationY = 0;
        }

        private void OnDrawGizmosSelected()
        {

            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded)
                Gizmos.color = transparentGreen;
            else
                Gizmos.color = transparentRed;
            if (body != null)
            {
                //draw ground collider
                Gizmos.DrawSphere(body.AppliedPhysics.position + (body.AppliedPhysics.rotation * -Vector3.up * GroundedOriginOffset), GroundedRadius);
            }
            else
            {
                Gizmos.DrawSphere(transform.position + (transform.rotation * -Vector3.up * GroundedOriginOffset), GroundedRadius);
            }
        }



        public override void OnEnterAnchor(SplitterEvent evt)
        {
            InSpace = false;
            //body.AppliedPhysics.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    public static class hitHelpers
    {
       
        public struct WorldHit
        {
            public Vector3 point;
            public Rigidbody rigidbody;
            public SplitterSubscriber sub;
            public Vector3 normal;
        }
        
        public static WorldHit toWorldHit(this RaycastHit worldspaceHit, SplitterAnchor anchorContext, out WorldHit hit)
        {
            hit.point = Vector3.zero;
            hit.normal = Vector3.zero;
            hit.rigidbody = null;
            hit.sub = null;

            hit.point = anchorContext.AnchorPointToWorldPoint(worldspaceHit.point);
            hit.normal = anchorContext.AnchorDirectionToWorldDirection(worldspaceHit.normal);

            if (worldspaceHit.rigidbody)
                anchorContext.GetWorldTransform(worldspaceHit.rigidbody.transform).TryGetComponent(out hit.rigidbody);
            else
                hit.rigidbody = null;

            if (hit.rigidbody != null)
                hit.rigidbody.transform.TryGetComponent(out hit.sub);

            return hit;
        }

        public static WorldHit toWorldHit(this RaycastHit worldspaceHit, out WorldHit hit)
        {
            hit.point = Vector3.zero;
            hit.normal = Vector3.zero;
            hit.rigidbody = null;
            hit.sub = null;

            hit.point = worldspaceHit.point;
            hit.normal = worldspaceHit.normal;
            hit.rigidbody = worldspaceHit.rigidbody;

            if (hit.rigidbody != null)
                hit.rigidbody.transform.TryGetComponent(out hit.sub);

            return hit;
        }
    }
}

