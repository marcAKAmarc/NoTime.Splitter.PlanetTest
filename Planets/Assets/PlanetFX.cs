using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

[Serializable]
public class FacadeData
{
    public Transform Facade;
    private Material[] Materials;
    public float transparentDistance;
    public float opaqueDistance;

    public Material[] GetMaterials()
    {
        return Materials;
    }
    public void Init()
    {
        Materials = Facade.GetComponentsInChildren<Renderer>().Select(r=>r.material).ToArray();
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
    public Transform PlanetAtmosphereTexture;
    public Vector3 _initialScale;
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

    [HideInInspector]
    public Color DustCloudColor;
    [HideInInspector]
    public Color DustParticleColor;
}
public class PlanetFX : MonoBehaviour
{
    public Transform SunLight;
    public List<FacadeData> Facades;
    public List<DustData> Dust;
    public DustAvoidanceReporter DustAvoidanceReporter;

    public List<AtmosphereData> Atmospheres;
    public SkyboxBlender AtmosphereBlender;
    // Start is called before the first frame update
    private void Start()
    {
        FacadesStart();
    }
    private void FacadesStart() { 
        Facades.ForEach(x => x.Init());
    }

    private void OnPreRender()
    {
        AtmospherePreRender();
        FacadesPreRender();
        DustPreRender();
    }
    private void AtmospherePreRender()
    {
        foreach(var ad in Atmospheres)
        {
            if (ad._initialScale == Vector3.zero)
                ad._initialScale = ad.PlanetAtmosphereTexture.localScale;

            float x = ad.farRadius;
           

            if( (transform.position - ad.PlanetTransform.position).sqrMagnitude < ad.farRadius * ad.farRadius)
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
                float positionalBlend =
                    (
                        Mathf.Clamp01(
                            Vector3.Dot((transform.position - ad.PlanetTransform.position).normalized, -SunLight.forward)
                        )
                        *
                        (distanceBlend - ad.farBlendValue)
                     ) + ad.farBlendValue;
                float positionalExposure = (
                        Mathf.Clamp01(
                            Vector3.Dot((transform.position - ad.PlanetTransform.position).normalized, -SunLight.forward)
                        )
                        *
                        (distanceExposure - ad.farExposureValue)
                     ) + ad.farExposureValue;

                AtmosphereBlender.blend = positionalBlend;
                AtmosphereBlender.exposure = positionalExposure;
            }

            ad.PlanetAtmosphereTexture.rotation = Quaternion.LookRotation( (ad.PlanetTransform.position - transform.position).normalized);

            float angle = (Mathf.Deg2Rad * 90f) - Mathf.Acos(ad.nearRadius / x);
            //Debug.Log("angle: " + angle.ToString());
            ad.PlanetAtmosphereTexture.localScale = ad._initialScale * x * Mathf.Tan((Mathf.Deg2Rad*90f) - Mathf.Acos(ad.nearRadius / x))/ad.nearRadius;

            //SHIFT VALUE
            float shiftFactor = 1f - Mathf.Abs(Vector3.Dot((transform.position - ad.PlanetTransform.position).normalized, -SunLight.forward));
            float shiftDistance = x * Mathf.Tan((Mathf.Deg2Rad * 90f) - Mathf.Acos(ad.nearRadius / x)) * .333f;

            ad.PlanetAtmosphereTexture.position = ad.PlanetTransform.position + (
                -SunLight.forward * shiftDistance
            );

            ad.PlanetAtmosphereTexture.localScale = ad.PlanetAtmosphereTexture.localScale * (
                ((1f - (shiftDistance/ad.nearRadius))*.25f) + .25f
            ) * 2f;
        }
    }
    private void FacadesPreRender() { 
        foreach(var f in Facades)
        {

            float sqrDist = (transform.position - f.Facade.position).sqrMagnitude;

           
            if(sqrDist < Mathf.Pow(f.transparentDistance, 2))
            {
                foreach(var m in f.GetMaterials())
                    m.color = new Color(m.color.r, m.color.g, m.color.b, 0f);
            }
            else if(sqrDist > Mathf.Pow(f.opaqueDistance,2))
            {
                foreach (var m in f.GetMaterials())
                    m.color = new Color(m.color.r, m.color.g, m.color.b, 1f);
            }
            else
            {
                float linear = (sqrDist - Mathf.Pow(f.transparentDistance, 2)) / (Mathf.Pow(f.opaqueDistance, 2) - Mathf.Pow(f.transparentDistance, 2));
                //linear = (2 * linear) - (linear * linear);
                foreach (var m in f.GetMaterials())
                    m.color = new Color(m.color.r, m.color.g, m.color.b, linear);
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
    private void DustPreRender()
    {
        foreach(var dustData in Dust)
        {
            if((dustData.DustGroup.position - transform.position).sqrMagnitude < Mathf.Pow(dustData.GroupActivateRadius, 2f))
            {
                if (!dustData.DustGroup.gameObject.activeSelf)
                {

                    dustData.DustGroup.gameObject.SetActive(true);
                    DustInit(dustData);
                    dustData.DustGroup.GetComponent<PlanetBoidParent>().dustAvoidanceReporter = DustAvoidanceReporter;

                }
                foreach(var d in dustData.DustGroup.GetComponentsInChildren<PlanetBoid>())
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
                    if ((transform.position - r.transform.position).sqrMagnitude <= Mathf.Pow(dustData.UnitCloseOpaqueRadius,2f))
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

                    r.material.color = new Color(dustData.DustCloudColor.r, dustData.DustCloudColor.g, dustData.DustCloudColor.b, _camAlpha * _dayNightActivityAlpha * dustData.maxAlpha);                    
                }

                //THIS ONE WORKS THROUGH SHARED MATERIAL
                dustData.DustGroup.GetComponentInChildren<ParticleSystemRenderer>().sharedMaterial.color = new Color(
                    dustData.DustParticleColor.r,
                    dustData.DustParticleColor.g,
                    dustData.DustParticleColor.b,
                    _dayNightActivityAlpha * dustData.ParticleMaxAlpha
                );
                
                
            }
            else
            {
                if (dustData.DustGroup.gameObject.activeSelf)
                {
                    dustData.DustGroup.gameObject.SetActive(false);
                    foreach (PlanetBoid b in dustData.DustGroup.GetComponentsInChildren<PlanetBoid>())
                    {
                        b.DustAvoidanceReporters = b.DustAvoidanceReporters.Where(d=>d.GetInstanceID() != DustAvoidanceReporter.GetInstanceID()).ToList();
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
        foreach(var ad in Atmospheres)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(ad.PlanetTransform.position, ad.nearRadius);
            Gizmos.DrawWireSphere(ad.PlanetTransform.position, ad.farRadius);
        }
    }

    private Color[] colors = new Color[4] { Color.red, Color.yellow, Color.green, Color.blue };
    int colorIndex = 0;
    private void FacadesDrawGizmos() { 
        colorIndex = 0;
        foreach(var f in Facades)
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
