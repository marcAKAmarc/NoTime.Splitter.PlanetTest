using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using NoTime.Splitter.Core;
using NoTime.Splitter.Core.Internal;
using Unity.VisualScripting;

namespace NoTime.Splitter
{
#if UNITY_2021_2_OR_NEWER
    [Icon("Assets/NoTime/Splitter/Core/Icons/EditorIcon.png")]
#endif
    public class SplitterAnchor : MonoBehaviour
    {
        [Tooltip("For example, ships have higher priority than planets.")]
        public float EntrancePriority;
        [Tooltip("Any subscribers that enter any of the entrance triggers will be entered into the simulation in this anchor.")]
        public List<Collider> EntranceTriggers = new List<Collider>();
        [Tooltip("Any subscribers that are not in any of the exit triggers will be exit the simulation of this anchor.")]
        public List<Collider> StayTriggers = new List<Collider>();

        public List<MonoBehaviour> RunInSimulationSpace = new List<MonoBehaviour>();

        [HideInInspector]
        public bool SyncSubscriberTransforms = false;
        [HideInInspector]
        public bool SyncSubscriberChildTransforms = false;

        [Tooltip("Render simulated anchor and subscribers at world position: (100, 100, 100).")]
        [HideInInspector] //...since this now has a custom editor
        public bool SimulationVisible;
        
        [Tooltip("Setting SyncTransforms to true will increase physics query accuracy at the cost of computation time.")]
        [HideInInspector]
        private bool CallPhysicsSync = false;

        [HideInInspector]
        [DoNotSerialize]
        public Scene? Scene;
        private PhysicsScene PhysicsScene;
        private Scene MainScene;
        private string SceneName;
        private GameObject PhysicsAnchorGO;
        private SplitterAnchorSimulation PhysicsAnchor;
        private Rigidbody Body = null;
        private List<GameObject> subscribers;
        private Dictionary<int, int> ids;
        private Dictionary<int, GoRigid> idToPhysicsGo;
        private Dictionary<int, GoRigid> idToMainGo;
        private Dictionary<int, List<MatchedTransform>> PhysicsGoIdToLocalSyncs;

        private bool deleted = false;

        private SplitterSubscriber mySubscriber;

        private class MatchedTransform
        {
            public Transform mainTransform;
            public Transform physicsTransform;
        }

        private struct MovePositionCacheItem
        {
            public SplitterSubscriber subscriber;
            public Vector3 positionValue;
        }
        private struct MoveRotationCacheItem
        {
            public SplitterSubscriber subscriber;
            public Vector3 rotationValue;
        }

        private void Awake()
        {
            if (subscribers == null)
                subscribers = new List<GameObject>();
            if (ids == null)
                ids = new Dictionary<int, int>();
            if (idToPhysicsGo == null)
                idToPhysicsGo = new Dictionary<int, GoRigid>();
            if (idToMainGo == null)
                idToMainGo = new Dictionary<int, GoRigid>();
            if (PhysicsGoIdToLocalSyncs == null)
                PhysicsGoIdToLocalSyncs = new Dictionary<int, List<MatchedTransform>>();

            Body = transform.GetComponent<Rigidbody>();
            mySubscriber = transform.GetComponent<SplitterSubscriber>();
        }

        
        void Start()
        {
            if (Scene == null)
                CreateAnchorSimulationScene();
        }

        private SplitterSubscriber GetMySubscriber()
        {
            return mySubscriber;
        } 
        internal Transform GetAnchorSimulation()
        {
            if (PhysicsAnchorGO == null)
                return null;
            else if (PhysicsAnchorGO.GetComponent<SplitterAnchorSimulation>() == null)
                return null;
            else
                return PhysicsAnchorGO.GetComponent<SplitterAnchorSimulation>().transform;
        }


        private string createSceneName(string goName)
        {
            return goName + "_SplitterScene_" + Statics.GetSceneCounter().ToString();
        }
        private void CreateAnchorSimulationScene()
        {
            if (deleted)
                return;

            MainScene = SceneManager.GetActiveScene();
            Scene = SceneManager.CreateScene(
                createSceneName(gameObject.name), 
                new CreateSceneParameters(LocalPhysicsMode.Physics3D)
            );
            PhysicsScene = Scene.Value.GetPhysicsScene();

            SceneManager.SetActiveScene(Scene.Value);
            PhysicsAnchorGO = Instantiate(
                gameObject, 
                Vector3.one * 100f, 
                transform.localRotation
            );
            SceneManager.SetActiveScene(MainScene);

            PhysicsAnchorGO.transform.localScale = transform.lossyScale;
            if (PhysicsAnchorGO.transform.GetComponent<Rigidbody>() != null)
            {
                PhysicsAnchorGO.transform.GetComponent<Rigidbody>().mass = 999999999f;
                PhysicsAnchorGO.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            }

            PhysicsAnchorGO.name = PhysicsAnchorGO.name + "-Physics";
            
            StripGameObjectAsAnchor(PhysicsAnchorGO);
            
            //add anchor simulation
            PhysicsAnchorGO.AddComponent<SplitterAnchorSimulation>();
            PhysicsAnchor = PhysicsAnchorGO.GetComponent<SplitterAnchorSimulation>();
            PhysicsAnchorGO.GetComponent<SplitterAnchorSimulation>().Anchor = this;
            PhysicsAnchorGO.GetComponent<SplitterAnchorSimulation>().DeactivateTriggerColliders =
                PhysicsAnchorGO.GetComponent<SplitterAnchor>().StayTriggers;

            SetupLocalTransformSyncCache(transform, PhysicsAnchorGO, PhysicsAnchorGO);
            
            //unity messages
            foreach (var gobj in this.transform.GetComponentsInChildren<Transform>().Select(x => x.gameObject))
            {
                gobj.SendMessage("OnSimulationStart", new SplitterEvent { Anchor = this, SimulatedSubscriber = null, Subscriber = null, SimulatedAnchor = PhysicsAnchorGO.transform }, SendMessageOptions.DontRequireReceiver);
            }
        }
        public GameObject RegisterInScene(SplitterSubscriber subscriber)
        {
            //bail case for anchors entering other anchors
            //other has anchor
            //and
            //entrance priority is less than mine
            //or
            //entrance priority is equal
            //and
            //other instance id > my id
            if (
                subscriber.transform.GetComponent<SplitterAnchor>() != null
                &&
                (
                    subscriber.transform.GetComponent<SplitterAnchor>().EntrancePriority < EntrancePriority
                    ||
                    (
                        subscriber.transform.GetComponent<SplitterAnchor>().EntrancePriority == EntrancePriority
                        &&
                        subscriber.gameObject.GetInstanceID() > this.gameObject.GetInstanceID()
                    )
                )
            )
                return null;

            //if this id is already in id's, then bail
            if (ids.ContainsKey(subscriber.gameObject.GetInstanceID()))
                return null;

            //make sure we have scene already.  if this was set in editor, we may not have a scene yet.
            if (Scene == null)
                CreateAnchorSimulationScene();

            //execute subscriber's PreSimulationInstantiation
            if(subscriber.PreSimulationInstantiation != null)
                subscriber.PreSimulationInstantiation();

            //create the new sim object
            SceneManager.SetActiveScene(Scene.Value);

            var newGo = Instantiate(
                subscriber.gameObject,
                PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(subscriber.transform.GetComponent<Rigidbody>().position)),
                PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(transform.rotation) * subscriber.transform.GetComponent<Rigidbody>().rotation)
            );
            SceneManager.SetActiveScene(MainScene);

