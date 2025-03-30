using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StarGemBehaviour : MonoBehaviour
{
    private GemExciterBehaviour gemExciter;
    private List<Material> materials;

    private Color ExciteColor;
    private Color GroundColor;

    private void Start()
    {
        gemExciter = transform.GetComponentInChildren<GemExciterBehaviour>();
        materials = transform.GetComponentsInChildren<Renderer>()
            .Select(r => r.material).ToList();

        ExciteColor = materials[0].GetColor("_EmissionColor");
        GroundColor = Color.Lerp(ExciteColor, Color.gray, .2f);
    }

    private bool prevExciting;
    private int m;
    public void Update()
    {
        if(gemExciter.GetExcitingCount() > 0 != prevExciting)
        {
            for(m = 0; m < materials.Count; m++)
            {
                if (gemExciter.GetExcitingCount() > 0)
                    materials[m].SetColor("_EmissionColor", ExciteColor);
                else
                    materials[m].SetColor("_EmissionColor", GroundColor);
            }
        }
        prevExciting = gemExciter.GetExcitingCount() > 0;
    }

 
}
