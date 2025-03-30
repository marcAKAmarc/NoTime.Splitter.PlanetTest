using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemConnectorBehaviour : MonoBehaviour
{
    private List<SplitterSubscriber> InsertedGems;
    public GemConnectorBehaviour linkedConnector;

    private void Awake()
    {
        InsertedGems = new List<SplitterSubscriber>();
    }
    private int otentI;
    private SplitterSubscriber otentSub;
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null)
            return;
        if (IsAttractiveGem(other.attachedRigidbody.tag)
            && other.attachedRigidbody.TryGetComponent(out otentSub)
        )
        {
            InsertedGems.Add(otentSub);
            SendLinkedConnectorGem(InsertedGems[InsertedGems.Count - 1]);
            for(otentI = 0; otentI < linkedConnector.InsertedGems.Count; otentI++)
            {
                NotifyMyGemOfLinkedGem(
                    InsertedGems[InsertedGems.Count -1]
                    ,linkedConnector.InsertedGems[otentI]
                );
            }
        }
    }
    private int oteI;
    private SplitterSubscriber otexSub;
    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody == null)
            return;
        if (
            IsAttractiveGem(other.attachedRigidbody.tag)
            && other.TryGetComponent<SplitterSubscriber>(out otexSub)
        )
        {
            for(oteI = 0; oteI < InsertedGems.Count; oteI++)
            {
                if(InsertedGems[oteI] == otexSub)
                {
                    ReceiveRemoveLinkedConnectorGem(InsertedGems[oteI]);
                    InsertedGems.RemoveAt(oteI);
                    return;
                }
            }
        }
    }
    private bool IsAttractiveGem(string tag)
    {
        //oo la la
        return tag == "NeutronGem" || tag == "NegatronGem" || tag == "PositronGem";
    }
    private bool IsPowerGem(string tag)
    {
        return tag == "StarGem";
    }

    private void SendLinkedConnectorGem(SplitterSubscriber gem)
    {
        linkedConnector.ReceiveAddLinkedConnectorGem(gem);
    }

    private int rlcgI;

    private void ReceiveAddLinkedConnectorGem(SplitterSubscriber gem)
    {
        for(rlcgI = 0; rlcgI < InsertedGems.Count; rlcgI++)
        {
            NotifyMyGemOfLinkedGem(InsertedGems[rlcgI], gem);
        }
    }
    private PositronGemBehaviour _notifPositron;
    private NegatronBehaviour _notifNegatron;
    private NeutronGemBehaviour _notifNeutron;
    private void NotifyMyGemOfLinkedGem(SplitterSubscriber myGem, SplitterSubscriber linkedGem)
    {
        if (
                myGem.tag == "PositronGem"
                &&
                myGem.TryGetComponent(out _notifPositron)
            )
        {
            _notifPositron.RegisterRemoteGem(linkedGem);
        }
        else if (
            myGem.tag == "NeutronGem"
            &&
            myGem.TryGetComponent(out _notifNeutron)
        )
        {
            _notifNeutron.RegisterRemoteGem(linkedGem);
        }
        else if (
            myGem.tag == "NegatronGem"
            &&
            myGem.TryGetComponent(out _notifNegatron)
        )
        {
            _notifNegatron.RegisterRemoteGem(linkedGem);
        }
    }
    private int rrlcgI;
    private PositronGemBehaviour rrlcgPositron;
    private NegatronBehaviour rrlcgNegatron;
    private NeutronGemBehaviour rrlcgNeutron;
    private void ReceiveRemoveLinkedConnectorGem(SplitterSubscriber gem)
    {
        for (rrlcgI = 0; rrlcgI < InsertedGems.Count; rlcgI++)
        {
            if (
                InsertedGems[rrlcgI].tag == "PositronGem"
                &&
                InsertedGems[rrlcgI].TryGetComponent(out rrlcgPositron)
            )
            {
                rrlcgPositron.UnregisterRemoteGem(gem);
            }
            else if (
                InsertedGems[rrlcgI].tag == "NeutronGem"
                &&
                InsertedGems[rrlcgI].TryGetComponent(out rrlcgNeutron)
            )
            {
                rrlcgNeutron.UnregisterRemoteGem(gem);
            }
            else if (
                InsertedGems[rrlcgI].tag == "NegatronGem"
                &&
                InsertedGems[rrlcgI].TryGetComponent(out rrlcgNegatron)
            )
            {
                rrlcgNegatron.UnregisterRemoteGem(gem);
            }
        }
    }
}
