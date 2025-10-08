using NoTime.Splitter.Demo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintDestroyer : MonoBehaviour
{
    public HintTrigger trigger;
    public bool DestroyWhenNotColliding = false;
    private bool hasPlayer = false;
    // Start is called before the first frame update
    private RigidbodyFpsController rfps;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out rfps))
        {
            hasPlayer = true;
        }    
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.TryGetComponent(out rfps))
        {
            hasPlayer = false;
        }
    }

    private void Update()
    {
        bool collisionValid = (hasPlayer && !DestroyWhenNotColliding) || (!hasPlayer && DestroyWhenNotColliding);
        bool hintReady = (!trigger.mustBeTriggeredBeforeDestroy || trigger.hasBeenTriggered);
        bool playerPosValid = trigger.PosRequirementForDestruction == HintTrigger.playerCollisionTypes.Either ||
            (trigger.PosRequirementForDestruction == HintTrigger.playerCollisionTypes.PlayerInside && trigger.hasPlayer) ||
            (trigger.PosRequirementForDestruction == HintTrigger.playerCollisionTypes.PlayerOutside && !trigger.hasPlayer);
        if (collisionValid && hintReady && playerPosValid)
        {
            Destroy(trigger.gameObject);
            Destroy(gameObject);
        }
    }
}
