using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositronGemBehaviour : MonoBehaviour
{

    public bool excited = false;
    public Collider attractionTrigger;
    public float attractionForce;
    private List<SplitterSubscriber> LocalAttracteds;
    private List<SplitterSubscriber> RemoteAttracteds;
    private SplitterSubscriber mySubscriber;

    //this is like D in PID
    private void Awake()
    {
        LocalAttracteds = new List<SplitterSubscriber>();
        RemoteAttracteds = new List<SplitterSubscriber>();
    }
    void Start()
    {
        mySubscriber = transform.GetComponentInParent<SplitterSubscriber>();
    }

    public void RegisterRemoteGem(SplitterSubscriber remoteSubscriber)
    {
        if(!RemoteAttracteds.Contains(remoteSubscriber))
            RemoteAttracteds.Add(remoteSubscriber);
    }

    private int urgI;
    public void UnregisterRemoteGem(SplitterSubscriber remoteSubscriber)
    {
        for(urgI = 0; urgI < RemoteAttracteds.Count; urgI++)
        {
            if (RemoteAttracteds[urgI] == remoteSubscriber)
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
                other.attachedRigidbody.GetComponent<SplitterSubscriber>()
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
                if (LocalAttracteds[i] == teSub)
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

    private void ApplyForces(SplitterSubscriber subscriber)
    {
        subscriber.AppliedPhysics.AddForce(
            (
                mySubscriber.AppliedPhysics.velocity
                - subscriber.AppliedPhysics.velocity
            )
            * attractionForce
        );

        mySubscriber.AppliedPhysics.AddForce(
            (
                subscriber.AppliedPhysics.velocity
                - mySubscriber.AppliedPhysics.velocity
            )
            * attractionForce
        );
    }

    private bool IsOtherTypeOfGem(string tag)
    {
        return tag == "NeutronGem" || tag == "NegatronGem";
    }
}
