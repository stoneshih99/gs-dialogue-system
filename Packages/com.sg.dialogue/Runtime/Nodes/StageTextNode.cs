using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// 在舞台中央顯示一段文字的對話節點。
    /// </summary>
    [Serializable]
    public class StageTextNode : BaseTextNode
    {
        [Tooltip("要以打字機效果顯示在舞台中央的文字。支援使用 {variableName} 的格式來插入變數。")]
        [TextArea(3, 10)]
        public string message;

        [Header("本地化")]
        [Tooltip("此訊息的本地化 Key。如果留空，則直接使用上方的 message 欄位作為原文。")]
        public string messageKey;

        [Header("打字機效果設定")]
        [Tooltip("每個字元出現的時間間隔（秒）。數值越小，打字速度越快。")]
        public float typingSpeed = 0.1f;
        
        // --- 抽象屬性實作 ---
        protected override string Text => message;
        protected override string TextKey => messageKey;

        protected override async UniTask DoShowText(DialogueController controller, string formattedText)
        {
            if (string.IsNullOrEmpty(formattedText))
            {
                Debug.LogWarning("StageTextNode: 格式化後文字為空，將不顯示任何舞台文字。");
                return;
            }
            
            if (controller.VisualManager == null)
            {
                Debug.LogError("StageTextNode: VisualManager 為 null，無法顯示舞台文字。");
                return;
            }
            
            controller.VisualManager.ShowStageText(formattedText, typingSpeed);

            // 如果正在打字，則等待完成
            // 注意：這裡不直接使用 WaitForInputAsync，而是等待打字機狀態
            // 當使用者點擊時，DialogueController.OnAdvanceRequested 會呼叫 VisualManager.CompleteStageTextTyping
            // 這會導致 IsStageTextTyping 變為 false，結束這個等待
            if (controller.VisualManager.IsStageTextTyping())
            {
                await UniTask.WaitUntil(() => !controller.VisualManager.IsStageTextTyping());
            }
        }

        public override void OnExit(DialogueController controller)
        {
            if (controller.VisualManager != null)
            {
                controller.VisualManager.HideStageText();
            }
        }
    }
}
