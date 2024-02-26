using NoTime.Splitter.Demo;
using UnityEngine;

public class MysteryObjectBehavior : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<RigidbodyFpsController>() != null)
        {
            transform.GetComponent<AutomaticOrbit>().enabled = true;
        }
    }
}
