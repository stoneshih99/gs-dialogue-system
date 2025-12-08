using UnityEditor;
using UnityEngine;
using System.IO;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 編輯器工具視窗，用於快速建立對話系統相關的 ScriptableObject 資源。
    /// </summary>
    public class DialogueAssetCreatorWindow : EditorWindow
    {
        private string _baseFileName = "NewDialogue"; // 預設的基礎檔案名稱
        private string _targetFolderPath = "Assets/"; // 預設的儲存路徑

        /// <summary>
        /// 開啟對話資源建立器視窗。
        /// </summary>
        [MenuItem("SG/Dialogue/Create Dialogue Assets...", false, 0)]
        public static void ShowWindow()
        {
            GetWindow<DialogueAssetCreatorWindow>("Dialogue Asset Creator");
        }

        /// <summary>
        /// 繪製視窗內容。
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("對話資源建立器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 基礎檔案名稱輸入框
            _baseFileName = EditorGUILayout.TextField("基礎檔案名稱", _baseFileName);

            // 目標資料夾選擇
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("儲存路徑", _targetFolderPath);
            if (GUILayout.Button("選擇資料夾", GUILayout.Width(100)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("選擇儲存對話資源的資料夾", _targetFolderPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 將絕對路徑轉換為專案相對路徑
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        _targetFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        Debug.LogWarning("請選擇專案內的資料夾。");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            // 建立按鈕
            if (GUILayout.Button("建立所有對話資源"))
            {
                CreateDialogueAssets();
            }
        }

        /// <summary>
        /// 建立對話圖、本地化表格和對話狀態資產。
        /// </summary>
        private void CreateDialogueAssets()
        {
            if (string.IsNullOrEmpty(_baseFileName))
            {
                EditorUtility.DisplayDialog("錯誤", "基礎檔案名稱不能為空。", "確定");
                return;
            }

            if (string.IsNullOrEmpty(_targetFolderPath) || !_targetFolderPath.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog("錯誤", "請選擇一個有效的專案內資料夾。", "確定");
                return;
            }

            // 確保目標資料夾存在
            if (!AssetDatabase.IsValidFolder(_targetFolderPath))
            {
                Directory.CreateDirectory(Application.dataPath + _targetFolderPath.Substring("Assets".Length));
                AssetDatabase.Refresh();
            }

            // 建立 DialogueGraph
            CreateAsset<DialogueGraph>("Graph_" + _baseFileName);

            // 建立 LocalizationTable
            CreateAsset<LocalizationTable>("LocalizationTable_" + _baseFileName);

            // 建立 DialogueStateAsset
            CreateAsset<DialogueStateAsset>("State_" + _baseFileName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", $"已在 '{_targetFolderPath}' 中建立對話資源。", "確定");
            Close(); // 建立完成後關閉視窗
        }

        /// <summary>
        /// 泛型方法，用於建立指定類型的 ScriptableObject 資產。
        /// </summary>
        /// <typeparam name="T">要建立的 ScriptableObject 類型。</typeparam>
        /// <param name="assetName">資產的名稱。</param>
        private void CreateAsset<T>(string assetName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            string path = Path.Combine(_targetFolderPath, assetName + ".asset");
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"已建立 {typeof(T).Name}: {path}");
        }
    }
}
