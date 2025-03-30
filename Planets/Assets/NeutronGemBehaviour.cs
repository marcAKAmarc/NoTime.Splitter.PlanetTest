using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NeutronGemBehaviour : MonoBehaviour
{
    public bool excited = false;
    public Collider attractionTrigger;
    public float attractionForce;
    public int maxHistory;
    private List<HistoryItem> LocalAttracteds;
    private List<HistoryItem> RemoteAttracteds;
    private SplitterSubscriber mySubscriber;

    private class HistoryItem
    {
        public SplitterSubscriber Attracted;
        public List<Vector3> errors;
    }

    //this is like D in PID
    private void Awake()
    {
        LocalAttracteds = new List<HistoryItem>();
        RemoteAttracteds = new List<HistoryItem>();
    }
    void Start()
    {
        mySubscriber = transform.GetComponentInParent<SplitterSubscriber>();
    }

    private int rrgI;
    public void RegisterRemoteGem(SplitterSubscriber remoteSubscriber)
    {
        //make sure this subscriber is not already in remotes
        for(rrgI = 0; rrgI < RemoteAttracteds.Count; rrgI++)
        {
            if (RemoteAttracteds[rrgI].Attracted == remoteSubscriber)
                return;
        }

        RemoteAttracteds.Add(new HistoryItem()
        {
            Attracted = remoteSubscriber,
            errors = new List<Vector3>()
        });
    }
    private int urgI;
    public void UnregisterRemoteGem(SplitterSubscriber remoteSubscriber)
    {
        for (urgI = 0; urgI < RemoteAttracteds.Count; urgI++)
        {
            if (RemoteAttracteds[urgI].Attracted == remoteSubscriber)
            {
                RemoteAttracteds.RemoveAt(urgI);
                return;
            }
        }
    }
    public void ClearRemoteGems()
    {
        RemoteAttracteds.Clear();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (IsOtherTypeOfGem(other.attachedRigidbody.tag))
        {
            LocalAttracteds.Add(
                new HistoryItem()
                {
                    Attracted = other.attachedRigidbody.GetComponent<SplitterSubscriber>(),
                    errors = new List<Vector3>()
                }
            );
        }
    }

    private SplitterSubscriber teSub;
    private int i;
    private void OnTriggerExit(Collider other)
    {
        if (IsOtherTypeOfGem(other.attachedRigidbody.tag))
        {
            teSub = other.attachedRigidbody.GetComponent<SplitterSubscriber>();
            for (i = 0; i < LocalAttracteds.Count; i++)
            {
                if (LocalAttracteds[i].Attracted == teSub)
                {
                    LocalAttracteds.RemoveAt(i);
                    break;
                }
            }
        }
    }
    int fuI;
    private void FixedUpdate()
    {
        if (excited)
        {
            for (fuI = 0; fuI < LocalAttracteds.Count; fuI++)
            {
                ApplyForces(LocalAttracteds[fuI]);
            }
            for (fuI = 0; fuI < RemoteAttracteds.Count; fuI++)
            {
                ApplyForces(RemoteAttracteds[fuI]);
            }
        }
    }

    int afI;
    Vector3 afSum;
    private void ApplyForces(HistoryItem history)
    {
        //update errors
        history.errors.Insert(
            0,
            history.Attracted.AppliedPhysics.position
                - mySubscriber.AppliedPhysics.position
        );
        //ensure maxHistory
        while (history.errors.Count > maxHistory)
        {
            history.errors.RemoveAt(history.errors.Count - 1);
        }
        //get sum
        afSum = Vector3.zero;
        for (afI = 0; afI < history.errors.Count; afI++)
        {
            afSum += history.errors[afI];
        }
        //apply force
        history.Attracted.AppliedPhysics.AddForce(
            (-afSum)
            * attractionForce * 100f / maxHistory
        );

        mySubscriber.AppliedPhysics.AddForce(
            afSum
            * attractionForce * 100f / maxHistory
        );
    }
    
    private bool IsOtherTypeOfGem(string tag)
    {
        return tag == "PositronGem" || tag == "NegatronGem";
    }
}
