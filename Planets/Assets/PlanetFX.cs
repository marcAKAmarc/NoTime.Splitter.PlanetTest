using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class FacadeData
{
    public Transform Facade;
    private Material[] Materials;
    private Renderer[] Renderers;
    public float transparentDistance;
    public float opaqueDistance;

    public Renderer[] GetRenderers()
    {
        return Renderers;
    }
    public Material[] GetMaterials()
    {
        return Materials;
    }
    public void Init()
    {
        Materials = Facade.GetComponentsInChildren<Renderer>().Select(r => r.material).ToArray();
        Renderers = Facade.GetComponentsInChildren<Renderer>().ToArray();
    }
}

[Serializable]
public class AtmosphereData
{
    public Transform PlanetTransform;
    public float farRadius;
    public float nearRadius;
    public float farBlendValue;
    public float nearBlendValue;
    public float farExposureValue;
    public float nearExposureValue;
    public Color DayTintColor;
    public Color NightTintColor;
    public float SunIntensityNoon;
    public float SunIntensityHorizon;
    public Color SunNoonColor;
    public Color SunHorizonColor;
    public Color FlareNoonColor;
    public Color FlareHorizonColor;
    public float flareScaleNoon;
    public float flareScaleHorizon;
    //public float flareAlphaNoon;
    //public float flareAlphaHorizon;
    public float SunScaleNoon;
    public float SunScaleHorizon;
    public Transform PlanetAtmosphereTexture;
    public Transform PlanetSurfaceAtmosphereTexture;
    public float PlanetAtmosphereShiftMultiplier;
    public float PlanetAtmosphereShiftScaler;
    public Vector3 _initialScale;
    public Color EnvironmentalLightingColorDay;
    public Color EnvironmentalLightingColorNight;
}

[Serializable]
public class DustData
{
    public Transform DustGroup;
    public float GroupActivateRadius;
    public float GroupFullVisibilityRadius;
    public float UnitCloseTransparentRadius;
    public float UnitCloseOpaqueRadius;
    public float UnitFarOpaqueRadius;
    public float UnitFarTransparentRadius;
    public float maxAlpha;

    public float ParticleMaxAlpha;

    public Gradient InLightBlendColor;
    public Gradient InShadowBlendColor;

    public AnimationCurve DayNightAlphaCurve;

    [HideInInspector]
    public Color DustCloudColor;
    [HideInInspector]
    public Color DustParticleColor;
    [HideInInspector]
    public List<DustAudioData> AudioData;
}
public class DustAudioData
{
    public AudioSource source;
    public float initialVolume;
}
public class PlanetFX : MonoBehaviour
{
    public Transform SunLight;
    public Transform SunObj;
    public Renderer SunFlare;
    public Renderer SunModel;
    public float SunScale;
    public Color SunColor;
    public float SunIntensity;
    public float FlareScale;
    //public float FlareAlpha;
    public Color FlareColor;
    public float sunObjDistance;
    public Color DefaultEnvironmentalLightingColor;
    public List<FacadeData> Facades;
    public bool DustActive;
    public List<DustData> Dust;
    public DustAvoidanceReporter DustAvoidanceReporter;

    public List<AtmosphereData> Atmospheres;
    public SkyboxBlender AtmosphereBlender;



