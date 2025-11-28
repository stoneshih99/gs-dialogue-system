using System;
using System.Collections;
using SG.Dialogue.Core.Instructions;
using UnityEngine;

namespace SG.Dialogue
{
    /// <summary>
    /// DialogueNodeBase 是所有對話節點的抽象基底類別。
    /// 它包含了節點的唯一識別碼，並定義了節點處理邏輯和獲取下一個節點 ID 的虛擬方法。
    /// </summary>
    [Serializable]
    public abstract class DialogueNodeBase
    {
        /// <summary>
        /// 節點的唯一識別碼 (ID)。
        /// </summary>
        public string nodeId;

        /// <summary>
        /// 如果為 false，對話控制器在執行時將會跳過此節點及其所有子節點。
        /// </summary>
        [Tooltip("如果為 false，對話控制器在執行時將會跳過此節點。")]
        public bool IsEnabled = true;

        /// <summary>
        /// 處理節點的核心邏輯。每個具體的節點類型都必須覆寫此方法以執行其特定行為。
        /// 這個方法是一個協程，可以 yield return 各種指令來控制對話流程。
        /// 例如：
        /// - yield return new WaitForUserInput(); // 等待玩家點擊
        /// - yield return new AdvanceToNode("otherNodeId"); // 直接跳轉到另一個節點
        /// - yield return new WaitForSeconds(1.0f); // 等待一段時間
        /// </summary>
        /// <param name="controller">對話總控制器，提供對 UI、視覺、狀態等管理器的存取。</param>
        /// <returns>一個迭代器，用於協程執行。</returns>
        public abstract IEnumerator Process(DialogueController controller);

        /// <summary>
        /// 取得此節點的預設下一個節點 ID。
        /// 這個方法主要用於當節點執行完畢後，決定流程的預設走向。
        /// 對於大多數只有單一輸出的節點，這會直接回傳其 nextNodeId 欄位。
        /// 對於沒有輸出的節點（如 EndNode），或是有多個可能輸出的節點（如 ChoiceNode），
        /// 這個方法的行為可能會有所不同（例如返回 null）。
        /// </summary>
        /// <returns>下一個節點的 ID；如果沒有預設的下一個節點，則返回 null。</returns>
        public virtual string GetNextNodeId()
        {
            // 預設實作：嘗試透過反射取得名為 "nextNodeId" 的欄位值。
            // 這是一個通用的方法，適用於大多數只有一個「下一步」連接的簡單節點。
            // 派生類別可以覆寫此方法以提供更具體的邏輯。
            var field = GetType().GetField("nextNodeId");
            return field?.GetValue(this) as string;
        }
    }
}
