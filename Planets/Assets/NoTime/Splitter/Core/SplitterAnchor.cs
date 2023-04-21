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
            idToPhysicsGo.Add(subscriber.gameObject.GetInstanceID(), newGo);
            idToMainGo.Add(newGo.GetInstanceID(), subscriber.gameObject);

            SetupLocalTransformSyncCache(subscriber, newGo);

            foreach (var gobj in subscriber.transform.GetComponentsInChildren<Transform>().Select(x => x.gameObject))
            {
                gobj.SendMessage("OnEnterAnchor", new SplitterEvent { Anchor = this, SimulatedSubscriber = newGo.transform, Subscriber = subscriber, SimulatedAnchor = PhysicsAnchorGO.transform }, SendMessageOptions.DontRequireReceiver);
            }

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
            

            return newGo;
        }
        private void ReRegisterAllLocalTransformationsForAllPairs()
        {
            PhysicsGoIdToLocalSyncs.Clear();
            foreach (var key in ids.Keys())
            {
                SplitterSubscriber subscriber = idToMainGo[ids[key]].transform.GetComponent<SplitterSubscriber>();
                GameObject go = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
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

            var physicsGo = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];

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
                        physicsGo.GetComponent<Rigidbody>().velocity
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
                        physicsGo.GetComponent<Rigidbody>().angularVelocity
                    )
                );

            //update constraints
            subscriber.GetComponent<Rigidbody>().constraints = physicsGo.GetComponent<Rigidbody>().constraints;

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
        GameObject physicsGoToGetCollision;
        float forceRatio;
        internal void ApplyCollision(SplitterSubscriber subscriber, Collision collision)
        {
            if (!ids.ContainsKey(subscriber.gameObject.GetInstanceID()))
                return;
            
            Vector3 impulse = collision.impulse;
            if (Vector3.Dot(impulse, collision.GetContact(0).normal) < 0f)
                impulse *= -1f;

            physicsGoToGetCollision = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];

            _contactCount = collision.GetContacts(_contactPoints);

            _avgContactPoint = Vector3.zero;
            for (_cnt = 0; _cnt < _contactCount; _cnt++)
            {
                _avgContactPoint += _contactPoints[_cnt].point;
            }
            _avgContactPoint = _avgContactPoint * 1f / _contactCount;
            physicsGoToGetCollision.transform.GetComponent<Rigidbody>().AddForceAtPosition(
                PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(impulse)),
                physicsGoToGetCollision.transform.TransformPoint(subscriber.transform.InverseTransformPoint(_avgContactPoint)),
                ForceMode.Impulse
            );
        }

        
        
        internal void ApplyAddForce(Vector3 force, ForceMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().AddForce(
                PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(force)),
                mode
            );
        }
        
        internal void ApplyAddForceAtPosition(Vector3 force, Vector3 pos, ForceMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().AddForceAtPosition(
                PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(force)),
                PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(pos)),
                mode
            );
        }
        
        internal void ApplyAddRelativeForce(Vector3 relForce, ForceMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().AddRelativeForce(
                relForce,
                mode
            );
        }
        
        internal void ApplyAddRelativeTorque(Vector3 relTorque, ForceMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().AddRelativeTorque(
                relTorque,
                mode
            );
        }
        
        internal void ApplyAddTorque(Vector3 torque, ForceMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().AddTorque(
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
                
                Vector3 _simApplicationPosition = _Sim.GetComponent<Rigidbody>().ClosestPointOnBounds(_simExplosionPosition);
                if ((_simApplicationPosition - _Sim.transform.position).sqrMagnitude >
                    (_simExplosionPosition - _Sim.transform.position).sqrMagnitude
                )
                    _simApplicationPosition = _Sim.transform.position;

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
                _simApplicationPosition = _Sim.GetComponent<Rigidbody>().ClosestPointOnBounds(_simExplosionPosition);
                if ((_simApplicationPosition - _Sim.transform.position).sqrMagnitude >
                    (_simExplosionPosition - _Sim.transform.position).sqrMagnitude
                )
                    _simApplicationPosition = _Sim.transform.position;

                Vector3 _simApplicationDirection =
                    (
                        _simApplicationPosition -
                        (
                            _simExplosionPosition
                        )
                    ).normalized;
                    

                _Sim.GetComponent<Rigidbody>().AddForceAtPosition(scaledExplosionForce * _simApplicationDirection, _simApplicationPosition, mode);
            }
            else
            {
                _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
                _Sim.transform.GetComponent<Rigidbody>().AddExplosionForce(
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
            return transform.TransformDirection(PhysicsAnchorGO.transform.InverseTransformDirection(_Sim.transform.GetComponent<Rigidbody>().velocity));
        }
        internal void ApplyVelocity(Vector3 velocity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().velocity = PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(velocity));
        }

        internal void ApplyPosition(Vector3 position, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().position = PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(position));
        }

        
        internal void ApplyMovePosition(Vector3 position, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().MovePosition(PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(position)));
        }

        internal Quaternion GetApplyRotation( SplitterSubscriber subscriber)
        {
            //Debug.Log(this.name + " applying rotation: " + rotation.eulerAngles.ToString("G6"));
            //Debug.Log(this.name + " should be calculated to: " + (PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(transform.rotation) * rotation)).eulerAngles.ToString("G6"));
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return transform.rotation * (Quaternion.Inverse(PhysicsAnchorGO.transform.rotation) * _Sim.transform.GetComponent<Rigidbody>().rotation);
            //Debug.Log(this.name + " new rotation: " + _Sim.transform.GetComponent<Rigidbody>().rotation.eulerAngles.ToString("G6"));
        }
        internal void ApplyRotation(Quaternion rotation, SplitterSubscriber subscriber)
        {
            //Debug.Log(this.name + " applying rotation: " + rotation.eulerAngles.ToString("G6"));
            //Debug.Log(this.name + " should be calculated to: " + (PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(transform.rotation) * rotation)).eulerAngles.ToString("G6"));
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().rotation = PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(transform.rotation) * rotation);
            //Debug.Log(this.name + " new rotation: " + _Sim.transform.GetComponent<Rigidbody>().rotation.eulerAngles.ToString("G6"));
        }

        
        internal void ApplyMoveRotation(Quaternion rotation, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            Debug.Log("Before rotation " + _Sim.transform.GetComponent<Rigidbody>().rotation.ToString("G10"));
            Debug.Log("Here is what it should be translated back : " + GetApplyRotation(subscriber).eulerAngles.ToString("G10"));
            /*Debug.Log("Input " + rotation.eulerAngles.ToString("G6"));
            Debug.Log("Difference " + Quaternion.FromToRotation(_Sim.transform.GetComponent<Rigidbody>().rotation*Vector3.forward, rotation*Vector3.forward).eulerAngles.ToString("G6"));*/
            var tmprot = _Sim.transform.GetComponent<Rigidbody>().rotation;
            _Sim.transform.GetComponent<Rigidbody>().MoveRotation(PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(transform.rotation) * rotation));
            Debug.Log("AFTER rotation " + _Sim.transform.GetComponent<Rigidbody>().rotation.ToString("G10"));
            Debug.Log("Here is what it should be translated back : " + GetApplyRotation(subscriber).eulerAngles.ToString("G10"));
            if (tmprot != _Sim.transform.GetComponent<Rigidbody>().rotation)
            {
                Debug.Log("DIFFERENT");
            }
            else
            {
                Debug.Log("SAME");
            }
        }

        
        internal void ApplyDrag(float drag, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().drag = drag;
        }

        
        internal void ApplyMass(float mass, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().mass = mass;
        }
        
        internal void ApplyMaxDepenetrationVelocity(float max, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().maxDepenetrationVelocity = max;
        }

        internal Vector3 GetAngularVelocity(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return transform.TransformDirection(
                PhysicsAnchorGO.transform.InverseTransformDirection(
                    _Sim.transform.GetComponent<Rigidbody>().angularVelocity
                )
             );
        }
        internal void ApplyAngularVelocity(Vector3 velocity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().angularVelocity = PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(velocity));
        }

        internal void ApplyAngularDrag(float angularDrag, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().angularDrag = angularDrag;
        }

        internal void ApplyUseGravity(bool useGravity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().useGravity = useGravity;
        }

        internal void ApplyFreezeRotation(bool freezeRotation, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().freezeRotation = freezeRotation;
        }
        internal void ApplyConstraints(RigidbodyConstraints constraints, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().constraints = constraints;
        }

        internal RigidbodyConstraints ApplyGetConstraints(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return _Sim.transform.GetComponent<Rigidbody>().constraints;
        }
        private GameObject _Sim;
        internal void ApplyCollisionDetectionMode(CollisionDetectionMode mode, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().collisionDetectionMode = mode;
        }
        internal void ApplyCenterOfMass(Vector3 centerOfMass, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().centerOfMass = centerOfMass;
        }
        internal void ApplyInertiaTensorRotation(Quaternion rotation, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().inertiaTensorRotation = PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(transform.rotation) * rotation);
        }
        internal void ApplyInertiaTensor(Vector3 inertiaTensor, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().inertiaTensor = inertiaTensor;
        }
        internal void ApplyInterpolation(RigidbodyInterpolation interpolation, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().interpolation = interpolation;
        }
        internal void ApplySolverIterations(int solverIterations, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().solverIterations = solverIterations;
        }
        internal void ApplySleepThreshold(float sleepThreshold, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().sleepThreshold = sleepThreshold;
        }
        internal void ApplyMaxAngularVelocity(float maxAngularVelocity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().maxAngularVelocity = maxAngularVelocity;
        }
        internal void ApplySolverVelocityIterations(int solverVelocityIterations, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().solverVelocityIterations = solverVelocityIterations;
        }
        internal void ApplySetDensity(float density, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().SetDensity(density);
        }
        internal void ApplySleep(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().Sleep();
        }
        internal void ApplyWakeUp(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().WakeUp();
        }
        internal void ApplyResetCenterOfMass(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().ResetCenterOfMass();
        }

        internal void ApplyResetInertiaTensor(SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.transform.GetComponent<Rigidbody>().ResetInertiaTensor();
        }
        internal Vector3 ApplyGetRelativePointVelocity(Vector3 localPoint, SplitterSubscriber subscriber)
        {
            //TODO TEST LOCAL SPACE OR RELATIVE SPACE -- if this doesn't work, try just subscriber.transform.InverseTransformPoint(localPoint)
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            return 
                subscriber.transform.TransformDirection(
                    _Sim.transform.InverseTransformDirection(
                        _Sim.transform.GetComponent<Rigidbody>().GetRelativePointVelocity(
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
                        _Sim.transform.GetComponent<Rigidbody>().GetPointVelocity(
                            _Sim.transform.TransformPoint(
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
                    _Sim.transform.InverseTransformPoint(
                        _Sim.transform.GetComponent<Rigidbody>().ClosestPointOnBounds(
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
            _Sim.GetComponent<Rigidbody>().isKinematic = value;
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

        private GameObject _physGoOfSubscriber;
        private int _physGoOfSubscriberInstanceId;
        private Rigidbody _mainGoRigidbody;
        private int _iterator = 0;
        public void UpdateSubscriberRigidbody(GameObject mainGo)
        {

            _physGoOfSubscriber = idToPhysicsGo[mainGo.GetInstanceID()];
            _physGoOfSubscriberInstanceId = _physGoOfSubscriber.GetInstanceID();

            //lazy simulating.  This is called from fixed update, 
            //mustSimulateBeforeSubscriberUpdate is reset on waitforfixedupdate
            //This ensures that the scene if simulated prior to any updates
            if (mustSimulateBeforeSubscriberUpdate && !_physGoOfSubscriber.GetComponent<Rigidbody>().IsSleeping())
            {
                mustSimulateBeforeSubscriberUpdate = false;
                PhysicsScene.Simulate(Time.fixedDeltaTime);
            }
            
            _physGoOfSubscriber = idToPhysicsGo[mainGo.GetInstanceID()];
            _mainGoRigidbody = mainGo.GetComponent<Rigidbody>();

            _mainGoRigidbody.MoveRotation(
                this.transform.rotation * (Quaternion.Inverse(PhysicsAnchorGO.transform.rotation) * _physGoOfSubscriber.transform.rotation)
            );

            _mainGoRigidbody.MovePosition(
                this.transform.TransformPoint(
                    PhysicsAnchorGO.transform.InverseTransformPoint(_physGoOfSubscriber.transform.position)
                )
            );

            //compensate for anchors' movements
            mainGo.GetComponent<Rigidbody>().AddForce(
                (
                    //dv
                    (mainGo.GetComponent<SplitterSubscriber>().GetUltimatePointVelocity(mainGo.transform.position) - (_mainGoRigidbody.velocity)) / (Time.fixedDeltaTime)
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

        public GameObject GetSubSim(SplitterSubscriber subscriber)
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

#if UNITY_EDITOR
        void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        public void OnBeforeAssemblyReload()
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
        }
#endif
    }
}
