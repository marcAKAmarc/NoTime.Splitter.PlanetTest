using NoTime.Splitter.Demo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleTitleBehaviour : MonoBehaviour
{
    public float slowFadeSlowness;
    public float fastFadeFastness;
    public List<Text> texts;
    public List<Text> titleTexts;
    public Image titleImage;
    public RigidbodyFpsController rigidbodyFpsController;
    private enum states { fadeIn, initial, fadeOut, gameFadeIn, game}
    private states state = states.fadeIn;
    // Start is called before the first frame update
    void Awake()
    {
        rigidbodyFpsController.enabled = false;
        for (int i = 0; i < titleTexts.Count; i++)
        {
            titleTexts[i].color = new Color(titleTexts[i].color.r, titleTexts[i].color.g, titleTexts[i].color.b, 0);
        }
        for (int i = 0; i <texts.Count; i++)
        {
            texts[i].color = new Color(texts[i].color.r, texts[i].color.g, texts[i].color.b, 0);
        }
        titleImage.color = new Color(titleImage.color.r, titleImage.color.g, titleImage.color.b, 0);
        
    }

    // Update is called once per frame
    float fade = 0;
    void Update()
    {
        if(state == states.fadeIn)
        {
            if (Input.anyKeyDown)
            {
                //fade = 1;
                state = states.fadeOut;
            }
            else
                fade += Time.deltaTime / slowFadeSlowness;

            if (fade > 1f)
                fade = 1f;

            for (int i = 0; i < titleTexts.Count; i++)
            {
                titleTexts[i].color = new Color(titleTexts[i].color.r, titleTexts[i].color.g, titleTexts[i].color.b, fade*fade*fade);
            }
            titleImage.color = new Color(titleImage.color.r, titleImage.color.g, titleImage.color.b, fade*fade*fade);

            if (fade == 1f)
                state = states.initial;
        }
        else if (state == states.initial) {
            if (Input.anyKeyDown)
            {
                state = states.fadeOut;
            }
        }
        else if (state == states.fadeOut)
        {
            if (Input.anyKeyDown)
                fade = 0;
            else
                fade -= Mathf.Clamp01(Time.deltaTime / fastFadeFastness);
            if (fade < 0f)
                fade = 0f;

            for (int i = 0; i < titleTexts.Count; i++)
            {
                titleTexts[i].color = new Color(titleTexts[i].color.r, titleTexts[i].color.g, titleTexts[i].color.b, fade);
            }
            titleImage.color = new Color(titleImage.color.r, titleImage.color.g, titleImage.color.b, fade);

            if (fade == 0f)
                state = states.gameFadeIn;
        }
        else if (state == states.gameFadeIn)
        {
            rigidbodyFpsController.enabled = true;

            if (Input.anyKeyDown)
                fade = 1;
            else
                fade += Time.deltaTime / fastFadeFastness;
            if (fade > 1f)
                fade = 1f;

            for (int i = 0; i < texts.Count; i++)
            {
                texts[i].color = new Color(texts[i].color.r, texts[i].color.g, texts[i].color.b, Mathf.Clamp01(fade));
            }

            if (fade == 1f)
                state = states.game;
        }
        if(state == states.game)
        {
            fade = 0;
        }
    }


}