            ////start book keeping
            newGo.name = newGo.name + "-Physics";

            subscribers.Add(subscriber.gameObject);
            ids.Add(subscriber.gameObject.GetInstanceID(), newGo.GetInstanceID());
            idToPhysicsGo.Add(subscriber.gameObject.GetInstanceID(), new GoRigid()
            {
                gameObject = newGo.gameObject,
                rigidbody = newGo.transform.GetComponent<Rigidbody>(),
                subscriber = subscriber
            });
            idToMainGo.Add(
                newGo.GetInstanceID(),
                new GoRigid()
                {
                    gameObject = subscriber.gameObject,
                    rigidbody = subscriber.transform.GetComponent<Rigidbody>(),
                    subscriber = subscriber
                }
            );
            ////end bookkeeping

            //execute subsrcirber's PostSimulationInstantiation
            if (subscriber.PostSimulationInstantiation != null)
                subscriber.PostSimulationInstantiation();

            //should I just write in to check if original subscriber's kinematic is false
            //and set it to true?  NetworkRigidbody is a pain.

            newGo.transform.localScale = subscriber.transform.lossyScale;


            //update subscriber properties
            //velocity: do not update
            //angularvelocity: do not update
            var subRigid = subscriber.GetComponent<Rigidbody>();
            subRigid.drag = 0f;
            subRigid.angularDrag = 0f;
            //mass: i have previously experimented with updated mass -
            //do not update as it thorougly breaks cross-scene interaction
            subRigid.useGravity = false;
            //max depenetration velocity: do not update
            if (newGo.GetComponent<Rigidbody>().isKinematic)
                subscriber.GetComponent<Rigidbody>().isKinematic = false;
            subRigid.freezeRotation = false;
            subRigid.constraints = RigidbodyConstraints.None;
            //collision detection mode: do not update
            //center of mass: do not update
            //world center of mass: do not update
            //inertiaTensorRotation: do not update
            //inertiaTensor: do not update
            //position: no gets immediately updated
            //rotation: no gets immediately updated
            //interpolation: do not update - this get's immediately update
            //solverIterations: do not update
            //sleepThreshold: do not update
            subRigid.maxAngularVelocity = float.PositiveInfinity;
            //solverVelocity Iterations:  do not update
            //solverIterationCount: do not update
            

            if (!newGo.GetComponent<Rigidbody>().isKinematic)
            {
                //velocity
                newGo.GetComponent<Rigidbody>().velocity =

                    PhysicsAnchorGO.transform.TransformDirection(
                        transform.InverseTransformDirection(
                            (
                                subscriber.GetComponent<Rigidbody>().velocity
                            )
                        )
                    )

                    - PhysicsAnchorGO.transform.TransformDirection(
                        transform.InverseTransformDirection(
                            this.GetUltimatePointVelocity(
                                subscriber.GetComponent<Rigidbody>().position
                            )
                        )
                    );


                //angular velocity
                newGo.GetComponent<Rigidbody>().angularVelocity =
                    PhysicsAnchorGO.transform.TransformDirection(
                        this.transform.InverseTransformDirection(
                            subscriber.GetComponent<Rigidbody>().angularVelocity
                        )
                    );
            }

            
            SetupLocalTransformSyncCache(subscriber.transform, newGo, newGo);

            StripGameObjectAsSubscriber(newGo, subscriber);

            newGo.AddComponent<SplitterSubscriberSimulated>();
            newGo.GetComponent<SplitterSubscriberSimulated>().Authentic = subscriber;
            newGo.GetComponent<SplitterSubscriberSimulated>().Anchor = this;

            newGo.GetComponent<SplitterSubscriber>().enabled = false;
            Destroy(newGo.GetComponent<SplitterSubscriber>());




            //send UnityMessagesHere
            foreach (var gobj in subscriber.transform.GetComponentsInChildren<Transform>().Select(x => x.gameObject))
            {
                gobj.SendMessage("OnEnterAnchor", new SplitterEvent { Anchor = this, SimulatedSubscriber = newGo.transform, Subscriber = subscriber, SimulatedAnchor = PhysicsAnchorGO.transform }, SendMessageOptions.DontRequireReceiver);
            }

            

