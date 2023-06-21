using NoTime.Splitter;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoTime.Splitter.Demo
{
    public class RigidbodyFpsController : SplitterEventListener
    {

        public bool Grounded = true;
        public float GroundedOffset = .67f;
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
            body = transform.GetComponent<SplitterSubscriber>();
        }
        private void Start()
        {
            _verticalLook = VerticalLook;
            VerticalLookStart = Quaternion.identity;
        }
        void Update()
        {
            rotationX += Input.GetAxis("Mouse X") * sensitivityX;
            _rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            ShouldJump = ShouldJump || Input.GetKeyDown(KeyCode.Space);
            JetpackUp = (!Grounded && Input.GetKeyDown(KeyCode.Space)) || (JetpackUp && Input.GetKey(KeyCode.Space));
            JetpackDown = (!Grounded && Input.GetKeyDown(KeyCode.LeftShift)) || (JetpackDown && Input.GetKey(KeyCode.LeftShift));
        }
        Vector3 previousPosition;
        bool Moved = false;
        bool ShouldJump = false;

        Vector3 GravityForce = Vector3.zero;
        private void FixedUpdate()
        {
            //_rotation = body.rotation;

            GroundCheck();

            /*GravityForce = Vector3.zero;
            foreach (var go in GameObject.FindGameObjectsWithTag("GravitySource"))
            {
                GravityForce +=
                    (9.8f * 99f / (go.transform.position - transform.position).sqrMagnitude)
                    * (go.transform.position - transform.position).normalized;
            }

            body.AddForce(GravityForce);*/

            Look();

            if(_rotateToGravity)
                AlignRotationWithGravity();

            

            

            //body.MoveRotation(_rotation);

            Move();

            SpaceRotate();

            Jump();

            Jetpack();

            //sticky
            EnforceMinimumMovementDistance();

            //friction
            FrictionAndSlowdown();

        }


        private void Move()
        {
            if (
                !inControllerPosition &&
                (
                    (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
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

                float MoveForce = WalkForce;
                if (!Grounded)
                    MoveForce = WalkForce / 4f;


                body.AppliedPhysics.AddForce(direction * MoveForce, ForceMode.Acceleration);
                Moved = true;
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
                if (
                    previousPosition != null
                    && (previousPosition - body.AppliedPhysics.position).sqrMagnitude < MinimumMovementDistance * MinimumMovementDistance
                )
                    body.AppliedPhysics.MovePosition(previousPosition);

                previousPosition = body.AppliedPhysics.position;
            }
        }

        private void FrictionAndSlowdown()
        {
            if (Grounded &&
                    (
                        !Moved
                        ||
                        //going faster than maxwalkspeed
                        Vector3.ProjectOnPlane(body.AppliedPhysics.velocity, GravityForce.normalized).sqrMagnitude > MaxWalkSpeed * MaxWalkSpeed
                     )
            )
                body.AppliedPhysics.drag = StopForce;
            else
                body.AppliedPhysics.drag = 0f;
        }

        private void Jump()
        {
            if (ShouldJump && Grounded && !inControllerPosition)
            {
                body.AppliedPhysics.AddForce(JumpForce * body.transform.up, ForceMode.Impulse);
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
        private void GroundCheck()
        {
            //set collider layer to tmpExclue
            _oldLayer = gameObject.layer;
            gameObject.layer = 31;
            _spherePos = body.AppliedPhysics.position + (body.AppliedPhysics.rotation * -Vector3.up * GroundedOffset);
            Grounded = gameObject.scene.GetPhysicsScene().SphereCast(body.AppliedPhysics.position, GroundedRadius, _spherePos - body.AppliedPhysics.position, out _hit, GroundedOffset, GroundLayers, QueryTriggerInteraction.Ignore);
            if (Grounded)
            {
                //body.AppliedPhysics.MovePosition(transform.position + ((_spherePos - _hit.point).normalized * GroundedRadius*.9f));
            }
            
            
            gameObject.layer = _oldLayer;
        }

        private void AlignRotationWithGravity()
        {
            if (transform.GetComponent<GravityObject>().GravityDistance > 300f)
                return;
            GravityForce = transform.GetComponent<GravityObject>().GravityDirection * transform.GetComponent<GravityObject>().GravityAcceleration;
            Quaternion target = Quaternion.FromToRotation(body.AppliedPhysics.rotation * Vector3.down, GravityForce.normalized);
            body.AppliedPhysics.MoveRotation(Quaternion.Slerp(body.AppliedPhysics.rotation, target * body.AppliedPhysics.rotation, .1f 
                
            ));
        }

        private float rotationX = 0F;
        private float _rotationY = 0F;
        void Look()
        {
            Quaternion xQuaternion =
                Quaternion.AngleAxis(rotationX, Vector3.up);
            body.AppliedPhysics.MoveRotation(body.AppliedPhysics.rotation * xQuaternion);
            rotationX = 0;

            Quaternion yQuaternionAddition = Quaternion.AngleAxis(-_rotationY, Vector3.right);
            Quaternion potentialNewLocal = (yQuaternionAddition * _verticalLook.localRotation);
            if (Mathf.Abs(Quaternion.Angle(VerticalLookStart, potentialNewLocal)) < this.VerticalLookMaxAngle)
                _verticalLook.localRotation = potentialNewLocal;
            _rotationY = 0;
        }

        private void SpaceRotate()
        {
            if(transform.GetComponent<GravityObject>().GravityAcceleration == 0f)
                body.SmoothRotate(VerticalLook.rotation, 5f, .5f, .2f, 1f);

            /*if (Input.GetKey(KeyCode.Q))
                body.AppliedPhysics.AddRelativeTorque(Vector3.forward * RollSensitivity, ForceMode.Acceleration);
            if (Input.GetKey(KeyCode.E))
                body.AppliedPhysics.AddRelativeTorque(-Vector3.forward * RollSensitivity, ForceMode.Acceleration);*/

        }

        private void OnDrawGizmos()
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
                Gizmos.DrawSphere(body.AppliedPhysics.position + (body.AppliedPhysics.rotation * -Vector3.up * GroundedOffset), GroundedRadius);
            }
            else
            {
                Gizmos.DrawSphere(transform.position + (transform.rotation * -Vector3.up * GroundedOffset), GroundedRadius);
            }
        }

        

        public override void OnEnterAnchor(SplitterEvent evt)
        {
            this._verticalLook = evt.SimulatedSubscriber.GetComponent<RigidbodyFpsController>().VerticalLook;
        }

        public override void OnExitAnchor(SplitterEvent evt)
        {
            this._verticalLook = VerticalLook;
        }
    }
}

