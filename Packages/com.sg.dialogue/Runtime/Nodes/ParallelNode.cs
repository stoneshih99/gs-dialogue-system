using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Core.Instructions;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    [Serializable]
    public class ParallelNode : DialogueNodeBase
    {
        [Tooltip("此並行節點的描述性名稱，僅用於編輯器中識別。")]
        public string parallelName = "Parallel";

        [SerializeField]
        [TextArea(3, 6)]
        [Tooltip("此並行節點的詳細描述或註解。")]
        private string description;

        [SerializeReference, Tooltip("此並行節點包含的子節點列表。")]
        public List<DialogueNodeBase> childNodes = new List<DialogueNodeBase>();

        [Tooltip("要並行執行的所有分支的起始節點 ID 列表。這些 ID 必須對應到 childNodes 內的節點。")]
        public List<string> branchStartNodeIds = new List<string>();

        [Header("流程控制")]
        [Tooltip("當所有並行分支都執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        public override async UniTask Process(DialogueController controller)
        {
            if (branchStartNodeIds == null || branchStartNodeIds.Count == 0)
            {
                Debug.LogWarning($"[Dialogue] ParallelNode '{nodeId}' has no branches to execute.");
                return;
            }

            bool wasInputSwallowed = false;
            var tasks = new List<UniTask>();
            foreach (var startNodeId in branchStartNodeIds)
            {
                if (!string.IsNullOrEmpty(startNodeId))
                {
                    tasks.Add(controller.GetBranchEnumerator(startNodeId, () => wasInputSwallowed = true));
                }
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
            
            if (wasInputSwallowed)
            {
                await new WaitForUserInput();
            }
        }

        public override string GetNextNodeId()
        {
            return nextNodeId;
        }

        public override void ClearConnectionsForClipboard()
        {
            nextNodeId = null;
            branchStartNodeIds = new List<string>();
            foreach (var childNode in childNodes)
            {
                childNode.ClearConnectionsForClipboard();
            }
        }
    }
}
