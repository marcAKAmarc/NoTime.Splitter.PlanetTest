using System.Collections;
using UnityEngine;

public class LoopSoundCollection : MonoBehaviour
{
    public AudioSource StartAudio;
    public AudioSource LoopAudio;
    public AudioSource EndAudio;

    public float LoopStartOffset;
    public float LoopFadeOutTime;
    public float LoopFadeInTime;

    public string LoopFadeParameterName;

    public bool isPlaying = false;
    public bool isAfterLoop = false;

    private WaitForSecondsRealtime LoopWait;
    private Coroutine routine;
    public void Awake()
    {
        LoopWait = new WaitForSecondsRealtime(LoopStartOffset);
    }

    public void OnEnable()
    {
        LoopWait = new WaitForSecondsRealtime(LoopStartOffset);
        StartSound();
    }

    public void StartSound()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        routine = StartCoroutine(SectionA());
    }

    public IEnumerator SectionA()
    {
        if (!LoopAudio.isPlaying)
            StartAudio.Play();

        yield return LoopWait;

        float fadeRefTime;
        float fadeStartValue;
        //loop and fade in
        if (enabled)
        {
            //fade in
            LoopAudio.outputAudioMixerGroup.audioMixer.SetFloat(LoopFadeParameterName, ToDecibal(.00001f));
            yield return null;
            if (!LoopAudio.isPlaying)
                LoopAudio.Play();
            fadeRefTime = Time.time;
            LoopAudio.outputAudioMixerGroup.audioMixer.GetFloat(LoopFadeParameterName, out fadeStartValue);
            fadeStartValue = ToLinear(fadeStartValue);
            while (Time.time - fadeRefTime <= LoopFadeInTime)
            {
                LoopAudio.outputAudioMixerGroup.audioMixer.SetFloat(LoopFadeParameterName,
                    ToDecibal(
                        Mathf.Min(
                            fadeStartValue +
                            (1f - fadeStartValue) * (Time.time - fadeRefTime) / LoopFadeInTime
                        , 1f)
                    )
                );
                yield return null;
            }

            LoopAudio.outputAudioMixerGroup.audioMixer.SetFloat(LoopFadeParameterName, ToDecibal(1f));
        }

        while (enabled)
        {
            yield return null;
        }

        //not enabled
        EndAudio.Play();

        //fade loop audio
        if (LoopAudio.isPlaying)
        {
            //fade out loop audio
            fadeRefTime = Time.time;
            LoopAudio.outputAudioMixerGroup.audioMixer.GetFloat(LoopFadeParameterName, out fadeStartValue);
            fadeStartValue = ToLinear(fadeStartValue);
            while (Time.time - fadeRefTime <= LoopFadeOutTime)
            {

                LoopAudio.outputAudioMixerGroup.audioMixer.SetFloat(LoopFadeParameterName,
                    ToDecibal(
                        Mathf.Max(
                            fadeStartValue * (1 - (Time.time - fadeRefTime) / LoopFadeOutTime)
                        , 0f)
                    )
                );
                yield return null;
            }

            LoopAudio.outputAudioMixerGroup.audioMixer.SetFloat(LoopFadeParameterName, ToDecibal(.000001f));
            yield return null;
            LoopAudio.Stop();
        }
    }

    private float b = 1.02f;
    private float ToLinear(float logValue)
    {
        return Mathf.Pow(1.02f, logValue);
    }

    private float ToDecibal(float value)
    {
        return Mathf.Log(value, 1.02f);
    }
}
