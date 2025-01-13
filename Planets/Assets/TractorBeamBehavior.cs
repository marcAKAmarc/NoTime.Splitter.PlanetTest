using NoTime.Splitter;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TractorBeamBehavior : MonoBehaviour
{
    public float BeamRadius;
    public float BeamDistance;
    public float BeamForce;
    public LayerMask Mask;
    private Ray ray;
    private RaycastHit[] hits;
    private Rigidbody myRigid;
    private SplitterSubscriber mySubscriber;
    private bool doIt;
    public bool ViewBeam;
    public Transform NormalizedBeamVisualization;
    private List<Transform> beamVisuals = new List<Transform>();
    private List<Collider> colliders;
    // Start is called before the first frame update
    void Start()
    {
        ray = new Ray();
        hits = new RaycastHit[5];
        myRigid = transform.GetComponentInParent<Rigidbody>();
        mySubscriber = transform.GetComponentInParent<SplitterSubscriber>();
        colliders = transform.GetComponents<Collider>().ToList();
    }

    int i;
    void Update()
    {
        doIt = Input.GetKey(KeyCode.T);

        for (; i < colliders.Count; i++)
        {
            colliders[i].enabled = doIt;
        }
        
        //Debug.Log("dO IT: " + doIt.ToString());
        if (doIt && ViewBeam)
            VisualizeBeamInGame();
        else
            HideBeamInGame();
    }
    private Rigidbody _targetedBody;
    private SplitterSubscriber _targetedSubscriber;
    private RaycastHit _thisHit;
    void FixedUpdate()
    {
        //Debug.Log("Fixed DO IT:" + doIt.ToString());
        if (!doIt)
            return;
        //Debug.Log("TractorBeam FixedUpdate");
        ray.direction = transform.forward;
        ray.origin = transform.position + (transform.forward * BeamRadius);

        Debug.Log(ray.origin.ToString());
        
        Physics.SphereCastNonAlloc(ray, BeamRadius, hits, BeamDistance-BeamRadius, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
        
        //Debug.Log("Tractor beam result: " + hits.Length.ToString());

        _targetedSubscriber = null;
        _thisHit = hits.FirstOrDefault(x => x.transform != null && x.transform.GetComponentInParent<Rigidbody>() != null);
        if (_thisHit.collider == null)
            return;

        _targetedBody = _thisHit.transform.GetComponentInParent<Rigidbody>();

        if (_targetedBody == null)
            return;
        
        Debug.Log("Tractoring:  " + _targetedBody.name);

        if (_targetedBody != null)
            _targetedSubscriber = _targetedBody.GetComponent<SplitterSubscriber>();

        if (_targetedSubscriber != null)
            _targetedSubscriber.AppliedPhysics.AddForce(BeamForce * (myRigid.mass / (myRigid.mass + _targetedBody.mass)) * (ray.origin - _targetedBody.position).normalized);
        else
            _targetedBody.AddForce(BeamForce * (myRigid.mass / (myRigid.mass + _targetedBody.mass)) * (ray.origin - _targetedBody.position).normalized);

        mySubscriber.AppliedPhysics.AddForce(BeamForce * (_targetedBody.mass / (myRigid.mass + _targetedBody.mass)) * (_targetedBody.position - ray.origin).normalized);


        /*if(myRigid.mass /2f > _targetedBody.mass)
        {
            myRigid.
        }
        else if(_targetedBody.mass / 2f > myRigid.mass)
        {

        }
        else
        {

        }*/
        

    }

    private float vbPos;
    private int vbI;

    private void HideBeamInGame()
    {
        foreach (var beamItem in beamVisuals)
            beamItem.gameObject.SetActive(false);
    }
    private void VisualizeBeamInGame()
    {
        vbI = 0;
        vbPos = 0f;
        //remember, ray origin here is already pushed forward by radius
        while(vbPos < BeamDistance - BeamRadius)
        {
            if (beamVisuals.Count < vbI + 1)
                beamVisuals.Add(
                    Transform.Instantiate(NormalizedBeamVisualization)
                );
            beamVisuals[vbI].gameObject.SetActive(true);
            beamVisuals[vbI].position = ray.origin + (ray.direction * vbPos);
            beamVisuals[vbI].localScale = (BeamRadius / .5f) * Vector3.one;

            vbI += 1;
            vbPos += BeamRadius * 2f;
        }

        //Debug.Log(ray.origin.ToString());
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(ray.origin + ray.direction * BeamRadius,ray.origin + ray.direction * (BeamDistance-BeamRadius));
    }
}
