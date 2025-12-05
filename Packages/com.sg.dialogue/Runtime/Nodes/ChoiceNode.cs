using System;
using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Core.Instructions;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// ChoiceNode 是一個選項節點，用於在對話流程中提供多個選項供玩家選擇。
    /// 每個選項都可以有自己的條件、文本，並在被選擇後跳轉到不同的節點。
    /// </summary>
    [Serializable]
    public class ChoiceNode : DialogueNodeBase
    {
        [Tooltip("此節點包含的對話選項列表。")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        
        [Header("事件")]
        [Tooltip("當進入此節點時觸發的 UnityEvent。")]
        public UnityEvent onEnter;
        [Tooltip("當玩家做出選擇並退出此節點時觸發的 UnityEvent。")]
        public UnityEvent onExit;

        /// <summary>
        /// 處理選項節點的核心邏輯。
        /// 它會指示 UI 管理器顯示所有符合條件的選項，並返回一個等待使用者輸入的指令。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個包含對話指令的協程迭代器。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            // 指示 UI 管理器顯示選項。
            // 我們傳遞一個 Lambda 函數 `(condition) => condition.Check(controller)` 給 UiManager，
            // 讓 UI 層可以自行檢查每個選項的顯示條件是否滿足。
            controller.UiManager.ShowChoices(this, (condition) => condition.Check(controller));
            
            // 返回指令，告訴控制器在此暫停，等待使用者從 UI 上選擇一個選項。
            yield return new WaitForUserInput();
        }

        public override void OnExit(DialogueController controller)
        {
            base.OnExit(controller);
            onExit?.Invoke();
        }

        /// <summary>
        /// 選項節點沒有單一的預設「下一個」節點，因為流程將由玩家的選擇來決定。
        /// 因此，此方法總是返回 null。
        /// </summary>
        /// <returns>永遠返回 null。</returns>
        public override string GetNextNodeId()
        {
            return null;
        }
        
        public override void ClearConnectionsForClipboard()
        {
            // 清除 ChoiceNode 本身的 nextNodeId (如果有的話，雖然 ChoiceNode 通常沒有)
            // base.ClearConnectionsForClipboard(); // DialogueNodeBase 的 GetNextNodeId 已經返回 null，所以這裡不需要呼叫 base

            if (choices == null) return;
            foreach (var c in choices)
            {
                c.nextNodeId = null;
            }
        }

        public override void ClearUnityReferencesForClipboard()
        {
            // UnityEvent 內部可能包含對 Unity 物件的引用，但通常不需要在剪貼簿操作時清除其內部細節
            // 如果需要，可以考慮遍歷 GetPersistentEventCount() 並清除
        }
    }
}