            return newGo;
        }

        private void StripGameObjectAsSubscriber(GameObject newGo, SplitterSubscriber settings)
        {
            //disable and delete all behaviour not in RunInSimulatedSpace
            foreach (var behaviour in newGo.GetComponentsInChildren<Behaviour>().Where(x =>
                !newGo.GetComponent<SplitterSubscriber>().RunInSimulatedSpace.Any(y =>
                    y.GetInstanceID() == x.GetInstanceID()
                ) &&
                !(x.GetType() == typeof(SplitterSubscriber))
            ))
            {
                behaviour.enabled = false;
                Destroy(behaviour);
            }
            if (settings == null)
                Debug.Log("Anchor has null settings.  Name: " + gameObject.name);
            else if(settings.RunInSimulatedSpace == null)
            {
                Debug.Log("Anchor has null runInSimulatedSpace. Anchor name:" + gameObject.name + "; settings " + settings.gameObject.name);
            }
            //disable all behaviours in RunInSimulatedSpace
            foreach (var behaviour in settings.RunInSimulatedSpace)
            {
                behaviour.enabled = false;
            }

            //visibility
            if (!SimulationVisible)
            {
                foreach (var renderer in newGo.GetComponentsInChildren<Renderer>().ToList())
                {
                    renderer.enabled = false;
                }
            }

            //do not have new anchors in simulation
            foreach (var anchor in newGo.GetComponentsInChildren<SplitterAnchor>().ToList())
            {
                anchor.enabled = false;
                Destroy(anchor);
            }

            //do not have joints in simulation
            foreach (var joint in newGo.GetComponentsInChildren<Joint>())
            {
                Destroy(joint);
            }
        }

        private void StripGameObjectAsAnchor(GameObject newGo)
        {
            //disable and delete all monobehaviors not in RunInSimulatedSpace
            foreach (var behaviour in newGo.GetComponentsInChildren<Behaviour>().Where(x =>
                !(x.GetType() == typeof(SplitterAnchor))
                &&
                !this.RunInSimulationSpace.Any(
                    y =>
                    y.GetInstanceID() == x.GetInstanceID()
                )
            ))
            {
                //behaviour.enabled = false;
                Destroy(behaviour);
            }
            //disable all behaviours in RunInSimulatedSpace
            foreach (var behaviour in this.RunInSimulationSpace)
            {
                behaviour.enabled = false;
            }

            //visibility
            if (!this.SimulationVisible)
            {
                foreach (var renderer in newGo.GetComponentsInChildren<Renderer>().ToList())
                {
                    renderer.enabled = false;
                }
            }

            //do not have subscription or anchors in simulation
            if (newGo.GetComponent<SplitterAnchor>()!= null && newGo.GetComponent<SplitterAnchor>().Scene != null)
            {
                Debug.LogError("newGo Scene value on Destroy: " + newGo.GetComponent<SplitterAnchor>().Scene.ToString());
                throw new UnityException("Simulated Anchor has a Scene.  Scene: " + Scene.Value.name);
            }

            foreach (SplitterAnchor anchor in newGo.GetComponents<SplitterAnchor>())
            {
                Destroy(anchor);
                anchor.deleted = true;
            }

            foreach (SplitterSubscriber subsription in newGo.GetComponentsInChildren<SplitterSubscriber>().ToList())
            {
                subsription.enabled = false;
                Destroy(subsription);
            }
        }

        private void ReRegisterAllLocalTransformationsForAllPairs()
        {
            PhysicsGoIdToLocalSyncs.Clear();
            foreach (var key in ids.Keys)
            {
                SplitterSubscriber subscriber = idToMainGo[ids[key]].gameObject.transform.GetComponent<SplitterSubscriber>();
                GameObject go = idToPhysicsGo[subscriber.gameObject.GetInstanceID()].gameObject;
                SetupLocalTransformSyncCache(subscriber.transform, go, go);
            }
        }

        
        private void SetupLocalTransformSyncCache(Transform authentic, GameObject simulated, GameObject simulatedTopParent)
        {
            List<Transform> AuthenticChildren = authentic.GetComponentsInChildren<Transform>(true).Where(x => x.GetInstanceID() != authentic.transform.GetInstanceID()).ToList();
            List<Transform> NewGoChildren = simulated.GetComponentsInChildren<Transform>(true).Where(x => x.GetInstanceID() != simulated.transform.GetInstanceID()).ToList();
            List<MatchedTransform> Matches = new List<MatchedTransform>();

            for (var i = 0; i < AuthenticChildren.Count; i++)
            {

                Matches.Add(
                    new MatchedTransform
                    {
                        mainTransform = AuthenticChildren[i],
                        physicsTransform = NewGoChildren[i]
                    }
                );
            }
            PhysicsGoIdToLocalSyncs.Add(simulatedTopParent.GetInstanceID(), Matches);
        }

        private void RemoveFromLocalTransformSyncCache(Transform simulated, GameObject simulatedTopParent)
        {   
            List<Transform> simulatedChildren = simulated.GetComponentsInChildren<Transform>(true).ToList();
            List<MatchedTransform> matcheds = PhysicsGoIdToLocalSyncs[simulatedTopParent.GetInstanceID()];
            for (int i = 0; i < matcheds.Count; i++)
            {
                for(int j = 0; j < simulatedChildren.Count; j++)
                {
                    if (matcheds[i].physicsTransform == simulatedChildren[j])
                    {
                        matcheds.RemoveAt(i);
                        simulatedChildren.RemoveAt(j);
                        i--;
                        break;
                    }
                }
            }
        }
        public void UnregisterInScene(SplitterSubscriber subscriber)
        {
            if (!ids.ContainsKey(subscriber.gameObject.GetInstanceID())
                ||
                //this could get us in trouble
                PhysicsAnchorGO == null
            )
                return;

            var physicsGo = idToPhysicsGo[subscriber.gameObject.GetInstanceID()].gameObject;
            var physicsRigid = idToPhysicsGo[subscriber.gameObject.GetInstanceID()].rigidbody;

            var subRigid = subscriber.GetComponent<Rigidbody>();
            if (subRigid != null)
            {
                if (!subRigid.isKinematic)
                {
                    //velocity
                    subRigid.velocity =
                        transform.TransformDirection(
                            PhysicsAnchorGO.transform.InverseTransformDirection(
                                physicsRigid.velocity
                            )

                        )
                        +
                        this.GetUltimatePointVelocity(
                            subRigid.position
                        );

                    //angularVelocity
                	subRigid.AddTorque(
                    	GetUltimateAngularVelocity(subscriber)
                    	- subRigid.angularVelocity
                    	,
                    	ForceMode.VelocityChange
                	);
                }

                //update subscriber properties
                //velocity: do not update
                //angularvelocity: do not update
                subRigid.drag = physicsRigid.drag;
                subRigid.angularDrag = physicsRigid.angularDrag;
                //mass: i have previously experimented with updated mass -
                //do not update as it thorougly breaks cross-scene interaction
                subRigid.useGravity = physicsRigid.useGravity;
                //max depenetration velocity: do not update
                subRigid.isKinematic = physicsRigid.isKinematic;
                subRigid.freezeRotation = physicsRigid.freezeRotation;
                subRigid.constraints = physicsRigid.constraints;
                //collision detection mode: do not update
                //center of mass: do not update
                //world center of mass: do not update
                //inertiaTensorRotation: do not update
                //inertiaTensor: do not update
                //position - no gets immediately updated
                //rotation - no gets immediately updated
                //interpolation - no, this is only front end
                //solverIterations: do not update
                //sleepThreshold: do not update
                subRigid.maxAngularVelocity = physicsRigid.maxAngularVelocity;
                //solverVelocity Iterations: do not update
                //solverIterationCount: do not update
            }

            //enable all monobehaviours in RunInSimulatedSpace
            foreach (var behaviour in subscriber.RunInSimulatedSpace)
            {
                behaviour.enabled = true;
            }

            //send unity messages
            foreach (var gobj in subscriber.transform.GetComponentsInChildren<Transform>().Select(x => x.gameObject))
            {
                gobj.SendMessage("OnExitAnchor", new SplitterEvent { Anchor = this, SimulatedSubscriber = physicsGo.transform, Subscriber = subscriber, SimulatedAnchor = PhysicsAnchorGO.transform }, SendMessageOptions.DontRequireReceiver);
            }

            idToMainGo.Remove(physicsGo.GetInstanceID());
            idToPhysicsGo.Remove(subscriber.gameObject.GetInstanceID());
            ids.Remove(subscriber.gameObject.GetInstanceID());
            subscribers.Remove(subscriber.gameObject);
            PhysicsGoIdToLocalSyncs.Remove(physicsGo.GetInstanceID());
            Destroy(physicsGo);
        }

        List<MatchedTransform> _matched = null;
        private GoRigid simGO;
        public Transform GetMatchedSubscriberTransform(SplitterSubscriber subscriber, Transform splitterTransform)
        {
            simGO = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _matched = null;
            _matched = PhysicsGoIdToLocalSyncs[simGO.gameObject.GetInstanceID()];
            for(int i = 0; i < _matched.Count; i++)
            {
                if (_matched[i].mainTransform == splitterTransform)
                    return _matched[i].physicsTransform;
            }
            return null;
        }
        public Transform GetMatchedAnchorTransform(Transform anchorTransform)
        {
            _matched = null;
            _matched = PhysicsGoIdToLocalSyncs[PhysicsAnchorGO.GetInstanceID()];
            for (int i = 0; i < _matched.Count; i++)
            {
                if (_matched[i].mainTransform == anchorTransform)
                    return _matched[i].physicsTransform;
            }
            return null;
        }

        public Rigidbody GetSimulationBody(SplitterSubscriber subscriber)
        {
            return idToPhysicsGo[subscriber.gameObject.GetInstanceID()].rigidbody;
        }
        public Vector3 AnchorDirectionToWorldDirection(Vector3 Direction)
        {
            return transform.TransformDirection(PhysicsAnchorGO.transform.InverseTransformDirection(Direction));
        }
        public Vector3 WorldDirectionToAnchorDirection(Vector3 Direction)
        {
            return PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(Direction));
        }
        public Vector3 AnchorPointToWorldPoint(Vector3 Point)
        {
            return transform.TransformPoint(PhysicsAnchorGO.transform.InverseTransformPoint(Point));
        }
        public Vector3 WorldPointToAnchorPoint(Vector3 Point)
        {
            return PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(Point));
        }
        private SplitterSubscriber avtwvSubscriber;
        public Vector3 AnchorVelocityToWorldVelocity(Vector3 AnchorVelocity, Vector3 WorldPoint)
        {
            avtwvSubscriber = this.GetMySubscriber();
            if (avtwvSubscriber != null)
            {
                return transform.TransformDirection(PhysicsAnchorGO.transform.InverseTransformDirection(AnchorVelocity))
                    + avtwvSubscriber.AppliedPhysics.GetPointVelocity(WorldPoint);
            }
            else
            {
                return transform.TransformDirection(PhysicsAnchorGO.transform.InverseTransformDirection(AnchorVelocity))
                    + Body.GetPointVelocity(WorldPoint);
            }
        }

        private SplitterSubscriber ocentSub;
        public void OnCollisionEnter(Collision collision)
        {
            if (
                collision.body != null 
                && collision.body.TryGetComponent(out ocentSub)
                && ids.ContainsKey(ocentSub.gameObject.GetInstanceID()))
            {
                NegateMyCollision(collision);
            }
        }
        private SplitterSubscriber ocsSubscriber;
        public void OnCollisionStay(Collision collision)
        {
            if (collision.body == null)
                return;
            
            if (
                collision.body.TryGetComponent(out ocsSubscriber)
                && ids.ContainsKey(ocsSubscriber.gameObject.GetInstanceID()))
            {
                NegateMyCollision(collision);
            }
        }

        internal void ApplyCollision(SplitterSubscriber subscriber, Collision collision)
        {
#if UNITY_2022_1_OR_NEWER
            ApplyCollision_WithImpulsePerContactPoint(subscriber, collision);
#else
            ApplyCollision_WithAverageContactPoint(subscriber, collision);
#endif
        }

        ContactPoint[] _contactPoints = new ContactPoint[30];
        int _contactCount = 0;
        int _cnt = 0;
        Vector3 _avgContactPoint = Vector3.zero;
        Rigidbody physicsRigidToGetCollision;
        private float _crossCollisionFudgeAmt = 1.732f;
