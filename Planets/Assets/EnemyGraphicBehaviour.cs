using NoTime.Splitter;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class footPair
{
    public Transform footRInitial;
    public Transform footLInitial;
    [HideInInspector]
    public Vector3 footrLocalCenter, goalFootR, footlLocalCenter, goalFootL, footRCurrent, footLCurrent;
    [HideInInspector]
    public float prevFootROffsetSign, footROffsetSign;
    public Transform legR;
    public Transform legL;
    public Vector3 rOrigin, rDir, rHit, lOrigin, lDir, lHit;
    public bool rDidHit;
    public string hitGameObjectName = "";
}
public class EnemyGraphicBehaviour : MonoBehaviour
{

    public SplitterSubscriber sub;
    private SplitterAnchor prevSubAnchor;
    public FollowBehaviour follower;
    public GravityObject gravityObject;
    public float planetRadius;

    public Transform eyer;
    public Transform eyel;

    public List<footPair> feetPairs;
    /*public Transform footr;
    public Transform footl;
    public Vector3 footrLocalCenter, goalFootR;
    public Vector3 footlLocalCenter, goalFootL;*/

    public float rotateSpeed = 90f;

    public float stepSize;
    public float stepSizeBase = .25f;
    public float stepSizeGain = 1f;
    public float stepWidthRatio = .75f;
    public float stepBuffer = .1f;
    public float footSpeedBase = 2f;
    public float footSpeedGain = 3f;
    private float currentFootSpeed;
    public float stepLiftAmt = .5f;
    public float maximumVReach = 1.5f;
    public bool AlignWithGravity = true;
    private float AlignWithGravityAmt = 0f;
    public bool FaceAwayFromVelocity = false;
    private float FaceAwayAmt = 0f;
    public LayerMask footCollisionMask;
    /*public Transform legR;
    public Transform legL;*/
    // Start is called before the first frame update
    void Start()
    {
        ray = new Ray();
        prevSubAnchor = sub ? sub.Anchor : null;
        for(int i = 0; i < feetPairs.Count; i++)
        {
            InitFootpair(feetPairs[i], i);
        }
    }

    public void InitFootpair (footPair p, int pairNo)
    {
        p.footrLocalCenter = transform.InverseTransformPoint(p.footRInitial.position);
        p.footlLocalCenter = transform.InverseTransformPoint(p.footLInitial.position);
        p.prevFootROffsetSign = 1f;
        p.footROffsetSign = 0f;
        if (sub.Anchor != null)
        {
            p.goalFootL = sub.Anchor.transform.InverseTransformPoint(p.footLInitial.position);
            p.goalFootR = sub.Anchor.transform.InverseTransformPoint(p.footRInitial.position);

            if(pairNo % 4 == 1)
            {
                p.goalFootR += sub.Anchor.transform.InverseTransformPoint(
                    transform.forward  * (stepSize/2f)
                );
            }

            if(pairNo % 4 == 3)
            {
                p.goalFootR -= sub.Anchor.transform.InverseTransformPoint(
                    transform.forward * (stepSize / 2f)
                );
            }
            
        }
        else
        {
            p.goalFootL = p.footLInitial.position;
            p.goalFootR = p.footRInitial.position;
        }

        p.footRCurrent = p.footRInitial.position;
        p.footLCurrent = p.footLInitial.position;

        ray.origin = p.footRInitial.position;
        ray.direction = gravityObject.GravityDirection.normalized;
        if (Physics.Raycast(ray, out hitInfo, 2f))
        {
            p.footRInitial.position = hitInfo.point;
        }

        ray.origin = p.footLInitial.position;
        ray.direction = gravityObject.GravityDirection.normalized;
        if (Physics.Raycast(ray, out hitInfo, 2f))
        {
            p.footLInitial.position = hitInfo.point;
        }
    }

