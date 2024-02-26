using NoTime.Splitter;
using System.Collections;
using UnityEngine;

public class AutomaticOrbit : MonoBehaviour
{
    public bool OnStartOnly;
    private bool doIt = true;
    public GravityObject gravityObject;
    public float maxVelocityChange = 100f;

    [Tooltip("if outside orbital path, no further forces applied.  this allows for nudging out of orbit. 0f value to ignore")]
    public float orbitalPathWidth = 1f;

    [Tooltip("Flight Controller is only for adding rocket engines and fx.")]
    public FlightController flightController;


    // Update is called once per frame
    float prevGravVelSqr;

    Vector3 currentVelocityTowardGravity = Vector3.zero;
    Vector3 currentVelocityInOrbit = Vector3.zero;
    Vector3 additiveVelocity = Vector3.zero;

    float _initialDistance = 0f;

    private void Start()
    {
        if (OnStartOnly)
            StartCoroutine("ShutOffIn10Seconds");
    }

    IEnumerator ShutOffIn10Seconds()
    {
        yield return new WaitForSeconds(10f);
        this.enabled = false;
    }

    Vector3 _goalV;
    Vector3 _deltaV;
    private void FixedUpdate()
    {
        if (!doIt)
            return;

        //OUT OF RANGE 
        if (gravityObject.field == null)
            return;

        if (_initialDistance == 0f)
            _initialDistance = gravityObject.GravityDistance;

        //ALLOW TO BE NUDGED OUT OF ORBIT
        if (orbitalPathWidth > 0f && Mathf.Abs(_initialDistance - gravityObject.GravityDistance) > orbitalPathWidth)
            return;

        Vector3 orbitDirection;
        if (transform.GetComponent<SplitterSubscriber>().AppliedPhysics.velocity.sqrMagnitude < 1f)
            orbitDirection = Vector3.ProjectOnPlane(transform.forward, gravityObject.GravityDirection.normalized).normalized;
        else
            orbitDirection = Vector3.ProjectOnPlane(transform.GetComponent<SplitterSubscriber>().AppliedPhysics.velocity.normalized, gravityObject.GravityDirection.normalized).normalized;

        _goalV = Mathf.Sqrt(gravityObject.GravityAcceleration * gravityObject.GravityDistance) * orbitDirection;

        //have to add a small balancing force to keep object in orbit because physx 
        _goalV += Mathf.Clamp(_initialDistance - gravityObject.GravityDistance, 0f, .001f) * -gravityObject.GravityDirection;
        _deltaV = Vector3.ClampMagnitude(_goalV - transform.GetComponent<SplitterSubscriber>().AppliedPhysics.velocity, maxVelocityChange);
        transform.GetComponent<SplitterSubscriber>().AppliedPhysics.AddForce(_deltaV, ForceMode.VelocityChange);

        if (flightController != null)
        {
            flightController.RegisterAutopilotThrust(_deltaV);
        }
    }

    private void OnEnable()
    {
        _initialDistance = gravityObject.GravityDistance;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (currentVelocityTowardGravity * 100f));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + currentVelocityInOrbit);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position + currentVelocityInOrbit, transform.position + currentVelocityInOrbit + additiveVelocity);
    }
}