#if UNITY_2022_1_OR_NEWER
        Vector3 _impulse;
        ContactPoint _contact;
        private void ApplyCollision_WithImpulsePerContactPoint(SplitterSubscriber subscriber, Collision collision)
        {
            if (!ids.ContainsKey(subscriber.gameObject.GetInstanceID()))
                return;
           
            physicsRigidToGetCollision = idToPhysicsGo[subscriber.gameObject.GetInstanceID()].rigidbody;

            if (physicsRigidToGetCollision.isKinematic)
                return;

            _contactCount = collision.GetContacts(_contactPoints);
            for (_cnt = 0; _cnt < _contactCount; _cnt++)
            {
                _contact = collision.GetContact(_cnt);
                _impulse = _contact.impulse;
                if (Vector3.Dot(_impulse, _contact.normal) < 0f)
                    _impulse *= -1f;
                physicsRigidToGetCollision.AddForceAtPosition(
                    PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(_impulse)) * _crossCollisionFudgeAmt,
                    physicsRigidToGetCollision.transform.TransformPoint(subscriber.transform.InverseTransformPoint(_contact.point)),
                    ForceMode.Impulse
                );
            }
        }
#else
        private void ApplyCollision_WithAverageContactPoint(SplitterSubscriber subscriber, Collision collision)
        {
            if (!ids.ContainsKey(subscriber.gameObject.GetInstanceID()))
                return;
            
            Vector3 _impulse = collision.impulse;
            if (Vector3.Dot(_impulse, collision.GetContact(0).normal) < 0f)
                _impulse *= -1f;

            physicsRigidToGetCollision = idToPhysicsGo[subscriber.gameObject.GetInstanceID()].rigidbody;

            if (physicsRigidToGetCollision.isKinematic)
                return;

            _contactCount = collision.GetContacts(_contactPoints);

            _avgContactPoint = Vector3.zero;
            for (_cnt = 0; _cnt < _contactCount; _cnt++)
            {
                _avgContactPoint += _contactPoints[_cnt].point;
            }
            _avgContactPoint = _avgContactPoint * 1f / _contactCount;
            
            physicsRigidToGetCollision.AddForceAtPosition(
                PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(_impulse))*_crossCollisionFudgeAmt,
                physicsRigidToGetCollision.transform.TransformPoint(subscriber.transform.InverseTransformPoint(_avgContactPoint)),
                ForceMode.Impulse
            );
        }
