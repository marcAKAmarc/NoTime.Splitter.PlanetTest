using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class GemConnectorBehaviour : MonoBehaviour
{
    private List<SplitterSubscriber> InsertedGems = new List<SplitterSubscriber>();
    public GemConnectorBehaviour linkedConnector;
    public GemExciterBehaviour exciter;
    private bool IsPowering;
    public Transform Door;
    public Transform RestPosition;
    private int opeI;

    private void OnPoweredEvent(bool value)
    {
        if (exciter != null)
        {
            exciter.enabled = value;
            


            for(opeI = 0; opeI < InsertedGems.Count; opeI++)
            {
                if (value)
                    SetGemOfPositionalOverride(InsertedGems[opeI], RestPosition);
                else
                    SetGemOfPositionalOverride(InsertedGems[opeI], null);
            }
        }
    }

    //private SplitterAnchor updateDoorAnchor;
    //private SplitterSubscriber updateDoorSub;
    public void OnActivate()
    {
        //if (Input.GetKeyDown(KeyCode.Y))
        //{
            IsPowering = !IsPowering;
            OnPoweredEvent(IsPowering);
        //}
        /*if (Input.GetKeyDown(KeyCode.R))
        {
            Door.gameObject.SetActive(!Door.gameObject.activeSelf);
            if (transform.TryGetComponentInParent(out updateDoorAnchor))
            {
                updateDoorAnchor.GetMatchedAnchorTransform(Door).gameObject.SetActive(Door.gameObject.activeSelf);
            }

            if (
                transform.TryGetComponentInParent(out updateDoorSub)
                && updateDoorSub.Anchor != null
            )
            {
                updateDoorSub.Anchor.GetMatchedSubscriberTransform(updateDoorSub, Door).gameObject.SetActive(Door.gameObject.activeSelf);
            }
        }*/
    }
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
            for(otentI = 0; otentI < InsertedGems.Count; otentI++)
            {
                //make sure not already in InsertedGems
                if (InsertedGems[otentI] == otentSub)
                    return;
            }

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
            && other.attachedRigidbody.TryGetComponent<SplitterSubscriber>(out otexSub)
        )
        {
            for(oteI = 0; oteI < InsertedGems.Count; oteI++)
            {
                if(InsertedGems[oteI] == otexSub)
                {
                    SendRemoveLinkedConnectorGem(InsertedGems[oteI]);
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
    private void SendRemoveLinkedConnectorGem(SplitterSubscriber gem)
    {
        linkedConnector.ReceiveRemoveLinkedConnectorGem(gem);
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
        for (rrlcgI = 0; rrlcgI < InsertedGems.Count; rrlcgI++)
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

    private void SetGemOfPositionalOverride(SplitterSubscriber myGem, Transform posOverride)
    {
        if (
            myGem.tag == "NeutronGem"
            &&
            myGem.TryGetComponent(out _notifNeutron)
        )
        {
            _notifNeutron.PositionalOverride = posOverride;
        }
        else if (
            myGem.tag == "NegatronGem"
            &&
            myGem.TryGetComponent(out _notifNegatron)
        )
        {
            _notifNegatron.PositionOverride = posOverride;
        }
    }
}
