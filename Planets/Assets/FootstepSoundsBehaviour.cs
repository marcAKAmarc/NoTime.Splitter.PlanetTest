using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class FootStepGroup
{
    public List<AudioSource> sources;
}
public class FootstepSoundsBehaviour : MonoBehaviour
{
    [Serialize]
    public List<FootStepGroup> FootStepsGround;
    public float PitchVariance = 1f;
    public float CurrentCharacterSpeed;
    private float SoundSpeed = 2f; //at 3m speed
    private float Timer = 1f;

    private int StepIndex = 0;
    // Update is called once per frame
    void Update()
    {
        SoundSpeed =  4f*CurrentCharacterSpeed/7f;
        if (SoundSpeed > 3)
            SoundSpeed -= 1f;


        if (SoundSpeed < 1f)
            return;

        if (SoundSpeed > 5f)
            SoundSpeed = 5f;

        Timer -= Time.deltaTime * SoundSpeed;
        if (Timer <= 0f)
        {
            Timer = 1f;
            StepIndex = Mathf.FloorToInt(Random.Range(0, FootStepsGround.Count) * .99f);
            foreach(AudioSource aud in FootStepsGround[StepIndex].sources)
            {
                aud.pitch = 1f + Random.Range(-1f * PitchVariance / 2f, PitchVariance / 2f);
                aud.Play();
            }
            
        }
    }
}
