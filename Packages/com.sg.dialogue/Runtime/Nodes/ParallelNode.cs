using System;
using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Core.Instructions;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// ParallelNode 是一個並行節點，它作為一個容器，可以包含自己的子節點。
    /// 當流程進入此節點時，它會同時啟動多個對話分支（子流程）。
    /// 主流程會在此暫停，直到所有並行分支都執行完畢後，才會繼續前進到下一個節點。
    /// 這對於同時觸發多個獨立事件（例如，兩個角色同時做動作、播放音效和顯示特效）非常有用。
    /// </summary>
    [Serializable]
    public class ParallelNode : DialogueNodeBase
    {
        [Tooltip("此並行節點的描述性名稱，僅用於編輯器中識別。")]
        public string parallelName = "New Parallel";

        [SerializeReference, Tooltip("此並行節點包含的子節點列表。")]
        public List<DialogueNodeBase> childNodes = new List<DialogueNodeBase>();

        [Tooltip("要並行執行的所有分支的起始節點 ID 列表。這些 ID 必須對應到 childNodes 內的節點。")]
        public List<string> branchStartNodeIds = new List<string>();

        [Header("流程控制")]
        [Tooltip("當所有並行分支都執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理並行節點的核心邏輯。
        /// 它會為每個分支啟動一個獨立的協程，並等待所有協程都完成。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個協程迭代器，用於等待所有分支完成。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            // 檢查是否有分支可以執行
            if (branchStartNodeIds == null || branchStartNodeIds.Count == 0)
            {
                Debug.LogWarning($"[對話] 並行節點 '{nodeId}' 沒有設定任何要執行的分支。");
                yield break; // 直接結束此節點的處理
            }

            var branches = new List<Coroutine>();
            foreach (var startNodeId in branchStartNodeIds)
            {
                if (!string.IsNullOrEmpty(startNodeId))
                {
                    // 為每個分支啟動一個獨立的執行協程
                    // DialogueController 的 ExecuteBranch 方法會處理一個完整的子流程
                    branches.Add(controller.ExecuteBranch(startNodeId));
                }
            }

            // 等待所有分支協程執行完畢
            // yield return 會等待一個協程執行完成
            foreach (var branch in branches)
            {
                yield return branch;
            }
            
            // 當所有分支都執行完畢後，Process 方法結束，
            // DialogueController 會自動根據 GetNextNodeId() 的結果前進到下一個節點。
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此並行節點本身的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
    }
}
