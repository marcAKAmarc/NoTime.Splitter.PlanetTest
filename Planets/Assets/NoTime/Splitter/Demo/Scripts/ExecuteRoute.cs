using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using NoTime.Splitter;
using System.Linq;

namespace NoTime.Splitter.Demo
{
    public class ExecuteRoute : SplitterEventListener
    {
        public Collider PlayTrigger;
        public List<RoutePoint> Route;
        private int CurrentIndex = 0;
        private float currentJourney = 0f;
        private Vector3 smoothPositionJourney;
        private Vector3 smoothRotationJourney;
        private Rigidbody body;

        private bool play = false;

        private void Start()
        {
            body = transform.GetComponent<Rigidbody>();
            thisJourneyTime = (Route[CurrentIndex].speed + Route[(CurrentIndex + 1) % Route.Count].speed / 2f) /
                        (Route[(CurrentIndex + 1) % Route.Count].transform.position - Route[CurrentIndex].transform.position).magnitude;
        }

        private Vector3 nextPos;
        private Quaternion nextRot;
        private float thisJourneyTime;
        private void FixedUpdate()
        {
            if (play)
            {
                if (currentJourney > 1f )
                {
                    CurrentIndex = (CurrentIndex + 1) % Route.Count;
                    currentJourney = 0f;

                    thisJourneyTime = (Route[CurrentIndex].speed + Route[(CurrentIndex + 1) % Route.Count].speed / 2f) /
                        (Route[(CurrentIndex + 1) % Route.Count].transform.position - Route[CurrentIndex].transform.position).magnitude;

                    if (CurrentIndex == 0)
                    {
                        play = false;
                        return;
                    }
                }

                

                currentJourney += Time.fixedDeltaTime * (Mathf.Lerp(Route[CurrentIndex].speed, Route[(CurrentIndex + 1) % Route.Count].speed, currentJourney)) /
                    (Route[(CurrentIndex + 1) % Route.Count].transform.position - Route[CurrentIndex].transform.position).magnitude;


            }

            var newPos = Vector3.SmoothDamp(
                body.position,
                Vector3.Lerp(Route[CurrentIndex].transform.position, Route[(CurrentIndex + 1) % Route.Count].transform.position, currentJourney),
                ref smoothPositionJourney,
                thisJourneyTime
            );
            var goalVelocity = ((newPos - body.position) / Time.fixedDeltaTime);
            body.velocity += goalVelocity - body.velocity;

            body.MoveRotation(

                    SmoothDampQuaternion(
                        body.rotation,
                        Quaternion.Slerp(Route[CurrentIndex].transform.rotation, Route[(CurrentIndex + 1) % Route.Count].transform.rotation, currentJourney),
                        ref smoothRotationJourney,
                        thisJourneyTime
                    )

            );
        }

        public void Play()
        {
            if (play == false)
            {
                play = true;
            }
        }

        public override void OnSimulationStart(SplitterEvent evt)
        {
            evt.SimulatedAnchor.GetComponent<ExecuteRoute>().enabled = false;
        }

        private static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime)
        {
            Vector3 c = current.eulerAngles;
            Vector3 t = target.eulerAngles;
            return Quaternion.Euler(
              Mathf.SmoothDampAngle(c.x, t.x, ref currentVelocity.x, smoothTime),
              Mathf.SmoothDampAngle(c.y, t.y, ref currentVelocity.y, smoothTime),
              Mathf.SmoothDampAngle(c.z, t.z, ref currentVelocity.z, smoothTime)
            );
        }
    }

    [Serializable]
    public class RoutePoint
    {
        public float speed;
        public Transform transform;
    }
}