using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitBehaviour : MonoBehaviour
{
    private SplitterSubscriber sub;
    public Transform vinePosition;

    // Start is called before the first frame update
    void Start()
    {
        TryGetComponent(out sub);   
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        /*PIDToPosition(
            sub.AppliedPhysics.position - vinePosition.position,
            sub.AppliedPhysics.velocity,
            p, i, d, iMax
        );*/
    }

    public float p;
    public float i;
    public float d;
    public float iMax;
    private List<Vector3> errors;
    private List<Vector3> velocities;
    private List<Vector3> errorInts;
    public int History;
    private Vector3 PIDToPosition(Vector3 error, Vector3 velocity, float pGain, float dGain, float iGain, float iMaximum)
    {
        errors.Insert(0, error);
        if (errors.Count == 1)
            velocities.Insert(0, Vector3.zero);
        else
            velocities.Insert(0, velocity/*Vector3.Project(velocity, errors[0])*/);
        errorInts.Insert(0, errors[0] * Time.fixedDeltaTime);
        //create static history
        while (errors.Count < History)
        {
            errors.Insert(0, errors[0]);
            velocities.Insert(0, velocities[0]);
            errorInts.Insert(0, errors[0] * Time.fixedDeltaTime);
        }

        if (errors.Count > History)
        {
            errors.RemoveAt(errors.Count - 1);
            velocities.RemoveAt(velocities.Count - 1);
            errorInts.RemoveAt(errorInts.Count - 1);
        }

        Vector3 errorSum = Vector3.zero;
        for (int i = 0; i < errorInts.Count; i++)
        {
            errorSum += errorInts[i];
        }

        return
            (errors[0] * pGain)
            + (
                velocities[0]
                *
                dGain
            )
            +
            //do we need this?
            Vector3.ClampMagnitude(errorSum, iMaximum) * iGain;
    }

}
