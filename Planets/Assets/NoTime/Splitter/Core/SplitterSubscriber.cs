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

        [HideInInspector]
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
            if(isActiveAndEnabled)
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
                            Anchor.GetSubSim(this).rigidbody.GetPointVelocity(
                                Anchor.GetSubSim(this).gameObject.transform.TransformPoint(transform.InverseTransformPoint(WorldPoint))
                            )
                        )
                    )
                    +
                    Anchor.transform.GetComponent<SplitterSubscriber>().GetUltimatePointVelocity(WorldPoint);
            else
                return
                    Anchor.transform.TransformDirection(
                        Anchor.GetSim().transform.InverseTransformDirection(
                            Anchor.GetSubSim(this).rigidbody.GetPointVelocity(
                                Anchor.GetSubSim(this).gameObject.transform.TransformPoint(transform.InverseTransformPoint(WorldPoint))
                            )
                        )
                    )
                    +
                    Anchor.GetPointVelocity(WorldPoint);
        }
    }
}
