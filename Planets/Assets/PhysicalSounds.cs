using NoTime.Splitter;
using NoTime.Splitter.Demo;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class HitSoundData
{
    public Transform original;
    public float valueLow;
    public float valueHigh;
    public float volumeLow;
    public float volumeHigh;
    public float lowpassLow;
    public float lowpassHigh;
    public float pitchVariance;

    [HideInInspector]
    [DoNotSerialize]
    public float lastHitTime;
    [HideInInspector]
    [DoNotSerialize]
    public float hitSqrMagnitude;
}
[Serializable]
public class ScrapeSoundData
{
    public Transform original;
    public float valueLow;
    public float valueHigh;
    public float volumeLow;
    public float volumeHigh;
    public float lowpassLow;
    public float lowpassHigh;
    public float volDecayFactor;
    public float lowpassDecayFactor;
    public float goalVol;
    public float goalLowPass;
}
public class PhysicalSounds : MonoBehaviour
{
    public List<HitSoundData> HitSounds;
    public List<ScrapeSoundData> ScrapeSounds;

    private float _vol;
    private Transform _t;
    private List<GameObject> _SoundCache;
    private List<float> _timers;
    public void Awake()
    {
        _SoundCache = new List<GameObject>();
        _timers = new List<float>();
    }
    private void OnCollisionEnterSounds(Vector3 worldPoint, float impulseSquareMagnitude)
    {
        //Debug.Log("enter sounds");

        if (impulseSquareMagnitude < 1f)
            return;

        for (int i = 0; i < HitSounds.Count; i++)
        {

            if (
                Time.time - HitSounds[i].lastHitTime < .01f
                &&
                impulseSquareMagnitude < HitSounds[i].hitSqrMagnitude
            )
                return;

            //Debug.Log("MAG: " + Mathf.Sqrt(impulseSquareMagnitude).ToString());

            _vol = Mathf.Clamp(
                impulseSquareMagnitude.Map(
                    //HitSounds[i].valueLow,
                    Mathf.Pow(HitSounds[i].valueLow, 2),
                    HitSounds[i].volumeLow,
                    //HitSounds[i].valueHigh,
                    Mathf.Pow(HitSounds[i].valueHigh, 2),
                    HitSounds[i].volumeHigh
                ),
                HitSounds[i].volumeLow,
                HitSounds[i].volumeHigh
            );
            float _cutOff = Mathf.Clamp(
                impulseSquareMagnitude.Map(
                    //HitSounds[i].valueLow,
                    Mathf.Pow(HitSounds[i].valueLow, 2),
                    HitSounds[i].lowpassLow,
                    //HitSounds[i].valueHigh,
                    Mathf.Pow(HitSounds[i].valueHigh, 2),
                    HitSounds[i].lowpassHigh
                ),
                HitSounds[i].lowpassLow,
                HitSounds[i].lowpassHigh
            );
            //Debug.Log(transform.name + "sound data - impulse mag: " + Mathf.Sqrt(impulseSquareMagnitude).ToString("G6") + "; vol: " + _vol.ToString("G6") + "; ");
            if (_vol > .01f)
            {
                HitSounds[i].hitSqrMagnitude = impulseSquareMagnitude;
                HitSounds[i].lastHitTime = Time.time;

                _t = Instantiate(HitSounds[i].original, worldPoint, Quaternion.identity, transform);
                _t.GetComponent<AudioSource>().pitch += (Random.value * HitSounds[i].pitchVariance) - (HitSounds[i].pitchVariance / 2f);
                _t.GetComponent<AudioSource>().volume = _vol;
                if (_t.GetComponent<AudioLowPassFilter>() != null)
                {
                    _t.GetComponent<AudioLowPassFilter>().cutoffFrequency = _cutOff;
                }

                _t.gameObject.SetActive(true);
                _t.GetComponent<AudioSource>().Play();
                _SoundCache.Insert(0, _t.gameObject);
                _timers.Add(0f);

                //reset for next one
                _t = HitSounds[i].original;
                if (_t.GetComponent<AudioLowPassFilter>() != null)
                {
                    _t.GetComponent<AudioLowPassFilter>().cutoffFrequency = 0f;
                }
            }
        }
    }

    private bool CollisionIsScrape(Vector3 impulse, Vector3 relativeVelocity)
    {
        return Mathf.Abs(Vector3.Dot(impulse.normalized, relativeVelocity.normalized)) < .2f;
    }