    //float prevFootROffsetSign, footROffsetSign;
    // Update is called once per frame
    void Update()
    {
        if (FaceAwayFromVelocity)
        {
            FaceAwayAmt += Time.deltaTime;
            if (FaceAwayAmt > 1f)
                FaceAwayAmt = 1f;
        }
        else
        {
            FaceAwayAmt -= Time.deltaTime;
            if (FaceAwayAmt < 0f)
                FaceAwayAmt = 0f;
        }

        if (AlignWithGravity)
        {
            AlignWithGravityAmt += Time.deltaTime;
            if (AlignWithGravityAmt > 1f)
                AlignWithGravityAmt = 1f;
        }
        else
        {
            AlignWithGravityAmt -= Time.deltaTime;
            if (AlignWithGravityAmt < 0f)
                AlignWithGravityAmt = 0f;
        }

        if (sub.AppliedPhysics.velocity.sqrMagnitude > 1f)
        {
            //transform.rotation = 
            transform.position = sub.AppliedPhysics.position;
            Quaternion goalSpiderRotation =
                Quaternion.LookRotation(
                        Vector3.ProjectOnPlane(
                            Vector3.Slerp(sub.AppliedPhysics.velocity.normalized, -sub.AppliedPhysics.velocity.normalized, FaceAwayAmt),
                            -gravityObject.GravityDirection.normalized
                        ),
                        -gravityObject.GravityDirection.normalized
                    );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    goalSpiderRotation,
                    Mathf.Min(
                        (rotateSpeed / Quaternion.Angle(transform.rotation, goalSpiderRotation)) * Time.deltaTime,
                        .5f
                    )
                );
        }

