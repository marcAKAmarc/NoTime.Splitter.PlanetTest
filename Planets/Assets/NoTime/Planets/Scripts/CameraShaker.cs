using System.Collections.Generic;
using UnityEngine;

public struct CameraShakeInput
{
    public float Attack;
    public float Amplitude;
    public float AngularAmplitude;
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
    private Quaternion originalRotation;

    public float maxMagnitude = .3f;
    public bool test;
    public float testAmp;
    public float testAngularAmp;
    public float testFreq;
    public float testDecay;
    public float testAttack;
    public void AddInput(CameraShakeInput input)
    {
        if (input.startTime == 0f)
            input.startTime = Time.time;

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
        originalRotation = transform.rotation;

        //set shake position
        foreach (CameraShakeInput _in in inputs)
        {
            transform.position += RelativeOutputForPositionalInput(_in);
            transform.rotation *= Quaternion.Euler(RelativeOutputForAngularInput(_in));
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
        transform.rotation = originalRotation;
    }

    private Vector3 RelativeOutputForPositionalInput(CameraShakeInput input)
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
    private Vector3 RelativeOutputForAngularInput(CameraShakeInput input)
    {
        return new Vector3(
            Mathf.Cos((Time.time) * 2f * Mathf.PI * input.Frequency) * input.AngularAmplitude *
            (1 - (Time.time - input.startTime) / input.Decay),
            Mathf.Sin((Time.time) * input.Asymmetry.x * 2f * Mathf.PI * input.Frequency) * input.AngularAmplitude *
            (1 - (Time.time - input.startTime) / input.Decay),
            Mathf.Sin((Time.time) * input.Asymmetry.y * 2f * Mathf.PI * input.Frequency) * input.AngularAmplitude *
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
                Attack = testAttack,
                Amplitude = testAmp,
                AngularAmplitude = testAngularAmp,
                Decay = testDecay,
                Frequency = testFreq,
                Asymmetry = new Vector2(.8f, .64f),
                startTime = Time.time
            });
        }
    }
}
