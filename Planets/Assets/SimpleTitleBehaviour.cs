using NoTime.Splitter;
using NoTime.Splitter.Demo;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SimpleTitleBehaviour : MonoBehaviour
{
    public float slowFadeSlowness;
    public float fastFadeFastness;
    public List<Text> texts;
    public List<TextMeshProUGUI> tmpTexts;
    public List<Image> images;
    public List<Text> titleTexts;
    public Image titleImage;
    public Image blackout;
    public RigidbodyFpsController rigidbodyFpsController;
    public SplitterSubscriber subscriber;
    public AudioMixer GameMixer;
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
        for (int i = 0; i < tmpTexts.Count; i++)
        {
            tmpTexts[i].color = new Color(tmpTexts[i].color.r, tmpTexts[i].color.g, tmpTexts[i].color.b, 0);

        }
        if (titleImage != null)
            titleImage.color = new Color(titleImage.color.r, titleImage.color.g, titleImage.color.b, 0);
        for(int i = 0; i < images.Count; i++)
        {
            images[i].color = new Color(images[i].color.r, images[i].color.g, images[i].color.b, 0);
        }
        
    }
    private void Start()
    {
        subscriber.AppliedPhysics.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
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

            if(titleImage != null)
                titleImage.color = new Color(titleImage.color.r, titleImage.color.g, titleImage.color.b, fade*fade*fade);

            blackout.color = new Color(blackout.color.r, blackout.color.g, blackout.color.b, 1f - (fade * fade * fade));

            GameMixer.SetFloat("MasterVolume", (1f - fade) * -80f);

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
            blackout.color = new Color(0f, 0f, 0f, 0f);
            GameMixer.SetFloat("MasterVolume", 0f);
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

            if(titleImage != null)
                titleImage.color = new Color(titleImage.color.r, titleImage.color.g, titleImage.color.b, fade);

            if (fade == 0f)
                state = states.gameFadeIn;
        }
        else if (state == states.gameFadeIn)
        {
            rigidbodyFpsController.enabled = true;
            subscriber.AppliedPhysics.constraints = RigidbodyConstraints.None;
            subscriber.AppliedPhysics.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
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
            for (int i = 0; i < tmpTexts.Count; i++)
            {
                tmpTexts[i].color = new Color(tmpTexts[i].color.r, tmpTexts[i].color.g, tmpTexts[i].color.b, Mathf.Clamp01(fade));

            }
            for (int i = 0; i < images.Count; i++)
            {
                images[i].color = new Color(images[i].color.r, images[i].color.g, images[i].color.b, Mathf.Clamp01(fade));
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