#endif

        private SplitterSubscriber _nmcSubscriber;
        private Rigidbody _nmcBody;
        internal void NegateMyCollision(Collision collision)
        {
            if (mySubscriber == null && Body == null)
                return;

            Vector3 impulse = collision.impulse;
            

            _nmcSubscriber = mySubscriber;

            if (_nmcSubscriber != null && _nmcSubscriber.isActiveAndEnabled)
            {
                if (_nmcSubscriber.AppliedPhysics.isKinematic)
                    return;

                _contactCount = collision.GetContacts(_contactPoints);
                if (Vector3.Dot(impulse, _contactPoints[0].normal) < 0f)
                {
                    impulse *= -1f;
                }
#if UNITY_2022_1_OR_NEWER
                for (_cnt = 0; _cnt < _contactCount; _cnt++)
                {
                    _nmcSubscriber.AppliedPhysics.AddForceAtPosition(
                        -impulse,
                        _contactPoints[_cnt].point,
                        ForceMode.Impulse
                    );
                }
#else
                _avgContactPoint = Vector3.zero;
                for (_cnt = 0; _cnt < _contactCount; _cnt++)
                {
                    _avgContactPoint += _contactPoints[_cnt].point;
                }
                _avgContactPoint = _avgContactPoint * 1f / _contactCount;
                _nmcSubscriber.AppliedPhysics.AddForceAtPosition(
                    -impulse,
                    _avgContactPoint,
                    ForceMode.Impulse
                );
#endif
            }
            else if (Body != null)
            {
                if(Body.isKinematic)
                    return;

                _contactCount = collision.GetContacts(_contactPoints);
                if (Vector3.Dot(impulse, _contactPoints[0].normal) < 0f)
                {
                    impulse *= -1f;
                }
                _avgContactPoint = Vector3.zero;
                for (_cnt = 0; _cnt < _contactCount; _cnt++)
                {
                    _avgContactPoint += _contactPoints[_cnt].point;
                }
                _avgContactPoint = _avgContactPoint * 1f / _contactCount;
                Body.AddForceAtPosition(
                    -impulse,
                    _avgContactPoint,
                    ForceMode.Impulse
                );
            }
        }
        
        internal void ApplyAddForce(Vector3 force, ForceMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.AddForce(
                PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(force)),
                mode
            );
        }

        internal void ApplyAddForceAtPosition(Vector3 force, Vector3 pos, ForceMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.AddForceAtPosition(
                PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(force)),
                PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(pos)),
                mode
            );
        }
        
        internal void ApplyAddRelativeForce(Vector3 relForce, ForceMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.AddRelativeForce(
                relForce,
                mode
            );
        }

        internal void ApplyAddRelativeTorque(Vector3 relTorque, ForceMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.AddRelativeTorque(
                relTorque,
                mode
            );
        }
        
        internal void ApplyAddTorque(Vector3 torque, ForceMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.AddTorque(
                PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(torque)),
                mode
            );
        }
        
        internal void ApplyAddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardModifier, ForceMode mode, SplitterSubscriber subscriber)
        {

            if (upwardModifier > 0f)
            {
                _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
                Vector3 _simExplosionPosition = PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(explosionPosition));

                Vector3 _simApplicationPosition = _Sim.rigidbody.ClosestPointOnBounds(_simExplosionPosition);
                if ((_simApplicationPosition - _Sim.rigidbody.position).sqrMagnitude >
                    (_simExplosionPosition - _Sim.rigidbody.position).sqrMagnitude
                )
                    _simApplicationPosition = _Sim.rigidbody.position;

                float scaledExplosionForce =
                    (
                        1f -
                        Mathf.Sqrt(
                            (
                                (_simApplicationPosition - _simExplosionPosition).sqrMagnitude 
                                / 
                                Mathf.Pow(explosionRadius, 2)
                            )
                        )
                    )
                    *
                    explosionForce;
                if (scaledExplosionForce <= 0)
                    scaledExplosionForce = 0f;

                //redefines
                _simExplosionPosition = PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(explosionPosition));
                _simExplosionPosition += -
                    (
                        PhysicsAnchorGO.transform.up
                        * upwardModifier
                    );
                _simApplicationPosition = _Sim.rigidbody.ClosestPointOnBounds(_simExplosionPosition);
                if ((_simApplicationPosition - _Sim.rigidbody.position).sqrMagnitude >
                    (_simExplosionPosition - _Sim.rigidbody.position).sqrMagnitude
                )
                    _simApplicationPosition = _Sim.rigidbody.position;

                Vector3 _simApplicationDirection =
                    (
                        _simApplicationPosition -
                        (
                            _simExplosionPosition
                        )
                    ).normalized;


                _Sim.rigidbody.AddForceAtPosition(scaledExplosionForce * _simApplicationDirection, _simApplicationPosition, mode);
            }
            else
            {
                _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
                _Sim.rigidbody.AddExplosionForce(
                    explosionForce,
                    PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(explosionPosition)),
                    explosionRadius,
                    0f,
                    mode
                );
            }
        }

        internal Vector3 GetVelocity(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return transform.TransformDirection(PhysicsAnchorGO.transform.InverseTransformDirection(_Sim.rigidbody.velocity))
                + GetUltimatePointVelocity(subscriber.AppliedPhysics.position);
        }
#if !UNITY_6000_0_OR_NEWER
        internal void ApplyVelocity(Vector3 velocity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.velocity = 
                PhysicsAnchorGO.transform.TransformDirection(
                    transform.InverseTransformDirection(velocity)
                ) - GetUltimatePointVelocity(subscriber.AppliedPhysics.position);
        }
#endif
#if UNITY_6000_0_OR_NEWER
        internal void ApplyLinearVelocity(Vector3 linearVelocity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.linearVelocity = 
                PhysicsAnchorGO.transform.TransformDirection(
                    transform.InverseTransformDirection(linearVelocity)
                ) - GetUltimatePointVelocity(subscriber.AppliedPhysics.position);
        }
#endif
        internal void ApplyPosition(Vector3 position, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.position = PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(position));
        }

        internal Vector3 GetPosition(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return this.transform.TransformPoint(PhysicsAnchorGO.transform.InverseTransformPoint(_Sim.rigidbody.position));
        }
        
        internal void ApplyMovePosition(Vector3 position, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.MovePosition(PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(position)));
        }

        internal Quaternion GetApplyRotation(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return this.getRotation() * (Quaternion.Inverse(PhysicsAnchorGO.transform.rotation) * _Sim.rigidbody.rotation);
        }
        internal void ApplyRotation(Quaternion rotation, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.rotation = PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(this.getRotation()) * rotation);
        }
        public Quaternion TranslateWorldRotationToAnchorRotation(Quaternion rotation)
        {
            return PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(this.getRotation()) * rotation);
        }
        public Quaternion TranslateAnchorRotationToWorldRotation(Quaternion rotation)
        {
            return this.getRotation() * (Quaternion.Inverse(PhysicsAnchorGO.transform.rotation) * rotation);
        }
        internal void ApplyMoveRotation(Quaternion rotation, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.MoveRotation(PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(this.getRotation()) * rotation));
        }

#if !UNITY_6000_0_OR_NEWER
        internal void ApplyDrag(float drag, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.drag = drag;
        }
#endif

#if UNITY_6000_0_OR_NEWER
        internal void ApplyLinearDamping(float linearDamping, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.linearDamping = linearDamping;
        }