        //align with gravity
        if (Grounded)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(
                    Vector3.Cross(
                        -gravityObject.GravityDirection,
                        Vector3.Cross(
                            transform.forward, -gravityObject.GravityDirection
                        )
                    ),
                    -gravityObject.GravityDirection
                ),
                Mathf.Pow(AlignWithGravityAmt,2f)
            );
        }

        Vector3 lookTargetPos = transform.position + transform.forward * 10f;
        if (follower.target != null)
            lookTargetPos = follower.target.AppliedPhysics.position;

        Quaternion eyeRotation = Quaternion.Slerp(
                eyer.rotation,
                Quaternion.LookRotation(
                        lookTargetPos - sub.AppliedPhysics.position
                ),
                .1f
            );
        eyer.rotation = eyeRotation;
        eyel.rotation = eyeRotation;


        stepSize = stepSizeBase + (stepSizeGain * Mathf.Min(1f, (sub.AppliedPhysics.velocity.sqrMagnitude / 100f)));

        currentFootSpeed = footSpeedBase + (footSpeedGain * Mathf.Min(1f, sub.AppliedPhysics.velocity.sqrMagnitude / 100f));

        /*if (AlignWithGravity && Grounded)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(
                    Vector3.Cross(
                        -gravityObject.GravityDirection,
                        Vector3.Cross(
                            transform.forward, -gravityObject.GravityDirection
                        )
                    ),
                    -gravityObject.GravityDirection
                ),
                1f
            );
        }*/

        Grounded = false;

        for(int i = 0; i < feetPairs.Count; i++)
        {
            updateLeg(feetPairs[i]);
        }

        

        prevSubAnchor = sub.Anchor;
    }
    private Ray ray;
    private RaycastHit hitInfo;
    private bool Grounded = false;
    private void updateLeg(footPair p) {


        Vector3 thisFootLift = Vector3.zero;

        if(sub.Anchor != prevSubAnchor)
        {
            //switch foot position to new anchor
            //world step
            if (prevSubAnchor != null)
            {
                p.goalFootR = prevSubAnchor.transform.TransformPoint(p.goalFootR);
                p.goalFootL = prevSubAnchor.transform.TransformPoint(p.goalFootL);
            }
            //into locality if needed
            if (sub.Anchor != null)
            {
                p.goalFootR = sub.Anchor.transform.InverseTransformPoint(p.goalFootR);
                p.goalFootL = sub.Anchor.transform.InverseTransformPoint(p.goalFootL);
            }
        }
        

        Vector3 footRPos;
        if (sub.Anchor != null)
            footRPos = transform.InverseTransformPoint(sub.Anchor.transform.TransformPoint(p.goalFootR));
        else
            footRPos = transform.InverseTransformPoint(p.goalFootR);

        footRPos = footRPos - Vector3.up * footRPos.y;// new Vector3(footRPos.x, 0f, footRPos.z);
        footRPos += Vector3.up * p.footrLocalCenter.y;

        bool rFootMovedForward = false;

        if (Mathf.Abs(footRPos.x - p.footrLocalCenter.x) > (stepWidthRatio * stepSize / 2f) + stepBuffer)
        {
            footRPos += Vector3.right * (-Mathf.Sign(footRPos.x - p.footrLocalCenter.x) * stepSize * stepWidthRatio);
        }
        if (Mathf.Abs(footRPos.z - p.footrLocalCenter.z) > (stepSize / 2f) + stepBuffer)
        {
            rFootMovedForward = true;
            footRPos += Vector3.forward * (-Mathf.Sign(footRPos.z - p.footrLocalCenter.z) * stepSize);
        }
        //quick do this here before we transform footRPos back out to world
        p.prevFootROffsetSign = p.footROffsetSign;
        p.footROffsetSign = Mathf.Sign(footRPos.z - p.footrLocalCenter.z);
        footRPos = transform.TransformPoint(footRPos);

        p.goalFootR = footRPos;

        ray.origin = footRPos + (-gravityObject.GravityDirection.normalized * maximumVReach);
        ray.direction = gravityObject.GravityDirection.normalized;

        p.rOrigin = ray.origin;
        p.rDir = ray.direction;

        if (Physics.Raycast(ray, out hitInfo, maximumVReach*2f, footCollisionMask.value, QueryTriggerInteraction.Ignore))
        {
            Grounded = true;
            thisFootLift = stepLiftAmt * Mathf.Sin(
                (Vector3.ProjectOnPlane(hitInfo.point - p.footRCurrent, transform.up).sqrMagnitude / Mathf.Pow(stepSize, 2f))
                * (Mathf.PI)
            ) * transform.up * 2f;

            if ((hitInfo.point - p.footRCurrent).sqrMagnitude > Mathf.Pow(currentFootSpeed * Time.deltaTime, 2f))
                p.footRCurrent = p.footRCurrent + (((hitInfo.point+thisFootLift) - p.footRCurrent).normalized * currentFootSpeed * Time.deltaTime);
            else
                p.footRCurrent = hitInfo.point;
            //footr.position = Vector3.Lerp(footr.position, hitInfo.point, Time.deltaTime * footSpeed);

            p.rHit = hitInfo.point;
            p.rDidHit = true;
            p.hitGameObjectName = hitInfo.collider.gameObject.name;
        }
        else
        {
            p.footRCurrent = Vector3.Lerp(p.footRCurrent, p.goalFootR - (transform.up * 1f), .1f);
            p.rHit = transform.position;//p.footRCurrent;
            p.rDidHit = false;
        }

        if(sub.Anchor)
            p.goalFootR = sub.Anchor.transform.InverseTransformPoint(p.goalFootR);
        

        Vector3 footLPos;
        if (sub.Anchor != null)
            footLPos = transform.InverseTransformPoint(sub.Anchor.transform.TransformPoint(p.goalFootL));
        else
            footLPos = transform.InverseTransformPoint(p.goalFootL);

        footLPos = footLPos - Vector3.up * footLPos.y;// new Vector3(footRPos.x, 0f, footRPos.z);
        footLPos += Vector3.up * p.footlLocalCenter.y;
        if (Mathf.Abs(footLPos.x - p.footlLocalCenter.x) > (stepSize * stepWidthRatio / 2f) + stepBuffer)
        {
            footLPos += Vector3.right * (-Mathf.Sign(footLPos.x - p.footlLocalCenter.x) * stepSize * stepWidthRatio);
        }

        //move forward naturally be cause out of bounds...
        if (Mathf.Abs(footLPos.z - p.footlLocalCenter.z) > (stepSize / 2f) + stepBuffer) 
        {
            footLPos += Vector3.forward * (-Mathf.Sign(footLPos.z - p.footlLocalCenter.z) * stepSize);
        }
        //else move forward because right foot crossed the center
        else if (!rFootMovedForward && p.footROffsetSign != p.prevFootROffsetSign)
        //if (Mathf.Abs(footLPos.z - footlLocalCenter.z) > stepSize / 2f)
        {
            footLPos += Vector3.forward * ((p.footlLocalCenter.z - footLPos.z) + (Mathf.Sign(p.footlLocalCenter.z - footLPos.z) * stepSize / 2f));    
        }
        footLPos = transform.TransformPoint(footLPos);

        /*ray.origin = footLPos;
        ray.direction = (planet.position - footLPos).normalized;
        if (Physics.Raycast(ray, out hitInfo, 1f))
        {
            footLPos = hitInfo.point;
        }*/
        p.goalFootL = footLPos;




        ray.origin = footLPos + (-gravityObject.GravityDirection.normalized*maximumVReach);
        ray.direction = gravityObject.GravityDirection.normalized;

        p.lOrigin = ray.origin;
        p.lDir = ray.direction;

        if (Physics.Raycast(ray, out hitInfo, maximumVReach*2f, footCollisionMask.value, QueryTriggerInteraction.Ignore))
        {
            Grounded = true;
            thisFootLift = stepLiftAmt * Mathf.Sin(
                (Vector3.ProjectOnPlane(hitInfo.point - p.footLCurrent, transform.up).sqrMagnitude / Mathf.Pow(stepSize,2f))
                * (Mathf.PI)
            ) * transform.up * 2f;

            if ((hitInfo.point - p.footLCurrent).sqrMagnitude > Mathf.Pow(currentFootSpeed * Time.deltaTime, 2f))
                p.footLCurrent = p.footLCurrent + (((hitInfo.point+thisFootLift) - p.footLCurrent).normalized * currentFootSpeed * Time.deltaTime);
            else
                p.footLCurrent = hitInfo.point;
            //footl.position = Vector3.Lerp(footl.position, hitInfo.point, Time.deltaTime * footSpeed);

            p.lHit = hitInfo.point;
        }
        else
        {
            p.footLCurrent = Vector3.Lerp(p.footLCurrent, p.goalFootL - (transform.up * 1f), .1f);
            p.lHit = p.footLCurrent;
        }
        
        if(sub.Anchor)
            p.goalFootL = sub.Anchor.transform.InverseTransformPoint(p.goalFootL);


        p.legR.rotation = Quaternion.LookRotation(
            (p.footRCurrent - p.legR.position).normalized,
            Vector3.Cross(
                (p.footRCurrent - p.legR.position).normalized,
                Vector3.Cross(
                    transform.up, 
                    (p.footRCurrent - p.legR.position).normalized
                )
            )
        );
        p.legL.rotation = Quaternion.LookRotation(
            (p.footLCurrent - p.legL.position).normalized,
            Vector3.Cross(
                Vector3.Cross(
                    (p.footLCurrent - p.legL.position).normalized,
                    transform.up
                ),
                (p.footLCurrent - p.legL.position).normalized
            )
        );

        p.legR.localScale = Vector3.forward * ((p.legR.position - p.footRCurrent).magnitude/1f) + Vector3.right + Vector3.up;
        p.legL.localScale = Vector3.forward * ((p.legL.position - p.footLCurrent).magnitude/1f) + Vector3.right + Vector3.up;

        
    }


    private void OnDrawGizmosSelected()
    {
        
        Color prevColor = Gizmos.color;
        for (int i = 0; i < feetPairs.Count; i++)
        {
            Gizmos.color = Color.green;
            if (sub.Anchor)
            {
                Gizmos.DrawRay(transform.position, sub.Anchor.transform.TransformPoint(feetPairs[i].goalFootR) - transform.position);
                Gizmos.DrawSphere(sub.Anchor.transform.TransformPoint(feetPairs[i].goalFootR), .2f);
                Gizmos.DrawRay(transform.position, sub.Anchor.transform.TransformPoint(feetPairs[i].goalFootL) - transform.position);
                Gizmos.DrawWireSphere(sub.Anchor.transform.TransformPoint(feetPairs[i].goalFootL), .2f);
            }
            else
            {
                Gizmos.DrawRay(transform.position, feetPairs[i].goalFootR - transform.position);
                Gizmos.DrawSphere(feetPairs[i].goalFootR, .2f);
                Gizmos.DrawRay(transform.position, feetPairs[i].goalFootL - transform.position);
                Gizmos.DrawWireSphere(feetPairs[i].goalFootL, .2f);
            }

            Gizmos.color = Color.cyan;
            if (feetPairs[i].rDidHit)
            {
                Gizmos.color = Color.red;
                
            }
            Gizmos.DrawRay(feetPairs[i].rOrigin, feetPairs[i].rHit - feetPairs[i].rOrigin);
            //Gizmos.DrawRay(feetPairs[i].lOrigin, feetPairs[i].lHit);


            Gizmos.color = Color.gray;
            Gizmos.DrawSphere(transform.TransformPoint(feetPairs[i].footrLocalCenter), .2f);
            Gizmos.DrawWireSphere(transform.TransformPoint(feetPairs[i].footlLocalCenter), .2f);
        }
        Gizmos.color = prevColor;
    }
}