    float scrapeVol;
    Vector3 otherVel;
    Vector3 myVel;
    private void OnCollisionStaySounds(Vector3 worldPoint, float impulseSquareMagnitude, float pointRelativeVelocitySquareMagnitude)
    {
        for (int i = 0; i < ScrapeSounds.Count; i++)
        {

            _vol = Mathf.Clamp(
                pointRelativeVelocitySquareMagnitude.Map(
                    Mathf.Pow(ScrapeSounds[i].valueLow, 2f),
                    ScrapeSounds[i].volumeLow,
                    Mathf.Pow(ScrapeSounds[i].valueHigh, 2f),
                    ScrapeSounds[i].volumeHigh
                ),
                ScrapeSounds[i].volumeLow,
                ScrapeSounds[i].volumeHigh
            );
            float _cutOff = Mathf.Clamp(
                pointRelativeVelocitySquareMagnitude.Map(
                    Mathf.Pow(ScrapeSounds[i].valueLow, 2f),
                    ScrapeSounds[i].lowpassLow,
                    Mathf.Pow(ScrapeSounds[i].valueHigh, 2f),
                    ScrapeSounds[i].lowpassHigh
                ),
                ScrapeSounds[i].lowpassLow,
                ScrapeSounds[i].lowpassHigh
            );
            if (_vol > .008f)
            {

                _t = ScrapeSounds[i].original;
                ScrapeSounds[i].goalVol = _vol;
                ScrapeSounds[i].goalLowPass = _cutOff;

            }
            else
            {
                _t = ScrapeSounds[i].original;
                ScrapeSounds[i].goalVol = ScrapeSounds[i].volumeLow;
                ScrapeSounds[i].goalLowPass = ScrapeSounds[i].lowpassLow;
            }
        }
        
        /*
        //Debug.Log("stay sounds");
        if (collision.transform.GetComponentInParent<RigidbodyFpsController>() != null)
            return;

        if (collision.transform.GetComponentInParent<SplitterSubscriber>() != null)
        {
            otherVel = collision.rigidbody.transform.GetComponent<SplitterSubscriber>().AppliedPhysics.GetPointVelocity(collision.contacts[0].point);
        }
        else if (collision.rigidbody != null)
        {
            otherVel = collision.rigidbody.GetPointVelocity(collision.contacts[0].point);
        }
        else
            otherVel = Vector3.zero;

        if (transform.GetComponent<SplitterSubscriber>() != null)
        {
            myVel = transform.GetComponent<SplitterSubscriber>().AppliedPhysics.GetPointVelocity(collision.contacts[0].point);
        }
        else if (transform.GetComponent<Rigidbody>() != null)
            myVel = transform.GetComponent<Rigidbody>().GetPointVelocity(collision.contacts[0].point);
        else
            myVel = Vector3.zero;

        for (int i = 0; i < ScrapeSounds.Count; i++)
        {

            _vol = Mathf.Clamp(
                (otherVel - myVel).sqrMagnitude.Map(
                    Mathf.Pow(ScrapeSounds[i].valueLow, 2f),
                    ScrapeSounds[i].volumeLow,
                    Mathf.Pow(ScrapeSounds[i].valueHigh, 2f),
                    ScrapeSounds[i].volumeHigh
                ),
                ScrapeSounds[i].volumeLow,
                ScrapeSounds[i].volumeHigh
            );
            float _cutOff = Mathf.Clamp(
                (otherVel - myVel).sqrMagnitude.Map(
                    Mathf.Pow(ScrapeSounds[i].valueLow, 2f),
                    ScrapeSounds[i].lowpassLow,
                    Mathf.Pow(ScrapeSounds[i].valueHigh, 2f),
                    ScrapeSounds[i].lowpassHigh
                ),
                ScrapeSounds[i].lowpassLow,
                ScrapeSounds[i].lowpassHigh
            );
            if (_vol > .008f)
            {

                _t = ScrapeSounds[i].original;
                ScrapeSounds[i].goalVol = _vol;
                ScrapeSounds[i].goalLowPass = _cutOff;

            }
            else
            {
                _t = ScrapeSounds[i].original;
                ScrapeSounds[i].goalVol = ScrapeSounds[i].volumeLow;
                ScrapeSounds[i].goalLowPass = ScrapeSounds[i].lowpassLow;
            }
        }*/
    }

    ContactPoint[] contacts = new ContactPoint[1];
    public void OnCollisionEnter(Collision collision)
    {
        if(CollisionIsNatural(collision)
            && !CollisionIsScrape(collision.impulse, collision.relativeVelocity)
        )
        {
            collision.GetContacts(contacts);
            OnCollisionEnterSounds(
                contacts[0].point,
                collision.impulse.sqrMagnitude
            );
        }
    }
    
    public void OnCollisionStay(Collision collision)
    {
        

        if (CollisionIsNatural(collision)
            && CollisionIsScrape(collision.impulse, collision.relativeVelocity)
        )
        {
            collision.GetContacts(contacts);
            OnCollisionStaySounds(
                contacts[0].point,
                collision.impulse.sqrMagnitude,
                (
                    contacts[0].thisCollider.attachedRigidbody.GetPointVelocity(contacts[0].point)
                    -
                    contacts[0].otherCollider.attachedRigidbody.GetPointVelocity(contacts[0].point)
                ).sqrMagnitude
            );
        }
    }

