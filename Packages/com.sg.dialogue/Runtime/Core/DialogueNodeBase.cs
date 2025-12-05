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
        /// 當對話流程離開此節點時，由 DialogueController 呼叫。
        /// 子類別可以覆寫此方法來執行清理邏輯，例如隱藏 UI 元素。
        /// </summary>
        /// <param name="controller">對話總控制器，提供對管理器的存取。</param>
        public virtual void OnExit(DialogueController controller)
        {
            // 預設什麼都不做。
        }

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
        
        #region Clipboard Hooks

        /// <summary>
        /// 當節點已經被 JSON 反序列化出來，準備作為「複製結果」使用時呼叫。
        /// 你可以在 override 裡清除 runtime 狀態、重設一些欄位（但不含 nodeId）。
        /// </summary>
        public virtual void OnAfterClonedFromClipboard()
        {
            // 預設什麼都不做
        }

        /// <summary>
        /// 複製到剪貼簿時，用來清除「連線相關」資訊（避免貼上後連回原本 graph）。
        /// 例如 nextNodeId / childIds 等。
        /// </summary>
        public virtual void ClearConnectionsForClipboard()
        {
            // 預設什麼都不做，有需要的子類別再 override
        }

        /// <summary>
        /// 複製到剪貼簿時，用來清除不應該被 JSON 直存的 Unity 物件引用
        /// （例如暫時的 GameObject、runtime only 參考）。
        /// </summary>
        public virtual void ClearUnityReferencesForClipboard()
        {
            // 預設不做事，有需要的子類別 override，
            // 或呼叫下面這個 helper: ClearAllUnityObjectFields();
        }

        /// <summary>
        /// 共用 helper：把這個 node 上所有 UnityEngine.Object 欄位清成 null。
        /// 若你的節點需要這種行為，可以在 override 裡呼叫它。
        /// </summary>
        protected void ClearAllUnityObjectFields()
        {
            var type = GetType();
            var fields = type.GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.FlattenHierarchy);

            foreach (var field in fields)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    field.SetValue(this, null);
                }
            }
        }

        #endregion
    }
}
