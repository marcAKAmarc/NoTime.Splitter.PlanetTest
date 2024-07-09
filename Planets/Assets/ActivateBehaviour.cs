using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ActivateBehaviour : MonoBehaviour
{
    public List<MonoBehaviour> Scripts;
    public void Activate()
    {
        for (int i = 0; i < Scripts.Count; i++)
        {
            Type thisType = Scripts[i].GetType();
            MethodInfo theMethod = thisType.GetMethod("OnActivate");
            theMethod.Invoke(Scripts[i], null);
        }
    }
    
}
