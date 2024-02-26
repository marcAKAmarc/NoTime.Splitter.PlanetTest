using System.Collections;
using UnityEngine;

namespace NoTime.Splitter.Demo
{
    public class SpinningPlateOnBoatBehavior : SplitterEventListener
    {

        public Vector3 GoalAngularVelocity;
        public float delay;
        private Rigidbody Rigidbody;

        private bool canUpdatePhysics = false;
        // Start is called before the first frame update
        void Start()
        {
            Rigidbody = transform.GetComponent<Rigidbody>();
            StartCoroutine("Delay");
        }

        IEnumerator Delay()
        {
            yield return new WaitForSeconds(delay);
            canUpdatePhysics = true;
        }

        // Update is called once per frame
        private void FixedUpdate()
        {
            if (!canUpdatePhysics)
                return;
            var velAdd = GoalAngularVelocity - transform.InverseTransformDirection(Rigidbody.angularVelocity * Mathf.Rad2Deg);
            Rigidbody.AddRelativeTorque(velAdd, ForceMode.Acceleration);

        }
    }
}
