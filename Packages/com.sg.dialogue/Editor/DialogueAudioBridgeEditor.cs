using System.Collections.Generic;
using SG.Dialogue.Events;
using UnityEditor;
using UnityEngine;

namespace SG.Dialogue.Editor
{
    [CustomEditor(typeof(DialogueAudioBridge))]
    public class DialogueAudioBridgeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DialogueAudioBridge bridge = (DialogueAudioBridge)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Auto Populate Audio Events"))
            {
                FindAllEvents();
            }
        }

        private void FindAllEvents()
        {
            
            string[] guids = AssetDatabase.FindAssets("t:AudioEvent");
            List<AudioEvent> events = new List<AudioEvent>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioEvent audioEvent = AssetDatabase.LoadAssetAtPath<AudioEvent>(path);
                if (audioEvent != null)
                {
                    events.Add(audioEvent);
                }
            }

            SerializedProperty allAudioEventsProp = serializedObject.FindProperty("allAudioEvents");
            allAudioEventsProp.ClearArray();
            
            for (int i = 0; i < events.Count; i++)
            {
                allAudioEventsProp.InsertArrayElementAtIndex(i);
                SerializedProperty element = allAudioEventsProp.GetArrayElementAtIndex(i);
                element.objectReferenceValue = events[i];
            }
            
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log($"[DialogueAudioBridge] Automatically populated {events.Count} AudioEvents.");
        }
    }
}
