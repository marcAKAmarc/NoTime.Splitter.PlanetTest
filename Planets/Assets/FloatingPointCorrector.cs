using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FloatingPointCorrector : MonoBehaviour
{
    private List<Transform> topTransforms;
    public float Range = 1000f;
    private float SquareRange;
    // Start is called before the first frame update
    void Start()
    {
        SquareRange = Range * Range;
        topTransforms = FindObjectsOfType<Transform>(true).Where(x => x.parent == null).ToList();
    }

    // Update is called once per frame
    void lateUpdate()
    {
       if(transform.position.sqrMagnitude > SquareRange)
        {
            Vector3 offset = transform.position;
            for(int i = 0; i < topTransforms.Count; i++)
            {
                topTransforms[i].position -= offset;
            }
        }
    }
}
