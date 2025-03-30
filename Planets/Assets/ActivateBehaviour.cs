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

    private object[] parameters = new object[1];
    public void Activate(Transform passThroughTransform)
    {
        parameters[0] = passThroughTransform;
        for (int i = 0; i < Scripts.Count; i++)
        {
            Type thisType = Scripts[i].GetType();
            MethodInfo theMethod = thisType.GetMethod("OnActivate");
            theMethod.Invoke(Scripts[i], parameters);
        }
    }

}
