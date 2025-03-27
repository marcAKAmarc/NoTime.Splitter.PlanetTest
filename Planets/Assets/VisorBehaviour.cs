using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ImageAndAlpha
{
    public Image image;
    public float initialAlpha;

    public void setInitialAlpha ()
    {
        initialAlpha = image.color.a;
    }

    public float getInitialAlpha()
    {
        return initialAlpha;
    }

}
public class VisorBehaviour : MonoBehaviour
{
    public bool visorOn;
    
    public RectTransform myTransform;
    public Camera playerCamera;
    public Transform LookTransform;
    public Vector2 originalPos;

    public List<ImageAndAlpha> images;
    public float FadeMultiplier;

    // Start is called before the first frame update
    void Start()
    {
        originalPos = myTransform.anchoredPosition;

        for(int i = 0; i < images.Count; i++)
        {
            images[i].setInitialAlpha();
            images[i].image.color =
                        new Color(images[i].image.color.r,
                            images[i].image.color.g,
                            images[i].image.color.b,
                            0f
                        );
        }
    }

    // Update is called once per frame
    public float _degrees;
    public float _viewConst;
    public float _amtFromCenter;
    void LateUpdate()
    {
        _degrees = LookTransform.localEulerAngles.x;
        if (_degrees > 180f)
            _degrees -= 360f;
        if (_degrees < -180f)
            _degrees += 360f;
        /*if(playerCamera.aspect > 1)
            _viewConst = playerCamera.fieldOfView / playerCamera.aspect;
        else
            _viewConst = playerCamera.fieldOfView * playerCamera.aspect;*/
        _viewConst = playerCamera.fieldOfView;
        _amtFromCenter = _degrees/_viewConst;
        
        myTransform.anchoredPosition = new Vector2(originalPos.x, originalPos.y +  (_amtFromCenter* myTransform.sizeDelta.y));
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            visorOn = !visorOn;
            if (currentVisorEvent != null)
                StopCoroutine(currentVisorEvent);
            currentVisorEvent = VisorEvent(visorOn);
            StartCoroutine(currentVisorEvent);    
        }
    }

    private WaitForEndOfFrame wait = new WaitForEndOfFrame();
    IEnumerator currentVisorEvent = null;
    IEnumerator VisorEvent(bool on)
    {
        Debug.Log("Starting Visor on=" + on);
        while (true)
        {
            for (int i = 0; i < images.Count; i++)
            {
                if (on)
                {
                    images[i].image.color =
                        new Color(images[i].image.color.r,
                            images[i].image.color.g,
                            images[i].image.color.b,
                            images[i].image.color.a + ((images[i].getInitialAlpha() - images[i].image.color.a) * Mathf.Min(Time.deltaTime * FadeMultiplier, 1f))
                        );
                }
                else
                {
                    images[i].image.color =
                        new Color(images[i].image.color.r,
                            images[i].image.color.g,
                            images[i].image.color.b,
                            images[i].image.color.a - (images[i].image.color.a * Mathf.Min(Time.deltaTime * FadeMultiplier, 1f))
                        );
                }
            }

            if (on)
            {
                if (images[0].image.color.a > images[0].getInitialAlpha() - .01f)
                {

                    break;
                }

            }
            else
            {
                if (images[0].image.color.a < .01f)
                {
                    break;
                }
            }
            yield return wait;
        }

        //FINALLY
        //set to what they should be
        for (int i = 0; i < images.Count; i++)
        {
            if (on)
            {
                images[i].image.color =
                        new Color(images[i].image.color.r,
                            images[i].image.color.g,
                            images[i].image.color.b,
                            images[i].getInitialAlpha()
                        );
            }
            else
            {
                images[i].image.color =
                        new Color(images[i].image.color.r,
                            images[i].image.color.g,
                            images[i].image.color.b,
                            0f
                        );
            }
        }
    }
}
