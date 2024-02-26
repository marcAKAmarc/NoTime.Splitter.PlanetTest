using UnityEngine;

public class PlanetBehaviour : MonoBehaviour
{
    public Vector3 spin;
    private bool doit = true;
    private void FixedUpdate()
    {
        if (doit)
            transform.GetComponent<Rigidbody>().angularVelocity = spin;//* transform.GetComponent<Rigidbody>().mass);
        doit = false;
    }
}
