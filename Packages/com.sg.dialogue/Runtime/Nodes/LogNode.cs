using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SG.Dialogue.Core.Instructions;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// LogNode 是一個日誌節點，用於在對話流程中向 Unity Console 印出一條偵錯訊息。
    /// 這在測試對話流程、檢查變數值或確認某個分支是否被執行時非常有用。
    /// </summary>
    [Serializable]
    public class LogNode : DialogueNodeBase
    {
        [Tooltip("訊息的類型（Log, Warning, Error）。")]
        public LogType messageType = LogType.Log;

        [Tooltip("要印出的訊息。您可以在其中使用 {variableName} 來顯示變數的值。")]
        [TextArea] public string message;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理日誌節點的核心邏輯。
        /// 它會根據設定的訊息類型，將格式化後的訊息印出到 Unity Console。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個空的協程迭代器。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            string formattedMessage = controller.FormatString(message); // 使用控制器內建的格式化方法

            switch (messageType)
            {
                case LogType.Log:
                    Debug.Log($"[對話日誌] {formattedMessage}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[對話日誌] {formattedMessage}");
                    break;
                case LogType.Error:
                    Debug.LogError($"[對話日誌] {formattedMessage}");
                    break;
            }

            // 此節點是立即執行的，不需等待。
            yield break;
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此日誌節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
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
