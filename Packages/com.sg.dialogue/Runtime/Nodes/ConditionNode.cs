using System;
using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Conditions;
using SG.Dialogue.Core.Instructions;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// ConditionNode 是一個條件節點，用於根據一組條件的滿足與否，將對話流程導向不同的分支。
    /// 就像程式碼中的 if-else 語句。
    /// </summary>
    [Serializable]
    public class ConditionNode : DialogueNodeBase
    {
        [Tooltip("要進行檢查的條件。")]
        public Condition Condition = new Condition();

        [Header("流程控制")]
        [Tooltip("當條件評估結果為 True (真) 時，對話流程要前往的下一個節點 ID。")]
        public string TrueNextNodeId;
        
        [Tooltip("當條件評估結果為 False (假) 時，對話流程要前往的下一個節點 ID。")]
        public string FalseNextNodeId;

        /// <summary>
        /// 處理條件節點的核心邏輯。
        /// 它會評估條件，並返回一個指令來告訴控制器下一步要去哪個節點。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個包含對話指令的協程迭代器。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            // 檢查條件是否滿足
            bool result = Condition.Check(controller);

            // 根據條件結果選擇對應的下一個節點 ID
            string nextNodeId = result ? TrueNextNodeId : FalseNextNodeId;

            // 如果有有效的下一個節點，則返回一個 AdvanceToNode 指令來跳轉
            if (!string.IsNullOrEmpty(nextNodeId))
            {
                yield return new AdvanceToNode(nextNodeId);
            }
            else
            {
                // 如果對應的分支沒有設定下一個節點，則警告並結束對話，以避免流程卡住
                Debug.LogWarning($"條件節點 '{nodeId}' 的結果為 {result}，但對應的下一個節點 ID 為空。對話將結束。");
                yield return new EndDialogue();
            }
        }

        /// <summary>
        /// 條件節點的下一個節點 ID 取決於運行時的條件評估結果，因此沒有一個固定的「預設」下一個節點。
        /// 實際的下一個節點 ID 是在 Process 方法中動態決定的。
        /// </summary>
        /// <returns>永遠返回 null。</returns>
        public override string GetNextNodeId()
        {
            return null;
        }
    }
}
