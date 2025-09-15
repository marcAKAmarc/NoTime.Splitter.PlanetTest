using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSuctionBehaviour : MonoBehaviour
{
    public SuctionBehaviour SuctionBehaviour;
    public GameObject SuctionParticles;
    public void OnActivate()
    {
        SuctionBehaviour.enabled = !SuctionBehaviour.enabled;
        SuctionParticles.SetActive(SuctionBehaviour.enabled);
    }
}
