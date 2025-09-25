using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthProviderBehaviour : MonoBehaviour
{
    private List<Renderer> renderers;
    public float Amt = 1f;
    private Color origColor;
    private float AmtColorBoosted;
    public float BoostAmt = 2f;
    public float currentBoostAmt = 0f;
    
    private float GiveRate = .1f;
    private HealthBehaviour ProvidingTo = null;
    public Image HealthImage;
    public Color HealthImageOrigColor;
    public AudioSource sound;

    private void Awake()
    {
        renderers = new List<Renderer>();
    }
    // Start is called before the first frame update
    void Start()
    {
        HealthImageOrigColor = HealthImage.color;
        foreach (Renderer r in transform.GetComponentsInChildren<MeshRenderer>())
        {
            renderers.Add(r);
        }
        if (renderers.Count > 0)
            origColor = renderers[0].material.GetColor("_EmissionColor");
    }
    
    // Update is called once per frame
    void Update()
    {
        float giveAmt = 0f;
        float takenAmt = 0f;
        if(ProvidingTo != null || currentBoostAmt != 0)
        {
            
            currentBoostAmt -= 4f * BoostAmt * Time.deltaTime;
            if (currentBoostAmt <= 0f)
                currentBoostAmt = 0f;

            if (ProvidingTo != null)
            {
                giveAmt = Mathf.Min(GiveRate * Time.deltaTime, Amt);
                takenAmt = ProvidingTo.AddHealth(giveAmt);
            }
            if(takenAmt < giveAmt)
            {
                ProvidingTo = null;
            }

            Amt -= takenAmt;

            Color newColor, currentColor;
            foreach (Renderer r in renderers)
            {
                currentColor = r.material.GetColor("_EmissionColor");
                newColor =
                    Color.Lerp(
                        Color.Lerp(Color.gray, origColor, Amt),
                        origColor * 2f,
                        currentBoostAmt
                    );
                r.material.SetColor("_EmissionColor", newColor);
            }
            

            if (startSound)
            {
                sound.volume = Amt;
                if (takenAmt == 0)
                    sound.volume = 0;
                sound.Play();
                startSound = false;
            }

        }
        sound.volume -= Time.deltaTime / 5f;
        if (sound.volume < 0f)
            sound.volume = 0f;

    }

    public float Spend(float amount)
    {
        float ret = Mathf.Min(amount, Amt);
        Amt -= amount;
        Mathf.Clamp01(Amt);
        
        Color newColor, currentColor;
        foreach(Renderer r in renderers)
        {
            currentColor = r.material.GetColor("_EmissionColor");
            newColor = Color.Lerp(Color.gray, origColor, Amt);

            r.material.SetColor("_EmissionColor", newColor);
        }

        return ret;
    }
    private bool startSound;
    public void OnActivate(Transform playerT)
    {
        ProvidingTo = playerT.gameObject.GetComponent<HealthBehaviour>();
        currentBoostAmt = Amt * BoostAmt;
        
        if(Amt > 0)
        {
            startSound = true;
        }
    }
}
