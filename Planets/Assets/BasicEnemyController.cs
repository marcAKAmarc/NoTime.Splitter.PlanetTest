using NoTime.Splitter;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class followParameters
{
    public float TargetPForce, MinPForce, MaxPForce, TargetDForce, ForgetTime, RememberTime;
    public bool alignToGravity = true;
    public bool faceAwayFromVelocity = false;
    private Guid id;

    public void Init()
    {
        id = Guid.NewGuid();
    }
    
    public void ApplyValuesToBehaviour(FollowBehaviour b)
    {
        b.targetPForce = TargetPForce;
        b.minPForce = MinPForce;
        b.maxPForce = MaxPForce;
        b.targetDForce = TargetDForce;
        b.ForgetTime = ForgetTime;
        b.RememberTime = RememberTime;
    }
    public void ApplyValuesToBehaviour(EnemyGraphicBehaviour b)
    {
        b.AlignWithGravity = alignToGravity;
        b.FaceAwayFromVelocity = faceAwayFromVelocity;
    }

    public bool Equals(followParameters p)
    {
        return id == p.id;
    }
}
public enum EnemyMode {Default, Sneaking, Attacking, Repelled, High}
public class BasicEnemyController : MonoBehaviour
{   
    public EnemyGraphicBehaviour enemyGraphicBehaviour;
    public FollowBehaviour followBehaviour;
    public SplitterSubscriber target;

    public float SneakRange, StrategicRange, AttackRange;

    public followParameters Default, Sneaking, Attacking, Repelled, Strategic;
    private followParameters currentFollowParamenters;

    private List<Collider> gemColliders;
    int numberOfGems = 0;
    public void Start()
    {
        Default.Init();
        Sneaking.Init();
        Attacking.Init();
        Strategic.Init();
        Repelled.Init();
        gemColliders = new List<Collider>();
    }

    // Start is called before the first frame update


    // Update is called once per frame
    void Update()
    {
        followParameters calcedFollowParams = GetFollowParams();
        if (currentFollowParamenters == null || !calcedFollowParams.Equals(currentFollowParamenters))
        {
            calcedFollowParams.ApplyValuesToBehaviour(followBehaviour);
            calcedFollowParams.ApplyValuesToBehaviour(enemyGraphicBehaviour);
            if (calcedFollowParams.Equals(Default))
                followBehaviour.target = null;
            else
                followBehaviour.target = target;
            currentFollowParamenters = calcedFollowParams;
        }

        if (currentFollowParamenters.Equals(Repelled))
        {
            followBehaviour.RepelDir = GetAvgRepelDirection();
        }
        else
        {
            followBehaviour.RepelDir = Vector3.zero;
        }
    }
    string chosenDB = "";
    string strSneak = "sneak";
    string strAttack = "attack";
    string strStrategic = "strategic";
    string strRepel = "repel";
    string strDefault = "default";
    public followParameters GetFollowParams()
    {
        if(target == null)
        {
            chosenDB = strDefault;
            return Default;
        }
        followParameters newParams = Default;
        float sqrMagnitude = (target.AppliedPhysics.position - transform.position).sqrMagnitude;
        if (sqrMagnitude < Mathf.Pow(SneakRange, 2f))
        {
            newParams = Sneaking;
            chosenDB = strSneak;
        }
        if(sqrMagnitude < Mathf.Pow(StrategicRange, 2f))
        {
            newParams = Strategic;
            chosenDB = strStrategic;
        }
        if(sqrMagnitude < Mathf.Pow(AttackRange, 2f))
        {
            newParams = Attacking;
            chosenDB = strAttack;
        }

        if(numberOfGems > 0 && sqrMagnitude < Mathf.Pow(SneakRange, 2f))
        {
            newParams = Repelled;
            chosenDB = strRepel;
        }

        return newParams;
    }

    public Vector3 GetAvgRepelDirection()
    {
        Vector3 GemDir = Vector3.zero;
        for(int i = 0; i < gemColliders.Count; i++)
        {
            GemDir += gemColliders[i].transform.position - transform.position;
        }
        return GemDir.normalized;
    }
    public void OnRepelEnter(Collider other)
    {
        if (!other.isTrigger)
        {
            numberOfGems++;
            gemColliders.Add(other);
        }

    }

    public void OnRepelExit(Collider other)
    {
        if (!other.isTrigger)
        {
            numberOfGems--;
            int removalIndex = -1;
            for (int i = 0; i < gemColliders.Count; i++)
            {
                if (gemColliders[i].gameObject == other.gameObject)
                {
                    removalIndex = i;
                    break;
                }
            }
            if (removalIndex > -1)
                gemColliders.RemoveAt(removalIndex);
        }
    }
}


