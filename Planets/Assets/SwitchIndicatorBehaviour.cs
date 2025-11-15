using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchIndicatorBehaviour : MonoBehaviour
{
    
    public List<float> to;
    public float JourneyTime;
    private float JourneyStart;
    private float currentJourney;
    private float startEmission;

    public MeshRenderer meshRenderer;
    private Material m;
    private float mRed, mGreen, mBlue;

    public FlightController fController;
    private float intensityMultiplier = 0f;
    private void InitMaterial()
    {
        if (m == null)
        {
            m = meshRenderer.material;
            mRed = m.GetColor("_EmissionColor").r;
            mGreen = m.GetColor("_EmissionColor").g;
            mBlue = m.GetColor("_EmissionColor").b;
        }
    }
    private void OnEnable()
    {
        InitMaterial();
        InitJourney();
    }

    private void OnDisable()
    {
        CycleTos();
    }

    bool wasPowered = false;
    // Update is called once per frame
    void Update()
    {
        if (fController.PoweredByDrainer != wasPowered)
        {
            InitJourney();
        }

        float goal = to[0];
        if (!fController.PoweredByDrainer)
            goal = 0f;

        currentJourney = (Time.time - JourneyStart) / JourneyTime;
        if (currentJourney > 1f)
            currentJourney = 1f;

        intensityMultiplier = ((goal - startEmission) * currentJourney) +startEmission;

        m.SetColor(
            "_EmissionColor",
            new Color(mRed * intensityMultiplier, mGreen * intensityMultiplier, mBlue * intensityMultiplier, 1f)
        );
        

        wasPowered = fController.PoweredByDrainer;
    }

    public void OnActivate()
    {
        if (this.enabled == false)
            this.enabled = true;
        else if (this.enabled == true)
        {
            CycleTos();
            InitJourney();
        }

    }

    private void InitJourney()
    {
        JourneyStart = Time.time;
        startEmission = intensityMultiplier;
    }

    private void CycleTos()
    {
        //cycle tos
        float oldStart = to[0];
        to.RemoveAt(0);
        to.Add(oldStart);
    }
}
