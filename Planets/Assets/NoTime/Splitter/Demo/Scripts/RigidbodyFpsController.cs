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
        public float maximumY = 60F;
        public Transform VerticalLook;
        private Transform _verticalLook;
        private Quaternion VerticalLookStart;
        public float JumpForce;



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

            AlignRotationWithGravity();

            

            

            //body.MoveRotation(_rotation);

            Move();

            Jump();


            //sticky
            EnforceMinimumMovementDistance();

            //friction
            FrictionAndSlowdown();

        }


        private void Move()
        {
            if (Grounded &&
                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
            )
            {
                var direction = Vector3.zero;
                if (Input.GetKey(KeyCode.W))
                    direction += Vector3.forward;
                if (Input.GetKey(KeyCode.A))
                    direction += Vector3.left;
                if (Input.GetKey(KeyCode.D))
                    direction += Vector3.right;
                if (Input.GetKey(KeyCode.S))
                    direction += Vector3.back;
                direction = direction.normalized;

                direction = body.rotation * direction;

                body.AddForce(direction * WalkForce * Time.fixedDeltaTime, ForceMode.Acceleration);
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
                    && (previousPosition - body.position).sqrMagnitude < MinimumMovementDistance * MinimumMovementDistance
                )
                    body.MovePosition(previousPosition);

                previousPosition = body.position;
            }
        }

        private void FrictionAndSlowdown()
        {
            if (Grounded &&
                    (
                        !Moved
                        ||
                        //going faster than maxwalkspeed
                        Vector3.ProjectOnPlane(body.velocity, GravityForce.normalized).sqrMagnitude > MaxWalkSpeed * MaxWalkSpeed
                     )
            )
                body.drag = StopForce;
            else
                body.drag = 0f;
        }

        private void Jump()
        {
            if (ShouldJump && Grounded)
            {
                body.AddForce(JumpForce * body.transform.up, ForceMode.Impulse);
            }
            ShouldJump = false;
        }

        private RaycastHit _hit = new RaycastHit();
        private Vector3 _spherePos;
        private int _oldLayer;
        private void GroundCheck()
        {
            //set collider layer to tmpExclue
            _oldLayer = gameObject.layer;
            gameObject.layer = 31;
            _spherePos = body.position + (body.rotation * -Vector3.up * GroundedOffset);
            Grounded = gameObject.scene.GetPhysicsScene().SphereCast(body.position, GroundedRadius, _spherePos - body.position, out _hit, GroundedOffset, GroundLayers, QueryTriggerInteraction.Ignore);
            gameObject.layer = _oldLayer;
        }

        private void AlignRotationWithGravity()
        {
            GravityForce = transform.GetComponent<GravityObject>().GravityDirection * transform.GetComponent<GravityObject>().GravityForce;

            Debug.Log("before align with grav " + body.rotation.eulerAngles.ToString("G10"));
            Quaternion target = Quaternion.FromToRotation(body.rotation * Vector3.down, GravityForce.normalized);
            body.MoveRotation(Quaternion.Slerp(body.rotation, target * body.rotation, .1f));
            Debug.Log(" after align with grav " + body.rotation.eulerAngles.ToString("G10"));
            Debug.Log(" before stupid one " + body.rotation.eulerAngles.ToString("G10"));
            body.MoveRotation(Quaternion.AngleAxis(1f, Vector3.up) * body.rotation);
            Debug.Log(" after stupid one " + body.rotation.eulerAngles.ToString("G10"));
            
            Debug.Log(" my rot " + transform.rotation.eulerAngles.ToString("G10"));
        }

        private float rotationX = 0F;
        private float _rotationY = 0F;
        void Look()
        {
            Debug.Log("before look " + body.rotation.eulerAngles.ToString("G6"));
            Quaternion xQuaternion =
                Quaternion.AngleAxis(rotationX, Vector3.up);
                //Quaternion.FromToRotation(body.rotation * Vector3.forward, Quaternion.AngleAxis(rotationX, body.rotation * Vector3.up) * body.rotation * Vector3.forward);
            body.MoveRotation(body.rotation * xQuaternion);
            Debug.Log("after look " + body.rotation.eulerAngles.ToString("G6"));
            //_rotation = _rotation*xQuaternion;
            rotationX = 0;

            Quaternion yQuaternionAddition = Quaternion.AngleAxis(-_rotationY, Vector3.right);
            Quaternion potentialNewLocal = (yQuaternionAddition * _verticalLook.localRotation);
            if (Mathf.Abs(Quaternion.Angle(VerticalLookStart, potentialNewLocal)) < this.maximumY)
                _verticalLook.localRotation = potentialNewLocal;

            
            _rotationY = 0;
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
                Gizmos.DrawSphere(body.position + (body.rotation * -Vector3.up * GroundedOffset), GroundedRadius);
            }
            else
            {
                Gizmos.DrawSphere(transform.position + (transform.rotation * -Vector3.up * GroundedOffset), GroundedRadius);
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<ExecuteRoute>()
                && other.GetComponentInParent<ExecuteRoute>().enabled
                && other.GetComponentInParent<ExecuteRoute>().PlayTrigger.GetInstanceID() == other.GetInstanceID())
                other.GetComponentInParent<ExecuteRoute>().Play();
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

