using System.Collections.Generic;
using UnityEngine;

public class HoverboardBehavior : MonoBehaviour
{
    public List<Transform> FloatEngines;
    private Rigidbody body;

    public float PushDistance = 1f;
    private float RayCastDist = 10f;
    public float AirSpeed;
    public float Nominal;
    public KeyCode BoostKey;
    public float BoostForce;

    private int collisions = 0;

    private void Start()
    {
        body = transform.GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        collisions += 1;
    }

    private void OnTriggerExit(Collider other)
    {
        collisions -= 1;
    }

    RaycastHit _hit;
    bool _isGrounded;
    // Update is called once per frame
    void FixedUpdate()
    {
        _isGrounded = false;
        for (var i = 0; i < FloatEngines.Count; i++)
        {
            var engine = FloatEngines[i];
            Physics.Raycast(FloatEngines[i].transform.position, -body.transform.up, out _hit, RayCastDist);
            if (_hit.collider != null)
            {
                if (_hit.distance <= PushDistance)
                {
                    _isGrounded = true;
                    float speedTowardCollider =
                        -(
                            Quaternion.Inverse(
                                Quaternion.FromToRotation(
                                    Vector3.up, _hit.normal
                                )
                            ) * Vector3.Project(
                                body.GetPointVelocity(
                                    FloatEngines[i].transform.position
                                ),
                                _hit.normal
                            )
                        ).y;

                    speedTowardCollider = Mathf.Pow((speedTowardCollider + AirSpeed) /
                        (2 * AirSpeed), 2);
                    if (speedTowardCollider < 0)
                        speedTowardCollider = 0f;

                    body.AddForceAtPosition(
                        _hit.normal *
                        ((PushDistance - _hit.distance) / PushDistance) * Vector3.Dot(body.transform.up, _hit.normal) * (speedTowardCollider)
                        * Nominal * body.mass * Time.fixedDeltaTime,
                        FloatEngines[i].transform.position
                    );
                }
            }

            //handle speed up and slow down (on flat ground only)
            if (_isGrounded && Vector3.Dot(transform.up, Vector3.up) > .98f)
            {
                //if there is a rider and velocity less than max, add some
                if (collisions > 0 && body.velocity.sqrMagnitude < 100f)
                {
                    if (body.velocity == Vector3.zero)
                        body.velocity = Vector3.forward * .0000001f;
                    body.velocity += body.velocity.normalized * Time.fixedDeltaTime * .5f;
                }

                //if no rider and on flat and velocity isn't pretty much zero, subtract some
                if (collisions == 0)
                {
                    if (body.velocity.sqrMagnitude > 0.1f)
                    {
                        body.velocity -= body.velocity.normalized * Time.fixedDeltaTime * .1f;
                    }
                    //stop if close to slow
                    else
                    {
                        body.velocity = Vector3.zero;
                    }
                }
            }
        }
    }
}
