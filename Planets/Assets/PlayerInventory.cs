using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int LifeResources = 0;
    
    public void PutLifeResource()
    {
        LifeResources += 1;
        Debug.Log("LifeResources: " + LifeResources.ToString());
    }
}
