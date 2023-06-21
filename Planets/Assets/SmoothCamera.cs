using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothCamera : MonoBehaviour
{
    public Transform followTransform;
    public Vector3 followTransformRelativeOffset;
    private Vector3 currentVelocity = Vector3.zero;
    public float SmoothFactor;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, followTransform.rotation, SmoothFactor);//SmoothDampQuaternion(transform.rotation, followTransform.rotation, ref currentVelocity, SmoothFactor - (SmoothFactor * Mathf.Clamp01(Quaternion.Angle(transform.rotation, followTransform.rotation)/90f)));
        transform.position = followTransform.position + followTransform.TransformDirection(followTransformRelativeOffset);
    }

    private static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime)
    {
        Vector3 c = current.eulerAngles;
        Vector3 t = target.eulerAngles;
        return Quaternion.Euler(
          Mathf.SmoothDampAngle(c.x, t.x, ref currentVelocity.x, smoothTime),
          Mathf.SmoothDampAngle(c.y, t.y, ref currentVelocity.y, smoothTime),
          Mathf.SmoothDampAngle(c.z, t.z, ref currentVelocity.z, smoothTime)
        );
    }
}
