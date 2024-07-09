using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenCloseButtonBehaviour : MonoBehaviour
{
    public BrassShipDoorBehavior door;
    private BrassShipDoorBehavior.DoorRequestStates previousState = BrassShipDoorBehavior.DoorRequestStates.open;
    public void OnActivate()
    {
        if (previousState == BrassShipDoorBehavior.DoorRequestStates.open)
            door.DoorRequestState = BrassShipDoorBehavior.DoorRequestStates.closed;
        else
            door.DoorRequestState = BrassShipDoorBehavior.DoorRequestStates.open;

        previousState = door.DoorRequestState;
    }
}
