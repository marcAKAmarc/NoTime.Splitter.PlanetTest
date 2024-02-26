using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlanetBoidParent : MonoBehaviour
{
    public List<PlanetBoid> planetBoids;
    public Light SunLight;
    public Vector3 avgFacingDirection;
    public Vector3 avgPosition;
    public DustAvoidanceReporter dustAvoidanceReporter;
    public float soundPitchVariance = .02f;
    public bool init = false;

    // Start is called before the first frame update
    private void Start()
    {
        Debug.Log("Start");
        Reset();
    }
    void OnEnable()
    {
        Debug.Log("Enabled");
        if (planetBoids != null)
            Reset();
    }
    private void Init()
    {
        init = true;
        Reset();
    }
    private void Reset()
    {
        avgFacingDirection = Vector3.zero;
        avgPosition = transform.position;
        planetBoids = transform.GetComponentsInChildren<PlanetBoid>().ToList();

        var sources = transform.GetComponentsInChildren<AudioSource>().ToList();
        for (i = 0; i < sources.Count; i++)
        {
            sources[i].pitch = sources[i].pitch + ((Random.value * soundPitchVariance) - (soundPitchVariance / 2f));
        }

        StartCoroutine(Interruptable());
    }

    private Vector3 dirs;
    private Vector3 pos;
    private PlanetBoid boid1;
    private PlanetBoid boid2;
    private Vector3 _crowdingDir;
    private Vector3 _avoidanceDir;
    private Collider avoidance;
    private int i;
    private int j;
    private int crowdCount;

    public float maxCalculationTime = .00001f;
    float startTime = 0f;

    private void StartTimers()
    {
        startTime = Time.realtimeSinceStartup;
    }
    private bool isAlarm()
    {
        return Time.realtimeSinceStartup - startTime > maxCalculationTime;
    }
    bool _hasInterrupted = false;
    private Ray litRay = new Ray();
    IEnumerator Interruptable()
    {
        StartTimers();
        while (true)
        {
            if (init == false)
                yield return null;
            _hasInterrupted = false;
            dirs = Vector3.zero;
            pos = Vector3.zero;
            for (i = 0; i < planetBoids.Count; i++)
            {
                //facing direction
                dirs += planetBoids[i].transform.forward;
                pos += planetBoids[i].transform.position;
            }
            avgFacingDirection = dirs.normalized;
            avgPosition = pos / planetBoids.Count;


            //maybe pause
            if (isAlarm())
            {
                _hasInterrupted = true;
                yield return null;
                StartTimers();
            }

            //crowding direction
            for (i = 0; i < planetBoids.Count; i++)
                planetBoids[i].initCrowdingDirection();


            for (i = 0; i < planetBoids.Count; i++)
            {
                boid1 = planetBoids[i];
                for (j = i + 1; j < planetBoids.Count; j++)
                {
                    boid2 = planetBoids[j];
                    _crowdingDir = boid1.transform.position - boid2.transform.position;
                    if (
                        _crowdingDir.sqrMagnitude
                        <
                        Mathf.Pow(Mathf.Max(boid1.CrowdingDistance, boid2.CrowdingDistance), 2f)
                    )
                    {
                        boid1.addCrowdingDirection(_crowdingDir);
                        boid2.addCrowdingDirection(-_crowdingDir);
                    }


                }
                boid1.finalizeCrowdingDirection();
            }
            boid2.finalizeCrowdingDirection();

            //maybe pause
            if (isAlarm())
            {
                _hasInterrupted = true;
                yield return null;
                StartTimers();
            }

            //avoidance direction
            for (i = 0; i < planetBoids.Count; i++)
                planetBoids[i].initAvoidanceDirection();

            for (i = 0; i < planetBoids.Count; i++)
            {
                boid1 = planetBoids[i];
                boid1.Hidden = false;
                for (j = 0; j < dustAvoidanceReporter.avoidances.Count; j++)
                {
                    avoidance = dustAvoidanceReporter.avoidances[j];
                    _avoidanceDir = boid1.transform.position - avoidance.bounds.ClosestPoint(boid1.transform.position);
                    if (
                        _avoidanceDir.sqrMagnitude
                        <
                        Mathf.Pow(boid1.AvoidanceDistance, 2f)
                    )
                    {
                        boid1.addAvoidanceDirection(_avoidanceDir);
                        //boid1.Hidden = true;
                    }
                }
                boid1.finalizeAvoidanceDirection();
            }

            //lit
            for (i = 0; i < planetBoids.Count; i++)
            {
                litRay.origin = planetBoids[i].transform.position;
                litRay.direction = -SunLight.transform.forward;
                planetBoids[i].lit = !Physics.Raycast(litRay, 500f);
                //maybe pause
                if (isAlarm())
                {
                    _hasInterrupted = true;
                    yield return null;
                    StartTimers();
                }
            }

            if (!_hasInterrupted)
            {
                //don't just keep looping until we run out of time.
                //once through is enough.
                yield return null;
                StartTimers();
                continue;
            }

            //we have been interrupted previously, so it's okay to pause here if needed
            else if (isAlarm())
            {
                yield return null;
                StartTimers();
            }

        }
    }
    void Update()
    {
        for (i = 0; i < planetBoids.Count; i++)
            planetBoids[i].UpdatePosition();

    }
}
