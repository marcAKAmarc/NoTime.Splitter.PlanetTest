using UnityEngine;

namespace NoTime.Splitter
{
    public struct SplitterEvent
    {
        public SplitterAnchor Anchor;
        public Transform SimulatedAnchor;
        public SplitterSubscriber Subscriber;
        public Transform SimulatedSubscriber;

        private string Desc;
        public override string ToString()
        {
            Desc = "SplitterEvent - Anchor: " + Anchor.transform.name + "; Simulated Anchor: " + SimulatedAnchor.name;
            if (Subscriber != null)
                Desc += "; Subscriber: " + Subscriber.transform.name;
            if (SimulatedSubscriber != null)
                Desc += "; Simulated Subscriber: " + SimulatedSubscriber.name;
            return Desc;
        }
    }
    
}
