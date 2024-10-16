using System;
using UnityEngine;

namespace NoTime.Splitter.Core
{
    //
    // Summary:
    //     Control of an object's position through physics simulation.
    public class AppliedPhysics
    {
        private Rigidbody body;
        private SplitterSubscriber subscriber;

        private SplitterAnchor anchor
        {
            get
            {
                return subscriber.Anchor;
            }
        }

        public AppliedPhysics(SplitterSubscriber _subscriber, Rigidbody _body)
        {
            subscriber = _subscriber;
            body = _body;
        }

        //
        // Summary:
        //     The velocity vector of the rigidbody. It represents the rate of change of Rigidbody
        //     position.
        public Vector3 velocity
        {
            get
            {
                if (!subscriber.Simulating())
                    return body.velocity;
                else
                    return anchor.GetVelocity(subscriber);
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.velocity = value;
                }
                else
                {
                    //body.velocity = value;
                    anchor.ApplyVelocity(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The angular velocity vector of the rigidbody measured in radians per second.
        public Vector3 angularVelocity
        {
            get
            {
                if (!subscriber.Simulating())
                    return body.angularVelocity;
                else
                    return anchor.GetAngularVelocity(subscriber);
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.angularVelocity = value;
                }
                else
                {
                    //body.angularVelocity = value;
                    anchor.ApplyAngularVelocity(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The drag of the object.
        public float drag
        {
            get
            {
                return body.drag;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.drag = value;
                }
                else
                {
                    //body.drag = value;
                    anchor.ApplyDrag(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The angular drag of the object.
        public float angularDrag
        {
            get
            {
                return body.angularDrag;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.angularDrag = value;
                }
                else
                {
                    //body.angularDrag = value;
                    anchor.ApplyAngularDrag(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The mass of the rigidbody.
        public float mass
        {
            get
            {
                return body.mass;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.mass = value;
                }
                else
                {
                    body.mass = value;
                    anchor.ApplyMass(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     Controls whether gravity affects this rigidbody.
        public bool useGravity
        {
            get
            {
                return body.useGravity;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.useGravity = value;
                }
                else
                {
                    //body.useGravity = value;
                    anchor.ApplyUseGravity(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     Maximum velocity of a rigidbody when moving out of penetrating state.
        public float maxDepenetrationVelocity
        {
            get
            {
                return body.maxDepenetrationVelocity;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.maxDepenetrationVelocity = value;
                }
                else
                {
                    body.maxDepenetrationVelocity = value;
                    anchor.ApplyMaxDepenetrationVelocity(value, subscriber);
                }
            }
        }

        // Summary:
        //     Controls whether physics affects the rigidbody.
        public bool isKinematic
        {
            get
            {
                return body.isKinematic;
            }
            set
            {
                if (!subscriber.Simulating())
                    body.isKinematic = value;
                else
                {
                    //body.isKinematic = value;
                    anchor.ApplyIsKinematic(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     Controls whether physics will change the rotation of the object.
        public bool freezeRotation
        {
            get
            {
                return body.freezeRotation;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.freezeRotation = value;
                }
                else
                {
                    //body.freezeRotation = value;
                    anchor.ApplyFreezeRotation(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     Controls which degrees of freedom are allowed for the simulation of this Rigidbody.
        public RigidbodyConstraints constraints
        {
            get
            {
                if (!subscriber.Simulating())
                    return body.constraints;
                else
                    return anchor.ApplyGetConstraints(subscriber);
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.constraints = value;
                }
                else
                {
                    anchor.ApplyConstraints(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The Rigidbody's collision detection mode.
        public CollisionDetectionMode collisionDetectionMode
        {
            get
            {
                return body.collisionDetectionMode;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.collisionDetectionMode = value;
                }
                else
                {
                    body.collisionDetectionMode = value;
                    anchor.ApplyCollisionDetectionMode(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The center of mass relative to the transform's origin.
        public Vector3 centerOfMass
        {
            get
            {
                return body.centerOfMass;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.centerOfMass = value;
                }
                else
                {
                    body.centerOfMass = value;
                    anchor.ApplyCenterOfMass(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The center of mass of the rigidbody in world space (Read Only).
        public Vector3 worldCenterOfMass
        {
            get
            {
                return body.centerOfMass;
            }
        }

        //
        // Summary:
        //     The rotation of the inertia tensor.
        public Quaternion inertiaTensorRotation
        {
            get
            {
                return body.inertiaTensorRotation;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.inertiaTensorRotation = value;
                    anchor.ApplyInertiaTensorRotation(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The inertia tensor of this body, defined as a diagonal matrix in a reference
        //     frame positioned at this body's center of mass and rotated by Rigidbody.inertiaTensorRotation.
        public Vector3 inertiaTensor
        {
            get
            {
                return body.inertiaTensor;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.inertiaTensor = value;
                }
                else
                {
                    body.inertiaTensor = value;
                    anchor.ApplyInertiaTensor(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     Should collision detection be enabled? (By default always enabled).
        //public extern bool detectCollisions
        //{
        //    [MethodImpl(MethodImplOptions.InternalCall)]
        //    get;
        //    [MethodImpl(MethodImplOptions.InternalCall)]
        //    set;
        //}

        //
        // Summary:
        //     The position of the rigidbody.
        public Vector3 position
        {
            get
            {
                if (!subscriber.Simulating())
                    return body.position;
                else
                    return anchor.GetPosition(subscriber);
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.position = value;
                }
                else
                {
                    anchor.ApplyPosition(value, subscriber);
                    //must do the immediate update with move position / move rotation
                    body.position =
                        anchor.AnchorPointToWorldPoint(subscriber.GetSimulationBody().position);
                }
            }
        }

        //
        // Summary:
        //     The rotation of the Rigidbody.
        public Quaternion rotation
        {
            get
            {
                if (!subscriber.Simulating())
                {
                    return body.rotation;
                }
                else
                {
                    return anchor.GetApplyRotation(subscriber);
                }
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.rotation = value;
                }
                else
                {
                    anchor.ApplyRotation(value, subscriber);
                    body.rotation = anchor.GetApplyRotation(subscriber);
                }
            }
        }

        //
        // Summary:
        //     Interpolation allows you to smooth out the effect of running physics at a fixed
        //     frame rate.
        public RigidbodyInterpolation interpolation
        {
            get
            {
                return body.interpolation;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.interpolation = value;
                }
                else
                {
                    body.interpolation = value;
                    //anchor.ApplyInterpolation(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The solverIterations determines how accurately Rigidbody joints and collision
        //     contacts are resolved. Overrides Physics.defaultSolverIterations. Must be positive.
        public int solverIterations
        {
            get
            {
                return body.solverIterations;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.solverIterations = value;
                    anchor.ApplySolverIterations(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The mass-normalized energy threshold, below which objects start going to sleep.
        public float sleepThreshold
        {
            get
            {
                return body.sleepThreshold;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.sleepThreshold = value;
                    anchor.ApplySleepThreshold(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The maximimum angular velocity of the rigidbody measured in radians per second.
        //     (Default 7) range { 0, infinity }.
        public float maxAngularVelocity
        {
            get
            {
                return body.maxAngularVelocity;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.maxAngularVelocity = value;
                }
                else
                {
                    //body.maxAngularVelocity = value;
                    anchor.ApplyMaxAngularVelocity(value, subscriber);
                }
            }
        }


        //
        // Summary:
        //     The solverVelocityIterations affects how how accurately Rigidbody joints and
        //     collision contacts are resolved. Overrides Physics.defaultSolverVelocityIterations.
        //     Must be positive.
        public int solverVelocityIterations
        {
            get
            {
                return body.solverVelocityIterations;
            }
            set
            {
                if (!subscriber.Simulating())
                {
                    body.solverVelocityIterations = value;
                }
                else
                {
                    body.solverVelocityIterations = value;
                    anchor.ApplySolverVelocityIterations(value, subscriber);
                }
            }
        }

        //
        // Summary:
        //     The linear velocity below which objects start going to sleep. (Default 0.14)
        //     range { 0, infinity }.
        /*[Obsolete("The sleepVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.", true)]
        public float sleepVelocity
        {
            get
            {
                return 0f;
            }
            set
            {
            }
        }

        //
        // Summary:
        //     The angular velocity below which objects start going to sleep. (Default 0.14)
        //     range { 0, infinity }.
        [Obsolete("The sleepAngularVelocity is no longer supported. Use sleepThreshold to specify energy.", true)]
        public float sleepAngularVelocity
        {
            get
            {
                return 0f;
            }
            set
            {
            }
        }

        //
        // Summary:
        //     Force cone friction to be used for this rigidbody.
        [Obsolete("Cone friction is no longer supported.", true)]
        public bool useConeFriction
        {
            get
            {
                return false;
            }
            set
            {
            }
        }*/

        public int solverIterationCount
        {
            get
            {
                return solverIterations;
            }
            set
            {
                solverIterations = value;
            }
        }

        public int solverVelocityIterationCount
        {
            get
            {
                return solverVelocityIterations;
            }
            set
            {
                solverVelocityIterations = value;
            }
        }

        //
        // Summary:
        //     Sets the mass based on the attached colliders assuming a constant density.
        //
        // Parameters:
        //   density:
        public void SetDensity(float density)
        {
            if (!subscriber.Simulating())
            {
                body.SetDensity(density);
            }
            else
            {
                body.SetDensity(density);
                anchor.ApplySetDensity(density, subscriber);
            }
        }

        //
        // Summary:
        //     Moves the kinematic Rigidbody towards position.
        //
        // Parameters:
        //   position:
        //     Provides the new position for the Rigidbody object.
        public void MovePosition(Vector3 position)
        {
            if (!subscriber.Simulating())
                body.MovePosition(position);
            else
            {
                anchor.ApplyMovePosition(position, subscriber);
                //must do the immediate update with move position / move rotation
                body.MovePosition(
                    anchor.AnchorPointToWorldPoint(subscriber.GetSimulationBody().position)
                );
            }
        }

        //
        // Summary:
        //     Rotates the rigidbody to rotation.
        //
        // Parameters:
        //   rot:
        //     The new rotation for the Rigidbody.
        public void MoveRotation(Quaternion rotation)
        {
            if (!subscriber.Simulating())
                body.MoveRotation(rotation);
            else
            {
                anchor.ApplyMoveRotation(rotation, subscriber);
                body.MoveRotation(
                    anchor.TranslateAnchorRotationToWorldRotation(subscriber.GetSimulationBody().rotation)
                );
            }
        }

        //
        // Summary:
        //     Forces a rigidbody to sleep at least one frame.
        public void Sleep()
        {
            if (!subscriber.Simulating())
            {
                body.Sleep();
            }
            else
            {
                body.Sleep();
                anchor.ApplySleep(subscriber);
            }
        }

        //
        // Summary:
        //     Is the rigidbody sleeping?
        public bool IsSleeping()
        {
            return body.IsSleeping();
        }

        //
        // Summary:
        //     Forces a rigidbody to wake up.
        public void WakeUp()
        {
            if (!subscriber.Simulating())
            {
                body.WakeUp();
            }
            else
            {
                body.WakeUp();
                anchor.ApplyWakeUp(subscriber);
            }
        }

        //
        // Summary:
        //     Reset the center of mass of the rigidbody.
        public void ResetCenterOfMass()
        {
            if (!subscriber.Simulating())
            {
                body.ResetCenterOfMass();
            }
            else
            {
                body.ResetCenterOfMass();
                anchor.ApplyResetCenterOfMass(subscriber);
            }
        }

        //
        // Summary:
        //     Reset the inertia tensor value and rotation.
        public void ResetInertiaTensor()
        {
            if (!subscriber.Simulating())
            {
                body.ResetInertiaTensor();
            }
            else
            {
                body.ResetInertiaTensor();
                anchor.ApplyResetInertiaTensor(subscriber);
            }
        }

        //
        // Summary:
        //     The velocity relative to the rigidbody at the point relativePoint.
        //
        // Parameters:
        //   relativePoint:
        public Vector3 GetRelativePointVelocity(Vector3 relativePoint)
        {
            if (!subscriber.Simulating())
            {
                return body.GetRelativePointVelocity(relativePoint);
            }
            else
            {
                return anchor.ApplyGetRelativePointVelocity(relativePoint, subscriber);
            }
        }

        //
        // Summary:
        //     The velocity of the rigidbody at the point worldPoint in global space.
        //
        // Parameters:
        //   worldPoint:
        public Vector3 GetPointVelocity(Vector3 worldPoint)
        {
            if (!subscriber.Simulating())
            {
                return body.GetPointVelocity(worldPoint);
            }
            else
            {
                return anchor.ApplyGetPointVelocity(worldPoint, subscriber);
            }
        }

        //
        // Summary:
        //     Adds a force to the Rigidbody.
        //
        // Parameters:
        //   force:
        //     Force vector in world coordinates.
        //
        //   mode:
        //     Type of force to apply.
        public void AddForce(Vector3 force, ForceMode mode)
        {
            if (!subscriber.Simulating())
            {
                body.AddForce(force, mode);
            }
            else
            {
                anchor.ApplyAddForce(force, mode, subscriber);
            }
        }

        //
        // Summary:
        //     Adds a force to the Rigidbody.
        //
        // Parameters:
        //   force:
        //     Force vector in world coordinates.
        //
        //   mode:
        //     Type of force to apply.
        public void AddForce(Vector3 force)
        {
            AddForce(force, ForceMode.Force);
        }

        //
        // Summary:
        //     Adds a force to the Rigidbody.
        //
        // Parameters:
        //   x:
        //     Size of force along the world x-axis.
        //
        //   y:
        //     Size of force along the world y-axis.
        //
        //   z:
        //     Size of force along the world z-axis.
        //
        //   mode:
        //     Type of force to apply.
        public void AddForce(float x, float y, float z, ForceMode mode)
        {
            AddForce(new Vector3(x, y, z), mode);
        }

        //
        // Summary:
        //     Adds a force to the Rigidbody.
        //
        // Parameters:
        //   x:
        //     Size of force along the world x-axis.
        //
        //   y:
        //     Size of force along the world y-axis.
        //
        //   z:
        //     Size of force along the world z-axis.
        //
        //   mode:
        //     Type of force to apply.
        public void AddForce(float x, float y, float z)
        {
            AddForce(new Vector3(x, y, z), ForceMode.Force);
        }

        //
        // Summary:
        //     Adds a force to the rigidbody relative to its coordinate system.
        //
        // Parameters:
        //   force:
        //     Force vector in local coordinates.
        //
        //   mode:
        //     Type of force to apply.
        public void AddRelativeForce(Vector3 force, ForceMode mode)
        {
            if (!subscriber.Simulating())
                body.AddRelativeForce(force, mode);
            else
            {
                anchor.ApplyAddRelativeForce(force, mode, subscriber);
            }
        }

        //
        // Summary:
        //     Adds a force to the rigidbody relative to its coordinate system.
        //
        // Parameters:
        //   force:
        //     Force vector in local coordinates.
        //
        //   mode:
        //     Type of force to apply.
        public void AddRelativeForce(Vector3 force)
        {
            AddRelativeForce(force, ForceMode.Force);
        }

        //
        // Summary:
        //     Adds a force to the rigidbody relative to its coordinate system.
        //
        // Parameters:
        //   x:
        //     Size of force along the local x-axis.
        //
        //   y:
        //     Size of force along the local y-axis.
        //
        //   z:
        //     Size of force along the local z-axis.
        //
        //   mode:
        //     Type of force to apply.
        public void AddRelativeForce(float x, float y, float z, ForceMode mode)
        {
            AddRelativeForce(new Vector3(x, y, z), mode);
        }

        //
        // Summary:
        //     Adds a force to the rigidbody relative to its coordinate system.
        //
        // Parameters:
        //   x:
        //     Size of force along the local x-axis.
        //
        //   y:
        //     Size of force along the local y-axis.
        //
        //   z:
        //     Size of force along the local z-axis.
        //
        //   mode:
        //     Type of force to apply.
        public void AddRelativeForce(float x, float y, float z)
        {
            AddRelativeForce(new Vector3(x, y, z), ForceMode.Force);
        }

        //
        // Summary:
        //     Adds a torque to the rigidbody.
        //
        // Parameters:
        //   torque:
        //     Torque vector in world coordinates.
        //
        //   mode:
        //     The type of torque to apply.
        public void AddTorque(Vector3 torque, ForceMode mode)
        {
            if (!subscriber.Simulating())
                body.AddTorque(torque, mode);
            else
            {
                anchor.ApplyAddTorque(torque, mode, subscriber);
            }
        }

        //
        // Summary:
        //     Adds a torque to the rigidbody.
        //
        // Parameters:
        //   torque:
        //     Torque vector in world coordinates.
        //
        //   mode:
        //     The type of torque to apply.
        public void AddTorque(Vector3 torque)
        {
            AddTorque(torque, ForceMode.Force);
        }

        //
        // Summary:
        //     Adds a torque to the rigidbody.
        //
        // Parameters:
        //   x:
        //     Size of torque along the world x-axis.
        //
        //   y:
        //     Size of torque along the world y-axis.
        //
        //   z:
        //     Size of torque along the world z-axis.
        //
        //   mode:
        //     The type of torque to apply.
        public void AddTorque(float x, float y, float z, ForceMode mode)
        {
            AddTorque(new Vector3(x, y, z), mode);
        }

        //
        // Summary:
        //     Adds a torque to the rigidbody.
        //
        // Parameters:
        //   x:
        //     Size of torque along the world x-axis.
        //
        //   y:
        //     Size of torque along the world y-axis.
        //
        //   z:
        //     Size of torque along the world z-axis.
        //
        //   mode:
        //     The type of torque to apply.
        public void AddTorque(float x, float y, float z)
        {
            AddTorque(new Vector3(x, y, z), ForceMode.Force);
        }

        //
        // Summary:
        //     Adds a torque to the rigidbody relative to its coordinate system.
        //
        // Parameters:
        //   torque:
        //     Torque vector in local coordinates.
        //
        //   mode:
        //     Type of force to apply.
        public void AddRelativeTorque(Vector3 torque, ForceMode mode)
        {
            if (!subscriber.Simulating())
                body.AddRelativeTorque(torque, mode);
            else
            {
                anchor.ApplyAddRelativeTorque(torque, mode, subscriber);
            }
        }

        //
        // Summary:
        //     Adds a torque to the rigidbody relative to its coordinate system.
        //
        // Parameters:
        //   torque:
        //     Torque vector in local coordinates.
        //
        //   mode:
        //     Type of force to apply.
        public void AddRelativeTorque(Vector3 torque)
        {
            AddRelativeTorque(torque, ForceMode.Force);
        }

        //
        // Summary:
        //     Adds a torque to the rigidbody relative to its coordinate system.
        //
        // Parameters:
        //   x:
        //     Size of torque along the local x-axis.
        //
        //   y:
        //     Size of torque along the local y-axis.
        //
        //   z:
        //     Size of torque along the local z-axis.
        //
        //   mode:
        //     Type of force to apply.
        public void AddRelativeTorque(float x, float y, float z, ForceMode mode)
        {
            AddRelativeTorque(new Vector3(x, y, z), mode);
        }

        //
        // Summary:
        //     Adds a torque to the rigidbody relative to its coordinate system.
        //
        // Parameters:
        //   x:
        //     Size of torque along the local x-axis.
        //
        //   y:
        //     Size of torque along the local y-axis.
        //
        //   z:
        //     Size of torque along the local z-axis.
        //
        //   mode:
        //     Type of force to apply.
        public void AddRelativeTorque(float x, float y, float z)
        {
            AddRelativeTorque(x, y, z, ForceMode.Force);
        }

        //
        // Summary:
        //     Applies force at position. As a result this will apply a torque and force on
        //     the object.
        //
        // Parameters:
        //   force:
        //     Force vector in world coordinates.
        //
        //   position:
        //     Position in world coordinates.
        //
        //   mode:
        //     Type of force to apply.
        public void AddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode)
        {
            if (!subscriber.Simulating())
                body.AddForceAtPosition(force, position, mode);
            else
                anchor.ApplyAddForceAtPosition(force, position, mode, subscriber);
        }

        //
        // Summary:
        //     Applies force at position. As a result this will apply a torque and force on
        //     the object.
        //
        // Parameters:
        //   force:
        //     Force vector in world coordinates.
        //
        //   position:
        //     Position in world coordinates.
        //
        //   mode:
        //     Type of force to apply.
        public void AddForceAtPosition(Vector3 force, Vector3 position)
        {
            AddForceAtPosition(force, position, ForceMode.Force);
        }

        //
        // Summary:
        //     Applies a force to a rigidbody that simulates explosion effects.
        //
        // Parameters:
        //   explosionForce:
        //     The force of the explosion (which may be modified by distance).
        //
        //   explosionPosition:
        //     The centre of the sphere within which the explosion has its effect.
        //
        //   explosionRadius:
        //     The radius of the sphere within which the explosion has its effect.
        //
        //   upwardsModifier:
        //     Adjustment to the apparent position of the explosion to make it seem to lift
        //     objects.
        //
        //   mode:
        //     The method used to apply the force to its targets.
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier, ForceMode mode)
        {
            if (!subscriber.Simulating())
                body.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, mode);
            else
                anchor.ApplyAddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, mode, subscriber);
        }

        //
        // Summary:
        //     Applies a force to a rigidbody that simulates explosion effects.
        //
        // Parameters:
        //   explosionForce:
        //     The force of the explosion (which may be modified by distance).
        //
        //   explosionPosition:
        //     The centre of the sphere within which the explosion has its effect.
        //
        //   explosionRadius:
        //     The radius of the sphere within which the explosion has its effect.
        //
        //   upwardsModifier:
        //     Adjustment to the apparent position of the explosion to make it seem to lift
        //     objects.
        //
        //   mode:
        //     The method used to apply the force to its targets.
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier)
        {
            AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, ForceMode.Force);
        }

        //
        // Summary:
        //     Applies a force to a rigidbody that simulates explosion effects.
        //
        // Parameters:
        //   explosionForce:
        //     The force of the explosion (which may be modified by distance).
        //
        //   explosionPosition:
        //     The centre of the sphere within which the explosion has its effect.
        //
        //   explosionRadius:
        //     The radius of the sphere within which the explosion has its effect.
        //
        //   upwardsModifier:
        //     Adjustment to the apparent position of the explosion to make it seem to lift
        //     objects.
        //
        //   mode:
        //     The method used to apply the force to its targets.
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius)
        {
            AddExplosionForce(explosionForce, explosionPosition, explosionRadius, 0f, ForceMode.Force);
        }

        //
        // Summary:
        //     The closest point to the bounding box of the attached colliders.
        //
        // Parameters:
        //   position:
        public Vector3 ClosestPointOnBounds(Vector3 position)
        {
            if (!subscriber.Simulating())
            {
                return body.ClosestPointOnBounds(position);
            }
            else
            {
                return anchor.ApplyClosestPointOnBound(position, subscriber);
            }
        }

        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo, float maxDistance, QueryTriggerInteraction queryTriggerInteraction)
        {
            //should always be done outside of the simulation!
            return body.SweepTest(direction, out hitInfo, maxDistance, queryTriggerInteraction);
        }

        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return SweepTest(direction, out hitInfo, maxDistance, QueryTriggerInteraction.UseGlobal);
        }

        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo)
        {
            return SweepTest(direction, out hitInfo, float.PositiveInfinity, QueryTriggerInteraction.UseGlobal);
        }


        //
        // Summary:
        //     Like Rigidbody.SweepTest, but returns all hits.
        //
        // Parameters:
        //   direction:
        //     The direction into which to sweep the rigidbody.
        //
        //   maxDistance:
        //     The length of the sweep.
        //
        //   queryTriggerInteraction:
        //     Specifies whether this query should hit Triggers.
        //
        // Returns:
        //     An array of all colliders hit in the sweep.
        public RaycastHit[] SweepTestAll(Vector3 direction, float maxDistance, QueryTriggerInteraction queryTriggerInteraction)
        {
            //should always run outise of the simulation 
            return body.SweepTestAll(direction, maxDistance, queryTriggerInteraction);
        }


        public RaycastHit[] SweepTestAll(Vector3 direction, float maxDistance)
        {
            return SweepTestAll(direction, maxDistance, QueryTriggerInteraction.UseGlobal);
        }

        public RaycastHit[] SweepTestAll(Vector3 direction)
        {
            return SweepTestAll(direction, float.PositiveInfinity, QueryTriggerInteraction.UseGlobal);
        }

        [Obsolete("Use Spllitter.maxAngularVelocity instead.")]
        public void SetMaxAngularVelocity(float a)
        {
            maxAngularVelocity = a;
        }
    }
}
