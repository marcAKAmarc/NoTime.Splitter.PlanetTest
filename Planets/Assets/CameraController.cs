using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera camera;
    [Serializable]
    public enum CameraStateType {Standard, Pilot}
    [Serializable]
    public struct CameraState
    {
        public CameraStateType type;
        public float FOV;
        public int priority;
    }

    public List<CameraState> cameraStates;
    private List<CameraState> currentStateStack;


    public void Awake()
    {
        currentStateStack = new List<CameraState>();
        AddState(CameraStateType.Standard);
    }

    public void AddState(CameraStateType type)
    {
        //check if already there
        for(int i = 0; i < currentStateStack.Count; i++)
        {
            if (currentStateStack[i].type == type)
                return;
        }

        addStateByPriority(getStateType(type));

        fovProcessing = true;
    }

    public void RemoveState(CameraStateType type)
    {
        int removeAt = -1;
        for(int i = 0; i  < currentStateStack.Count; i++)
        {
            if (currentStateStack[i].type == type)
            {
                removeAt = i;
                break;
            }
        }

        if (removeAt == -1)
            return;

        currentStateStack.RemoveAt(removeAt);

        fovProcessing = true;
    }

    private CameraState getStateType(CameraStateType type)
    {
        for(int i = 0; i < cameraStates.Count; i++)
        {
            if (cameraStates[i].type == type)
                return cameraStates[i];
        }
        throw new Exception("Could not find type of CameraState " + type.ToString());
    }

    private void addStateByPriority(CameraState state)
    {
        if(currentStateStack.Count == 0)
        {
            currentStateStack.Add(state);
            return;
        }

        int insertIndex = -1;
        for(int i = 0; i < currentStateStack.Count; i++)
        {
            if (currentStateStack[i].priority <= state.priority)
                insertIndex = i;
        }

        if(insertIndex == -1)
        {
            currentStateStack.Add(state);
            return;
        }
        else
        {
            currentStateStack.Insert(insertIndex, state);
        }
    }

    bool fovProcessing = false;
    public void Update()
    {
        if (fovProcessing)
        {
            float newFOV = camera.fieldOfView + ((currentStateStack[0].FOV - camera.fieldOfView) / 5f);
            if (Mathf.Abs(newFOV - currentStateStack[0].FOV) <= .01f)
            {
                newFOV = currentStateStack[0].FOV;
                fovProcessing = false;
            }
            camera.fieldOfView = newFOV;
        }
    }
}
