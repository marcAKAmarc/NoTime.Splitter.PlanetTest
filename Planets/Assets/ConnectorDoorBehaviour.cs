using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectorDoorBehaviour : MonoBehaviour
{

    public GameObject doorColliderGO;
    public List<Transform> doorManipulators;
    private SplitterAnchor oaDoorAnchor;
    private SplitterSubscriber oaDoorSub;

    private float goal = 0;
    private float journey = 0;
    public float DoorOpenTime = 1f;
    public void OnActivate()
    {
        //colliders
        doorColliderGO.SetActive(!doorColliderGO.activeSelf);
        if (transform.TryGetComponentInParent(out oaDoorAnchor))
        {
            oaDoorAnchor.GetMatchedAnchorTransform(doorColliderGO.transform).gameObject.SetActive(doorColliderGO.activeSelf);
        }

        if (
            transform.TryGetComponentInParent(out oaDoorSub)
            && oaDoorSub.Anchor != null
        )
        {
            oaDoorSub.Anchor.GetMatchedSubscriberTransform(oaDoorSub, doorColliderGO.transform).gameObject.SetActive(doorColliderGO.activeSelf);
        }
    }

    private int doorI;
    private void Update()
    {
        if (doorColliderGO.activeSelf && goal != 0f)
            goal = 0f;
        else if (!doorColliderGO.activeSelf && goal != 1f)
            goal = 1f;

        if(journey != goal)
        {
            journey += Mathf.Sign(goal - journey) * (Time.deltaTime/DoorOpenTime);
            if (journey > 1f)
                journey = 1f;
            if (journey < 0f)
                journey = 0f;

            for(doorI = 0; doorI < doorManipulators.Count; doorI++)
            {
                doorManipulators[doorI].localScale = 
                    Vector3.Lerp(Vector3.up + Vector3.forward, Vector3.one, journey);
            }
        }
    }
}
