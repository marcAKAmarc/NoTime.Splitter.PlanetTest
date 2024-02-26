using System.Collections.Generic;
using UnityEngine;

public class CameraShakeInput
{
    public float Attack = .2f;
    public float Amplitude;
    public float Frequency;
    public float Decay;
    public float startTime;
    public Vector2 Asymmetry;

    public void db()
    {
        Debug.Log("Amplitude: " + Amplitude.ToString() + "; Freq: " + Frequency.ToString() + "; Decay: " + Decay.ToString() + "; startTime: " + startTime.ToString() + "; Asymmetry: " + Asymmetry.ToString());
    }
}
public class CameraShaker : MonoBehaviour
{
    public Transform CameraTransform;
    private List<CameraShakeInput> inputs = new List<CameraShakeInput>();
    private Vector3 originalPosition;

    public float maxMagnitude = .3f;
    public bool test;
    public float testAmp;
    public float testFreq;
    public float testDecay;
    public float testAttack;
    public void AddInput(CameraShakeInput input)
    {
        inputs.Add(input);
    }
    private void OnPreRender()
    {
        //clean inputs
        for (var i = 0; i < inputs.Count; i++)
        {
            if (inputs[i].startTime + inputs[i].Decay <= Time.time)
                inputs.RemoveAt(i);
        }

        //record original position
        originalPosition = transform.position;

        //set shake position
        foreach (CameraShakeInput _in in inputs)
        {
            transform.position += RelativeOutputForInput(_in);
        }

        if ((transform.position - originalPosition).magnitude > maxMagnitude)
        {
            transform.position = originalPosition + (transform.position - originalPosition).normalized * maxMagnitude;
        }
    }

    private void OnPostRender()
    {
        //reset position
        transform.position = originalPosition;
    }

    private Vector3 RelativeOutputForInput(CameraShakeInput input)
    {
        return new Vector3(
            Mathf.Cos((Time.time) * 2f * Mathf.PI * input.Frequency) * input.Amplitude *
            (1 - (Time.time - input.startTime) / input.Decay),
            Mathf.Sin((Time.time) * input.Asymmetry.x * 2f * Mathf.PI * input.Frequency) * input.Amplitude *
            (1 - (Time.time - input.startTime) / input.Decay),
            Mathf.Sin((Time.time) * input.Asymmetry.y * 2f * Mathf.PI * input.Frequency) * input.Amplitude *
            (1 - (Time.time - input.startTime) / input.Decay)
        ) * Mathf.Min((Time.time - input.startTime) / input.Attack, 1f);
    }


    private void Update()
    {
        //for testing
        if (test && inputs.Count == 0)
        {
            inputs.Add(new CameraShakeInput
            {
                Amplitude = testAmp,
                Decay = testDecay,
                Frequency = testFreq,
                Asymmetry = new Vector2(.8f, .64f),
                startTime = Time.time
            });
        }
    }
}
