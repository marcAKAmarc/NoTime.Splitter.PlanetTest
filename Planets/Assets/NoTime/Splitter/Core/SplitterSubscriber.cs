using NoTime.Splitter.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace NoTime.Splitter
{

    public delegate void PreSimulationInstantiation();
    public delegate void PostSimulationInstantiation();

#if UNITY_2021_2_OR_NEWER
    [Icon("Assets/NoTime/Splitter/Core/Icons/EditorIcon.png")]
#endif


    [RequireComponent(typeof(Rigidbody))]
    public class SplitterSubscriber : MonoBehaviour
    {
        [Tooltip("Scripts that need to execute physics operations relative to the anchor, i.e. character controllers")]
        public List<MonoBehaviour> RunInSimulatedSpace;

        [HideInInspector]
        public SplitterAnchor Anchor;

        [HideInInspector]
        public PreSimulationInstantiation PreSimulationInstantiation;
        [HideInInspector]
        public PostSimulationInstantiation PostSimulationInstantiation;

        private List<SplitterAnchor> AnchorStack;
        private SplitterAnchor ManuallyEnteredAnchor;
        private List<AnchorTrigger> CurrentAnchorTriggers;

        public AppliedPhysics AppliedPhysics;

        [HideInInspector]
        public Rigidbody Body;

        [HideInInspector]
        public bool OverrideAnchorSettings = false;
        [HideInInspector]
        public bool SyncTransform = false;
        [HideInInspector]
        public bool SyncChildTransforms = false;



        private SplitterAnchor _awakeAnchor;

        private struct AnchorTrigger
        {
            public Collider Collider;
            public SplitterAnchor Anchor;
            public bool isStayTrigger;
            public bool isEntranceTrigger;
        }

        private void Awake()
        {
            AppliedPhysics = new AppliedPhysics(this, transform.GetComponent<Rigidbody>());
            if (AnchorStack == null)
                AnchorStack = new List<SplitterAnchor>();
            if (CurrentAnchorTriggers == null)
                CurrentAnchorTriggers = new List<AnchorTrigger>();

            Body = transform.GetComponent<Rigidbody>();

            notifyAttachedAnchor(true);

        }
        void Start()
        {
            if (NeedToUpdateContext())
                CheckAndExecuteContextUpdate();
        }
        public Rigidbody GetSimulationBody()
        {
            if (Anchor == null)
                return null;
            else
                return Anchor.GetSimulationBody(this);
        }

        private int istIndex;
        private bool IsStayTrigger(SplitterAnchor _otherAnchor, Collider other)
        {
            istIndex = 0;
            for (istIndex = 0; istIndex < _otherAnchor.StayTriggers.Count; istIndex += 1)
                if (_otherAnchor.StayTriggers[istIndex].GetInstanceID() == other.GetInstanceID())//.Any(x => x.GetInstanceID() == other.GetInstanceID());
                    return true;
            return false;
        }

        private int ietIndex;
        private bool IsEntranceTrigger(SplitterAnchor _otherAnchor, Collider other)
        {
            ietIndex = 0;
            for (ietIndex = 0; ietIndex < _otherAnchor.EntranceTriggers.Count; ietIndex += 1)
                if (_otherAnchor.EntranceTriggers[ietIndex].GetInstanceID() == other.GetInstanceID())//.Any(x => x.GetInstanceID() == other.GetInstanceID());
                    return true;
            return false;
        }

        SplitterAnchor _otherAnchor;
        SplitterAnchor _attachedAnchor;
        private void ProcessPotentialAnchorEntrance(Collider other)
        {
            
            _otherAnchor = other.gameObject.GetComponentInParent<SplitterAnchor>();
            if (_otherAnchor == null)
                return;

            _attachedAnchor = gameObject.GetComponent<SplitterAnchor>();

            if (_otherAnchor != null
                &&
                _otherAnchor.enabled
                && !(
                    _attachedAnchor != null
                    &&
                    _attachedAnchor.enabled
                    &&
                    _attachedAnchor.EntrancePriority <= _otherAnchor.EntrancePriority
                )
            )
            {
                bool isStayTrigger = IsStayTrigger(_otherAnchor, other);
                bool isEntranceTrigger = IsEntranceTrigger(_otherAnchor, other);

                if (isStayTrigger || isEntranceTrigger) {
                    AddToTriggerStack(
                        new AnchorTrigger { 
                            Anchor = _otherAnchor, 
                            Collider = other, 
                            isEntranceTrigger = isEntranceTrigger, 
                            isStayTrigger = isStayTrigger 
                        }
                    );

                    //if entrance and stay in triggerstack,
                    //add to anchor stack
                    if ( 
                        TriggerStackHasStayAndEntrance(_otherAnchor)
                    )
                    {
                        AddToAnchorStack(
                            _otherAnchor
                        );
                    }
                }
            }

            if (NeedToUpdateContext())
            {
                UpdateContext();
            }
        }

        private int _triggerScanIndex;
        private bool _triggerScanStay;
        private bool _triggerScanEntrance;
        private bool TriggerStackHasStayAndEntrance(SplitterAnchor anchor)
        {
            _triggerScanEntrance = false;
            _triggerScanStay = false;
            _triggerScanIndex = 0;
            for(; _triggerScanIndex < CurrentAnchorTriggers.Count; _triggerScanIndex++)
            {
                if (
                    CurrentAnchorTriggers[_triggerScanIndex].Anchor.gameObject == anchor.gameObject
                )
                {
                    _triggerScanEntrance = _triggerScanEntrance ||
                        CurrentAnchorTriggers[_triggerScanIndex].isEntranceTrigger;

                    _triggerScanStay = _triggerScanStay ||
                        CurrentAnchorTriggers[_triggerScanIndex].isStayTrigger;

                    if (_triggerScanEntrance && _triggerScanStay)
                        return true;
                }
            }

            return false;
        }

        private void OnDisable()
        {
            notifyAttachedAnchor(false);
            if (!_quitting && gameObject.scene.isLoaded)
            {
                if (Anchor != null)
                    HandleExitSplitterContext();
            }
            //SplitterSystem.InvestigatoryEvents -= Investigate;
        }

        private void OnEnable()
        {
            notifyAttachedAnchor(true);
            flickerColliders();
            UpdateContext();
            //SplitterSystem.InvestigatoryEvents += Investigate;
        }
        private void Investigate(string eventName)
        {
            if (transform.name.Contains("TEST"))
            {
                Debug.Log(eventName + " finished.");
                Debug.Log("    position = " + transform.GetComponent<Rigidbody>().position);
            }
        }

        private void ProcessPotentialAnchorExit(Collider other)
        {
            RemoveFromTriggerStack(other);
            CleanStacks();
            FilterAnchorStackByTriggerStack();
        }
        private void OnTriggerEnter(Collider other)
        {
            ProcessPotentialAnchorEntrance(other);
        }
        private void OnTriggerExit(Collider other)
        {
            ProcessPotentialAnchorExit(other);
        }
        private void FilterAnchorStackByTriggerStack()
        {

            for (_i = 0; _i < AnchorStack.Count; _i++)
            {
                if(
                    !(
                        //keep current anchor
                       (Anchor != null && AnchorStack[_i].gameObject == Anchor.gameObject)
                       ||
                       //keep if we have triggers for this anchor
                       //TODO: shouldn't this be specific to stay anchors?
                       AnchorInCurrentAnchorTriggers(AnchorStack[_i])
                    )
                )
                {
                    AnchorStack.RemoveAt(_i);
                    _i -= 1;
                }
            }
            /*AnchorStack = AnchorStack.Where(x =>
                //keep current anchor
                (Anchor != null && x.gameObject.GetInstanceID() == Anchor.gameObject.GetInstanceID())
                ||
                //keep if we have triggers for this anchor
                //TODO:  shouldn't this be specific to stay anchors?
                AnchorInCurrentAnchorTriggers(x)
            ).ToList();*/
        }
        private int _aicatI;
        private bool AnchorInCurrentAnchorTriggers(SplitterAnchor anchor)
        {
            for(_aicatI = 0; _aicatI < CurrentAnchorTriggers.Count; _aicatI++)
            {
                if (CurrentAnchorTriggers[_aicatI].Anchor == anchor)
                    return true;
            }
            return false;
        }

        private int _cicatI;
        private bool ColliderInCurrentAnchorTriggers(Collider collider)
        {
            //replacing this code:
            //CurrentAnchorTriggers.Any(x => x.Collider.GetInstanceID() == anchorTrigger.Collider.GetInstanceID())
            for (_cicatI = 0; _cicatI < CurrentAnchorTriggers.Count; _cicatI++)
            {
                if (CurrentAnchorTriggers[_cicatI].Collider == collider)
                    return true;
            }
            return false;
        }

        public void SimulationExitedAnchor(SplitterAnchor Anchor)
        {
            RemoveFromAnchorStack(Anchor);
            if (NeedToUpdateContext())
                UpdateContext();
        }

        private void AddToAnchorStack(SplitterAnchor anchor)
        {  
            if (AnchorStackHasAnchor(anchor))
                return;
            
            AnchorStack.Add(anchor);
            AnchorStack = AnchorStack.OrderByDescending(x => x.EntrancePriority).ToList();
        }
        private int _ashaI;
        private bool AnchorStackHasAnchor(SplitterAnchor anchor)
        {
            //replacing this code:
            /*if (AnchorStack.Any(x => x != null && x.gameObject != null && x.gameObject.GetInstanceID() == anchor.gameObject.GetInstanceID()))
                return;*/
            //first, make sure this isn't already in the stack
            //things can be null here if an anchor was deleted so we have to check
            //make sure it isn't already in anchor stack
            for (_ashaI = 0; _ashaI < AnchorStack.Count; _ashaI++)
            {
                if (AnchorStack[_ashaI] == anchor)
                    return true;
            }
            return false;
        }
        private void RemoveFromAnchorStack(SplitterAnchor anchor)
        {
            AnchorStack = AnchorStack.Where(x => x != null && x.gameObject != null && x.gameObject.GetInstanceID() != anchor.gameObject.GetInstanceID()).ToList();
        }

        private void AddToTriggerStack(AnchorTrigger anchorTrigger)
        {
            //make sure it is not currently in the trigger stack
            if (ColliderInCurrentAnchorTriggers(anchorTrigger.Collider))
                return;
            CurrentAnchorTriggers.Add(anchorTrigger);
        }

        int _i;
        private void RemoveFromTriggerStack(Collider collider)
        {
            for(_i = 0; _i < CurrentAnchorTriggers.Count; _i++)
            {
                if (CurrentAnchorTriggers[_i].Collider == collider)
                {
                    CurrentAnchorTriggers.RemoveAt(_i);
                    _i -= 1;
                }
            }
            //CurrentAnchorTriggers = CurrentAnchorTriggers.Where(x => x.Collider.GetInstanceID() != collider.GetInstanceID()).ToList();
        }
        

        private bool NeedToUpdateContext()
        {
            //in a manually entered anchor
            if (ManuallyEnteredAnchor != null && ManuallyEnteredAnchor.GetInstanceID() == Anchor.GetInstanceID())
                return false;

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
        private IEnumerator UpdateContextAtEndOfFixedUpdate()
        {
            yield return _updateWait;
            CheckAndExecuteContextUpdate();
        }

        private void CheckAndExecuteContextUpdate()
        {
            //check if we would just re enter this context.  bail if so.
            if (!NeedToUpdateContext())
                return;
            
            if (Anchor != null)
            {
                HandleExitSplitterContext();
            }
            if (this.enabled && AnchorStack.FirstOrDefault() != null)
            {
                HandleEnterSplitterContext(AnchorStack.FirstOrDefault());
            }
        }

        public void HandleAnchorDestruction(SplitterAnchor anchor)
        {   
            foreach (Collider col in anchor.StayTriggers.Union(anchor.EntranceTriggers))
                RemoveFromTriggerStack(col);

            RemoveFromAnchorStack(anchor);

            if(Anchor == anchor)
                Anchor = null;
            CleanStacks();

        }
        private void RemoveFromStacks(SplitterAnchor anchor)
        {
            RemoveFromAnchorStack(anchor);
            foreach (Collider collider in anchor.StayTriggers)
                RemoveFromTriggerStack(collider);
        }
        private void CleanStacks()
        {
            for(_i = 0; _i < CurrentAnchorTriggers.Count; _i++)
            {
                if(
                    CurrentAnchorTriggers[_i].Collider == null
                    || CurrentAnchorTriggers[_i].Collider.gameObject == null
                    || CurrentAnchorTriggers[_i].Anchor == null
                )
                {
                    CurrentAnchorTriggers.RemoveAt(_i);
                    _i -= 1;
                }
            }
            //CurrentAnchorTriggers = CurrentAnchorTriggers.Where(x => x.Collider != null && x.Collider.gameObject != null && x.Anchor != null).ToList();
            
            for(_i = 0; _i < AnchorStack.Count; _i++)
            {
                if(
                    AnchorStack[_i] == null 
                    || AnchorStack[_i].gameObject == null
                )
                {
                    AnchorStack.RemoveAt(_i);
                    _i -= 1;
                }
            }
            //AnchorStack = AnchorStack.Where(x => x != null && x.gameObject != null).ToList();
        }

        public void ManuallyEnterAnchor(SplitterAnchor anchor)
        {

            if (ManuallyEnteredAnchor != null && ManuallyEnteredAnchor.GetInstanceID() == anchor.GetInstanceID())
                return;

            if (ManuallyEnteredAnchor != null)
                ManuallyExitAnchor();

            if (Anchor != null)
                HandleExitSplitterContext();

            HandleEnterSplitterContext(anchor);

            ManuallyEnteredAnchor = anchor;
        }

        public void ManuallyExitAnchor()
        {
            if (ManuallyEnteredAnchor == null)
                return;

            HandleExitSplitterContext();
            ManuallyEnteredAnchor = null;
        }

        private void HandleEnterSplitterContext(SplitterAnchor anchor)
        {
            Anchor = anchor;

            Anchor.RegisterInScene(this);
        }

        private void HandleExitSplitterContext()
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

            if (!Simulating())
                return;
            if (!InvolvedInMySimulation(collision))
            {
                Anchor.ApplyCollision(this, collision);
            }
        }
        private void OnCollisionStay(Collision collision)
        {
            if (!Simulating())
                return;
            if (!InvolvedInMySimulation(collision))
            {
                Anchor.ApplyCollision(this, collision);
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            ProcessPotentialAnchorExit(collision.collider);
        }

        bool _quitting;
        private void OnApplicationQuit()
        {
            _quitting = true;
        }
        private void OnDestroy()
        {
            if (Anchor != null && !_quitting && gameObject.scene.isLoaded)
                HandleExitSplitterContext();

            notifyAttachedAnchor(false);
        }

        SplitterAnchor _invInSim_FoundAnchor;
        SplitterSubscriber _invInSim_FoundSub;
        private bool InvolvedInMySimulation(Collision t)
        {


            //if no anchor or anchor is not mine
            //AND
            //no subscriber or subscriber not in my anchor
            //then not in my simulation

            /*if (
                (t.transform.GetComponentInParent<SplitterAnchor>() == null
                    ||
                    t.transform.GetComponentInParent<SplitterAnchor>().GetInstanceID()
                    != Anchor.GetInstanceID()
                ) &&
                (t.transform.GetComponentInParent<SplitterSubscriber>() == null || t.transform.GetComponentInParent<SplitterSubscriber>().Anchor == null || t.transform.GetComponentInParent<SplitterSubscriber>().Anchor.GetInstanceID() != Anchor.GetInstanceID())
            )
                return false;
            else
                return true;*/

            //same logic, just optimized:

            _invInSim_FoundSub = null;
            //as long as we know that subscribers MUST have a rigidbody,
            //we can assume that this transform has the subscriber and not reach to parents
            if(t.rigidbody != null)
                _invInSim_FoundSub = t.rigidbody.GetComponent<SplitterSubscriber>();

            if (_invInSim_FoundSub != null
                && _invInSim_FoundSub.Anchor != null
                && _invInSim_FoundSub.Anchor == Anchor
            )
                return true;

            _invInSim_FoundAnchor = null;
            //not every anchor has a rigidbody, and anchors can have colliders that are 
            //are children, so we need to do getcomponent in parent
            //we also can not start this check from SplitterAnchor.Stay(Entrance)Colliders (which would be
            //faster) because not every collider is listed there.
            _invInSim_FoundAnchor = t.transform.GetComponentInParent<SplitterAnchor>();

            if (_invInSim_FoundAnchor != null
                && _invInSim_FoundAnchor == Anchor)
                return true;

            return false;
        }

        List<Collider> _cols;
        int _iFEAE;
        private void flickerColliders()
        {
            _cols = transform.GetComponentsInChildren<Collider>().Where(x => x.isTrigger == false).ToList();
            _iFEAE = 0;
            for (; _iFEAE < _cols.Count(); _iFEAE++)
            {
                _cols[_iFEAE].enabled = false;
                _cols[_iFEAE].enabled = true;
            }
        }

        private SplitterAnchor _naaAnchor;
        private void notifyAttachedAnchor(bool exists)
        {
            _naaAnchor = transform.GetComponent<SplitterAnchor>();
            if (_naaAnchor == null)
                return;
            if (exists)
                _naaAnchor.setMySubscriber(this);
            else
                _naaAnchor.setMySubscriber(null);
        }
    }
}
