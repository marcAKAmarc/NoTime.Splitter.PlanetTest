using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

public class PlanetFX : MonoBehaviour
{
    public List<FacadeData> Facades;
    // Start is called before the first frame update
    private void Start()
    {
        Facades.ForEach(x => x.Init());
    }
    private void OnPreRender()
    {
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

    private Color[] colors = new Color[4] { Color.red, Color.yellow, Color.green, Color.blue};
    int colorIndex = 0;
    private void OnDrawGizmosSelected()
    {
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
}
