using System.Collections.Generic;
using SG.Dialogue.Variables;
using UnityEditor;
using UnityEngine;

namespace SG.Dialogue.Editor.Debugger
{
    public class DialogueVariableDebugger : EditorWindow
    {
        private DialogueController _targetController;
        private Vector2 _scrollPosition;
        
        // 為了避免每幀產生大量 GC，我們只在 Repaint 時獲取資料，或者使用簡易的計數器
        // 但為了編輯器的即時性，暫時接受 Export 的開銷（通常變數數量不多）

        [MenuItem("SG Framework/Dialogue/Variable Debugger")]
        public static void ShowWindow()
        {
            GetWindow<DialogueVariableDebugger>("Variable Debugger");
        }

        private void OnEnable()
        {
            // 嘗試自動尋找場景中的 Controller
            if (_targetController == null)
            {
                _targetController = FindObjectOfType<DialogueController>();
            }
            
            // 啟用自動重繪，這樣 Runtime 的數值變更才能即時顯示
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            DrawHeader();

            if (_targetController == null)
            {
                EditorGUILayout.HelpBox("No DialogueController found in the scene.", MessageType.Info);
                if (GUILayout.Button("Find in Scene"))
                {
                    _targetController = FindObjectOfType<DialogueController>();
                }
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawGlobalVariables();
            GUILayout.Space(10);
            DrawLocalVariables();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target Controller", EditorStyles.boldLabel);
            _targetController = (DialogueController)EditorGUILayout.ObjectField("Controller", _targetController, typeof(DialogueController), true);
            EditorGUILayout.Space();
            
            if (Application.isPlaying)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("Status: Playing", EditorStyles.miniLabel);
            }
            else
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("Status: Editor Mode (Static Data Only)", EditorStyles.miniLabel);
            }
            GUI.color = Color.white;
            EditorGUILayout.Space();
            
            // 分隔線
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space();
        }

        private void DrawGlobalVariables()
        {
            EditorGUILayout.LabelField("Global Variables (DialogueStateAsset)", EditorStyles.boldLabel);

            if (_targetController.GlobalState == null)
            {
                EditorGUILayout.HelpBox("Global State Asset is missing.", MessageType.Warning);
                return;
            }

            // 使用 Box 風格包裹內容
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Ints
            DrawVariableGroup("Integers", 
                () => _targetController.GlobalState.ExportInts(),
                (key, val) => _targetController.GlobalState.SetInt(key, val));

            // Bools
            DrawVariableGroup("Booleans", 
                () => _targetController.GlobalState.ExportBools(),
                (key, val) => _targetController.GlobalState.SetBool(key, val));

            // Strings
            DrawVariableGroup("Strings", 
                () => _targetController.GlobalState.ExportStrings(),
                (key, val) => _targetController.GlobalState.SetString(key, val));

            EditorGUILayout.EndVertical();
        }

        private void DrawLocalVariables()
        {
            EditorGUILayout.LabelField("Local Variables (Current Session)", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Local variables are only available during Play Mode.", MessageType.Info);
                return;
            }

            if (_targetController.LocalState == null)
            {
                // 理論上不會發生，因為 LocalState 在建構子就 new 了
                return; 
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Ints
            DrawVariableGroup("Integers", 
                () => _targetController.LocalState.ExportInts(),
                (key, val) => _targetController.LocalState.SetInt(key, val));

            // Bools
            DrawVariableGroup("Booleans", 
                () => _targetController.LocalState.ExportBools(),
                (key, val) => _targetController.LocalState.SetBool(key, val));

            // Strings
            DrawVariableGroup("Strings", 
                () => _targetController.LocalState.ExportStrings(),
                (key, val) => _targetController.LocalState.SetString(key, val));

            EditorGUILayout.EndVertical();
        }

        // 泛型方法來處理不同類型的變數繪製與修改
        // 因為 Export 回傳的是 List<Pair>，我們需要針對不同型別處理 UI
        
        private void DrawVariableGroup(string label, System.Func<List<DialogueStateAsset.IntPair>> getter, System.Action<string, int> setter)
        {
            var list = getter();
            if (list.Count == 0) return;

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            foreach (var pair in list)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(pair.key, GUILayout.Width(150));
                
                int newValue = EditorGUILayout.IntField(pair.value);
                if (newValue != pair.value)
                {
                    setter(pair.key, newValue);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private void DrawVariableGroup(string label, System.Func<List<DialogueStateAsset.BoolPair>> getter, System.Action<string, bool> setter)
        {
            var list = getter();
            if (list.Count == 0) return;

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            foreach (var pair in list)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(pair.key, GUILayout.Width(150));
                
                bool newValue = EditorGUILayout.Toggle(pair.value);
                if (newValue != pair.value)
                {
                    setter(pair.key, newValue);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private void DrawVariableGroup(string label, System.Func<List<DialogueStateAsset.StringPair>> getter, System.Action<string, string> setter)
        {
            var list = getter();
            if (list.Count == 0) return;

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            foreach (var pair in list)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(pair.key, GUILayout.Width(150));
                
                string newValue = EditorGUILayout.TextField(pair.value);
                if (newValue != pair.value)
                {
                    setter(pair.key, newValue);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }
}
