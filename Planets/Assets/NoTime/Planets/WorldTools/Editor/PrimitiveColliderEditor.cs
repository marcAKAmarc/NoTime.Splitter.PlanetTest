using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(PrimitiveCollider))]
public class PrimitiveColliderEditor : Editor
{

    public override void OnInspectorGUI()
    {

        // Draw the default inspector fields
        DrawDefaultInspector();

        // Get the TargetMover instance being edited
        PrimitiveCollider targetMover = (PrimitiveCollider)target;

        // Add a button to the inspector
        if (GUILayout.Button("FaceAwayFromOrigin"))
        {

            Undo.SetCurrentGroupName("Zero out selected gameObjects");
            int group = Undo.GetCurrentGroup();
            // Record the current state for Undo functionality
            Undo.RecordObject(targetMover.transform, "CreateShits");

            foreach (Transform t in targetMover.transform)
            {
                Transform cube = Instantiate(targetMover.Orig, t);
                Undo.RegisterCreatedObjectUndo(cube.gameObject, "created cube");
                Move(cube.gameObject);
                SwapXYScale(cube.gameObject);
            }
            Undo.CollapseUndoOperations(group);
        }
    }

    public void Move(GameObject targetMover)
    {
        
        targetMover.transform.localPosition = Vector3.zero;
        targetMover.transform.localRotation = Quaternion.identity;
        targetMover.transform.localScale = Vector3.one;

        Mesh parentMesh = targetMover.transform.parent.GetComponent<MeshFilter>().sharedMesh;
        List<float> distance = new List<float>();
        
        foreach (Vector3 v in parentMesh.vertices)
        {
            foreach (Vector3 v2 in parentMesh.vertices)
            {
                distance.Add(

                            (v
                            -
                            v2).magnitude


                );
            }
        }

        distance = distance.Distinct().Skip(1).ToList();

        targetMover.transform.localScale = new Vector3(distance[0], distance[1], .0001f);

        Vector3 normal = targetMover.transform.parent.TransformVector(parentMesh.normals[0]);
        float height = 0f;
        Vector3 result = Vector3.up;
        for(int i = 0; i < parentMesh.vertices.Length - 1; i++)
        {
            Vector3[] vs = parentMesh.vertices;
            if (distance.Any(x => (vs[i] - vs[i+1]).magnitude == x))
            {
                float thisHeight = targetMover.transform.parent.TransformPoint(vs[i]).y - targetMover.transform.parent.TransformPoint(vs[i + 1]).y;
                if (thisHeight > height)
                {
                    height = thisHeight;
                    result = targetMover.transform.parent.TransformDirection(vs[i] - vs[i+1]).normalized;
                }
            }
        }

        // Change the position of the GameObject
        targetMover.transform.rotation = Quaternion.LookRotation(
            normal,
            result
        );
    }

    public void SwapXYScale(GameObject targetMover)
    {
        targetMover.transform.localScale = new Vector3(targetMover.transform.localScale.y, targetMover.transform.localScale.x, targetMover.transform.localScale.z);

    }
}

