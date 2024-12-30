using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NoTime.Splitter.Internal
{
    //This hides this script from the editor menus.
    //This script should never be manually added to a gameobject.
    //This script is added as a component at runtime as needed.
    [AddComponentMenu("")]
    public class SplitterSubscriberSimulated : MonoBehaviour
    {
        public SplitterAnchor Anchor;
        public SplitterSubscriber Authentic;
        [HideInInspector]
        public List<Collider> CurrentAnchorTriggers;

        private List<Component> OnSimCollisionEnterReceivers;
        private List<Component> OnSimCollisionExitReceivers;
        private List<Component> OnSimCollisionStayReceivers;

        private void Awake()
        {
            CurrentAnchorTriggers = new List<Collider>();
        }
        private void Start()
        {
            OnSimCollisionStayReceivers = Authentic.GetComponentsInChildren<Component>().Where(x => HasMethod(x, "OnSimulationCollisionStay")).ToList();
            OnSimCollisionExitReceivers = Authentic.GetComponentsInChildren<Component>().Where(x => HasMethod(x, "OnSimulationCollisionExit")).ToList();
            OnSimCollisionEnterReceivers = Authentic.GetComponentsInChildren<Component>().Where(x => HasMethod(x, "OnSimulationCollisionEnter")).ToList();
        }
        private bool HasMethod(object objectToCheck, string methodName)
        {
            var type = objectToCheck.GetType();
            return type.GetMethod(methodName) != null;
        }
        private void OnTriggerEnter(Collider other)
        {

            //Deactivation Colliders
            if (other.gameObject.GetComponentInParent<SplitterAnchorSimulation>() != null
                && other.gameObject.GetComponentInParent<SplitterAnchorSimulation>().enabled
                && (
                    other.gameObject.GetComponentInParent<SplitterAnchorSimulation>().DeactivateTriggerColliders.Any(x => x.GetInstanceID() == other.GetInstanceID())
                )
            )
            {
                AddTriggerStack(other);
            }
        }
        private void OnTriggerExit(Collider other)
        {

            if (CurrentAnchorTriggers.Count == 0)
                return;

            CleanTriggerStack();

            RemoveFromTriggerStack(other);
            if (CurrentAnchorTriggers.Count == 0)
                Authentic.SimulationExitedAnchor(Anchor);
        }

        private void AddTriggerStack(Collider collider)
        {
            CleanTriggerStack();
            if (CurrentAnchorTriggers.Any(x => x.GetInstanceID() == collider.GetInstanceID()))
                return;
            CurrentAnchorTriggers.Add(collider);
        }

        private void RemoveFromTriggerStack(Collider collider)
        {
            CurrentAnchorTriggers = CurrentAnchorTriggers.Where(x => x.GetInstanceID() != collider.GetInstanceID()).ToList();
        }

        private void CleanTriggerStack()
        {
            if (CurrentAnchorTriggers.Any(x => x == null))
                CurrentAnchorTriggers = CurrentAnchorTriggers.Where(x => x != null).ToList();
        }


        //Pass through events
        private void OnCollisionEnter(Collision collision)
        {
            foreach(var m in OnSimCollisionEnterReceivers)
            {
                m.SendMessage("OnSimulationCollisionEnter", new SplitterEvent
                {
                    Anchor = this.Anchor,
                    SimulatedSubscriber = this.transform,
                    Subscriber = Authentic,
                    SimulatedAnchor = null,
                    Collision = collision
                }, SendMessageOptions.DontRequireReceiver);
            }
            /*Authentic.BroadcastMessage("OnSimulationCollisionEnter", new SplitterEvent
            {
                Anchor = this.Anchor,
                SimulatedSubscriber = this.transform,
                Subscriber = Authentic,
                SimulatedAnchor = null,
                Collision = collision
            }, SendMessageOptions.DontRequireReceiver);*/
            //Debug.Log("splitter Col enter");
            /*Authentic.GetComponentsInChildren<Transform>().ToList().ForEach(x =>
            {
                x.SendMessage("OnSimulationCollisionEnter", new SplitterEvent
                {
                    Anchor = this.Anchor,
                    SimulatedSubscriber = this.transform,
                    Subscriber = Authentic,
                    SimulatedAnchor = null,
                    Collision = collision
                }, SendMessageOptions.DontRequireReceiver);
            });*/
        }

        private void OnCollisionStay(Collision collision)
        {
            //Debug.Log("splitter Col Stay");

            foreach (var m in OnSimCollisionStayReceivers)
            {
                m.SendMessage("OnSimulationCollisionStay", new SplitterEvent
                {
                    Anchor = this.Anchor,
                    SimulatedSubscriber = this.transform,
                    Subscriber = Authentic,
                    SimulatedAnchor = null,
                    Collision = collision
                }, SendMessageOptions.DontRequireReceiver);
            }
            /*Authentic.BroadcastMessage("OnSimulationCollisionStay", new SplitterEvent
            {
                Anchor = this.Anchor,
                SimulatedSubscriber = this.transform,
                Subscriber = Authentic,
                SimulatedAnchor = null,
                Collision = collision
            }, SendMessageOptions.DontRequireReceiver);*/

            /*Authentic.GetComponentsInChildren<Transform>().ToList().ForEach(x =>
            {
                x.SendMessage("OnSimulationCollisionStay", new SplitterEvent
                {
                    Anchor = this.Anchor,
                    SimulatedSubscriber = this.transform,
                    Subscriber = Authentic,
                    SimulatedAnchor = null,
                    Collision = collision
                }, SendMessageOptions.DontRequireReceiver);
            });*/
        }

        private void OnCollisionExit(Collision collision)
        {

            foreach (var m in OnSimCollisionExitReceivers)
            {
                m.SendMessage("OnSimulationCollisionExit", new SplitterEvent
                {
                    Anchor = this.Anchor,
                    SimulatedSubscriber = this.transform,
                    Subscriber = Authentic,
                    SimulatedAnchor = null,
                    Collision = collision
                }, SendMessageOptions.DontRequireReceiver);
            }

            /*Authentic.BroadcastMessage("OnSimulationCollisionExit", new SplitterEvent
            {
                Anchor = this.Anchor,
                SimulatedSubscriber = this.transform,
                Subscriber = Authentic,
                SimulatedAnchor = null,
                Collision = collision
            }, SendMessageOptions.DontRequireReceiver);
            */
            /*Authentic.GetComponentsInChildren<Transform>().ToList().ForEach(x =>
            {
                x.SendMessage("OnSimulationCollisionExit", new SplitterEvent
                {
                    Anchor = this.Anchor,
                    SimulatedSubscriber = this.transform,
                    Subscriber = Authentic,
                    SimulatedAnchor = null,
                    Collision = collision
                }, SendMessageOptions.DontRequireReceiver);
            });*/
        }

    }
}