    public void OnSimulationCollisionEnter(SplitterEvent evt)
    {
        if (!CollisionIsScrape(evt.Collision.impulse, evt.Collision.relativeVelocity))
        {
            evt.Collision.GetContacts(contacts);
            OnCollisionEnterSounds(
                evt.Anchor.AnchorPointToWorldPoint(contacts[0].point),
                evt.Collision.impulse.sqrMagnitude
            );
        }
    }

    public void OnSimulationCollisionStay(SplitterEvent evt)
    {
        if (CollisionIsScrape(evt.Collision.impulse, evt.Collision.relativeVelocity))
        {
            evt.Collision.GetContacts(contacts);
            OnCollisionStaySounds(
                evt.Anchor.AnchorPointToWorldPoint(contacts[0].point),
                evt.Collision.impulse.sqrMagnitude,
                (
                    contacts[0].thisCollider.attachedRigidbody.GetPointVelocity(contacts[0].point)
                    -
                    contacts[0].otherCollider.attachedRigidbody.GetPointVelocity(contacts[0].point)
                ).sqrMagnitude
            );
        }
    }

    void Update()
    {
        for (int i = 0; i < _timers.Count; i++)
        {
            _timers[i] += Time.deltaTime;
            if (_timers[i] > 3.0f)
            {
                Destroy(_SoundCache[_SoundCache.Count - 1]);
                _SoundCache.RemoveAt(_SoundCache.Count - 1);
                _timers.RemoveAt(i);
                i -= 1;
            }
        }

        for (int i = 0; i < ScrapeSounds.Count; i++)
        {
            ScrapeSounds[i].original.GetComponent<AudioSource>().volume =
                Mathf.Lerp(ScrapeSounds[i].original.GetComponent<AudioSource>().volume, ScrapeSounds[i].goalVol, Mathf.Clamp01(ScrapeSounds[i].volDecayFactor * Time.deltaTime));
            ScrapeSounds[i].goalVol -= ScrapeSounds[i].volDecayFactor * Time.deltaTime;
            if (ScrapeSounds[i].goalVol < ScrapeSounds[i].volumeLow)
                ScrapeSounds[i].goalVol = ScrapeSounds[i].volumeLow;
            if (ScrapeSounds[i].original.GetComponent<AudioLowPassFilter>() != null)
            {
                ScrapeSounds[i].original.GetComponent<AudioLowPassFilter>().cutoffFrequency =
                    Mathf.Lerp(ScrapeSounds[i].original.GetComponent<AudioLowPassFilter>().cutoffFrequency, ScrapeSounds[i].goalLowPass, Mathf.Clamp01(ScrapeSounds[i].volDecayFactor * Time.deltaTime));
                ScrapeSounds[i].goalLowPass -= ScrapeSounds[i].lowpassDecayFactor * Time.deltaTime;
                if (ScrapeSounds[i].goalLowPass < ScrapeSounds[i].lowpassLow)
                    ScrapeSounds[i].goalLowPass = ScrapeSounds[i].lowpassLow;
            }



        }
    }

    private bool CollisionIsNatural(Collision collision)
    {
        //i am subscriber of collider's anchor
        if (transform.GetComponent<SplitterSubscriber>() != null
            && transform.GetComponent<SplitterSubscriber>().Simulating()
            && collision.rigidbody.GetComponent<SplitterAnchor>()
            && collision.rigidbody.GetComponent<SplitterAnchor>().IsInMySimulation(transform.GetComponent<SplitterSubscriber>())
        )
            return false;

        //i am anchor of collider's subscriber
        if (transform.GetComponent<SplitterAnchor>() != null &&
            collision.rigidbody.transform.GetComponent<SplitterSubscriber>() != null
            && transform.GetComponent<SplitterAnchor>().IsInMySimulation(
                collision.collider.transform.GetComponent<SplitterSubscriber>()
            )
         )
            return false;

        //we are subscribers with shared anchor
        if (transform.GetComponent<SplitterSubscriber>() != null
            && collision.rigidbody.GetComponent<SplitterSubscriber>() != null
            && transform.GetComponent<SplitterSubscriber>().Simulating()
            && collision.rigidbody.GetComponent<SplitterSubscriber>().Simulating()
            && transform.GetComponent<SplitterSubscriber>().Anchor.gameObject == collision.rigidbody.GetComponent<SplitterSubscriber>().Anchor.gameObject
        )
            return false;

        return true;
    }
}

public static class floatExtensions
{
    public static float Map(this float val, float x1, float y1, float x2, float y2)
    {
        float m = (y2 - y1) / (x2 - x1);
        float b = y2 - (x2 * m);
        return (val * m) + b;

        //return (val * (y2 - y1) / (x2 - x1)) + (y2 - (x2 * ((y2 - y1) / x2 - x1)));
    }
}

public static class AudioExtensions
{
    public static float LinearToDecibel(float linear)
    {
        float dB;

        if (linear != 0)
            dB = 20.0f * Mathf.Log10(linear);
        else
            dB = -144.0f;

        return dB;
    }

    public static float DecibelToLinear(float dB)
    {
        float linear = Mathf.Pow(10.0f, dB / 20.0f);

        return linear;
    }
}
