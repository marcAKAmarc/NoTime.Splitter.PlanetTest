using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositronGemBehaviour : MonoBehaviour
{

    public bool excited = false;
    private int excitements = 0;
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
        if (IsOtherTypeOfGem(other.attachedRigidbody.gameObject) 
            && other.attachedRigidbody.TryGetComponent(out otentSub)
        )
        {
            //make sure not already in local attractedds 
            for (otentI = 0; otentI < LocalAttracteds.Count; otentI++)
            {
                if (LocalAttracteds[otentI] == otentSub)
                    return;
            }
            LocalAttracteds.Add(
                other.attachedRigidbody.GetComponent<SplitterSubscriber>()
            );
        }
    }

    private SplitterSubscriber teSub;
    private int i;
    private void OnTriggerExit(Collider other)
    {
        if (IsOtherTypeOfGem(other.attachedRigidbody.gameObject))
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
            /*(
                mySubscriber.AppliedPhysics.velocity
                - subscriber.AppliedPhysics.velocity
            )
            * attractionForce*/
            GetForce(mySubscriber, subscriber)
        );

        mySubscriber.AppliedPhysics.AddForce(
            /*(
                subscriber.AppliedPhysics.velocity
                - mySubscriber.AppliedPhysics.velocity
            )
            * attractionForce*/
            GetForce(subscriber, mySubscriber)
        );
    }
    public Vector3 GetForce(SplitterSubscriber to, SplitterSubscriber from)
    {
        return
            (
                to.AppliedPhysics.velocity
                - from.AppliedPhysics.velocity
            ) * attractionForce / (LocalAttracteds.Count + RemoteAttracteds.Count);
    }
    private bool IsOtherTypeOfGem(string tag)
    {
        return tag == "NeutronGem" || tag == "NegatronGem";
    }

    NeutronGemBehaviour neutron;
    NegatronBehaviour negatron;
    private bool IsOtherTypeOfGem(GameObject go)
    {
        return go.TryGetComponent(out neutron) || go.TryGetComponent(out negatron);
    }
}
