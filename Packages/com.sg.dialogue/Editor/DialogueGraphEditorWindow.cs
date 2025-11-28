// filepath: Assets/Dialogue/Editor/DialogueGraphEditorWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using SG.Dialogue;
using SG.Dialogue.Editor.Dialogue.Editor;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 舊版 Graph 編輯視窗，現在只負責將資產開啟導向新的 DialogueLocalizationWindow。
    /// 保留類別以避免專案中其他地方引用失效。
    /// </summary>
    public class DialogueGraphEditorWindow : EditorWindow
    {
        // [MenuItem("SG/Dialogue/Graph Editor")] // 註銷此行，因為現在主要使用 DialogueLocalizationWindow
        /// <summary>
        /// 顯示舊版對話圖編輯器視窗，現在會直接導向新的 DialogueLocalizationWindow。
        /// </summary>
        public static void ShowWindow()
        {
            var wnd = DialogueLocalizationWindow.ShowWindowAndFocus();
            wnd.titleContent = new GUIContent("Dialogue Graph & Loc"); // 設定視窗標題
        }

        /// <summary>
        /// 當雙擊 DialogueGraph 資產時，自動開啟 DialogueLocalizationWindow 編輯器視窗並載入該圖。
        /// </summary>
        /// <param name="instanceId">資產的 Instance ID。</param>
        /// <param name="line">資產在檔案中的行號（此處未使用）。</param>
        /// <returns>如果成功處理資產則為 true，否則為 false。</returns>
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId); // 根據 Instance ID 獲取物件
            if (obj is DialogueGraph graph) // 如果物件是對話圖資產
            {
                var wnd = DialogueLocalizationWindow.ShowWindowAndFocus(); // 開啟並聚焦到本地化視窗
                wnd.SetGraph(graph); // 將對話圖設定到視窗中
                return true; // 表示已處理此資產開啟事件
            }
            return false; // 未處理此資產開啟事件
        }
    }
}
#endif
