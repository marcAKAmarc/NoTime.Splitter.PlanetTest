using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace NoTime.Splitter.Demo
{
    public class RigidbodyFpsController : SplitterEventListener
    {

        public bool Grounded = true;
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
        public float JetpackForce;
        private bool JetpackUp = false;
        private bool JetpackDown = false;


        [HideInInspector]
        public bool inControllerPosition = false;

        private SplitterSubscriber body;

        private void Awake()
        {
            _spearRotationTarget = transform.rotation;
            body = transform.GetComponent<SplitterSubscriber>();
        }
        private void Start()
        {
            _verticalLook = VerticalLook;
            VerticalLookStart = Quaternion.identity;
        }

        bool freezeLook;
        void Update()
        {
            rotationX += Input.GetAxis("Mouse X") * sensitivityX;
            _rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            ShouldJump = ShouldJump || Input.GetKeyDown(KeyCode.Space);
            JetpackUp = (!Grounded && Input.GetKeyDown(KeyCode.Space)) || (JetpackUp && Input.GetKey(KeyCode.Space));
            JetpackDown = (!Grounded && Input.GetKeyDown(KeyCode.LeftShift)) || (JetpackDown && Input.GetKey(KeyCode.LeftShift));
            freezeLook = Input.GetKey(KeyCode.F);
        }
        Vector3 previousPosition;
        bool Moved = false;
        bool ShouldJump = false;


        bool InSpace = false;
        private void FixedUpdate()
        {
            if (body.Anchor == null)
                InSpace = true;
            if (InSpace)
                body.AppliedPhysics.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
            else
                body.AppliedPhysics.constraints = RigidbodyConstraints.FreezeRotation;
            GroundCheck();

            
            GravityLook();

            /*if (_rotateToGravity)
                AlignRotationWithGravity();*/

            Move();


            Jump();

            Jetpack();

            //sticky
            //EnforceMinimumMovementDistance();

            //friction
            if(!TempDisableFriction)
                FrictionAndSlowdown();

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
                    direction = Vector3.ProjectOnPlane(direction, _hit.normal).normalized;
                    

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
        private SplitterSubscriber _hitSub;
        private SplitterAnchor _hitAnchor;

        private void FrictionAndSlowdown()
        {
            if (InSpace || !Grounded || TempDisableFriction || _hit.rigidbody == null)
            {
                /*if (_hit.rigidbody == null)
                    Debug.Log("No rigidbody on _hit.");
                if (InSpace)
                    Debug.Log("In Space");
                if (!Grounded)
                    Debug.Log("Not Grounded");*/
                
                return;

            }

            if (_hit.rigidbody != null)
            {
                _hitSub = _hit.rigidbody.GetComponent<SplitterSubscriber>();
                _hitAnchor = _hit.rigidbody.GetComponent<SplitterAnchor>();
            }
            fricVel = body.AppliedPhysics.velocity;

            
            if(body.Anchor != null && _hitAnchor != null && body.Anchor.gameObject == _hitAnchor.gameObject)
            {
                //Debug.Log("working from same anchor");
                fricVel -= body.Anchor.AnchorVelocityToWorldVelocity(Vector3.zero, body.AppliedPhysics.position);
            }
            else if (_hitSub != null)
            {
                //Debug.Log(" working from hitsub");
                fricVel -= _hitSub.AppliedPhysics.GetPointVelocity(body.AppliedPhysics.position);
            }
            //what about just rigids?
            else if (body.Anchor != null)
            {
                Debug.Log("working from body anchor");
                fricVel -= body.Anchor.AnchorVelocityToWorldVelocity(Vector3.zero, body.AppliedPhysics.position);
            }
            else
            {
                Debug.Log("working form else");
                fricVel -= _hit.rigidbody.GetPointVelocity(body.AppliedPhysics.position);
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
        private void GroundCheck()
        {
            if (InSpace)
            {
                Grounded = false;
                if(_gravityObject != null)
                    _gravityObject.ApplyGravity = true;
                return;
            }
            if (preventGroundCheck || TempDisableGroundCheck)
                return;
            //gravDir = (_gravityObject.GravityDirection * _gravityObject.GravityAcceleration).normalized;
            //set collider layer to tmpExclue
            _oldLayer = gameObject.layer;
            gameObject.layer = 31;
            _spherePos = body.AppliedPhysics.position + (body.AppliedPhysics.rotation * -Vector3.up * GroundedOriginOffset);
            Grounded = gameObject.scene.GetPhysicsScene().SphereCast(body.AppliedPhysics.position, GroundedRadius, (_spherePos - body.AppliedPhysics.position).normalized, out _hit, GroundedOriginCastDistance, GroundLayers, QueryTriggerInteraction.Ignore);
            gameObject.layer = _oldLayer;

            if (Grounded)
            {
                Vector3 pointUnderneath = Vector3.Project(_hit.point - body.AppliedPhysics.position, body.AppliedPhysics.rotation * Vector3.down) + body.AppliedPhysics.position;


                body.AppliedPhysics.position =
                    pointUnderneath +
                    (body.AppliedPhysics.rotation * Vector3.up //body up
                        *
                        GroundedRiseAmt
                    );
                if (_gravityObject == null)
                    _gravityObject = transform.GetComponent<GravityObject>();

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
}

