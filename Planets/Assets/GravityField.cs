using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GravityField : SplitterEventListener
{
    public int PriorityLayer;
    public float surfaceRadius;
    public float farRadius;
    public float surfaceForce;
    public float farForce;
    public float fieldDistance;
    // Start is called before the first frame update

    private float worldSurfaceRadius;
    private float worldFarRadius;

    public float GetGravityForce(Vector3 position)
    {
        return GetGravityAcceleration(
            (transform.position - position).magnitude
        );
    }
    public float GetGravityAcceleration(float distance)
    {
        worldSurfaceRadius = transform.lossyScale.x * surfaceRadius / 2f;
        worldFarRadius = transform.lossyScale.x * farRadius / 2f;
        //Debug.Log("distance: " + distance.ToString());
        return (
                    (worldFarRadius * (worldSurfaceRadius - distance) * (surfaceForce - farForce))
                    / (distance * (worldFarRadius - worldSurfaceRadius))
               ) + surfaceForce;
    }

    void OnDrawGizmosSelected()
    {
        worldSurfaceRadius = transform.lossyScale.x * surfaceRadius / 2f;
        worldFarRadius = transform.lossyScale.x * farRadius / 2f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, worldSurfaceRadius);
        Gizmos.DrawWireSphere(transform.position, worldFarRadius);
    }

    /*public override void OnSimulationStart(SplitterEvent Evt)
    {
        Evt.SimulatedAnchor.GetComponentInChildren<GravityField>().transform.GetComponent<Collider>().enabled = true;
    }*/
}

