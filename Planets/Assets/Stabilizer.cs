using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Stabilizer : SplitterSubscriber
{
    public Quaternion TargetRotation;
    public float MaxRotateSpeed;
    public float RotateFactor;
    public float DampenFactor;
    private Quaternion GoalRotation;
    public float Capability;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetGoalToTargetRotation()
    {
        GoalRotation = TargetRotation;
    }
    public void FixedUpdate()
    {
        SetDirection();
        Rotate();
    }
    private Quaternion _myRotation;
    void SetDirection()
    {
        if (transform.GetComponent<SplitterSubscriber>() != null)
            _myRotation = transform.GetComponent<SplitterSubscriber>().AppliedPhysics.rotation;
        else
            _myRotation = transform.GetComponent<Rigidbody>().rotation;

        GoalRotation = Quaternion.Slerp(GoalRotation, TargetRotation, Mathf.Clamp01(Time.fixedDeltaTime / 3f));
        GoalRotation = Quaternion.Slerp(_myRotation, GoalRotation, Capability);
    }

    public void Rotate()
    {
        if (transform.GetComponent<SplitterSubscriber>() != null)
            transform.GetComponent<SplitterSubscriber>().SmoothRotate(GoalRotation, MaxRotateSpeed, RotateFactor, DampenFactor, Mathf.Pow(Capability, 4f));
        else
            transform.GetComponent<Rigidbody>().SmoothRotate(GoalRotation, MaxRotateSpeed, RotateFactor, DampenFactor, Mathf.Pow(Capability, 4f));
    }
}
