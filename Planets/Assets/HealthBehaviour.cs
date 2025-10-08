using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using System.Collections.Generic;

public class HealthBehaviour : MonoBehaviour
{
    public float health = 1f;
    public float Lifetime;
    public Image healthBar;
    public Image highlight;
    public Image blackOut;
    private Vector2 initialWidth;
    private RectTransform imageRect;
    private float highlightAlpha;

    public PostProcessVolume healthyVolume;
    public PostProcessVolume lowHealthVolume;

    public HintController hintController;

    private Coroutine flashEvent = null;
    private void Awake()
    {
        deathWait = new WaitForSeconds(1f);
    }
    void Start()
    {
        

        highlightColors = new List<Color>();
        blackoutColors = new List<Color>();
        for(int i = 0; i < 100; i++)
        {
            highlightColors.Add(new Color(highlight.color.r, highlight.color.g, highlight.color.b, ((float)i) / 100f));
            blackoutColors.Add(new Color(0, 0, 0, ((float)i) / 100f));
        }

        StartCoroutine(healthAnimation());
        initialWidth = healthBar.rectTransform.sizeDelta;
        imageRect = healthBar.rectTransform;
    }

    List<Color> blackoutColors;
    private IEnumerator healthAnimation()
    {
        while (true)
        {
            lowHealthVolume.weight = Mathf.Max(0f, (1.5f * ((1f - health) *  (Mathf.Sin(Time.timeSinceLevelLoad) + 1f)/2f) - .5f));
            healthyVolume.weight = 1f - lowHealthVolume.weight;


            blackOut.color = blackoutColors[Mathf.RoundToInt(Mathf.Max(0,1f - (health * 8f))*99)];
            
            yield return null;
        }
    }

    private List<Color> highlightColors;
    private IEnumerator flash()
    {
        while (highlightAlpha < 1f)
        {
            highlightAlpha += Time.deltaTime / .25f;
            if(highlightAlpha > 1f)
            {
                highlightAlpha = 1f;
                break;
            }

            highlight.color = highlightColors[Mathf.RoundToInt(highlightAlpha * 99)];
            yield return null;
        }

        highlight.color = highlight.color = highlightColors[Mathf.RoundToInt(highlightAlpha * 99)]; ;
        yield return null;
        
        while (highlightAlpha > 0f)
        {
            highlightAlpha -= Time.deltaTime / .25f;
            if (highlightAlpha < 0f)
            {
                highlightAlpha = 0f;
                break;
            }
            highlight.color = highlight.color = highlightColors[Mathf.RoundToInt(highlightAlpha * 99)]; ;
            yield return null;
        }

        highlight.color = highlight.color = highlightColors[Mathf.RoundToInt(highlightAlpha * 99)]; ;
        flashEvent = null;
    }
    // Start is called before the first frame update
    

    public float AddHealth(float incoming)
    {
        if (incoming > 0f && flashEvent == null)
            flashEvent = StartCoroutine(flash());

        float ret = incoming;
        if (health + incoming > 1f)
            ret = 1f - health;

        float prevHealth = health;
        health += ret;
        if (health >= .3f && prevHealth < .3f)
            hintController.DeactivateHint(hintType.Health);

        return ret;
    }

    Coroutine reloadCoroutine;
    // Update is called once per frame
    void Update()
    {

        if(health <= 0f || Input.GetKeyDown(KeyCode.Backspace))
        {
            health = 0f;
            if(reloadCoroutine == null)
                reloadCoroutine = StartCoroutine(ReloadScene());
        }

        float prevHealth = health;
        //300 seconds of health
        health -= Time.deltaTime/Lifetime;//1 / 180f * Time.deltaTime;

        if (health < .3f && prevHealth >= .3f)
        {
            hintController.ActivateHint(hintType.Health);
        }

        imageRect.sizeDelta = new Vector2(initialWidth.x * health, initialWidth.y);
    }

    WaitForSeconds deathWait;
    IEnumerator ReloadScene()
    {
        yield return deathWait;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
