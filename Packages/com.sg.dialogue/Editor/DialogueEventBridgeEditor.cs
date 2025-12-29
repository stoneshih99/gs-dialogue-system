using System.Collections.Generic;
using SG.Dialogue.Events;
using UnityEditor;
using UnityEngine;

namespace SG.Dialogue.Editor
{
    [CustomEditor(typeof(DialogueEventBridge))]
    public class DialogueEventBridgeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DialogueEventBridge bridge = (DialogueEventBridge)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Auto Populate Game Events"))
            {
                FindAllEvents();
            }
        }

        private void FindAllEvents()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameEvent");
            List<GameEvent> events = new List<GameEvent>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameEvent gameEvent = AssetDatabase.LoadAssetAtPath<GameEvent>(path);
                if (gameEvent != null)
                {
                    events.Add(gameEvent);
                }
            }

            SerializedProperty allGameEventsProp = serializedObject.FindProperty("gameEvents");
            allGameEventsProp.ClearArray();
            
            for (int i = 0; i < events.Count; i++)
            {
                allGameEventsProp.InsertArrayElementAtIndex(i);
                SerializedProperty element = allGameEventsProp.GetArrayElementAtIndex(i);
                element.objectReferenceValue = events[i];
            }
            
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log($"[DialogueEventBridgeEditor] Automatically populated {events.Count} GameEvents.");
        }
    }
}