    private void Start()
    {
        FacadesStart();
        DustSoundsStart();
    }
    private void FacadesStart()
    {
        Facades.ForEach(x => x.Init());
    }
    private void DustSoundsStart()
    {
        foreach (var dg in Dust)
        {
            dg.AudioData = dg.DustGroup.GetComponentsInChildren<AudioSource>()
                .Select(
                    x => new DustAudioData()
                    {
                        initialVolume = x.volume,
                        source = x
                    }
                ).ToList();
        }
    }
    private void OnPreRender()
    {

        AtmospherePreRender();
        SunPreRender();
        FacadesPreRender();
        DustPreRender();
    }
    AtmosphereData _closestAtmosphere;
    private void SunPreRender()
    {

        SunObj.position = transform.position + (-SunLight.forward * sunObjDistance);
        SunObj.rotation = Quaternion.LookRotation((transform.position - SunObj.position).normalized);
        /*_closestAtmosphere = Atmospheres.FirstOrDefault(x => (transform.position - x.PlanetTransform.position).sqrMagnitude < Mathf.Pow(x.farRadius, 2f));
        if (_closestAtmosphere != null) {
            SunFlare.sharedMaterial.color = new Color(
                    SunFlare.sharedMaterial.color.r,
                    SunFlare.sharedMaterial.color.g,
                    SunFlare.sharedMaterial.color.b,
                    


                    Mathf.Clamp(
                        //angle of sunset is no flare
                        Vector3.Dot(
                            (transform.position - _closestAtmosphere.PlanetTransform.position).normalized,
                            (SunObj.position - _closestAtmosphere.PlanetTransform.position).normalized
                        ),
                        //this is the overall distance to planet  - must be full when we go to space :)
                        Mathf.Clamp(
                            Map(
                                (transform.position - _closestAtmosphere.PlanetTransform.position).sqrMagnitude, 
                                Mathf.Pow(_closestAtmosphere.nearRadius,2f), 0f, 
                                Mathf.Pow(_closestAtmosphere.farRadius,2f), 1f
                            ),
                            .05f,
                            1f
                        ),
                        1f
                    )


                );
        }*/

    }
    private void AtmospherePreRender()
    {
        foreach (var ad in Atmospheres)
        {
            if (ad._initialScale == Vector3.zero)
                ad._initialScale = ad.PlanetAtmosphereTexture.localScale;

            float x = ad.farRadius;


            if ((transform.position - ad.PlanetTransform.position).sqrMagnitude < ad.farRadius * ad.farRadius)
            {
                //x  
                x = (transform.position - ad.PlanetTransform.position).magnitude;
                //m                                                                        
                float mBlend = (ad.farBlendValue - ad.nearBlendValue) / (ad.farRadius - ad.nearRadius);
                float mExposure = (ad.farExposureValue - ad.nearExposureValue) / (ad.farRadius - ad.nearRadius);
                //b         
                float bBlend = ad.farBlendValue - (mBlend * ad.farRadius);
                float bExposure = ad.farExposureValue - (mExposure * ad.farRadius);

                float distanceBlend = (mBlend * x) + bBlend;
                float distanceExposure = (mExposure * x) + bExposure;

                float dayNightFactor =
                    Mathf.Clamp01(
                            Vector3.Dot((transform.position - ad.PlanetTransform.position).normalized, -SunLight.forward)
                     );
                float activeZoneFactor = Map(x, ad.nearRadius, 0, ad.farRadius, 1f);

                float positionalBlend =
                    (
                        dayNightFactor
                        *
                        (distanceBlend - ad.farBlendValue)
                     ) + ad.farBlendValue;
                float positionalExposure = (
                        dayNightFactor
                        *
                        (distanceExposure - ad.farExposureValue)
                     ) + ad.farExposureValue;

                Color tint =
                    Color.Lerp(
                        Color.Lerp(ad.NightTintColor, ad.DayTintColor, dayNightFactor + (dayNightFactor - Mathf.Pow(dayNightFactor, 2f))),
                        Color.white,
                        activeZoneFactor
                    );
                Color sunColor =
                    Color.Lerp(
                        Color.Lerp(ad.SunHorizonColor, ad.SunNoonColor, dayNightFactor + (dayNightFactor - Mathf.Pow(dayNightFactor, 2f))),
                        SunColor,
                        activeZoneFactor
                    );
                Color flareColor =
                    Color.Lerp(
                        Color.Lerp(ad.FlareHorizonColor, ad.FlareNoonColor, dayNightFactor + (dayNightFactor - Mathf.Pow(dayNightFactor, 2f))),
                        FlareColor,
                        activeZoneFactor
                    );
                float planetSunScale = Map(
                        activeZoneFactor,
                        0f,
                        Map(dayNightFactor, 0f, ad.SunScaleHorizon, 1f, ad.SunScaleNoon),
                        1f,
                        SunScale
                    );
                float planetSunFlareScale = Map(
                    activeZoneFactor,
                    0f,
                    Map(dayNightFactor, 0f, ad.flareScaleHorizon, 1f, ad.flareScaleNoon),
                    1f,
                    FlareScale
                );
                float sunIntensity = Map(
                    activeZoneFactor,
                    0f,
                    Map(dayNightFactor, 0f, ad.SunIntensityHorizon, 1f, ad.SunIntensityNoon),
                    1f,
                    SunIntensity
                );
                /*float flareAlpha = Map(
                    tintDistance,
                    0f,
                    Map(tintDayNight, 0f, ad.flareAlphaHorizon, 1f, ad.flareAlphaNoon),
                    1f,
                    FlareAlpha
                );*/
                AtmosphereBlender.exposure = positionalExposure;
                AtmosphereBlender.tint = new Color(tint.r, tint.g, tint.b, 1f);
                AtmosphereBlender.blend = positionalBlend;
                //RenderSettings.subtractiveShadowColor = Color.Lerp(Color.black, Color.Lerp(Color.blue, Color.white, .5f), positionalBlend);
                SunFlare.sharedMaterial.color = flareColor;
                SunModel.sharedMaterial.color = sunColor;
                SunModel.sharedMaterial.SetColor("_EmissionColor",
                    sunColor * sunIntensity
                );
                SunModel.transform.localScale = Vector3.one * planetSunScale;
                SunFlare.transform.localScale = Vector3.one * planetSunFlareScale;
                /*SunFlare.sharedMaterial.color = new Color(
                   flareColor.r,
                   flareColor.g,
                   flareColor.b,
                   flareAlpha
                );*/

                //Debug.Log("active zone factor: " + activeZoneFactor.ToString("G6"));
                //environmental lighting
                RenderSettings.ambientLight = Color.Lerp(
                    Color.Lerp(ad.EnvironmentalLightingColorNight,ad.EnvironmentalLightingColorDay, dayNightFactor),
                    DefaultEnvironmentalLightingColor,
                    activeZoneFactor
                );
            }
            

            ad.PlanetAtmosphereTexture.rotation = Quaternion.LookRotation((ad.PlanetTransform.position - transform.position).normalized);
            ad.PlanetSurfaceAtmosphereTexture.rotation = ad.PlanetAtmosphereTexture.rotation;
            float angle = (Mathf.Deg2Rad * 90f) - Mathf.Acos(ad.nearRadius / x);
            //Debug.Log("angle: " + angle.ToString());
            ad.PlanetAtmosphereTexture.localScale = ad._initialScale * x * Mathf.Tan((Mathf.Deg2Rad * 90f) - Mathf.Acos(Mathf.Clamp01(ad.nearRadius / x))) / ad.nearRadius;

            //SHIFT VALUE
            float shiftFactor = 1f - Mathf.Abs(Vector3.Dot((transform.position - ad.PlanetTransform.position).normalized, -SunLight.forward));
            float shiftDistance = Mathf.Max(x, ad.nearRadius/.9f) * Mathf.Tan((Mathf.Deg2Rad * 90f) - Mathf.Acos(Mathf.Min(ad.nearRadius / x, .9f))) * ad.PlanetAtmosphereShiftMultiplier;



            ad.PlanetAtmosphereTexture.localScale = /*ad.PlanetAtmosphereTexture.localScale **/Vector3.one * (
                ad.PlanetAtmosphereShiftScaler - (shiftDistance / ad.farRadius)
            );

            ad.PlanetAtmosphereTexture.position = ad.PlanetTransform.position + (
                -SunLight.forward * shiftDistance
            );
        }
    }
    private void FacadesPreRender()
    {
        foreach (var f in Facades)
        {

            float sqrDist = (transform.position - f.Facade.position).sqrMagnitude;


            if ((f.opaqueDistance > f.transparentDistance && sqrDist < Mathf.Pow(f.transparentDistance, 2))
                ||
                (f.opaqueDistance < f.transparentDistance && sqrDist > Mathf.Pow(f.transparentDistance, 2))
            )
            {
                foreach (var r in f.GetRenderers())
                    r.enabled = false;
                foreach (var m in f.GetMaterials())
                    m.color = new Color(m.color.r, m.color.g, m.color.b, 0f);
            }
            else
            {
                foreach (var r in f.GetRenderers())
                    r.enabled = true;
                foreach (var m in f.GetMaterials())
                    m.color = new Color(m.color.r, m.color.g, m.color.b,

                            Mathf.Clamp01(Map(sqrDist, Mathf.Pow(f.transparentDistance, 2), 0f, Mathf.Pow(f.opaqueDistance, 2), 1f))
                    );
                /*if (sqrDist > Mathf.Pow(f.opaqueDistance, 2))
                {

                    foreach (var m in f.GetMaterials())
                        m.color = new Color(m.color.r, m.color.g, m.color.b, 1f);
                }
                else
                {
                    float linear = (sqrDist - Mathf.Pow(f.transparentDistance, 2)) / (Mathf.Pow(f.opaqueDistance, 2) - Mathf.Pow(f.transparentDistance, 2));
                    foreach (var m in f.GetMaterials())
                        m.color = new Color(m.color.r, m.color.g, m.color.b, linear);
                }*/
            }
        }

    }

