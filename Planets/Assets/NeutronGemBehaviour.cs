using NoTime.Splitter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class HistoryItem
{
    public SplitterSubscriber Attracted;
    public List<Vector3> errors;
}
public class NeutronGemBehaviour : MonoBehaviour
{
    public bool excited = false;
    private int excitements = 0;
    public Collider attractionTrigger;
    public float attractionForce;
    public int maxHistory;
    public float maxAcceleration;
    public Transform PositionalOverride;
    private List<HistoryItem> LocalAttracteds;
    private List<HistoryItem> RemoteAttracteds;
    private SplitterSubscriber mySubscriber;



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
    public void AddExcitement(bool add)
    {
        if (add)
            excitements += 1;
        else
            excitements -= 1;

        if (excitements > 0)
            excited = true;
        if (excitements <= 0)
            excited = false;
    }

    private int otentI;
    private SplitterSubscriber otentSub;
    private void OnTriggerEnter(Collider other)
    {
        if (
            IsOtherTypeOfGem(other.attachedRigidbody.tag)
            && other.attachedRigidbody.TryGetComponent(out otentSub)    
        )
        {
            //make sure not already in local attracteds
            for (otentI = 0; otentI < LocalAttracteds.Count; otentI++)
            {
                if (LocalAttracteds[otentI].Attracted == otentSub)
                    return;
            }
            LocalAttracteds.Add(
                new HistoryItem()
                {
                    Attracted = otentSub,
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

    private int afI;
    private Vector3 afSum;
    private Vector3 afOffset;
    private void ApplyForces(HistoryItem history)
    {
        if (PositionalOverride)
            afOffset = PositionalOverride.position;
        else
            afOffset = mySubscriber.AppliedPhysics.position;

        //update errors
        history.errors.Insert(
            0,
            history.Attracted.AppliedPhysics.position
                - afOffset
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
        //apply max
        if(afSum.sqrMagnitude > Mathf.Pow(maxAcceleration,2))
        {
            afSum = afSum.normalized * maxAcceleration;
        }
        //apply force
        history.Attracted.AppliedPhysics.AddForce(
            (-afSum)
            * attractionForce * 30f / (maxHistory * (LocalAttracteds.Count + RemoteAttracteds.Count))
        );

        mySubscriber.AppliedPhysics.AddForce(
            afSum
            * attractionForce * 30f / (maxHistory * (LocalAttracteds.Count + RemoteAttracteds.Count))
        );
    }
    
    private bool IsOtherTypeOfGem(string tag)
    {
        return tag == "PositronGem" || tag == "NegatronGem";
    }
}
