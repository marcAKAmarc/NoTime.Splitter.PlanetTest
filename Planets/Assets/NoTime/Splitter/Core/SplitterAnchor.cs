using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


using UnityEngine;
using UnityEngine.SceneManagement;
using NoTime.Splitter.Helpers;
using NoTime.Splitter.Internal;
using UnityEditor;
using Unity.VisualScripting;
using UnityEngine.LowLevel;
using Assets.NoTime.Splitter.Core;

namespace NoTime.Splitter
{
    public class SplitterAnchor : MonoBehaviour
    {
        [Tooltip("For example, ships have higher priority than planets.")]
        public float EntrancePriority;
        [Tooltip("Any subscribers that enter any of the entrance triggers will be entered into the simulation in this anchor.")]
        public List<Collider> EntranceTriggers;
        [Tooltip("Any subscribers that are not in any of the exit triggers will be exit the simulation of this anchor.")]
        public List<Collider> StayTriggers;

        [Tooltip("Render simulated anchor")]
        public bool SimulationVisible = false;

        private Scene? Scene;
        private PhysicsScene PhysicsScene;
        private Scene MainScene;
        private string SceneName;
        private GameObject PhysicsAnchorGO;
        private Rigidbody body = null;


        [HideInInspector]
        [SerializeField]
        private DictionaryIntInt ids;
        [HideInInspector]
        [SerializeField]
        private DictionaryIntGameObject idToPhysicsGo;
        [HideInInspector]
        [SerializeField]
        private DictionaryIntGameObject idToMainGo;
        [HideInInspector]
        [SerializeField]
        private Dictionary<int, List<MatchedTransform>> PhysicsGoIdToLocalSyncs;

        private bool visible = true;

        private bool mustSimulateBeforeSubscriberUpdate;

        private class MatchedTransform
        {
            public Transform mainTransform;
            public Transform physicsTransform;
        }

        private void Awake()
        {
            if (ids == null)
                ids = new DictionaryIntInt();
            if (idToPhysicsGo == null)
                idToPhysicsGo = new DictionaryIntGameObject();
            if (idToMainGo == null)
                idToMainGo = new DictionaryIntGameObject();
            if (PhysicsGoIdToLocalSyncs == null)
                PhysicsGoIdToLocalSyncs = new Dictionary<int, List<MatchedTransform>>();

            if(transform.GetComponent<Rigidbody>() != null)
                body = transform.GetComponent<Rigidbody>();
        }
        void Start()
        {
            if (Scene == null)
                CreateAnchorSimulationScene();
            StartCoroutine(ResetSimulatorFlag());
        }

