using UnityEngine;

namespace NoTime.Splitter
{
    //We never want to add this directly to a GameObject,
    //so this will keep it out of the component menu
    [AddComponentMenu("")]
    public class SplitterEventListener : MonoBehaviour
    {
        //Event occurs for a subscriber when it enters an anchor
        public virtual void OnEnterAnchor(SplitterEvent evt)
        {

        }

        //Event occurs for a subscriber when it exits an anchor
        public virtual void OnExitAnchor(SplitterEvent evt)
        {

        }

        //Event occurs for anchor when it starts simulating
        public virtual void OnSimulationStart(SplitterEvent evt)
        {

        }
    }
}
