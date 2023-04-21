using System.Collections;
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

        private void Awake()
        {
            CurrentAnchorTriggers = new List<Collider>();
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
            CurrentAnchorTriggers = CurrentAnchorTriggers.Where(x => x != null).ToList();
        }


    }
}