        private void CreateAnchorSimulationScene()
        {
            //Physics.autoSimulation = false;
            MainScene = SceneManager.GetActiveScene();
            Scene = SceneManager.CreateScene(gameObject.name + System.Guid.NewGuid().ToString(), new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            PhysicsScene = Scene.Value.GetPhysicsScene();

            SceneManager.SetActiveScene(Scene.Value);
            PhysicsAnchorGO = Instantiate(gameObject, Vector3.one * 100f, transform.localRotation);
            SceneManager.SetActiveScene(MainScene);

            PhysicsAnchorGO.transform.localScale = transform.lossyScale;
            if (PhysicsAnchorGO.transform.GetComponent<Rigidbody>() != null)
            {
                PhysicsAnchorGO.transform.GetComponent<Rigidbody>().mass = 999999999f;
                PhysicsAnchorGO.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            }
            
            PhysicsAnchorGO.name = PhysicsAnchorGO.name + "-Physics";

            //disable and delete all monobehaviors not in RunInSimulatedSpace
            foreach (var behaviour in PhysicsAnchorGO.GetComponentsInChildren<Behaviour>().Where(x=>
                !(x.GetType() == typeof(SplitterAnchor))
            ))
            {
                behaviour.enabled = false;
                Destroy(behaviour);
            }

            PhysicsAnchorGO.AddComponent<SplitterAnchorSimulation>();
            PhysicsAnchorGO.GetComponent<SplitterAnchorSimulation>().Anchor = this;
            PhysicsAnchorGO.GetComponent<SplitterAnchorSimulation>().DeactivateTriggerColliders =
                PhysicsAnchorGO.GetComponent<SplitterAnchor>().StayTriggers;

            //visibility
            if (!this.SimulationVisible)
            {
                foreach (var renderer in PhysicsAnchorGO.GetComponentsInChildren<Renderer>().ToList())
                {
                    renderer.enabled = false;
                }
            }

            //do not have subscription or anchors in simulation
            if (PhysicsAnchorGO.GetComponent<SplitterAnchor>().Scene != null)
            {
                Debug.LogError("PhysicsAnchorGo Scene value on Destroy: " + PhysicsAnchorGO.GetComponent<SplitterAnchor>().Scene.ToString());
                throw new UnityException("Simulated Anchor has a Scene.  Scene: " + Scene.Value.name);
            }
            Destroy(PhysicsAnchorGO.GetComponent<SplitterAnchor>());
            foreach (var subsription in PhysicsAnchorGO.GetComponentsInChildren<SplitterSubscriber>().ToList())
            {
                subsription.enabled = false;
                Destroy(subsription);
            }
            //visibility
            if (!visible)
            {
                foreach (var renderer in PhysicsAnchorGO.GetComponentsInChildren<Renderer>().ToList())
                {
                    renderer.enabled = false;
                }
            }

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

            //if this id is already in id's bail
            if (ids.ContainsKey(subscriber.gameObject.GetInstanceID()))
                return null;

            //make sure we have scene already.  if this was set in editor, we may not have a scene yet.
            if (Scene == null)
                CreateAnchorSimulationScene();

            //create the new sim object
            SceneManager.SetActiveScene(Scene.Value);
            var newGo = Instantiate(
                subscriber.gameObject,
                PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(subscriber.transform.position)),
                PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(transform.rotation) * subscriber.transform.rotation)
            );
            SceneManager.SetActiveScene(MainScene);

            newGo.transform.localScale = subscriber.transform.lossyScale;

            //positional
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
                

            //rotational

            newGo.GetComponent<Rigidbody>().angularVelocity =
                PhysicsAnchorGO.transform.TransformDirection(
                    this.transform.InverseTransformDirection(
                        subscriber.GetComponent<Rigidbody>().angularVelocity
                    )
                );

            newGo.name = newGo.name + "-Physics";

            ids.Add(subscriber.gameObject.GetInstanceID(), newGo.GetInstanceID());
            idToPhysicsGo.Add(subscriber.gameObject.GetInstanceID(), new GoRigid()
            {
                gameObject = newGo.gameObject,
                rigidbody = newGo.transform.GetComponent<Rigidbody>()
            });
            idToMainGo.Add(
                newGo.GetInstanceID(), 
                new GoRigid() { 
                    gameObject = subscriber.gameObject, 
                    rigidbody = subscriber.transform.GetComponent<Rigidbody>() 
                }
            );

            SetupLocalTransformSyncCache(subscriber, newGo);

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

            //disable all behaviours in RunInSimulatedSpace
            foreach (var behaviour in subscriber.RunInSimulatedSpace)
            {
                behaviour.enabled = false;
            }

            newGo.AddComponent<SplitterSubscriberSimulated>();
            newGo.GetComponent<SplitterSubscriberSimulated>().Authentic = subscriber;
            newGo.GetComponent<SplitterSubscriberSimulated>().Anchor = this;  

            newGo.GetComponent<SplitterSubscriber>().enabled = false;
            Destroy(newGo.GetComponent<SplitterSubscriber>());


            //visibility
            if (!subscriber.SimulationVisible)
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
            foreach(var joint in newGo.GetComponentsInChildren<Joint>())
            {
                Destroy(joint);
            }

