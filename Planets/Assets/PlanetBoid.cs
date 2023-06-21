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
    public float AvoidanceDistance;
    public float AvoidanceWeight;
    public float CrowdingWeight;
    public float GroupDirectionWeight;
    public float GroupCenterWeight;
    public float distanceFromPlanet;
    public bool Hidden = false;
    public float HiddenAlpha = 1f;
    public bool lit = false;
    public List<DustAvoidanceReporter> DustAvoidanceReporters;
    private List<PlanetBoid> localBoids;
    private List<PlanetBoid> allBoids;
    private PlanetBoidParent boidParent;

    private Vector3 dbPreviousSpawn = Vector3.zero;
    private Color dbColor = Color.white;

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

    private int _placementAttempts = 0;
    private int _maxPlacementAttempts = 4;
    private bool inAnyAvoidances = false;
    private bool nearAnyAvoidances = false;
    public void Wrap(Vector3 position, float distance)
    {
        if((transform.position - position).sqrMagnitude > Mathf.Pow(distance, 2f))
        {
            Hidden = true;
            HiddenAlpha = 0f;

            _placementAttempts = 0;

            transform.position = position - ((transform.position - position).normalized * distance * .9f);
            transform.position = planet.position + (transform.position - planet.position).normalized * distanceFromPlanet;

            inAnyAvoidances = boidParent.dustAvoidanceReporter.avoidances.Any(c => c.bounds.Contains(transform.position));
            nearAnyAvoidances = boidParent.dustAvoidanceReporter.avoidances.Any(
                            c => (transform.position - c.ClosestPointOnBounds(transform.position)).sqrMagnitude < Mathf.Pow(CrowdingDistance, 2f)     
                    );
            dbPreviousSpawn = transform.position;
            dbColor = Color.white;
            //while inside avoidance, rotate some around position

            while (
                _placementAttempts <= _maxPlacementAttempts
                &&
                (
                    inAnyAvoidances 
                    ||
                    nearAnyAvoidances
                )
            )
            {
                transform.RotateAround(transform.parent.position, (position - planet.position).normalized, 1f);
                transform.position = planet.position + (transform.position - planet.position).normalized * distanceFromPlanet;
                dbPreviousSpawn = transform.position;
                dbColor = Color.yellow;
                _placementAttempts += 1;
            }
            //if we couldn't find a spot, just place it decently outside of radius so we try again later
            if (_placementAttempts > _maxPlacementAttempts)
            {
                transform.position = position + (1.5f * (transform.position - position).normalized * distance);
                dbPreviousSpawn = transform.position;
                dbColor = Color.red;
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

    public void Update()
    {
        if (Hidden)
            HiddenAlpha = Mathf.Lerp(HiddenAlpha, 0, .1f * Time.deltaTime);
        else
            HiddenAlpha = Mathf.Lerp(HiddenAlpha, 1, .1f * Time.deltaTime);
    }

    Vector3 GoalDir;
    Vector3 previousPosition;
    Vector3 planetToMe;
    public void UpdatePosition()
    {

        GoalDir = (
            (AvoidanceDir*AvoidanceWeight)
            +(AwayDir*CrowdingWeight) 
            +(boidParent.avgFacingDirection * GroupDirectionWeight) 
            + ((boidParent.avgPosition-transform.position).normalized*GroupCenterWeight)
        ).normalized;

        if (GoalDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(GoalDir, (transform.position - planet.position).normalized),
                TurnSpeed * Time.deltaTime
            );
        }
        //advance
        
        transform.position += transform.forward * Speed * Time.deltaTime;
        planetToMe = (transform.position - planet.position).normalized;
        transform.position = planet.position + planetToMe * distanceFromPlanet;

        transform.rotation = Quaternion.FromToRotation(transform.up, planetToMe) * transform.rotation;//Quaternion.LookRotation((transform.position - previousPosition).normalized, (transform.position - planet.position).normalized);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (AvoidanceDir * 3f));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = dbColor;
        Gizmos.DrawWireSphere(dbPreviousSpawn,.5f);
    }
}
