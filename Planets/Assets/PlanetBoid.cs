using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlanetBoid : MonoBehaviour
{
    public Transform planet;
    public float Speed;
    public float TurnSpeed;
    public float NeighborhoodRadius;
    public float CrowdingDistance;
    public float AvoidanceWeight;
    public float CrowdingWeight;
    public float GroupDirectionWeight;
    public float GroupCenterWeight;
    public float distanceFromPlanet;
    public List<DustAvoidanceReporter> DustAvoidanceReporters;
    private List<PlanetBoid> localBoids;
    private List<PlanetBoid> allBoids;
    private PlanetBoidParent boidParent;

    private void Awake()
    {
        localBoids = new List<PlanetBoid>();
        allBoids = transform.parent.GetComponentsInChildren<PlanetBoid>().ToList();
        boidParent = transform.parent.GetComponent<PlanetBoidParent>();
    }
    private void Start()
    {
        Speed = Speed + ((Random.value) - .5f);
        transform.position = transform.position + new Vector3((Random.value * 20f) - 10f, (Random.value * 20f) - 10f, (Random.value * 20f) - 10f);
    }

    public void Wrap(Vector3 position, float distance)
    {
        if((transform.position - position).sqrMagnitude > Mathf.Pow(distance, 2f))
        {
            transform.position -= 2f * (transform.position - position);
            transform.position = planet.position + (transform.position - planet.position).normalized * distanceFromPlanet;

            //while inside avoidance, rotate some around position
            
            while(
                DustAvoidanceReporters.Any(
                    x=>x.avoidances.Any(c=> c.bounds.Contains(transform.position)))
                ||
                DustAvoidanceReporters.SelectMany(
                    x => x.avoidances.Select(
                        c => transform.position - c.ClosestPointOnBounds(transform.position)
                    )
                ).Any(
                    x => x.sqrMagnitude < Mathf.Pow(CrowdingDistance, 2f)
                )
            )
            {
                transform.RotateAround(transform.parent.position, (position - planet.position).normalized, 1f);
                transform.position = planet.position + (transform.position - planet.position).normalized * distanceFromPlanet;
            }
        }
    }

    private Vector3 AwayDir = Vector3.zero;
    public void initCrowdingDirection()
    {
        AwayDir = Vector3.zero;
    }
    public void addCrowdingDirection(Vector3 dir)
    {
        AwayDir += dir.normalized;
    }
    public void finalizeCrowdingDirection()
    {
        AwayDir = AwayDir.normalized;
    }

    private Vector3 AvoidanceDir = Vector3.zero;

    public void initAvoidanceDirection()
    {
        AvoidanceDir = Vector3.zero;
    }
    public void addAvoidanceDirection(Vector3 dir)
    {
        AvoidanceDir += Vector3.ProjectOnPlane(dir, (transform.position - planet.position).normalized).normalized;
    }
    public void finalizeAvoidanceDirection()
    {
        AvoidanceDir = AvoidanceDir.normalized;
    }

    Vector3 GoalDir;
    Vector3 previousPosition;
    public void UpdatePosition()
    {

        GoalDir = ((AvoidanceDir*AvoidanceWeight)+(AwayDir*CrowdingWeight) + (boidParent.avgFacingDirection * GroupDirectionWeight) + ((boidParent.avgPosition-transform.position).normalized*GroupCenterWeight)).normalized;
        if (transform.name.Contains("test"))
        {
            Debug.Log("AvoidanceDir: " + AvoidanceDir.ToString("G3"));
        }
        if (GoalDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(GoalDir, (transform.position - planet.position).normalized),
                TurnSpeed * Time.deltaTime
            );
        }
        //advance
        previousPosition = transform.position;
        transform.position += transform.forward * Speed * Time.deltaTime;
        transform.position = planet.position + (transform.position - planet.position).normalized * distanceFromPlanet;

        transform.rotation = Quaternion.FromToRotation(transform.up, (transform.position - planet.position).normalized) * transform.rotation;//Quaternion.LookRotation((transform.position - previousPosition).normalized, (transform.position - planet.position).normalized);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (AvoidanceDir * 3f));
    }
}
