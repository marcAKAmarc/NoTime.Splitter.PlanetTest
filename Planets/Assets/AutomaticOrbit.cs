using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class AutomaticOrbit : MonoBehaviour
{
    private bool doIt = true;
    public float shitvariable;
    public GravityObject gravityObject;


    // Update is called once per frame
    float prevGravVelSqr;

    Vector3 currentVelocityTowardGravity = Vector3.zero;
    Vector3 currentVelocityInOrbit = Vector3.zero;
    Vector3 additiveVelocity = Vector3.zero;

    float _initialDistance = 0f;

    private void Start()
    {
        StartCoroutine("Attempt2Wait");   
    }
    IEnumerator Attempt2Wait()
    {
        while(gravityObject.GravityAcceleration == 0f)
            yield return new WaitForFixedUpdate();
        //Attempt2();
    }
    Vector3 _goalV;
    private void FixedUpdate()
    {
        if (_initialDistance == 0f)
            _initialDistance = gravityObject.GravityDistance;
        if (Mathf.Abs(_initialDistance - gravityObject.GravityDistance) > 1f)
            return;
        
        Vector3 orbitDirection;
        if(transform.GetComponent<Rigidbody>().velocity.sqrMagnitude < 1f)
            orbitDirection = Vector3.ProjectOnPlane(transform.forward, gravityObject.GravityDirection.normalized).normalized;
        else
            orbitDirection = Vector3.ProjectOnPlane(transform.GetComponent<Rigidbody>().velocity.normalized, gravityObject.GravityDirection.normalized).normalized;
        
        _goalV = Mathf.Sqrt(gravityObject.GravityAcceleration * gravityObject.GravityDistance) * orbitDirection;

        //have to add a small balancing force to keep object in orbit because physx
        _goalV += Mathf.Clamp(_initialDistance - gravityObject.GravityDistance, 0f, .001f) * -gravityObject.GravityDirection;

        transform.GetComponent<Rigidbody>().AddForce(_goalV - transform.GetComponent<Rigidbody>().velocity, ForceMode.VelocityChange);

        //Debug.Log("Change in distance: " + (gravityObject.GravityDistance - _initialDistance).ToString());
    }
    private void attemprt1()
    {
        if (!doIt)
            return;

        var planet = gravityObject.field;
        if (planet == null)
        {
            Debug.Log("no planet");
            return;

        }
        var gravityAcceleration = 
            gravityObject.GravityAcceleration * gravityObject.GravityDirection;
            
            /*
                (9.8f * 99f / (planet.transform.position - (transform.position + transform.GetComponent<Rigidbody>().velocity*Time.fixedDeltaTime)).sqrMagnitude)
                * (planet.transform.position - transform.position).normalized;
        */
        currentVelocityTowardGravity = Vector3.Project(transform.GetComponent<Rigidbody>().velocity, gravityAcceleration.normalized);
        var sameDir = Mathf.Sign(Vector3.Dot(currentVelocityTowardGravity.normalized, gravityObject.GravityDirection));
        
        var g = gravityObject.GravityAcceleration;

        

        var offset = transform.position - planet.transform.position;
        var distance = offset.magnitude;

        var goalOrbitSpeed = Mathf.Sqrt(Mathf.Pow(distance, 2) + Mathf.Pow(distance - g, 2));

        Vector3 orbitDirection; 
        if(transform.GetComponent<Rigidbody>().velocity == Vector3.zero)
            orbitDirection = Vector3.ProjectOnPlane(transform.forward, gravityAcceleration.normalized).normalized;
        else
            orbitDirection = Vector3.ProjectOnPlane(transform.GetComponent<Rigidbody>().velocity, gravityAcceleration.normalized).normalized;
        currentVelocityInOrbit = Vector3.ProjectOnPlane(transform.GetComponent<Rigidbody>().velocity, -gravityAcceleration.normalized);
        var currentSpeedOrbit = currentVelocityInOrbit.magnitude * Vector3.Dot(currentVelocityInOrbit.normalized, orbitDirection);


        var accelerationInOrbit = goalOrbitSpeed - currentSpeedOrbit;
        if (accelerationInOrbit < 0f)
            Debug.Log("AccelerationInOrbit is negative");

        /*if (sameDir != 1)
            accelerationInOrbit = accelerationInOrbit / (1 + Mathf.Pow(currentVelocityTowardGravity.magnitude,1));*/
        if(sameDir == 1)
            transform.GetComponent<Rigidbody>().AddForce((accelerationInOrbit) * orbitDirection /** sameDir*/, ForceMode.Acceleration);


        //stop speed toward planet

         //transform.GetComponent<Rigidbody>().AddForce(-currentVelocityTowardGravity*.75f, ForceMode.Acceleration);

        //var planet = GameObject.FindGameObjectsWithTag("GravitySource").First();
        //var gravityForce = 
        //        (9.8f * 99f / (planet.transform.position - transform.position).sqrMagnitude)
        //        * (planet.transform.position - transform.position).normalized;
        //currentVelocityTowardGravity = Vector3.Project(transform.GetComponent<Rigidbody>().velocity, gravityForce.normalized)
        //    + (gravityForce/transform.GetComponent<Rigidbody>().mass);
        //var deltaTowardGravity = currentVelocityTowardGravity.magnitude;//* Vector3.Dot(currentVelocityTowardGravity.normalized, gravityForce.normalized);
        //var r = (transform.position - planet.transform.position).magnitude;
        //var goalDeltaInOrbit = Mathf.Sqrt(deltaTowardGravity * ((2 * r) + deltaTowardGravity)) * Vector3.Dot(currentVelocityTowardGravity.normalized, gravityForce.normalized);
        //goalDeltaInOrbit = goalDeltaInOrbit /** shitvariable*/;

        //var orbitDirection = Vector3.ProjectOnPlane(transform.forward, gravityForce.normalized).normalized;
        //currentVelocityInOrbit = Vector3.ProjectOnPlane(transform.GetComponent<Rigidbody>().velocity, -gravityForce.normalized);
        //var currentDeltaInOrbit = currentVelocityInOrbit.magnitude * Vector3.Dot(currentVelocityInOrbit.normalized, orbitDirection);

        //Debug.Log("goal: " + goalDeltaInOrbit.ToString() + "; current: " + currentVelocityInOrbit.ToString());
        //var additiveDeltaInOrbit = goalDeltaInOrbit - currentDeltaInOrbit;
        //additiveVelocity = additiveDeltaInOrbit * orbitDirection;

        //Debug.Log("vel added: " + additiveVelocity.ToString("G6") + "; mag: " + additiveDeltaInOrbit.ToString());
        //transform.GetComponent<Rigidbody>().AddForce(additiveVelocity, ForceMode.VelocityChange);

        //doIt = false;
        //var first = GameObject.FindGameObjectsWithTag("GravitySource").First();
        //var rSquared = (first.transform.position - transform.position).sqrMagnitude;

        //var GravityVector = Vector3.zero;
        //foreach (var go in GameObject.FindGameObjectsWithTag("GravitySource"))
        //{
        //    GravityVector +=
        //        (9.8f * 99f / (go.transform.position - transform.position).sqrMagnitude)
        //        * (go.transform.position - transform.position).normalized;
        //}
        //var gravAcceleration = GravityVector / transform.GetComponent<Rigidbody>().mass;
        //var resultingGravSpeed = Vector3.Project(transform.GetComponent<Rigidbody>().velocity, GravityVector.normalized) + (gravAcceleration);
        //var orbitalGoalSpeed = rSquared - (Mathf.Pow(Mathf.Sqrt(rSquared) - resultingGravSpeed.magnitude, 2));
        //var orbitalDirection = Vector3.ProjectOnPlane(transform.forward, GravityVector).normalized;
        //var orbitalVelocity =
        //        Vector3.ProjectOnPlane(transform.GetComponent<Rigidbody>().velocity, -GravityVector);
        //var OrbitalVelocityChange = (orbitalDirection * orbitalGoalSpeed)-orbitalVelocity; 

        //var thisGravVelSqr = Vector3.Dot(GravityVector, transform.GetComponent<Rigidbody>().velocity);

        //var f = 20f;

        //if (Input.GetKey(KeyCode.N) /*&& Mathf.Sign(thisGravVelSqr) != Mathf.Sign(prevGravVelSqr)*/)
        //{
        //    if (orbitalGoalSpeed > orbitalVelocity.magnitude + (f * transform.GetComponent<Rigidbody>().mass * Time.fixedDeltaTime))
        //        transform.GetComponent<Rigidbody>().AddForce(orbitalDirection * f);
        //    //transform.GetComponent<Rigidbody>().AddForce(OrbitalVelocityChange * transform.GetComponent<Rigidbody>().mass * Time.fixedDeltaTime);
        //    //transform.GetComponent<Rigidbody>().AddForce(orbitalGoalSpeed * orbitalDirection * transform.GetComponent<Rigidbody>().mass);
        //}

        //prevGravVelSqr = thisGravVelSqr;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (currentVelocityTowardGravity*100f));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + currentVelocityInOrbit);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position + currentVelocityInOrbit, transform.position + currentVelocityInOrbit + additiveVelocity);
    }
}
