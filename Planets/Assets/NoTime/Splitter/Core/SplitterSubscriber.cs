using Assets.NoTime.Splitter.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace NoTime.Splitter
{
    [RequireComponent(typeof(Rigidbody))]
    public class SplitterSubscriber : MonoBehaviour
    {
        [Tooltip("Scripts that need to execute physics operations relative to the anchor, i.e. character controllers")]
        public List<MonoBehaviour> RunInSimulatedSpace;

        public SplitterAnchor Anchor;

        [HideInInspector]
        [SerializeField]
        private List<SplitterAnchor> AnchorStack;

        [HideInInspector]
        [SerializeField]
        private List<Collider> CurrentAnchorTriggers;

        [Tooltip("Render simulated anchor")]
        public bool SimulationVisible = false;

        internal AppliedPhysics AppliedPhysics;

        private void Awake()
        {
            AppliedPhysics = new AppliedPhysics(this, transform.GetComponent<Rigidbody>());
            if(AnchorStack == null)
                AnchorStack = new List<SplitterAnchor>();
            if (CurrentAnchorTriggers == null)
                CurrentAnchorTriggers = new List<Collider>();
        }
        void Start()
        {
            if (NeedToUpdateContext())
                UpdateContextImmediate();
        }
        private void ProcessPotentialAnchorEntrance(Collider other)
        {
            if (other.gameObject.GetComponentInParent<SplitterAnchor>() != null
                && other.gameObject.GetComponentInParent<SplitterAnchor>().enabled)
            {
                //if activator or deactivator, track
                if (
                    other.gameObject.GetComponentInParent<SplitterAnchor>().StayTriggers.Any(x => x.GetInstanceID() == other.GetInstanceID())
                    ||
                    other.gameObject.GetComponentInParent<SplitterAnchor>().EntranceTriggers.Any(x => x.GetInstanceID() == other.GetInstanceID())
                )
                    AddTriggerStack(other);

                //if entrance and stay in triggerstack,
                    //add to anchor stack
                if (
                    CurrentAnchorTriggers.Any(y => 
                        other.gameObject.GetComponentInParent<SplitterAnchor>().EntranceTriggers.Any(
                            x => x.GetInstanceID() == y.GetInstanceID()
                        )
                    )
                    &&
                    CurrentAnchorTriggers.Any(y =>
                        other.gameObject.GetComponentInParent<SplitterAnchor>().StayTriggers.Any(
                            x => x.GetInstanceID() == y.GetInstanceID()
                        )
                    )
                )
                {
                    AddToAnchorStack(
                        other.gameObject.GetComponentInParent<SplitterAnchor>()
                    );
                }
            }

            if (NeedToUpdateContext())
            {
                UpdateContext();
            }
        }

        private void OnDisable()
        {
            UpdateContext();
        }
        private void OnEnable()
        {
            UpdateContext();
        }

        private void ProcessPotentialAnchorExit(Collider other)
        {
            RemoveFromTriggerStack(other);
            UpdateAnchorStackFromTriggerStack();
        }
        private void OnTriggerEnter(Collider other)
        {
            ProcessPotentialAnchorEntrance(other);
        }
        private void OnTriggerExit(Collider other)
        {
            ProcessPotentialAnchorExit(other);
        }
        private void UpdateAnchorStackFromTriggerStack()
        {
            AnchorStack = AnchorStack.Where(x =>
                //keep current anchor
                (Anchor != null && x.gameObject.GetInstanceID() == Anchor.gameObject.GetInstanceID())
                ||
                //keep if we have _entrance_ triggers for this anchor
                CurrentAnchorTriggers.Any(t =>
                    /*t.gameObject.GetComponentInParent<SplitterAnchor>().EntranceTriggers.Any(et=>et.gameObject.GetInstanceID() == t.gameObject.GetInstanceID())
                    &&*/
                    t.gameObject.GetComponentInParent<SplitterAnchor>().gameObject.GetInstanceID() == x.gameObject.GetInstanceID())
            ).ToList();
        }
        public void SimulationExitedAnchor(SplitterAnchor Anchor)
        {
            RemoveFromAnchorStack(Anchor);
            if (NeedToUpdateContext())
                UpdateContext();
        }

        private void AddToAnchorStack(SplitterAnchor anchor)
        {
            if (AnchorStack.Any(x => x.gameObject.GetInstanceID() == anchor.gameObject.GetInstanceID()))
                return;
            AnchorStack = AnchorStack.Where(x => x.EntrancePriority != anchor.EntrancePriority).ToList();
            AnchorStack.Add(anchor);
            AnchorStack = AnchorStack.OrderByDescending(x => x.EntrancePriority).ToList();
        }
        private void RemoveFromAnchorStack(SplitterAnchor anchor)
        {
            AnchorStack = AnchorStack.Where(x => x != null && x.gameObject != null && x.gameObject.GetInstanceID() != anchor.gameObject.GetInstanceID()).ToList();
        }

        private void AddTriggerStack(Collider collider)
        {
            if (CurrentAnchorTriggers.Any(x => x.GetInstanceID() == collider.GetInstanceID()))
                return;
            CurrentAnchorTriggers.Add(collider);
        }

        private void RemoveFromTriggerStack(Collider collider)
        {
            CurrentAnchorTriggers = CurrentAnchorTriggers.Where(x => x.GetInstanceID() != collider.GetInstanceID()).ToList();
        }

        private bool NeedToUpdateContext()
        {
            //null values for both
            if (Anchor == null && AnchorStack.FirstOrDefault() == null)
                return false;

            //null values differ one way
            if (Anchor != null && AnchorStack.FirstOrDefault() == null)
                return true;

            //null values differ another way
            if (Anchor == null && AnchorStack.FirstOrDefault() != null)
                return true;

            //different anchors
            if (Anchor != null && AnchorStack.FirstOrDefault() != null
                && Anchor.gameObject.GetInstanceID() != AnchorStack.FirstOrDefault().gameObject.GetInstanceID())
                return true;

            //same anchor
            if (Anchor != null && AnchorStack.FirstOrDefault() != null
                && Anchor.gameObject.GetInstanceID() == AnchorStack.FirstOrDefault().gameObject.GetInstanceID())
                return false;

            throw new UnityException("Failure in NeedToUpdateContext - Unknown state:  Anchor: " + Anchor.ToString()
                + "; AnchorStack First: " + AnchorStack.FirstOrDefault().ToString());
        }
        private void UpdateContext()
        {
            if (isActiveAndEnabled)
                StartCoroutine(UpdateContextAtEndOfFixedUpdate());
        }
        WaitForFixedUpdate _updateWait = new WaitForFixedUpdate();
        IEnumerator UpdateContextAtEndOfFixedUpdate()
        {
            yield return _updateWait;
            UpdateContextImmediate();
        }
        IEnumerator UpdateSubscriberAtEndOfFixedUpdate()
        {
            yield return _updateWait;
            if (Anchor != null)
            {
                Debug.Log("Updated subscriber " + transform.name);
                Anchor.UpdateSubscriberRigidbody(this.gameObject);
            }
        }
        private void UpdateContextImmediate()
        {

            if (Anchor != null)
            {
                HandleExitSplitterContext(Anchor);
            }
            if (this.enabled && AnchorStack.FirstOrDefault() != null)
            {
                HandleEnterSplitterContext(AnchorStack.FirstOrDefault());
            }
        }
        void FixedUpdate()
        {
            if (Anchor != null)
                StartCoroutine(UpdateSubscriberAtEndOfFixedUpdate());
        }
        //[ContextMenu("Set Anchor From Location - WARNING: BACKUP SCENE BEFORE USE")]
        //public void SetAnchorFromLocation()
        //{
        //    //This is bad as it could really mess up someone's scene
        //    Physics.Simulate(.00000001f);
        //    //Old failed attempt at accomplishing this the correct way:
        //    //var result = Physics.OverlapSphere(transform.position, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Collide);
        //    //var sanityAnchor = result.Where(x =>
        //    //        x.isTrigger
        //    //        && x.gameObject.GetComponentInParent<RigidContextAnchor>() != null
        //    //        && x.gameObject.GetComponentInParent<RigidContextAnchor>().enabled
        //    //).ToList();
        //    //Anchor = result.Where(x =>
        //    //        x.isTrigger
        //    //        && x.gameObject.GetComponentInParent<RigidContextAnchor>() != null
        //    //        && x.gameObject.GetComponentInParent<RigidContextAnchor>().enabled
        //    //        //not self collision
        //    //        && gameObject.GetComponentInParent<RigidContextAnchor>().gameObject.GetInstanceID() != gameObject.GetComponentInParent<RigidContextAnchor>().gameObject.GetInstanceID()
        //    //    )
        //    //    .OrderByDescending(x => x.gameObject.GetComponentInParent<RigidContextAnchor>().EntrancePriority)
        //    //    .Select(x => x.gameObject.GetComponentInParent<RigidContextAnchor>())
        //    //    .FirstOrDefault();
        //    //AnchorStack.Clear();
        //    //if (Anchor != null)
        //    //{
        //    //    AnchorStack.Add(Anchor);
        //    //}
        //}
        private void HandleEnterSplitterContext(SplitterAnchor anchor)
        {
            var result = anchor.RegisterInScene(this);

            Anchor = anchor;
        }

        private void HandleExitSplitterContext(bool leaveOnStack = false)
        {
            Anchor.UnregisterInScene(this);

            Anchor = null;
        }
        public bool Simulating()
        {
            return Anchor != null;
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessPotentialAnchorEntrance(collision.collider);
            //return;
            if (!Simulating())
                return;
            if (!InvolvedInMySimulation(collision.transform))
            {
                Anchor.ApplyCollision(this, collision);
            }
        }
        private void OnCollisionStay(Collision collision)
        {
            //return;
            if (!Simulating())
                return;
            if (!InvolvedInMySimulation(collision.transform))
            {
                Anchor.ApplyCollision(this, collision);
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            ProcessPotentialAnchorExit(collision.collider);
        }
        private bool InvolvedInMySimulation(Transform t)
        {
            //if no anchor or anchor is not mine
            //AND
            //no subscriber or subscriber not in my anchor
            //then not in my simulation
            if (
                (t.GetComponentInParent<SplitterAnchor>() == null
                    ||
                    t.GetComponentInParent<SplitterAnchor>().GetInstanceID()
                    != Anchor.GetInstanceID()
                ) &&
                (t.GetComponentInParent<SplitterSubscriber>() == null || t.GetComponentInParent<SplitterSubscriber>().Anchor == null || t.GetComponentInParent<SplitterSubscriber>().Anchor.GetInstanceID() != Anchor.GetInstanceID())
            )
                return false;
            else
                return true;
        }

        public Vector3 GetUltimatePointVelocity(Vector3 WorldPoint)
        {
            if (Anchor == null)
                return transform.GetComponent<Rigidbody>().GetPointVelocity(WorldPoint);

            if (Anchor.transform.GetComponent<SplitterSubscriber>() != null)
                return
                    Anchor.transform.TransformDirection(
                        Anchor.GetSim().transform.InverseTransformDirection(
                            Anchor.GetSubSim(this).transform.GetPointVelocity(
                                Anchor.GetSubSim(this).transform.TransformPoint(transform.InverseTransformPoint(WorldPoint))
                            )
                        )
                    )
                    +
                    Anchor.transform.GetComponent<SplitterSubscriber>().GetUltimatePointVelocity(WorldPoint);
            else
                return
                    Anchor.transform.TransformDirection(
                        Anchor.GetSim().transform.InverseTransformDirection(
                            Anchor.GetSubSim(this).transform.GetPointVelocity(
                                Anchor.GetSubSim(this).transform.TransformPoint(transform.InverseTransformPoint(WorldPoint))
                            )
                        )
                    )
                    +
                    Anchor.transform.GetPointVelocity(WorldPoint);
        }

        public Vector3 GetPointVelocity(Vector3 WorldPoint)
        {
            return Anchor.GetSubSim(this).GetComponent<Rigidbody>().GetPointVelocity(
                Anchor.GetSubSim(this).transform.TransformPoint(transform.InverseTransformPoint(WorldPoint))
            );
        }

        #region RigidInteractions
        public Vector3 velocity { 
            get {
                return transform.GetComponent<Rigidbody>().velocity;
            }
            set {
                if (!Simulating())
                {
                    transform.GetComponent<Rigidbody>().velocity = value;
                }
                else
                {
                    Anchor.ApplyVelocity(value, this);
                }
            }
        }
        public Vector3 position {
            get {
                return transform.GetComponent<Rigidbody>().position;
            }
            set {
                if (!Simulating())
                {
                    transform.GetComponent<Rigidbody>().position = value;
                }
                else
                {
                    Anchor.ApplyPosition(value, this);
                }
            }
        }

        public Quaternion rotation
        {
            get
            {
                if (!Simulating())
                {
                    Debug.Log("Getting from rigid");
                    return transform.GetComponent<Rigidbody>().rotation;
                }
                else
                    return Anchor.GetApplyRotation(this);
            }
            set
            {
                if (!Simulating())
                {
                    transform.GetComponent<Rigidbody>().rotation = value;
                }
                else
                {
                    Anchor.ApplyRotation(value, this);
                }
            }
        }

        //scalar?
        public float drag
        {
            get
            {
                return transform.GetComponent<Rigidbody>().drag;
            }
            set
            {
                if (!Simulating())
                {
                    transform.GetComponent<Rigidbody>().drag = value;
                }
                else
                {
                    transform.GetComponent<Rigidbody>().drag = value;
                    Anchor.ApplyDrag(value, this);
                }
            }
        }
        //scalar?
        public float angularDrag
        {
            get
            {
                return transform.GetComponent<Rigidbody>().angularDrag;
            }
            set
            {
                if (!Simulating())
                {
                    transform.GetComponent<Rigidbody>().angularDrag = value;
                }
                else
                {
                    transform.GetComponent<Rigidbody>().angularDrag = value;
                    Anchor.ApplyDrag(value, this);
                }
            }
        }
        public Vector3 angularVelocity
        {
            get
            {
                return transform.GetComponent<Rigidbody>().angularVelocity;
            }
            set
            {
                if (!Simulating())
                {
                    transform.GetComponent<Rigidbody>().angularVelocity = value;
                }
                else
                {
                    
                }
            }
        }

        //scalar?
        public bool useGravity
        {
            get
            {
                return transform.GetComponent<Rigidbody>().useGravity;
            }
            set
            {
                if (!Simulating())
                {
                    transform.GetComponent<Rigidbody>().useGravity = value;
                }
                else
                {
                    transform.GetComponent<Rigidbody>().useGravity = value;
                    Anchor.ApplyUseGravity(value, this);
                }
            }
        }
        public void AddForce(Vector3 force)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddForce(force);
            else
            {
                Anchor.ApplyAddForce(force, ForceMode.Force, this);
            }
        }

        public void AddForce(Vector3 force, ForceMode mode)
        {
            if (!Simulating())
            {
                transform.GetComponent<Rigidbody>().AddForce(force, mode);
            }
            else
            {
                Anchor.ApplyAddForce(force, mode, this);
            }
        }

        private Vector3 _forceNoMode;
        public void AddForce(float x, float y, float z)
        {
            _forceNoMode = new Vector3(x, y, z);
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddForce(x, y, z);
            else
            {
                Anchor.ApplyAddForce(_forceNoMode, ForceMode.Force, this);
            }
        }

        private Vector3 _forceWithMode;
        public void AddForce(float x, float y, float z, ForceMode mode)
        {
            _forceWithMode = new Vector3(x, y, z);
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddForce(x, y, z, mode);
            else
            {
                Anchor.ApplyAddForce(_forceWithMode, mode, this);
            }
        }

        public void AddRelativeForce(Vector3 force)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddRelativeForce(force);
            else
            {
                Anchor.ApplyAddRelativeForce(force, ForceMode.Force, this);
            }
        }

        public void AddRelativeForce(Vector3 force, ForceMode mode)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddRelativeForce(force, mode);
            else
            {
                Anchor.ApplyAddRelativeForce(force, mode, this);
            }
        }

        private Vector3 _relForceNoMode;
        public void AddRelativeForce(float x, float y, float z)
        {
            _relForceNoMode = new Vector3(x, y, z);
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddRelativeForce(x, y, z);
            else
            {
                Anchor.ApplyAddRelativeForce(_relForceNoMode, ForceMode.Force, this);
            }
        }

        private Vector3 _relForceWithMode;
        public void AddRelativeForce(float x, float y, float z, ForceMode mode)
        {
            _relForceWithMode = new Vector3(x, y, z);
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddRelativeForce(x, y, z, mode);
            else
            {
                Anchor.ApplyAddRelativeForce(_relForceWithMode, mode, this);
            }
        }

        public void AddTorque(Vector3 torque)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddTorque(torque);
            else
            {
                Anchor.ApplyAddTorque(torque, ForceMode.Force, this);
            }
        }

        public void AddTorque(Vector3 torque, ForceMode mode)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddTorque(torque, mode);
            else
            {
                Anchor.ApplyAddTorque(torque, mode, this);
            }
        }

        private Vector3 _torqueNoMode;
        public void AddTorque(float x, float y, float z)
        {
            _torqueNoMode = new Vector3(x, y, z);
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddTorque(x, y, z);
            else
            {
                Anchor.ApplyAddTorque(_torqueNoMode, ForceMode.Force, this);
            }
        }

        private Vector3 _torqueWithMode;
        public void AddTorque(float x, float y, float z, ForceMode mode)
        {
            _torqueWithMode = new Vector3(x, y, z);
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddTorque(x, y, z, mode);
            else
            {
                Anchor.ApplyAddTorque(_torqueWithMode, mode, this);
            }
        }

        public void AddRelativeTorque(Vector3 torque)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddRelativeTorque(torque);
            else
            {
                Anchor.ApplyAddRelativeTorque(torque, ForceMode.Force, this);
            }
        }

        public void AddRelativeTorque(Vector3 torque, ForceMode mode)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddRelativeTorque(torque, mode);
            else
            {
                Anchor.ApplyAddRelativeTorque(torque, mode, this);
            }
        }

        private Vector3 _relTorqueNoMode;
        public void AddRelativeTorque(float x, float y, float z)
        {
            _relTorqueNoMode = new Vector3(x, y, z);
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddRelativeTorque(x, y, z);
            else
            {
                Anchor.ApplyAddRelativeTorque(_relTorqueNoMode, ForceMode.Force, this);
            }
        }

        private Vector3 _relTorqueWithMode;
        public void AddRelativeTorque(float x, float y, float z, ForceMode mode)
        {
            _relTorqueWithMode = new Vector3(x, y, z);
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddRelativeTorque(x, y, z, mode);
            else
            {
                Anchor.ApplyAddRelativeTorque(_torqueWithMode, mode, this);
            }
        }

        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
            else
                Anchor.ApplyAddExplosionForce(explosionForce, explosionPosition, explosionRadius, 0f, ForceMode.Force, this);
        }

        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
            else
                Anchor.ApplyAddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, ForceMode.Force, this);
        }
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier, ForceMode mode)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
            else
                Anchor.ApplyAddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, mode, this);
        }

        public void MovePosition(Vector3 position)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().MovePosition(position);
            else
                Anchor.ApplyMovePosition(position, this);
        }

        public void MoveRotation(Quaternion rotation)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().MoveRotation(rotation);
            else
                Anchor.ApplyMoveRotation(rotation, this);
        }

        public void AddForceAtPosition(Vector3 force, Vector3 position)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddForceAtPosition(force, position);
            else
                Anchor.ApplyAddForceAtPosition(force, position, ForceMode.Force, this);
        }
        public void AddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode)
        {
            if (!Simulating())
                transform.GetComponent<Rigidbody>().AddForceAtPosition(force, position, mode);
            else
                Anchor.ApplyAddForceAtPosition(force, position, mode, this);
        }
        /*
        transform.GetComponent<Rigidbody>().velocity += new Vector3();
        transform.GetComponent<Rigidbody>().velocity = new Vector3();

        transform.GetComponent<Rigidbody>().position = new Vector3();
        transform.GetComponent<Rigidbody>().position += new Vector3();

        Move Position ?
        Move Rotation ?

        transform.GetComponent<Rigidbody>().AddForce(new Vector3());
        transform.GetComponent<Rigidbody>().AddForce(new Vector3(), ForceMode.Impulse);
        transform.GetComponent<Rigidbody>().AddForce(0f, 0f, 0f);
        transform.GetComponent<Rigidbody>().AddForce(0f, 0f, 0f, ForceMode.Force);

        transform.GetComponent<Rigidbody>().AddRelativeForce(new Vector3());
        transform.GetComponent<Rigidbody>().AddRelativeForce(new Vector3(), ForceMode.Force);
        transform.GetComponent<Rigidbody>().AddRelativeForce(0f, 0f, 0f);
        transform.GetComponent<Rigidbody>().AddRelativeForce(0f, 0f, 0f, ForceMode.Force);

        transform.GetComponent<Rigidbody>().AddTorque(new Vector3());
        transform.GetComponent<Rigidbody>().AddTorque(new Vector3(), ForceMode.Impulse);
        transform.GetComponent<Rigidbody>().AddTorque(0f, 0f, 0f);
        transform.GetComponent<Rigidbody>().AddTorque(0f, 0f, 0f, ForceMode.Force);

        transform.GetComponent<Rigidbody>().AddRelativeTorque(new Vector3());
        transform.GetComponent<Rigidbody>().AddRelativeTorque(new Vector3(), ForceMode.Force);
        transform.GetComponent<Rigidbody>().AddRelativeTorque(0f, 0f, 0f);
        transform.GetComponent<Rigidbody>().AddRelativeTorque(0f, 0f, 0f, ForceMode.Force);

        transform.GetComponent<Rigidbody>().AddExplosionForce(0f, new Vector3(), 0f);
        transform.GetComponent<Rigidbody>().AddExplosionForce(0f, new Vector3(), 0f, 0f);*/
        //transform.GetComponent<Rigidbody>(). .AddExplosionForce(0f, new Vector3(), 0f, 0f, ForceMode.Force);

        
        #endregion



    }

    internal static class SplitterExtensions{
        public static Vector3 GetPointVelocity(this Transform t, Vector3 point)
        {
            if (t.GetComponent<Rigidbody>() != null)
                return t.GetComponent<Rigidbody>().GetPointVelocity(point);
            else
                return Vector3.zero;
        }
    }
}
