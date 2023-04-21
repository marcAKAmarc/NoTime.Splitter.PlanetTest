using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoTime.Splitter;

namespace NoTime.Splitter.Demo
{
    public class BoatMovement : SplitterEventListener
    {
        public float maxAngle = 10f;
        public float timeFrame = 5f;

        private float currentJourney = 0f;
        private float goalAngle = 0f;
        private float smoothdamp;
        private bool delay = true;
        private float smoothDamp;
        private void Start()
        {
            StartCoroutine(Delay());
        }
        private WaitForFixedUpdate wait = new WaitForFixedUpdate();
        IEnumerator Delay()
        {
            yield return wait;
            delay = false;
        }
        private void FixedUpdate()
        {
            if (delay)
                return;

            currentJourney += Time.fixedDeltaTime / timeFrame;
            currentJourney = currentJourney % 1f;

            goalAngle = Mathf.Sin(2f * Mathf.PI * currentJourney) * maxAngle;

            //transform.GetComponent<Rigidbody>().MoveRotation(Quaternion.Euler(Vector3.forward * goalAngle));

            var currentAngle = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);
            var currentGoalVelocity = (goalAngle - currentAngle) * Mathf.Deg2Rad / Time.fixedDeltaTime;
            var goalVelocityChange = currentGoalVelocity - transform.GetComponent<Rigidbody>().angularVelocity.z;

            transform.GetComponent<Rigidbody>().AddTorque(Vector3.forward * goalVelocityChange, ForceMode.VelocityChange);

        }

        public override void OnSimulationStart(SplitterEvent evt)
        {
            evt.SimulatedAnchor.GetComponent<BoatMovement>().enabled = false;
        }
    }
}
