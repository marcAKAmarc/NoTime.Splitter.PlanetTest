using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NoTime.Splitter.Helpers
{
    public static class PhysicsHelpers
    {
        public static Rigidbody MovePositionVelocity(this Rigidbody body, Vector3 globalPosition)
        {
            body.velocity += ((globalPosition - body.position) / Time.fixedDeltaTime) - body.velocity;
            return body;
        }

        public static Rigidbody MoveRotationVelocity(this Rigidbody body, Vector3 globalRotation)
        {
            body.angularVelocity += (Quaternion.FromToRotation(body.rotation.eulerAngles, globalRotation).eulerAngles * Mathf.Deg2Rad / Time.fixedDeltaTime) - body.angularVelocity;
            return body;
        }

        public static Rigidbody MoveRotationVelocity(this Rigidbody body, Quaternion globalRotation)
        {
            body.angularVelocity += ((Quaternion.Inverse(body.rotation) * globalRotation).eulerAngles * Mathf.Deg2Rad / Time.fixedDeltaTime) - body.angularVelocity;
            return body;
        }
    }
}

