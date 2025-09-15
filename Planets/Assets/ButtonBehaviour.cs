using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonBehaviour : MonoBehaviour
{
    private Material material;
    private Color OriginalEmission;
    public Color TapEmission;
    private float Journey = 0f;
    // Start is called before the first frame update
    void Start()
    {
        material = transform.GetComponent<Renderer>().material;
        OriginalEmission = material.GetColor("_EmissionColor");
    }

    public void OnActivate()
    {
        Journey = 1f;
    }
    // Update is called once per frame
    void Update()
    {
        if(Journey != 0f)
        {
            material.SetColor("_EmissionColor",
                Color.Lerp(OriginalEmission, TapEmission, Journey)
            );
            Journey -= Time.deltaTime;
            if (Journey < 0f)
                Journey = 0f;
        }
    }
}
