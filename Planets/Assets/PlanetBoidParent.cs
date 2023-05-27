using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlanetBoidParent : MonoBehaviour
{
    public List<PlanetBoid> planetBoids;
    public Vector3 avgFacingDirection;
    public Vector3 avgPosition;
    public DustAvoidanceReporter dustAvoidanceReporter;
    // Start is called before the first frame update
    void Start()
    {
        avgFacingDirection = Vector3.zero;
        avgPosition = transform.position;
        planetBoids = transform.GetComponentsInChildren<PlanetBoid>().ToList();
    }

    private Vector3 dirs;
    private Vector3 pos;
    private PlanetBoid boid1;
    private PlanetBoid boid2;
    private Collider avoidance;
    private int i;
    private int j;
    private int crowdCount;
    // Update is called once per frame
    void Update()
    {
        dirs = Vector3.zero;
        pos = Vector3.zero;
        for(i = 0; i < planetBoids.Count; i++)
        {
            //facing direction
            dirs += planetBoids[i].transform.forward;
            pos += planetBoids[i].transform.position;
        }
        avgFacingDirection = dirs.normalized;
        avgPosition = pos / planetBoids.Count;


        //crowding direction
        for (i = 0; i < planetBoids.Count; i++)
            planetBoids[i].initCrowdingDirection();

        for(i = 0; i < planetBoids.Count; i++)
        {
            boid1 = planetBoids[i];
            for(j = i + 1; j < planetBoids.Count; j++)
            {
                boid2 = planetBoids[j];
                if(
                    (boid1.transform.position - boid2.transform.position).sqrMagnitude 
                    < 
                    Mathf.Pow(Mathf.Max(boid1.CrowdingDistance, boid2.CrowdingDistance),2f)
                )
                {
                    boid1.addCrowdingDirection(boid1.transform.position - boid2.transform.position);
                    boid2.addCrowdingDirection(boid2.transform.position - boid1.transform.position);
                }
            }
            boid1.finalizeCrowdingDirection();
        }

        //avoidance direction
        for (i = 0; i < planetBoids.Count; i++)
            planetBoids[i].initAvoidanceDirection();
        
        for (i = 0; i < planetBoids.Count; i++)
        {
            boid1 = planetBoids[i];
            for (j = 0; j < dustAvoidanceReporter.avoidances.Count; j++)
            {
                avoidance = dustAvoidanceReporter.avoidances[j];
                if (
                    (boid1.transform.position - avoidance.ClosestPointOnBounds(boid1.transform.position)).sqrMagnitude
                    <
                    Mathf.Pow(boid1.CrowdingDistance, 2f)
                )
                {
                    boid1.addAvoidanceDirection(boid1.transform.position - avoidance.transform.position);
                }
            }
            boid1.finalizeAvoidanceDirection();
        }

        for (i = 0; i < planetBoids.Count; i++)
            planetBoids[i].UpdatePosition();
    }
}