            //send UnityMessagesHere
            /*foreach (var gobj in newGo.GetComponentsInChildren<Transform>().Select(x => x.gameObject))
            {
                gobj.SendMessage("OnRigidContextSubscriberEntersAnchor", new SplitterContextEvent { Anchor = this, SimulatedSubscriber = newGo.transform, Subscriber = subscriber, SimulatedAnchor = PhysicsAnchorGO.transform }, SendMessageOptions.DontRequireReceiver);
            }*/
            foreach (var gobj in subscriber.transform.GetComponentsInChildren<Transform>().Select(x => x.gameObject))
            {
                gobj.SendMessage("OnEnterAnchor", new SplitterEvent { Anchor = this, SimulatedSubscriber = newGo.transform, Subscriber = subscriber, SimulatedAnchor = PhysicsAnchorGO.transform }, SendMessageOptions.DontRequireReceiver);
            }

            return newGo;
        }
        private void ReRegisterAllLocalTransformationsForAllPairs()
        {
            PhysicsGoIdToLocalSyncs.Clear();
            foreach (var key in ids.Keys())
            {
                SplitterSubscriber subscriber = idToMainGo[ids[key]].gameObject.transform.GetComponent<SplitterSubscriber>();
                GameObject go = idToPhysicsGo[subscriber.gameObject.GetInstanceID()].gameObject;
                SetupLocalTransformSyncCache(subscriber, go);
            }
        }
        private void SetupLocalTransformSyncCache(SplitterSubscriber subscriber, GameObject newGo)
        {
            List<MatchedTransform> Matches = new List<MatchedTransform>();


            for (var i = 0; i < subscriber.GetComponentsInChildren<Transform>().Where(x => x.GetInstanceID() != subscriber.transform.GetInstanceID()).Count(); i++)
            {

                Matches.Add(
                    new MatchedTransform
                    {
                        mainTransform = subscriber.GetComponentsInChildren<Transform>().Where(x => x.GetInstanceID() != subscriber.transform.GetInstanceID()).ToList()[i],
                        physicsTransform = newGo.GetComponentsInChildren<Transform>().Where(x => x.GetInstanceID() != newGo.transform.GetInstanceID()).ToList()[i]
                    }
                );
            }
            PhysicsGoIdToLocalSyncs.Add(newGo.GetInstanceID(), Matches);
        }
        public void UnregisterInScene(SplitterSubscriber subscriber)
        {
            if (!ids.ContainsKey(subscriber.gameObject.GetInstanceID()))
                return;

            var physicsGo = idToPhysicsGo[subscriber.gameObject.GetInstanceID()].gameObject;
            var physicsRigid = idToPhysicsGo[subscriber.gameObject.GetInstanceID()].rigidbody;
            //position
            /*subscriber.GetComponent<Rigidbody>().position =
                transform.TransformPoint(
                    PhysicsAnchorGO.transform.InverseTransformPoint(
                        physicsGo.GetComponent<Rigidbody>().position
                    )
                );*/
            //velocity
            subscriber.GetComponent<Rigidbody>().velocity =
                transform.TransformDirection(
                    PhysicsAnchorGO.transform.InverseTransformDirection(
                        physicsRigid.velocity
                    )

                )
                +
                this.GetUltimatePointVelocity(
                    subscriber.GetComponent<Rigidbody>().position
                );
            //rotation
            /*subscriber.GetComponent<Rigidbody>().rotation =
                this.transform.rotation * (
                    Quaternion.Inverse(PhysicsAnchorGO.transform.rotation)
                    * physicsGo.GetComponent<Rigidbody>().rotation
                 );*/
            //angularVelocity
            subscriber.GetComponent<Rigidbody>().angularVelocity = 
                this.transform.TransformDirection(
                    PhysicsAnchorGO.transform.InverseTransformDirection(
                        physicsRigid.angularVelocity
                    )
                );

            //update constraints
            subscriber.GetComponent<Rigidbody>().constraints = physicsRigid.constraints;

            //enable all monobehaviours in RunInSimulatedSpace
            foreach (var behaviour in subscriber.RunInSimulatedSpace)
            {
                behaviour.enabled = true;
            }

            //send unity messages
            /*foreach (var gobj in physicsGo.transform.GetComponentsInChildren<Transform>().Select(x => x.gameObject))
            {
                gobj.SendMessage("OnRigidContextSubscriberExitsAnchor", new SplitterContextEvent { Anchor = this, SimulatedSubscriber = physicsGo.transform, Subscriber = subscriber, SimulatedAnchor = PhysicsAnchorGO.transform }, SendMessageOptions.DontRequireReceiver);
            }*/

            foreach (var gobj in subscriber.transform.GetComponentsInChildren<Transform>().Select(x => x.gameObject))
            {
                gobj.SendMessage("OnExitAnchor", new SplitterEvent { Anchor = this, SimulatedSubscriber = physicsGo.transform, Subscriber = subscriber, SimulatedAnchor = PhysicsAnchorGO.transform }, SendMessageOptions.DontRequireReceiver);
            }

            idToMainGo.Remove(physicsGo.GetInstanceID());
            idToPhysicsGo.Remove(subscriber.gameObject.GetInstanceID());
            ids.Remove(subscriber.gameObject.GetInstanceID());
            PhysicsGoIdToLocalSyncs.Remove(physicsGo.GetInstanceID());
            Destroy(physicsGo);
        }
        private void OnCollisionEnter(Collision collision)
        {
            /*if (InInMySimulation(collision.transform))
            {
                NegateCollision(collision);
            }*/
        }
        private void OnCollisionStay(Collision collision)
        {
            /*if (InInMySimulation(collision.transform))
            {
                NegateCollision(collision);
            }*/
        }
        private bool InInMySimulation(Transform t)
        {
            //if t is not a subscriber in this anchor,
            //and t is also not a subscriber in an anchor subscriber chain
            if (
                (
                    t.GetComponentInParent<SplitterSubscriber>() == null 
                    || 
                    t.GetComponentInParent<SplitterSubscriber>().Anchor == null 
                    || 
                    (
                        t.GetComponentInParent<SplitterSubscriber>().Anchor.GetInstanceID() != this.GetInstanceID()
                        &&
                        //recursive
                        !InInMySimulation(t.GetComponentInParent<SplitterSubscriber>().Anchor.transform)
                    )
                )
            )
                return false;
            else
                return true;
        }
        private Vector3 GetUltimatePointVelocity(Vector3 WorldPos)
        {
            if (transform.GetComponent<SplitterSubscriber>() == null)
            {
                if (transform.GetComponent<Rigidbody>() != null)
                    return transform.GetComponent<Rigidbody>().GetPointVelocity(WorldPos);
                else
                    return Vector3.zero;
            }
            else
                return transform.GetComponent<SplitterSubscriber>().GetUltimatePointVelocity(WorldPos);
        }