    private void DustInit(DustData data)
    {
        data.DustCloudColor = data.DustGroup.GetComponentInChildren<MeshRenderer>().sharedMaterial.color;
        data.DustParticleColor = data.DustGroup.GetComponentInChildren<ParticleSystemRenderer>().sharedMaterial.color;
    }
    float _camAlpha;
    float _dayNightActivityAlpha;
    float _shadowAlpha;
    ParticleSystem.SizeOverLifetimeModule _sizeOverLifetime;
    ParticleSystem.ColorOverLifetimeModule _colorOverLifetimeModule;
    private void DustPreRender()
    {
        if (!DustActive)
            return;
        foreach (var dustData in Dust)
        {
            if ((dustData.DustGroup.position - transform.position).sqrMagnitude < Mathf.Pow(dustData.GroupActivateRadius, 2f))
            {
                if (!dustData.DustGroup.gameObject.activeSelf)
                {

                    dustData.DustGroup.gameObject.SetActive(true);
                    DustInit(dustData);
                    dustData.DustGroup.GetComponent<PlanetBoidParent>().dustAvoidanceReporter = DustAvoidanceReporter;

                }
                foreach (var d in dustData.DustGroup.GetComponentsInChildren<PlanetBoid>())
                {
                    d.Wrap(transform.position, dustData.UnitFarTransparentRadius);
                }
                //get daynight fade / active fade;
                _dayNightActivityAlpha = //night/day fade
                            Mathf.Pow(Mathf.Clamp01(Vector3.Dot((transform.position - dustData.DustGroup.position).normalized, -SunLight.forward)), 2f)
                            //activate / deactivate entire group fade
                            * Mathf.Clamp01(
                              Map(
                                (transform.position - dustData.DustGroup.position).sqrMagnitude,
                                Mathf.Pow(dustData.GroupActivateRadius, 2f), 0f,
                                Mathf.Pow(dustData.GroupFullVisibilityRadius, 2f), 1f
                              )
                            );
                //cloud
                foreach (var r in dustData.DustGroup.GetComponentsInChildren<MeshRenderer>())
                {
                    //this isn't perfect
                    r.transform.rotation = transform.rotation;
                    //Quaternion.LookRotation((r.transform.position-transform.position).normalized, transform.up);
                    if ((transform.position - r.transform.position).sqrMagnitude <= Mathf.Pow(dustData.UnitCloseOpaqueRadius, 2f))
                    {
                        _camAlpha =
                            //distance from camera fade
                            Mathf.Clamp01(
                                Map(
                                    (transform.position - r.transform.position).sqrMagnitude,
                                    Mathf.Pow(dustData.UnitCloseTransparentRadius, 2f), 0f,
                                    Mathf.Pow(dustData.UnitCloseOpaqueRadius, 2f), 1f
                                )
                            );

                    }
                    else if ((transform.position - r.transform.position).sqrMagnitude > Mathf.Pow(dustData.UnitFarOpaqueRadius, 2f))
                    {
                        _camAlpha =
                            //distance from camera fade
                            Mathf.Clamp01(
                                Map(
                                    (transform.position - r.transform.position).sqrMagnitude,
                                    Mathf.Pow(dustData.UnitFarOpaqueRadius, 2f), 1f,
                                    Mathf.Pow(dustData.UnitFarTransparentRadius, 2f), 0f
                                )
                            );

                    }
                    else
                    {
                        _camAlpha = 1f;
                    }
                    if (r.transform.parent.GetComponent<PlanetBoid>().lit)
                        _shadowAlpha = 1f;
                    else
                        _shadowAlpha = .5f;
                    r.material.color = new Color(
                        dustData.DustCloudColor.r,
                        dustData.DustCloudColor.g,
                        dustData.DustCloudColor.b,
                        _camAlpha * dustData.DayNightAlphaCurve.Evaluate(_dayNightActivityAlpha) * r.transform.parent.GetComponent<PlanetBoid>().HiddenAlpha * _shadowAlpha * dustData.maxAlpha);
                }

                //THIS ONE WORKS THROUGH SHARED MATERIAL
                dustData.DustGroup.GetComponentInChildren<ParticleSystemRenderer>().sharedMaterial.color = new Color(
                    dustData.DustParticleColor.r,
                    dustData.DustParticleColor.g,
                    dustData.DustParticleColor.b,
                    dustData.DayNightAlphaCurve.Evaluate(_dayNightActivityAlpha) * dustData.ParticleMaxAlpha
                //((2f*_dayNightActivityAlpha)-Mathf.Pow(_dayNightActivityAlpha,2f)) * dustData.ParticleMaxAlpha
                );

                //partical shadows?
                foreach (var ps in dustData.DustGroup.GetComponentsInChildren<ParticleSystem>())
                {
                    if (ps.transform.parent.GetComponent<PlanetBoid>().lit /*&& ps.sizeOverLifetime.sizeMultiplier != 1f*/)
                    {
                        _colorOverLifetimeModule = ps.colorOverLifetime;
                        _colorOverLifetimeModule.color = dustData.InLightBlendColor;
                        /*_sizeOverLifetime = ps.sizeOverLifetime;
                        _sizeOverLifetime.sizeMultiplier = 1f;*/
                    }
                    else if (ps.sizeOverLifetime.sizeMultiplier != 0f)
                    {
                        _colorOverLifetimeModule = ps.colorOverLifetime;
                        _colorOverLifetimeModule.color = dustData.InShadowBlendColor;
                        /*_sizeOverLifetime = ps.sizeOverLifetime;
                        _sizeOverLifetime.sizeMultiplier = 0f;*/
                    }
                }
                //sounds?
                for (int i = 0; i < dustData.AudioData.Count; i++)
                {
                    dustData.AudioData[i].source.volume = dustData.AudioData[i].initialVolume * _dayNightActivityAlpha;
                }
            }
            else
            {
                if (dustData.DustGroup.gameObject.activeSelf)
                {
                    dustData.DustGroup.gameObject.SetActive(false);
                    foreach (PlanetBoid b in dustData.DustGroup.GetComponentsInChildren<PlanetBoid>())
                    {
                        b.DustAvoidanceReporters = b.DustAvoidanceReporters.Where(d => d.GetInstanceID() != DustAvoidanceReporter.GetInstanceID()).ToList();
                    }

                    //sounds?
                    for (int i = 0; i < dustData.AudioData.Count; i++)
                    {
                        dustData.AudioData[i].source.volume = 0f;
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        AtmosphereDrawGizmos();
        //FacadesDrawGizmos();
    }
    private void AtmosphereDrawGizmos()
    {
        foreach (var ad in Atmospheres)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(ad.PlanetTransform.position, ad.nearRadius);
            Gizmos.DrawWireSphere(ad.PlanetTransform.position, ad.farRadius);
        }
    }

    private Color[] colors = new Color[4] { Color.red, Color.yellow, Color.green, Color.blue };
    int colorIndex = 0;
    private void FacadesDrawGizmos()
    {
        colorIndex = 0;
        foreach (var f in Facades)
        {
            Gizmos.color = colors[colorIndex];
            Gizmos.DrawWireSphere(f.Facade.position, f.transparentDistance);
            colorIndex++;
            colorIndex = colorIndex % colors.Length;
            Gizmos.color = colors[colorIndex];
            Gizmos.DrawWireSphere(f.Facade.position, f.opaqueDistance);
            colorIndex++;
            colorIndex = colorIndex % colors.Length;
        }
    }

    private float Map(float val, float x1, float y1, float x2, float y2)
    {
        float m = (y2 - y1) / (x2 - x1);
        float b = y2 - (x2 * m);
        return (val * m) + b;
    }


}
