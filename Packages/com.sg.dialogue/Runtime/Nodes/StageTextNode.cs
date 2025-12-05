using System;
using System.Collections;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// 在舞台中央顯示一段文字的對話節點。
    /// </summary>
    [Serializable]
    public class StageTextNode : DialogueNodeBase
    {
        [Tooltip("要以打字機效果顯示在舞台中央的文字。支援使用 {variableName} 的格式來插入變數。")]
        [TextArea(3, 10)]
        public string message;

        [Header("本地化")]
        [Tooltip("此訊息的本地化 Key。如果留空，則直接使用上方的 message 欄位作為原文。")]
        public string messageKey;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;
        
        [Header("打字機速度")]
        [Tooltip("打字機速度")]
        public float typingSpeed = 0.05f;

        public override IEnumerator Process(DialogueController controller)
        {
            // 遵循與 TextNode 相同的機制
            string rawText = !string.IsNullOrEmpty(messageKey) ? LocalizationManager.GetText(messageKey) : message;
            if (string.IsNullOrEmpty(rawText)) rawText = message;
            string formattedText = controller.FormatString(rawText);

            if (!string.IsNullOrEmpty(formattedText))
            {
                controller.VisualManager.ShowStageText(formattedText, typingSpeed);
            }
            
            // 這個節點會立即完成並讓對話流程繼續
            yield break;
        }

        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
        
        public override void ClearConnectionsForClipboard()
        {
            nextNodeId = null;
        }
    }
}
