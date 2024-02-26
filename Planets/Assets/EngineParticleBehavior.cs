using System.Collections.Generic;
using UnityEngine;

public class EngineParticleBehavior : MonoBehaviour
{

    RaycastHit hit;
    Ray ray;
    public Transform FarFlameCollisionPosition;
    float FarFlameCollisionDistance;
    public Transform Plane;
    public ParticleSystem FlameHitSystem;
    public ParticleSystem FlameSystem;
    public Transform DirectionalLight;
    private float HitParticleSizeOriginal;
    private float HitParticleSpeedOriginal;
    private float OriginalParticleRate;
    private List<Color> HitParticleOriginalColors;
    private List<float> HitParticleOriginalTimes;
    private List<GradientColorKey> HitParticleGradientColorKeys;
    private ParticleSystem.EmissionModule HitEmissionModule;
    private ParticleSystem.EmissionModule FlameEmissionModule;
    private ParticleSystem.MainModule MainModule;
    private ParticleSystem.ColorOverLifetimeModule ColorOverLifetimeModule;

    private int defaultLayer;
    // Start is called before the first frame update
    void Start()
    {
        defaultLayer = 1 << LayerMask.NameToLayer("Default");
        FarFlameCollisionDistance = (transform.position - FarFlameCollisionPosition.position).magnitude;
        OriginalParticleRate = FlameHitSystem.emission.rateOverTime.constant;
        MainModule = FlameHitSystem.main;
        HitParticleSizeOriginal = MainModule.startSize.constant;
        HitParticleSpeedOriginal = MainModule.startSpeed.constant;
        FlameEmissionModule = FlameSystem.emission;
        ColorOverLifetimeModule = FlameHitSystem.colorOverLifetime;
        HitParticleOriginalColors = new List<Color>();
        HitParticleOriginalTimes = new List<float>();
        HitParticleGradientColorKeys = new List<GradientColorKey>();
        foreach (var key in ColorOverLifetimeModule.color.gradient.colorKeys)
        {
            HitParticleOriginalColors.Add(key.color);
            HitParticleOriginalTimes.Add(key.time);
            HitParticleGradientColorKeys.Add(key);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!FlameEmissionModule.enabled)
        {
            EmitHitParticles(false, 0f, Vector3.zero);
            return;
        }

        ray.direction = transform.forward;
        ray.origin = transform.position;

        Physics.Raycast(ray, out hit, FarFlameCollisionDistance, defaultLayer, QueryTriggerInteraction.Ignore);


        if (hit.collider != null)
            EmitHitParticles(true, hit.distance, hit.normal);
        else
            EmitHitParticles(false, 0f, Vector3.zero);
    }
    Vector3[] ambientProbeDirections = new Vector3[1];
    Color[] ambientProbeResult = new Color[1];
    Gradient grad;
    void EmitHitParticles(bool on, float distance, Vector3 normal)
    {
        if (on)
        {
            Plane.position = transform.position + (transform.forward * distance);
            Plane.rotation = Plane.rotation * Quaternion.FromToRotation(Plane.up, normal);
            MainModule = FlameHitSystem.main;
            MainModule.startSize = HitParticleSizeOriginal + (1f - distance / FarFlameCollisionDistance) * 3f;
            MainModule.startSpeed = HitParticleSpeedOriginal + (1f - distance / FarFlameCollisionDistance) * 2f;
            HitEmissionModule = FlameHitSystem.emission;
            HitEmissionModule.enabled = true;
            if (DirectionalLight != null)
            {
                grad = new Gradient();
                RenderSettings.ambientProbe.Evaluate(ambientProbeDirections, ambientProbeResult);
                HitParticleGradientColorKeys = new List<GradientColorKey>();
                for (var i = 0; i < ColorOverLifetimeModule.color.gradient.colorKeys.Length; i++)
                {
                    HitParticleGradientColorKeys.Add(new GradientColorKey(
                        Color.Lerp(
                            HitParticleOriginalColors[i],
                            Color.Lerp(HitParticleOriginalColors[i], Color.Lerp(Color.black, Color.blue, .05f), .8f),
                            Mathf.Pow(
                                 Mathf.Min(
                                    1f,
                                    1f + Vector3.Dot(normal, DirectionalLight.forward)
                                )
                                , 3f)
                        ),
                        HitParticleOriginalTimes[i]
                    ));
                }
                grad.SetKeys(HitParticleGradientColorKeys.ToArray(), ColorOverLifetimeModule.color.gradient.alphaKeys);
                ColorOverLifetimeModule.color = grad;
            }
        }
        else
        {
            Plane.position = transform.position + (transform.forward * FarFlameCollisionDistance * 2f);
            Plane.rotation = Plane.rotation * Quaternion.FromToRotation(-transform.forward, normal);
            HitEmissionModule = FlameHitSystem.emission;
            HitEmissionModule.enabled = false;
        }

    }

    public void SetEngineOn(bool on)
    {
        if (on)
            FlameEmissionModule.enabled = true;
        else
            FlameEmissionModule.enabled = false;
    }
}
