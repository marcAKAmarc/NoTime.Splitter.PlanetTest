using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[Serializable]
public enum hintType
{
    WalkingMovement, Jumping, JetpackOnPlanet, HealthIntro, Health, Pilot, StargemPower, ShipControls, NULL, JetpackInSpace, ShipTut1, ShipTut2, ShipTut3
}

[Serializable]
public struct hintData{
    public string text;
    public string name;
    public hintType type;
    public int priority;
}
public class HintController : MonoBehaviour
{
    public List<hintData> hints;
    private List<hintData> currentHints;
    private hintType currentType;

    public TextMeshProUGUI text;
    public AudioSource source;
    public void Awake()
    {
        //hints = new List<hintData>();
        currentHints = new List<hintData>();
        currentType = hintType.NULL;
    }
    public void ActivateHint(hintType type)
    {
        bool alreadyActive = false;
        for(int i = 0; i < currentHints.Count; i++)
        {
            if (currentHints[i].type == type)
            {
                alreadyActive = true;
                break;
            }
        }
        if (alreadyActive)
            return;

        for(int i = 0; i < hints.Count; i++)
        {
            if (hints[i].type == type)
            {
                currentHints.Add(hints[i]);
                currentHints = currentHints.OrderByDescending(x => x.priority).ToList();
            }
        }
    }

    public void DeactivateHint(hintType type)
    {
        int removeAt = -1;
        for(int i = 0; i < currentHints.Count; i++)
        {
            if (currentHints[i].type == type)
            {
                removeAt = i;
                break;
            }
        }

        if(removeAt != -1)
            currentHints.RemoveAt(removeAt);
    }

    public void Update()
    {
        if(currentType != hintType.NULL && currentHints.Count == 0)
        {
            text.text = "";
            currentType = hintType.NULL;
        }
        
        if(currentHints.Count != 0 && currentType != currentHints[0].type)
        {
            source.Play();
            text.text = currentHints[0].text.Replace("\\n","\n");
            currentType = currentHints[0].type;
        }
    }
}