        ContactPoint[] _contactPoints = new ContactPoint[10];
        int _contactCount = 0;
        int _cnt = 0;
        Vector3 _avgContactPoint = Vector3.zero;
        Rigidbody physicsRigidToGetCollision;
        float forceRatio;
        internal void ApplyCollision(SplitterSubscriber subscriber, Collision collision)
        {
            if (!ids.ContainsKey(subscriber.gameObject.GetInstanceID()))
                return;
            
            Vector3 impulse = collision.impulse;
            if (Vector3.Dot(impulse, collision.GetContact(0).normal) < 0f)
                impulse *= -1f;

            physicsRigidToGetCollision = idToPhysicsGo[subscriber.gameObject.GetInstanceID()].rigidbody;

            _contactCount = collision.GetContacts(_contactPoints);

            _avgContactPoint = Vector3.zero;
            for (_cnt = 0; _cnt < _contactCount; _cnt++)
            {
                _avgContactPoint += _contactPoints[_cnt].point;
            }
            _avgContactPoint = _avgContactPoint * 1f / _contactCount;
            physicsRigidToGetCollision.transform.GetComponent<Rigidbody>().AddForceAtPosition(
                PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(impulse)),
                physicsRigidToGetCollision.transform.TransformPoint(subscriber.transform.InverseTransformPoint(_avgContactPoint)),
                ForceMode.Impulse
            );
        }

        internal void NegateCollision(Collision collision)
        {
            Vector3 impulse = collision.impulse;
            if (Vector3.Dot(impulse, collision.GetContact(0).normal) < 0f)
                impulse *= -1f;

            _contactCount = collision.GetContacts(_contactPoints);

            _avgContactPoint = Vector3.zero;
            for (_cnt = 0; _cnt < _contactCount; _cnt++)
            {
                _avgContactPoint += _contactPoints[_cnt].point;
            }
            _avgContactPoint = _avgContactPoint * 1f / _contactCount;
            transform.GetComponent<Rigidbody>().AddForceAtPosition(
                -2f*impulse,
                _avgContactPoint,
                ForceMode.Impulse
            );

            if(collision.rigidbody != null)
            {
                collision.rigidbody.AddForceAtPosition(
                    -2f*impulse,
                    _avgContactPoint,
                    ForceMode.Impulse
                );
            }
            /*var loop = PlayerLoop.GetCurrentPlayerLoop();
            var list = loop.subSystemList;*/
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
            return transform.TransformDirection(PhysicsAnchorGO.transform.InverseTransformDirection(_Sim.rigidbody.velocity));
        }
        internal void ApplyVelocity(Vector3 velocity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.velocity = PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(velocity));
        }

        internal void ApplyPosition(Vector3 position, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.position = PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(position));
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

        internal void ApplyMoveRotation(Quaternion rotation, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.MoveRotation(PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(this.getRotation()) * rotation));
        }

        
        internal void ApplyDrag(float drag, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.drag = drag;
        }

        
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
        internal Vector3 ApplyGetRelativePointVelocity(Vector3 localPoint, SplitterSubscriber subscriber)
        {
            //TODO TEST LOCAL SPACE OR RELATIVE SPACE -- if this doesn't work, try just subscriber.transform.InverseTransformPoint(localPoint)
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return 
                subscriber.transform.TransformDirection(
                    _Sim.gameObject.transform.InverseTransformDirection(
                        _Sim.rigidbody.GetRelativePointVelocity(
                            localPoint
                        )
                    )
                );
        }
        internal Vector3 ApplyGetPointVelocity(Vector3 worldPoint, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return
                transform.TransformDirection(
                    PhysicsAnchorGO.transform.InverseTransformDirection(
                        _Sim.rigidbody.GetPointVelocity(
                            _Sim.gameObject.transform.TransformPoint(
                                subscriber.transform.InverseTransformPoint(
                                    worldPoint
                                )
                            )
                        )
                    )
                );
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
        /*public void FixedUpdate()
        {
            if (Scene != null)
            {
                StartCoroutine(ResetSimulatorFlag());
            }
        }*/

        IEnumerator ResetSimulatorFlag()
        {
            while (true)
            {
                yield return _waitForFixedUpdate;
                mustSimulateBeforeSubscriberUpdate = true;
            }
        }
        public void Simulate()
        {
            PhysicsScene.Simulate(Time.fixedDeltaTime);
        }
        public void Export()
        {
            for (int i = 0; i < idToMainGo.Count(); i++)
            {
                UpdateSubscriberRigidbody(idToMainGo.values[i].gameObject);
            }
        }
        public void Sync()
        {
            for (int i = 0; i < idToMainGo.Count(); i++)
            {
                SyncSubscriberRigidbody(idToMainGo.values[i].gameObject);
            }
        }
        private GoRigid _SimSubscriber;
        private int _physGoOfSubscriberInstanceId;
        private Rigidbody _mainGoRigidbody;
        private int _iterator = 0;

        public void UpdateSubscriber(GameObject mainGo)
        {
            _SimSubscriber = idToPhysicsGo[mainGo.GetInstanceID()];
            mainGo.transform.rotation =
                this.getRotation() * (Quaternion.Inverse(PhysicsAnchorGO.transform.rotation) * _SimSubscriber.rigidbody.rotation);

            mainGo.transform.position =
                this.transform.TransformPoint(
                    PhysicsAnchorGO.transform.InverseTransformPoint(_SimSubscriber.rigidbody.position)
                );
        }
        public void UpdateSubscriberRigidbody(GameObject mainGo)
        {
            _SimSubscriber = idToPhysicsGo[mainGo.GetInstanceID()];
            _physGoOfSubscriberInstanceId = _SimSubscriber.gameObject.GetInstanceID();

            //lazy simulating.  This is called from fixed update, 
            //mustSimulateBeforeSubscriberUpdate is reset on waitforfixedupdate
            //This ensures that the scene if simulated prior to any updates
            /*if (mustSimulateBeforeSubscriberUpdate && !_SimSubscriber.gameObject.GetComponent<Rigidbody>().IsSleeping())
            {
                mustSimulateBeforeSubscriberUpdate = false;
                PhysicsScene.Simulate(Time.fixedDeltaTime);
            }*/

            _mainGoRigidbody = mainGo.GetComponent<Rigidbody>();



            /*_mainGoRigidbody.MoveRotation(
                this.getRotation() * (Quaternion.Inverse(PhysicsAnchorGO.transform.rotation) * _SimSubscriber.rigidbody.rotation)
            );

            _mainGoRigidbody.MovePosition(
                this.transform.TransformPoint(
                    PhysicsAnchorGO.transform.InverseTransformPoint(_SimSubscriber.rigidbody.position)
                )
            );*/

            /*_mainGoRigidbody.AddForce(
                transform.TransformDirection(
                    PhysicsAnchorGO.transform.InverseTransformPoint(
                        _SimSubscriber.rigidbody.position
                    )
                    -
                    transform.InverseTransformPoint(
                        _mainGoRigidbody.position
                    )
                )
                - 
                _mainGoRigidbody.velocity,
                ForceMode.VelocityChange
            );*/

            //cancel
            //_mainGoRigidbody.AddForce(-_mainGoRigidbody.velocity, ForceMode.Acceleration);
            //_mainGoRigidbody.AddTorque(-_mainGoRigidbody.angularVelocity, ForceMode.Acceleration);
            //compensate for anchors' movements
            _mainGoRigidbody.AddForce(
                (
                    //dv
                    (mainGo.GetComponent<SplitterSubscriber>().GetUltimatePointVelocity(_mainGoRigidbody.position) - (_mainGoRigidbody.velocity)) / (Time.fixedDeltaTime)
                ),
                ForceMode.Acceleration
            );

            _iterator = 0;
            //i think this should simply just be done for every transform instead of having an user defined explicit list
            for (_iterator = 0; _iterator < PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId].Count; _iterator++)
            {
                PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId][_iterator].mainTransform.localRotation =
                    PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId][_iterator].physicsTransform.localRotation;
                PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId][_iterator].mainTransform.localPosition =
                    PhysicsGoIdToLocalSyncs[_physGoOfSubscriberInstanceId][_iterator].physicsTransform.localPosition;
            }
        }

        public void SyncSubscriberRigidbody(GameObject mainGo)
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

        public GoRigid GetSubSim(SplitterSubscriber subscriber)
        {
            return idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
        }
        public GameObject GetSim()
        {
            return PhysicsAnchorGO;
        }
        private void OnDestroy()
        {
            if (Scene != null)
                SceneManager.UnloadSceneAsync(Scene.Value);
        }

        void OnEnable()
        {
            SplitterSystem.SplitterSimulate += Simulate;
            SplitterSystem.SplitterPhysicsExport += Export;
            SplitterSystem.SplitterSync += Sync;

            //AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            //AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        void OnDisable()
        {
            SplitterSystem.SplitterSimulate -= Simulate;
            SplitterSystem.SplitterPhysicsExport -= Export;
            SplitterSystem.SplitterSync -= Sync;
            //AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            //AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
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

        private Quaternion getRotation()
        {
            if (body != null)
                return body.rotation;
            else
                return transform.rotation;
        }
        private Vector3 getPosition()
        {
            if (body != null)
                return body.position;
            else
                return transform.position;
        }
        internal Vector3 GetPointVelocity(Vector3 WorldPoint)
        {
            if (body != null)
                return body.GetPointVelocity(WorldPoint);
            else
                return Vector3.zero;
        }
    }
}

