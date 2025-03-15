using UnityEditor;
using UnityEngine;
namespace NoTime.Splitter.Editors
{

    [CustomEditor(typeof(SplitterSubscriber))]
    public class SplitterSubscriberEditor : Editor
    {
        private bool syncSettingsOpen;

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            SplitterSubscriber subscriber = (SplitterSubscriber)target;

            HandleSubscriberSyncSettings(subscriber, serializedObject);
        }

        private void HandleSubscriberSyncSettings(SplitterSubscriber subscriber, SerializedObject serializedObject)
        {

            /*
             *  [HideInInspector]
                public bool SyncTransformPosition_OverrideAnchorSettings = false;
                [HideInInspector]
                public bool SyncTransformPosition = false;
                [HideInInspector]
                public bool SyncChildrenTransformPositions_OverrideAnchorSettings = false;
                [HideInInspector]
                public bool SyncChildrenTransformPositions = false;
             * 
             */

            syncSettingsOpen = EditorGUILayout.Foldout(syncSettingsOpen, "Sync Options");
            if (syncSettingsOpen)
            {
                //EditorGUILayout.Space();
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("OverrideAnchorSettings"),
                    new GUIContent(
                        "Override Anchor Settings:"
                    )
                );

                EditorGUI.BeginDisabledGroup(!subscriber.OverrideAnchorSettings);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(16f);
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("SyncTransform"),
                    new GUIContent(
                        "Sync Transform",
                        "This provides the most accurate positional syncing at the cost of some cpu processing power."
                        + "  Use this to directly move the Subscriber's transform to the exact position and rotation of the simulation."
                        + "  Note:  This will override anchor settings."
                    )
                );
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(16f);
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("SyncChildTransforms"),
                    new GUIContent(
                        "Sync Child Transforms",
                        "Use this in order to sync simulated updates to the subscribers child transforms."
                        + "  Note:  This will override anchor settings."
                    )
                );
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();

                serializedObject.ApplyModifiedProperties();
            }

        }
    }
}
