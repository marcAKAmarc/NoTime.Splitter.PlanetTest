using NoTime.Splitter.Demo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintTrigger : MonoBehaviour
{
    public HintController hintController;
    public hintType hintType;

    public bool mustBeTriggeredBeforeDestroy;
    public bool hasBeenTriggered;
    public enum playerCollisionTypes {Either, PlayerInside, PlayerOutside}
    public playerCollisionTypes PosRequirementForDestruction;

    public bool hasPlayer = false;

    private RigidbodyFpsController rfps;
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out rfps))
        {
            hintController.ActivateHint(hintType);
            hasBeenTriggered = true;
            hasPlayer = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out rfps))
        {
            hintController.DeactivateHint(hintType);
            hasPlayer = false;
        }
    }

    private void OnDestroy()
    {
        hintController.DeactivateHint(hintType);
    }
}
