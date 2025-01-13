using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using NoTime.Splitter.Helpers;
using NoTime.Splitter.Internal;
using NoTime.Splitter.Core;

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

        public List<MonoBehaviour> RunInSimulationSpace;

        private PositionalAccuracy accuracy = PositionalAccuracy.High;

        [Tooltip("Render simulated anchor")]
        public bool SimulationVisible = false;
        [Tooltip("Setting SyncTransforms to true will increase physics query accuracy at the cost of computation time.")]
        private bool SyncTransforms;


        private Scene? Scene;
        private PhysicsScene PhysicsScene;
        private Scene MainScene;
        private string SceneName;
        private GameObject PhysicsAnchorGO;
        private SplitterAnchorSimulation PhysicsAnchor;
        private Rigidbody body = null;

        private List<GameObject> subscribers;
        private Dictionary<int, int> ids;
        private Dictionary<int, GoRigid> idToPhysicsGo;
        private Dictionary<int, GoRigid> idToMainGo;
        private Dictionary<int, List<MatchedTransform>> PhysicsGoIdToLocalSyncs;

        private bool visible = true;

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

            if (transform.GetComponent<Rigidbody>() != null)
                body = transform.GetComponent<Rigidbody>();
        }

        
        void Start()
        {
            if (Scene == null)
                CreateAnchorSimulationScene();
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
            foreach (var behaviour in PhysicsAnchorGO.GetComponentsInChildren<Behaviour>().Where(x =>
                !(x.GetType() == typeof(SplitterAnchor))
                &&
                !PhysicsAnchorGO.GetComponent<SplitterAnchor>().RunInSimulationSpace.Any(
                    y =>
                    y.GetInstanceID() == x.GetInstanceID()
                )
            ))
            {
                behaviour.enabled = false;
                Destroy(behaviour);
            }
            //disable all behaviours in RunInSimulatedSpace
            foreach (var behaviour in this.RunInSimulationSpace)
            {
                behaviour.enabled = false;
            }

            PhysicsAnchorGO.AddComponent<SplitterAnchorSimulation>();
            PhysicsAnchor = PhysicsAnchorGO.GetComponent<SplitterAnchorSimulation>();
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
            foreach (SplitterSubscriber subsription in PhysicsAnchorGO.GetComponentsInChildren<SplitterSubscriber>().ToList())
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
                PhysicsAnchorGO.transform.TransformPoint(transform.InverseTransformPoint(subscriber.transform.GetComponent<Rigidbody>().position)),
                PhysicsAnchorGO.transform.rotation * (Quaternion.Inverse(transform.rotation) * subscriber.transform.GetComponent<Rigidbody>().rotation)
            );
            SceneManager.SetActiveScene(MainScene);

            newGo.transform.localScale = subscriber.transform.lossyScale;


            //update subscriber properties
            //velocity? NO
            //angularvelocity? NO
            var subRigid = subscriber.GetComponent<Rigidbody>();
            subRigid.drag = 0f;
            subRigid.angularDrag = 0f;
            //subRigid.mass = 1f;
            subRigid.useGravity = false;
            //max depenetration velocity?
            ////we only want to be kinematic at the lowest level... for now i guess.
            if (newGo.GetComponent<Rigidbody>().isKinematic)
                subscriber.GetComponent<Rigidbody>().isKinematic = false;
            subRigid.freezeRotation = false;
            subRigid.constraints = RigidbodyConstraints.None;
            //collision detection mode?
            //center of mass? NO
            //world center of mass? NO
            //inertiaTensorRotation?
            //inertiaTensor?
            //position - no gets immediately updated
            //rotation - no gets immediately updated
            //interpolation - no, this is only front end
            //solverIterations?
            //sleepThreshold?
            subRigid.maxAngularVelocity = float.PositiveInfinity;
            //solverVelocity Iterations?
            //solverIterationCount?
            

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

            newGo.name = newGo.name + "-Physics";

            subscribers.Add(subscriber.gameObject);
            ids.Add(subscriber.gameObject.GetInstanceID(), newGo.GetInstanceID());
            idToPhysicsGo.Add(subscriber.gameObject.GetInstanceID(), new GoRigid()
            {
                gameObject = newGo.gameObject,
                rigidbody = newGo.transform.GetComponent<Rigidbody>()
            });
            idToMainGo.Add(
                newGo.GetInstanceID(),
                new GoRigid()
                {
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
            foreach (var joint in newGo.GetComponentsInChildren<Joint>())
            {
                Destroy(joint);
            }

            //send UnityMessagesHere
            foreach (var gobj in subscriber.transform.GetComponentsInChildren<Transform>().Select(x => x.gameObject))
            {
                gobj.SendMessage("OnEnterAnchor", new SplitterEvent { Anchor = this, SimulatedSubscriber = newGo.transform, Subscriber = subscriber, SimulatedAnchor = PhysicsAnchorGO.transform }, SendMessageOptions.DontRequireReceiver);
            }

            return newGo;
        }
        private void ReRegisterAllLocalTransformationsForAllPairs()
        {
            PhysicsGoIdToLocalSyncs.Clear();
            foreach (var key in ids.Keys)
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
                //velocity? NO
                //angularvelocity? NO
                subRigid.drag = physicsRigid.drag;
                subRigid.angularDrag = physicsRigid.angularDrag;
                subRigid.mass = physicsRigid.mass;
                subRigid.useGravity = physicsRigid.useGravity;
                //max depenetration velocity?
                subRigid.isKinematic = physicsRigid.isKinematic;
                subRigid.freezeRotation = physicsRigid.freezeRotation;
                subRigid.constraints = physicsRigid.constraints;
                //collision detection mode?
                //center of mass? NO
                //world center of mass? NO
                //inertiaTensorRotation?
                //inertiaTensor?
                //position - no gets immediately updated
                //rotation - no gets immediately updated
                //interpolation - no, this is only front end
                //solverIterations?
                //sleepThreshold?
                subRigid.maxAngularVelocity = physicsRigid.maxAngularVelocity;
                //solverVelocity Iterations?
                //solverIterationCount?
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

        public void OnCollisionEnter(Collision collision)
        {
            if (
                collision.body != null 
                && collision.body.GetComponent<SplitterSubscriber>() != null
                && ids.ContainsKey(collision.body.GetComponent<SplitterSubscriber>().gameObject.GetInstanceID()))
            {
                NegateMyCollision(collision);
            }
        }
        public void OnCollisionStay(Collision collision)
        {

            if (
                collision.body != null
                && collision.body.GetComponent<SplitterSubscriber>() != null
                && ids.ContainsKey(collision.body.GetComponent<SplitterSubscriber>().gameObject.GetInstanceID()))
            {
                NegateMyCollision(collision);
            }
        }

        ContactPoint[] _contactPoints = new ContactPoint[10];
        int _contactCount = 0;
        int _cnt = 0;
        Vector3 _avgContactPoint = Vector3.zero;
        Rigidbody physicsRigidToGetCollision;
        internal void ApplyCollision(SplitterSubscriber subscriber, Collision collision)
        {
            if (!ids.ContainsKey(subscriber.gameObject.GetInstanceID()))
                return;
            
            Vector3 impulse = collision.impulse;
            if (Vector3.Dot(impulse, collision.GetContact(0).normal) < 0f)
                impulse *= -1f;

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
            
            physicsRigidToGetCollision.transform.GetComponent<Rigidbody>().AddForceAtPosition(
                PhysicsAnchorGO.transform.TransformDirection(transform.InverseTransformDirection(impulse)),
                physicsRigidToGetCollision.transform.TransformPoint(subscriber.transform.InverseTransformPoint(_avgContactPoint)),
                ForceMode.Impulse
            );
        }

        internal void NegateMyCollision(Collision collision)
        {
            //Debug.Log("Negating Collision caused by anchor " + gameObject.name + " with " + collision.body.name);
            Vector3 impulse = collision.impulse;
            if (Vector3.Dot(impulse, collision.GetContact(0).normal) < 0f)
            {
                impulse *= -1f;
            }

            if (transform.GetComponent<SplitterSubscriber>())
            {
                var subscriber = transform.GetComponent<SplitterSubscriber>();
                if (subscriber.AppliedPhysics.isKinematic)
                    return;

                _contactCount = collision.GetContacts(_contactPoints);

                _avgContactPoint = Vector3.zero;
                for (_cnt = 0; _cnt < _contactCount; _cnt++)
                {
                    _avgContactPoint += _contactPoints[_cnt].point;
                }
                _avgContactPoint = _avgContactPoint * 1f / _contactCount;
                subscriber.AppliedPhysics.AddForceAtPosition(
                    -impulse,
                    _avgContactPoint,
                    ForceMode.Impulse
                );
            }else if (transform.GetComponent<Rigidbody>())
            {
                var rigid = transform.GetComponent<Rigidbody>();
                if(rigid.isKinematic)
                    return;

                _contactCount = collision.GetContacts(_contactPoints);

                _avgContactPoint = Vector3.zero;
                for (_cnt = 0; _cnt < _contactCount; _cnt++)
                {
                    _avgContactPoint += _contactPoints[_cnt].point;
                }
                _avgContactPoint = _avgContactPoint * 1f / _contactCount;
                rigid.AddForceAtPosition(
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
        internal void ApplyVelocity(Vector3 velocity, SplitterSubscriber subscriber)
        {
            _Sim = idToPhysicsGo[subscriber.gameObject.GetInstanceID()];
            _Sim.rigidbody.velocity = 
                PhysicsAnchorGO.transform.TransformDirection(
                    transform.InverseTransformDirection(velocity)
                ) - GetUltimatePointVelocity(subscriber.AppliedPhysics.position);
        }

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

            if (accuracy.IsMediumOrBetter())
            {
                _psi = 0;
                for (; _psi < subscribers.Count; _psi++)
                {
                    SyncSubscriberRigidbody(subscribers[_psi].gameObject);
                }
            }

        }

        int _hsi;
        public void HardSync()
        {
            /*#if UNITY_2022_2_OR_NEWER
                        PhysicsScene.InterpolateBodies();
            #endif*/



            if (accuracy == PositionalAccuracy.High)
            {

                _hsi = 0;
                for (; _hsi < subscribers.Count; _hsi++)
                {
                    SyncSubscriberTransform(subscribers[_hsi].gameObject);
                }
            }

            if (SyncTransforms)
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

        private void SyncSubscriberChildTransforms(GameObject mainGo)
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
                foreach (GoRigid _gr in idToMainGo.Select(x => x.Value).ToList())
                {
                    UnregisterInScene(_gr.gameObject.GetComponent<SplitterSubscriber>());
                    _gr.gameObject.GetComponent<SplitterSubscriber>().HandleAnchorDestruction(this);
                }
            }
            if (Scene != null)
                SceneManager.UnloadSceneAsync(Scene.Value);
        }

        void OnEnable()
        {
            //we do this so all collision events occur again
            flickerEntryAndExits();

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

        Collider _col;
        int _iFEAE;
        private void flickerEntryAndExits()
        {
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
        int _iRAS;
        private void RemoveAllSubscribers()
        {
            //when game quits, you don't know the order of destruction,
            //so this could be filled with nulls
            subscribers = subscribers.Where(x => x != null).ToList();
            _iRAS = 0;
            for(; _iRAS < subscribers.Count; _iRAS++)
            {
                _removalSub = subscribers[_iRAS].GetComponent<SplitterSubscriber>();
                UnregisterInScene(_removalSub);
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

        internal Transform GetTransform(Vector3 LocalPoint)
        {
            if (body != null)
                return body.transform;
            else
                return transform;
        }
        public Vector3 GetUltimatePointVelocity(Vector3 WorldPos)
        {
            if (transform.GetComponent<SplitterSubscriber>() == null)
            {
                if (transform.GetComponent<Rigidbody>() != null)
                    return transform.GetComponent<Rigidbody>().GetPointVelocity(WorldPos);
                else
                    return Vector3.zero;
            }
            else
                return GetUltimatePointVelocity(WorldPos, transform.GetComponent<SplitterSubscriber>());
        }

        internal static Vector3 GetUltimatePointVelocity(Vector3 WorldPoint, SplitterSubscriber sub)
        {
            if (sub.Anchor == null)
                return sub.transform.GetComponent<Rigidbody>().GetPointVelocity(WorldPoint);

            if (sub.Anchor.transform.GetComponent<SplitterSubscriber>() != null)
                return
                    sub.Anchor.transform.TransformDirection(
                        sub.Anchor.GetSim().transform.InverseTransformDirection(
                            sub.Anchor.GetSubSim(sub).rigidbody.GetPointVelocity(
                                sub.Anchor.GetSim().gameObject.transform.TransformPoint(sub.Anchor.transform.InverseTransformPoint(WorldPoint))
                            )
                        )
                    )
                    +
                    GetUltimatePointVelocity(WorldPoint, sub.Anchor.transform.GetComponent<SplitterSubscriber>());
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
                return sub.transform.GetComponent<Rigidbody>().angularVelocity;

            if (sub.Anchor.transform.GetComponent<SplitterSubscriber>() != null)
                return
                    sub.Anchor.transform.TransformDirection(
                        sub.Anchor.GetSim().transform.InverseTransformDirection(
                            sub.Anchor.GetSubSim(sub).rigidbody.angularVelocity
                        )
                    )
                    +
                    GetUltimateAngularVelocity(sub.Anchor.transform.GetComponent<SplitterSubscriber>());
            if (sub.Anchor.transform.GetComponent<Rigidbody>() != null)
                return
                    sub.Anchor.transform.TransformDirection(
                        sub.Anchor.GetSim().transform.InverseTransformDirection(
                            sub.Anchor.GetSubSim(sub).rigidbody.angularVelocity
                        )
                    )
                    +
                    sub.Anchor.transform.GetComponent<Rigidbody>().angularVelocity;
            else
                return
                    sub.Anchor.transform.TransformDirection(
                        sub.Anchor.GetSim().transform.InverseTransformDirection(
                            sub.Anchor.GetSubSim(sub).rigidbody.angularVelocity
                        )
                    );
        }
    }

    public enum PositionalAccuracy { High, Medium, Low };
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
    }
}
