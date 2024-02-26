using UnityEngine;

public class InteriorLightBehavior : MonoBehaviour
{
    public float AlphaColor1Original;
    public float AlphaColor2Original;
    public float AlphaColor1Off = 242f / 255f;
    public float AlphaColor2Off = 0f;
    public MeshRenderer MeshRenderer;
    public float FadeTime;
    private bool On;
    private float SwitchTime;
    // Start is called before the first frame update
    private void Awake()
    {
        AlphaColor1Original = MeshRenderer.material.GetColor("_color1").a;
        AlphaColor2Original = MeshRenderer.material.GetColor("_color2").a;
    }

    Color _Color1;
    Color _Color2;
    // Update is called once per frame
    void Update()
    {
        if (Time.time - SwitchTime < FadeTime)
        {
            if (On)
            {
                //Debug.Log("Tweening on");
                _Color1 = MeshRenderer.material.GetColor("_color1");
                _Color1 = new Color(_Color1.r, _Color1.g, _Color1.b, Mathf.Min(_Color1.a + (1f - AlphaColor1Off) * Time.deltaTime / FadeTime, AlphaColor1Original));
                MeshRenderer.material.SetColor("_color1", _Color1);

                _Color2 = MeshRenderer.material.GetColor("_color2");
                _Color2 = new Color(_Color2.r, _Color2.g, _Color2.b, Mathf.Min(_Color2.a + (1f - AlphaColor2Off) * Time.deltaTime / FadeTime, AlphaColor2Original));
                MeshRenderer.material.SetColor("_color2", _Color2);
            }

            if (!On)
            {
                //Debug.Log("Tweening off");
                _Color1 = MeshRenderer.material.GetColor("_color1");
                _Color1 = new Color(_Color1.r, _Color1.g, _Color1.b, Mathf.Max(_Color1.a - (1f - AlphaColor1Off) * Time.deltaTime / FadeTime, 0f));
                MeshRenderer.material.SetColor("_color1", _Color1);

                _Color2 = MeshRenderer.material.GetColor("_color2");
                _Color2 = new Color(_Color2.r, _Color2.g, _Color2.b, Mathf.Max(_Color2.a - (1f - AlphaColor2Off) * Time.deltaTime / FadeTime, 0f));
                MeshRenderer.material.SetColor("_color2", _Color2);
            }
        }
    }

    public void Switch(bool val)
    {
        //Debug.Log("Switching...");
        if (!On && val == true)
        {
            //Debug.Log("On");
            SwitchTime = Time.time;
            On = true;
        }
        else if (On && val == false)
        {
            Debug.Log("Off");
            SwitchTime = Time.time;
            On = false;
        }
    }
}
