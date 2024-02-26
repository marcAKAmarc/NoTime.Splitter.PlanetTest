using NoTime.Splitter;
using UnityEngine;
public class CloneAndEnableOnKey : SplitterEventListener
{
    public Transform Original;
    public KeyCode code;
    public Transform To;
    public Vector3 Velocity;
    public Rigidbody AttachedRigidbody;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(code))
        {
            var go = Transform.Instantiate(Original, To.position, To.rotation);
            go.gameObject.SetActive(true);
            go.transform.GetComponent<Rigidbody>().velocity += AttachedRigidbody.velocity;
            go.GetComponent<Rigidbody>().AddForce(go.GetComponent<Rigidbody>().rotation * Velocity, ForceMode.VelocityChange);
        }
    }
}