#endif

        internal void ApplyMass(float mass, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.mass = mass;
        }

        internal void ApplyMaxDepenetrationVelocity(float max, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.maxDepenetrationVelocity = max;
        }

        internal Vector3 GetAngularVelocity(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return transform.TransformDirection(
                PhysicsAnchorGO.transform.InverseTransformDirection(
                    _Sim.rigidbody.angularVelocity
                )
             );
        }
        internal void ApplyAngularVelocity(Vector3 velocity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.angularVelocity = PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(velocity));
        }

        internal void ApplyAngularDrag(float angularDrag, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.angularDrag = angularDrag;
        }

        internal void ApplyUseGravity(bool useGravity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.useGravity = useGravity;
        }

        internal void ApplyFreezeRotation(bool freezeRotation, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.freezeRotation = freezeRotation;
        }
        internal void ApplyConstraints(RigidbodyConstraints constraints, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.constraints = constraints;
        }

        internal RigidbodyConstraints ApplyGetConstraints(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return _Sim.rigidbody.constraints;
        }
        private GoRigid _Sim;
        internal void ApplyCollisionDetectionMode(CollisionDetectionMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.collisionDetectionMode = mode;
        }
        internal void ApplyCenterOfMass(Vector3 centerOfMass, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.centerOfMass = centerOfMass;
        }
        internal void ApplyInertiaTensorRotation(Quaternion rotation, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.inertiaTensorRotation = PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(this.getRotation()) * rotation);
        }
        internal void ApplyInertiaTensor(Vector3 inertiaTensor, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.inertiaTensor = inertiaTensor;
        }
        internal void ApplyInterpolation(RigidbodyInterpolation interpolation, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.interpolation = interpolation;
        }
        internal void ApplySolverIterations(int solverIterations, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.solverIterations = solverIterations;
        }
        internal void ApplySleepThreshold(float sleepThreshold, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.sleepThreshold = sleepThreshold;
        }
        internal void ApplyMaxAngularVelocity(float maxAngularVelocity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.maxAngularVelocity = maxAngularVelocity;
        }
        internal void ApplySolverVelocityIterations(int solverVelocityIterations, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.solverVelocityIterations = solverVelocityIterations;
        }
        internal void ApplySetDensity(float density, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.SetDensity(density);
        }
        internal void ApplySleep(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.Sleep();
        }
        internal void ApplyWakeUp(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.WakeUp();
        }
        internal void ApplyResetCenterOfMass(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.ResetCenterOfMass();
        }

        internal void ApplyResetInertiaTensor(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.ResetInertiaTensor();
        }
        internal Vector3 ApplyGetRelativePointVelocity(Vector3 relativePoint, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return
                subscriber.transform.TransformDirection(
                    _Sim.gameObject.transform.InverseTransformDirection(
                        _Sim.rigidbody.GetRelativePointVelocity(
                            relativePoint
                        )
                    )
                ) - GetUltimatePointVelocity(relativePoint);
        }
        internal Vector3 ApplyGetPointVelocity(Vector3 worldPoint, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return GetUltimatePointVelocity(worldPoint, subscriber);
        }
        internal Vector3 ApplyClosestPointOnBound(Vector3 position, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return
                subscriber.transform.TransformPoint(
                    _Sim.gameObject.transform.InverseTransformPoint(
                        _Sim.rigidbody.ClosestPointOnBounds(
                            PhysicsAnchorGO.transform.TransformPoint(
                                transform.InverseTransformPoint(
                                    position
                                )
                            )
                        )
                    )
                );
        }

        internal void ApplyIsKinematic(bool value, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.isKinematic = value;
        }
        private WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

        private List<int> _cleanListKeys = new List<int>();
        private void CleanIdToMainGo()
        {
            _cleanListKeys.Clear();
            _cleanListKeys = idToMainGo.Where(x=> x.Value == null || x.Value.gameObject == null).Select(x=>x.Key).ToList();
            foreach(var key in _cleanListKeys)
            {
                idToMainGo.Remove(key);
            }
        }
        public void Simulate()
        {
            //Simulate our scene only if there is something to simulate!
            if(RunInSimulationSpace.Count > 0 || subscribers.Count > 0)
                PhysicsScene.Simulate(Time.fixedDeltaTime);
        }

        int _ei;
        public void Export()
        {
            _ei = 0;
            for (; _ei < subscribers.Count; _ei++)
            {
                UpdateSubscriberRigidbody(subscribers[_ei].gameObject);
            }
        }

        int _psi;
        public void PhysicsSync()
        {
            _psi = 0;
            for (; _psi < subscribers.Count; _psi++)
            {
                SyncSubscriberRigidbody(subscribers[_psi].gameObject);
            }
        }

        int _hsi;
        public void HardSync()
        {
            /*#if UNITY_2022_2_OR_NEWER
                        PhysicsScene.InterpolateBodies();
#endif*/

            _hsi = 0;
            for(; _hsi < subscribers.Count; _hsi++)
            {
                if(SyncSubscriberTransforms || SubscriberRequestsSyncTransform(subscribers[_hsi]))
                    SyncSubscriberTransform(subscribers[_hsi].gameObject);
            }

            _hsi = 0;
            for (; _hsi < subscribers.Count; _hsi++)
            {
                if(SyncSubscriberChildTransforms || SubscriberRequestsSyncChildTransforms(subscribers[_hsi]))
                    SyncSubscriberChildrenTransforms(subscribers[_hsi].gameObject);
            }

            //TODO: determine if this is ever needed and how to bubble this up to the editor for configuration.
            if (CallPhysicsSync  && false)
                Physics.SyncTransforms();
        }

        private GoRigid _SimSubscriber;
        private int _physGoOfSubscriberInstanceId;
        private Rigidbody _mainGoRigidbody;
        private int _iterator = 0;
        private void UpdateSubscriber(GameObject mainGo)
        {
            _SimSubscriber = idToPhysicsGo[mainGo.GetInstanceID()];
            mainGo.transform.rotation =
                this.getRotation() * (Quaternion.Inverse(PhysicsAnchorGO.transform.rotation) * _SimSubscriber.rigidbody.rotation);

            mainGo.transform.position =
                this.transform.TransformPoint(
                    PhysicsAnchorGO.transform.InverseTransformPoint(_SimSubscriber.rigidbody.position)
                );
        }


        private SplitterSubscriber _sub;
        private void UpdateSubscriberRigidbody(GameObject mainGo)
        {
            _SimSubscriber = idToPhysicsGo[mainGo.GetInstanceID()];
            _physGoOfSubscriberInstanceId = _SimSubscriber.gameObject.GetInstanceID();
            _mainGoRigidbody = mainGo.GetComponent<Rigidbody>();
            _sub = mainGo.GetComponent<SplitterSubscriber>();
            if (_mainGoRigidbody.isKinematic || _SimSubscriber.rigidbody.isKinematic)
                return;


            _mainGoRigidbody.AddForce(
                GetUltimatePointVelocity(
                    _mainGoRigidbody.position,
                    _sub
                ) - _mainGoRigidbody.velocity
                ,
                ForceMode.VelocityChange
            );

            //rotation
            _mainGoRigidbody.AddTorque(
                GetUltimateAngularVelocity(_sub)
                - _mainGoRigidbody.angularVelocity
                ,
                ForceMode.VelocityChange
            );
        }

        private void SyncSubscriberRigidbody(GameObject mainGo)
        {
            _SimSubscriber = idToPhysicsGo[mainGo.GetInstanceID()];
            _mainGoRigidbody = mainGo.GetComponent<Rigidbody>();
            _mainGoRigidbody.MoveRotation(
                this.getRotation() * (Quaternion.Inverse(PhysicsAnchorGO.transform.rotation) * _SimSubscriber.rigidbody.rotation)
            );

            _mainGoRigidbody.MovePosition(
                this.transform.TransformPoint(
                    PhysicsAnchorGO.transform.InverseTransformPoint(_SimSubscriber.rigidbody.position)
                )
            );
        }


        private void SyncSubscriberTransform(GameObject mainGo)
        {
            //this ruins interpolation...
            if (mainGo.GetComponent<Rigidbody>().interpolation != RigidbodyInterpolation.None)
                return;
            //extrapolation still failing?
            _SimSubscriber = idToPhysicsGo[mainGo.GetInstanceID()];
            mainGo.transform.rotation =
                this.getRotation()
                * (Quaternion.Inverse(PhysicsAnchorGO.transform.rotation) * _SimSubscriber.rigidbody.rotation);
            mainGo.transform.position = this.transform.TransformPoint(
                    PhysicsAnchorGO.transform.InverseTransformPoint(_SimSubscriber.rigidbody.position)
                );
        }
        private SplitterSubscriber _srstSubscriber;
        private bool SubscriberRequestsSyncTransform(GameObject mainGo)
        {
            _srstSubscriber = idToPhysicsGo[mainGo.GetInstanceID()].subscriber;
                return _srstSubscriber.OverrideAnchorSettings && _srstSubscriber.SyncTransform; 
        }


        private void SyncSubscriberChildrenTransforms(GameObject mainGo)
        {
            _SimSubscriber = idToPhysicsGo[mainGo.GetInstanceID()];
            _physGoOfSubscriberInstanceId = _SimSubscriber.gameObject.GetInstanceID();
            _mainGoRigidbody = mainGo.GetComponent<Rigidbody>();
            _sub = mainGo.GetComponent<SplitterSubscriber>();

            //transform sync
            _iterator = 0;
            for (_iterator = 0; _iterator < PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId].Count; _iterator++)
            {
                if (PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId][_iterator].physicsTransform.hasChanged)
                {
                    PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId][_iterator].mainTransform.localRotation =
                        PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId][_iterator].physicsTransform.localRotation;
                    PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId][_iterator].mainTransform.localPosition =
                        PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId][_iterator].physicsTransform.localPosition;

                    PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId][_iterator].physicsTransform.hasChanged = false;
                }
            }
        }
        private SplitterSubscriber _srsctSubscriber;
        private bool SubscriberRequestsSyncChildTransforms(GameObject mainGo)
        {
            _srsctSubscriber = idToPhysicsGo[mainGo.GetInstanceID()].subscriber;
            return _srsctSubscriber.OverrideAnchorSettings && _srsctSubscriber.SyncChildTransforms;
        }
        public GoRigid GetSubSim(SplitterSubscriber subscriber)
        {
            return idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
        }
        public GameObject GetSim()
        {
            return PhysicsAnchorGO;
        }
        bool _quitting;
        private void OnApplicationQuit()
        {
            _quitting = true;
        }
        private void OnDestroy()
        {
            //if anchor deleted mid play, unregister subscribers
            if (gameObject.scene.isLoaded && !_quitting && PhysicsAnchorGO != null) //Was Deleted
            {
                RemoveAllSubscribers();
            }

            if (Scene.HasValue)
                SceneManager.UnloadSceneAsync(Scene.Value);
        }

        void OnEnable()
        {
            //we do this so all collision events occur again
            if(Scene != null)
                InitColliders();

            SplitterSystem.SplitterSimulate += Simulate;
            SplitterSystem.SplitterPhysicsExport += Export;
            SplitterSystem.SplitterPhysicsSync += PhysicsSync;
            SplitterSystem.SplitterHardSync += HardSync;
            //AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            //AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        
        void OnDisable()
        {
            if (!_quitting && gameObject.scene.isLoaded)
                RemoveAllSubscribers();

            SplitterSystem.SplitterSimulate -= Simulate;
            SplitterSystem.SplitterPhysicsExport -= Export;
            SplitterSystem.SplitterPhysicsSync -= PhysicsSync;
            SplitterSystem.SplitterHardSync -= HardSync;

            //AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            //AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        public void InitColliders()
        {
            flickerEntryAndExits();
        }

        Collider _col;
        int _iFEAE;
        private void flickerEntryAndExits()
        {
            //quick cleans
            for(_iFEAE = 0; _iFEAE < EntranceTriggers.Count; _iFEAE++)
            {
                if (EntranceTriggers[_iFEAE] == null)
                {
                    EntranceTriggers.RemoveAt(_iFEAE);
                    _iFEAE--;
                }

            }
            for(_iFEAE = 0; _iFEAE < StayTriggers.Count; _iFEAE++)
            {
                if(StayTriggers[_iFEAE] == null)
                {
                    StayTriggers.RemoveAt(_iFEAE);
                    _iFEAE--;
                }
            }
            //EntranceTriggers = EntranceTriggers.Where(x => x != null).ToList();
            //StayTriggers = StayTriggers.Where(x => x != null).ToList();

            _iFEAE = 0;
            for(; _iFEAE < EntranceTriggers.Count; _iFEAE++)
            {
                _col = EntranceTriggers[_iFEAE];
                _col.enabled = false;
            }
            _iFEAE = 0;
            for (; _iFEAE < StayTriggers.Count; _iFEAE++)
            {
                _col = StayTriggers[_iFEAE];
                _col.enabled = false;
            }
            _iFEAE = 0;
            for (; _iFEAE < StayTriggers.Count; _iFEAE++)
            {
                _col = StayTriggers[_iFEAE];
                _col.enabled = true;
            }
            _iFEAE = 0;
            for (; _iFEAE < EntranceTriggers.Count; _iFEAE++)
            {
                _col = EntranceTriggers[_iFEAE];
                _col.enabled = true;
            }
        }

        SplitterSubscriber _removalSub;
        private void RemoveAllSubscribers()
        {
            //when game quits, you don't know the order of destruction,
            //so this could be filled with nulls
            subscribers = subscribers.Where(x => x != null).ToList();
            //for(; _iRAS < subscribers.Count; _iRAS++)
            while(subscribers.Count != 0)
            {
                _removalSub = subscribers[0].GetComponent<SplitterSubscriber>();
                //this method removes items from subscribers list, so we do not have to
                UnregisterInScene(_removalSub);
                _removalSub.HandleAnchorDestruction(this);
                _removalSub.Anchor = null;
            }
        }

        /*public void OnBeforeAssemblyReload()
        {
            SceneName = Scene.Value.name;
        }

        public void OnAfterAssemblyReload()
        {
            if (Scene == null && SceneName != "")
            {
                Scene = SceneManager.GetSceneByName(SceneName);
                PhysicsScene = Scene.Value.GetPhysicsScene();
            }

            ReRegisterAllLocalTransformationsForAllPairs();
        }*/

        public bool IsInMySimulation(SplitterSubscriber subscriber)
        {
            return ids.ContainsKey(subscriber.gameObject.GetInstanceID());
        }

        private Quaternion getRotation()
        {
            if (Body != null)
                return Body.rotation;
            else
                return transform.rotation;
        }
        private Vector3 getPosition()
        {
            if (Body != null)
                return Body.position;
            else
                return transform.position;
        }
        internal Vector3 GetPointVelocity(Vector3 WorldPoint)
        {
            if (Body != null)
                return Body.GetPointVelocity(WorldPoint);
            else
                return Vector3.zero;
        }

        internal Transform GetTransform(Vector3 LocalPoint)
        {
            if (Body != null)
                return Body.transform;
            else
                return transform;
        }


        private SplitterSubscriber _gupvSubscriber;
        public Vector3 GetUltimatePointVelocity(Vector3 WorldPos)
        {
            //_gupvSubscriber = transform.GetComponent<SplitterSubscriber>();
            if (
                !transform.TryGetComponent(out _gupvSubscriber)
                //_gupvSubscriber == null 
                || 
                !_gupvSubscriber.isActiveAndEnabled)
            {
                if (Body != null)
                    return Body.GetPointVelocity(WorldPos);
                else
                    return Vector3.zero;
            }
            else
                return GetUltimatePointVelocity(WorldPos, _gupvSubscriber);
        }

        internal static Vector3 GetUltimatePointVelocity(Vector3 WorldPoint, SplitterSubscriber sub)
        {
            if (sub.Anchor == null)
                return sub.Body.GetPointVelocity(WorldPoint);

            if (sub.Anchor.GetMySubscriber() != null && sub.Anchor.GetMySubscriber().isActiveAndEnabled)
                return
                    sub.Anchor.transform.TransformDirection(
                        sub.Anchor.GetSim().transform.InverseTransformDirection(
                            sub.Anchor.GetSubSim(sub).rigidbody.GetPointVelocity(
                                sub.Anchor.GetSim().gameObject.transform.TransformPoint(sub.Anchor.transform.InverseTransformPoint(WorldPoint))
                            )
                        )
                    )
                    +
                    GetUltimatePointVelocity(WorldPoint, sub.Anchor.GetMySubscriber());
            else
                return
                    sub.Anchor.transform.TransformDirection(
                        sub.Anchor.GetSim().transform.InverseTransformDirection(
                            sub.Anchor.GetSubSim(sub).rigidbody.GetPointVelocity(
                                sub.Anchor.GetSim().gameObject.transform.TransformPoint(sub.Anchor.transform.InverseTransformPoint(WorldPoint))
                            )
                        )
                    )
                    +
                    sub.Anchor.GetPointVelocity(WorldPoint);
        }

        internal static Vector3 GetUltimateAngularVelocity(SplitterSubscriber sub)
        {
            if (sub.Anchor == null)
                return sub.Body.angularVelocity;

            if (sub.Anchor.GetMySubscriber() != null && sub.Anchor.GetMySubscriber().isActiveAndEnabled)
                return
                    sub.Anchor.transform.TransformDirection(
                        sub.Anchor.GetSim().transform.InverseTransformDirection(
                            sub.Anchor.GetSubSim(sub).rigidbody.angularVelocity
                        )
                    )
                    +
                    GetUltimateAngularVelocity(sub.Anchor.GetMySubscriber());
            if (sub.Anchor.Body != null)
                return
                    sub.Anchor.transform.TransformDirection(
                        sub.Anchor.GetSim().transform.InverseTransformDirection(
                            sub.Anchor.GetSubSim(sub).rigidbody.angularVelocity
                        )
                    )
                    +
                    sub.Anchor.Body.angularVelocity;
            else
                return
                    sub.Anchor.transform.TransformDirection(
                        sub.Anchor.GetSim().transform.InverseTransformDirection(
                            sub.Anchor.GetSubSim(sub).rigidbody.angularVelocity
                        )
                    );
        }

        internal bool HasSubscriber(SplitterSubscriber subscriber)
        {
            return subscribers.Contains(subscriber.gameObject);
        }


        private GameObject _sssvSubscriber = null;
        public void SetVisibility( bool visible)
        {
            if(PhysicsAnchorGO != null)
                //set this sim visibility
                foreach(var renderer in PhysicsAnchorGO.GetComponentsInChildren<Renderer>().ToList())
                {
                    renderer.enabled = visible;
                }


            if(subscribers != null && idToPhysicsGo != null)
                //set subscriber sim visibility
                foreach (var sub in subscribers)
                { 
                    _sssvSubscriber = idToPhysicsGo[sub.gameObject.GetInstanceID()].gameObject;
                    foreach (var renderer in _sssvSubscriber.GetComponentsInChildren<Renderer>().ToList())
                    {
                        renderer.enabled = visible;
                    }
                }
        }

        internal void setMySubscriber(SplitterSubscriber sub)
        {
            mySubscriber = sub;
        }

        private GoRigid ssTGoRigid;
        private Transform simParentTransform;
        private Transform created;
        private Scene tempScene;
        public Transform AddSimulatedSubscriberTransform(Transform newTransform, Transform parent, SplitterSubscriber belongsTo)
        {
            tempScene = SceneManager.GetActiveScene();
            simParentTransform = GetMatchedSubscriberTransform(belongsTo, parent);
            SceneManager.SetActiveScene(Scene.Value);
            created = Instantiate(newTransform,
                WorldPointToAnchorPoint(newTransform.position),
                TranslateWorldRotationToAnchorRotation(newTransform.rotation)
            ) as Transform;
            created.parent = simParentTransform;
            SceneManager.SetActiveScene(tempScene);

            StripGameObjectAsSubscriber(created.gameObject, belongsTo);
            SetupLocalTransformSyncCache(newTransform, created.gameObject, belongsTo.gameObject);
            return created;
        }
        private Transform toRemove;
        private Transform simBelongsTo;
        public void RemoveSimulatedSubscriberTransform(Transform authentic, SplitterSubscriber belongsTo)
        {
            Debug.Log("removing authentic: " + authentic.gameObject.name + "; belongs to " + belongsTo.gameObject.name);
            toRemove = GetMatchedSubscriberTransform(belongsTo, authentic);
            Debug.Log("Removing something called " + toRemove.gameObject.name + " which belongs to " + belongsTo.gameObject.name);
            RemoveFromLocalTransformSyncCache(toRemove, idToPhysicsGo[belongsTo.gameObject.GetInstanceID()].gameObject);
            Destroy(toRemove.gameObject); 
        }
        public void RemoveSimulatedAnchorTransform(Transform authentic)
        {
            toRemove = GetMatchedAnchorTransform(authentic);
            RemoveFromLocalTransformSyncCache(toRemove, idToPhysicsGo[gameObject.GetInstanceID()].gameObject);
            Destroy(toRemove.gameObject);
        }
        public Transform AddSimulatedAnchorTransform(Transform newTransform, Transform parent)
        {
            tempScene = SceneManager.GetActiveScene();
            if (parent == this.transform)
                simParentTransform = PhysicsAnchorGO.transform;
            else
                //search children to find match
                simParentTransform = GetMatchedAnchorTransform(parent);
            SceneManager.SetActiveScene(Scene.Value);
            created = Instantiate(newTransform,
                WorldPointToAnchorPoint(newTransform.position),
                TranslateWorldRotationToAnchorRotation(newTransform.rotation)
            ) as Transform;
            created.parent = simParentTransform;
            SceneManager.SetActiveScene(tempScene);

            StripGameObjectAsAnchor(created.gameObject);
            Destroy(created.gameObject.GetComponent<Rigidbody>());
            SetupLocalTransformSyncCache(newTransform, created.gameObject, this.gameObject);

            return created;
        }
    }

    public enum PositionalAccuracy { Low, Medium, High };
    public static class PositionalAccuracyExtensions
    {
        public static bool IsMediumOrBetter(this PositionalAccuracy accuracy)
        {
            return accuracy == PositionalAccuracy.Medium || accuracy == PositionalAccuracy.High;
        }
    }
    public class GoRigid
    {
        public GameObject gameObject;
        public Rigidbody rigidbody;
        public SplitterSubscriber subscriber;
    }
}
