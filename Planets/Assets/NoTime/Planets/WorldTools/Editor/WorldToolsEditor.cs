
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


[CustomEditor(typeof(WorldTool))]
public class TargetMoverEditor : Editor
{
    private struct vdata
    {
        public float distance;
        public Vector3 worldDir;

        public override bool Equals(object obj)
        {
            if (!(obj is vdata))
                return false;
            vdata vdataObj = (vdata)obj;
            return vdataObj.distance == distance && vdataObj.worldDir == worldDir;
        }
    }
    public override void OnInspectorGUI()
    {
            
        // Draw the default inspector fields
        DrawDefaultInspector();

        // Get the TargetMover instance being edited
        WorldTool targetMover = (WorldTool)target;

        // Add a button to the inspector
        /*if (GUILayout.Button("FaceAwayFromOrigin"))
        {
            // Record the current state for Undo functionality
            Undo.RecordObject(targetMover.transform, "Move Target");

            targetMover.Move();
        }*/
        if (GUILayout.Button("Swap X Y scale"))
        {
            targetMover.transform.localScale = new Vector3(targetMover.transform.localScale.y, targetMover.transform.localScale.x, targetMover.transform.localScale.z); 
        }
         
    }
}
