using System;
using System.Collections;
using SG.Dialogue.Core.Instructions;
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

        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        [Header("打字機效果設定")]
        [Tooltip("每個字元出現的時間間隔（秒）。數值]越小，打字速度越快。")]
        public float typingSpeed = 0.1f;
        
        [Tooltip("打字機效果完成後，進入下一步驟（等待輸入或自動前進）前的額外延遲時間。")]
        public float postTypingDelay = 0.3f;

        public override IEnumerator Process(DialogueController controller)
        {
            // 取得原始文字，優先使用本地化 Key，失敗時回退到 message
            string rawText = message;
            
            if (!string.IsNullOrEmpty(messageKey))
            {
                string localized = LocalizationManager.GetText(messageKey);
                if (!string.IsNullOrEmpty(localized))
                {
                    rawText = localized;
                }
                else
                {
                    Debug.LogWarningFormat("StageTextNode: 無法從本地化系統取得 Key='{0}' 的文字，改用原始 message。", messageKey);
                }
            }
            
            if (string.IsNullOrEmpty(rawText))
            {
                Debug.LogWarning("StageTextNode: rawText 為空，將不顯示任何舞台文字。");
            }
            else
            {
                string formattedText = controller.FormatString(rawText);
            
                if (string.IsNullOrEmpty(formattedText))
                {
                    Debug.LogWarningFormat("StageTextNode: 格式化後文字為空，改用未格式化文字。原始文字='{0}'", rawText);
                    formattedText = rawText;
                }
            
                Debug.LogFormat("StageTextNode: 格式化後的文字='{0}'", formattedText);
            
                if (controller.VisualManager == null)
                {
                    Debug.LogError("StageTextNode: VisualManager 為 null，無法顯示舞台文字。");
                }
                else
                {
                    controller.VisualManager.ShowStageText(formattedText, typingSpeed);

                    Debug.LogFormat("isStageTextTyping={0}", controller.VisualManager.IsStageTextTyping());
                    while (controller.VisualManager.IsStageTextTyping())
                    {
                        yield return new WaitForSeconds(typingSpeed);
                    }
                }
            }
            
            if (postTypingDelay > 0f)
            {
                yield return new WaitForSeconds(postTypingDelay);
            }
            
            bool advance = false;
            float delay = 0f;

            switch (controller.autoAdvanceOverride)
            {
                case AutoAdvanceMode.ForceEnable:
                    advance = true;
                    delay = controller.forcedAutoAdvanceDelay;
                    break;
                case AutoAdvanceMode.ForceDisable:
                    advance = false;
                    break;
                case AutoAdvanceMode.Default:
                    if (controller.CurrentGraph != null && controller.CurrentGraph.autoAdvanceEnabled)
                    {
                        advance = true;
                        delay = controller.AutoAdvanceDelay;
                    }
                    break;
            }

            if (advance)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return new WaitForUserInput();
            }
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
