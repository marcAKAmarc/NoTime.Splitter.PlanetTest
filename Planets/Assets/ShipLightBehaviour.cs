using System.Collections;
using UnityEngine;

public class ShipLightBehaviour : MonoBehaviour
{
    public DrainerBehaviour Drainer;
    public Light Light;
    public Color ColorNormal;
    public Color ColorEmergency;
    public Renderer BulbRenderer;
    private enum States { Normal, Blink};
    private States State = States.Blink;
    // Start is called before the first frame update
    void Start()
    {
        Drainer.PowerEvents += OnPowerEvent;
        
        State = States.Blink;

        _blinkLightRoutine = BlinkLightRoutine();
        StartCoroutine(_blinkLightRoutine);
    }

    public void OnPowerEvent(bool val)
    {
        Light.color = ColorEmergency;
        BulbRenderer.material.SetColor("_EmissionColor", Light.color * 2f);
        Light.enabled = val;
        if (val == false)
        {
            StartCoroutine(_blinkLightRoutine);
        }
        else
        {
            StopCoroutine(_blinkLightRoutine);
            Light.color = ColorNormal;
            BulbRenderer.material.SetColor("_EmissionColor", Light.color * 1.25f);
            Light.enabled = true;
        }

    }


    private WaitForSeconds waitOn = new WaitForSeconds(.25f);
    private WaitForSeconds waitOff = new WaitForSeconds(.75f);
    private IEnumerator _blinkLightRoutine;
    public IEnumerator BlinkLightRoutine()
    {
        Light.color = ColorEmergency;
        BulbRenderer.material.SetColor("_EmissionColor", Light.color * 2f);
        Light.enabled = true;
        while(true)
        {
            yield return waitOn;
            if (State == States.Normal)
                break;
            Light.enabled = false;
            BulbRenderer.material.SetColor("_EmissionColor", Color.black);
            yield return waitOff;
            if (State == States.Normal)
                break;
            Light.enabled = true;
            BulbRenderer.material.SetColor("_EmissionColor", Light.color * 2f);

        }
    }
}
