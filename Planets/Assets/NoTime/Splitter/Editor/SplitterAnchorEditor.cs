using UnityEngine;
using UnityEditor;

namespace NoTime.Splitter.Editors
{
    [CustomEditor(typeof(SplitterAnchor))]
    public class SplitterAnchorEditor : Editor
    {
        private bool showSimulationValue = false;

        private bool syncSettingsOpen = false;

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            SplitterAnchor anchor = (SplitterAnchor)target;

            HandleShowSimulation(anchor, serializedObject);

            HandleSubscriberSyncSettings(anchor, serializedObject);
        }
        private void HandleSubscriberSyncSettings(SplitterAnchor anchor, SerializedObject serializedObject)
        {
            syncSettingsOpen = EditorGUILayout.Foldout(syncSettingsOpen, "Sync Options");
            if (syncSettingsOpen)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("SyncSubscriberTransforms"),
                    new GUIContent(
                        "Sync Subscriber Transforms",
                        "This provides the most accurate positional syncing at the cost of some cpu processing power."
                        +"  Use this to directly move the Subscriber's transform to the exact position and rotation of the simulation."
                        +"  Note:  This can be overridden by subscribers."
                    )
                );
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("SyncSubscriberChildTransforms"),
                    new GUIContent(
                        "Sync Subscriber Child Transforms",
                        "Use this in order to sync simulated updates to the subscribers child transforms."
                        +"  Note:  This can be overridden by subscribers."
                    )
                );
                /*EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("CallPhysicsSync"),
                    new GUIContent(
                        "Call Physics.Sync()",
                        "This calls Physics.Sync() in the main scene after updating subscriber transforms."
                    )
                );*/

                serializedObject.ApplyModifiedProperties();
            }
            
        }
        private void HandleShowSimulation(SplitterAnchor anchor, SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                new GUIContent(
                    "Show Simulation",
                    "Render simulated anchor and subscribers at world position: (100, 100, 100). Only occurs at runtime."
                ),
                new GUILayoutOption[] {
                    GUILayout.MaxWidth(EditorGUIUtility.labelWidth)
                }
            );


            // Start a code block to check for GUI changes
            EditorGUI.BeginChangeCheck();

            showSimulationValue = EditorGUILayout.Toggle(anchor.SimulationVisible);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.FindProperty("SimulationVisible").boolValue = showSimulationValue;
                serializedObject.ApplyModifiedProperties();

                anchor.SetVisibility(showSimulationValue);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void CustomLabel(string label, string toolTip)
        {
            EditorGUILayout.LabelField(
                new GUIContent(
                    label,
                    toolTip
                ),
                new GUILayoutOption[] {
                    GUILayout.MaxWidth(EditorGUIUtility.labelWidth)
                }
            );
        }
    }
}
