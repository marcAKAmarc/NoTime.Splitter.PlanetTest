using System.Collections.Generic;
using UnityEngine;

namespace NoTime.Splitter.Internal
{
    //This hides this script from the editor menus.
    //This script should never be manually added to a gameobject.
    //This script is added as a component at runtime as needed.
    [AddComponentMenu("")]
    public class SplitterAnchorSimulation : MonoBehaviour
    {
        public List<Collider> DeactivateTriggerColliders;
        public SplitterAnchor Anchor;
    }
}
