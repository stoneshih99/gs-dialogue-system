using System;
using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Core.Instructions;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// SequenceNode 是一個序列節點，它作為一個容器，可以包含自己的節點序列（子圖表）。
    /// 這允許將複雜的對話流程組織成可重用、可嵌套的模組，就像程式設計中的函式呼叫。
    /// </summary>
    [Serializable]
    public class SequenceNode : DialogueNodeBase
    {
        [Tooltip("此序列的描述性名稱，僅用於編輯器中識別。")]
        public string sequenceName = "Sequence"; // 重新加入 sequenceName 欄位

        /// <summary>
        /// 此序列節點的詳細描述或註解，用於說明其功能或流程。
        /// </summary>
        [SerializeField]
        [TextArea(3, 6)]
        [Tooltip("此序列節點的詳細描述或註解。")]
        private string description;

        [SerializeReference, Tooltip("此序列包含的子節點列表。這些節點構成了序列內部的對話流程。")]
        public List<DialogueNodeBase> childNodes = new List<DialogueNodeBase>();
        
        [Tooltip("此序列內部的起始節點 ID。當進入此序列時，流程將從這個子節點開始。")]
        public string startNodeId;

        [Header("流程控制")]
        [Tooltip("當此序列執行完畢後（即序列內的流程到達一個沒有後續節點的終點時），對話流程要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理序列節點的核心邏輯。
        /// 它會將序列結束後的「返回位址」推入執行堆疊，然後返回一個指令，告訴控制器前進到序列的起始節點。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個包含對話指令的協程迭代器。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            Debug.Log($"[對話] 進入序列: {sequenceName}"); // 調整回使用 sequenceName

            // 1. 將返回位址（即此序列結束後應前往的節點）推入執行堆疊。
            //    當序列內的流程結束時，控制器會從堆疊中彈出這個 ID 來繼續繼續執行。
            controller.PushToExecutionStack(nextNodeId);

            // 2. 返回一個指令，告訴控制器立即前進到此序列內部的起始節點。
            yield return new AdvanceToNode(startNodeId);
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此序列節點本身的下一個節點 ID。
        /// 注意：這與序列內部的流程無關，而是指整個序列節點完成後的走向。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }

        /// <summary>
        /// 清除剪貼簿相關的連線資訊。
        /// </summary>
        public override void ClearConnectionsForClipboard()
        {
            nextNodeId = null;
            startNodeId = null;
            // 遞迴清除子節點的連線
            foreach (var childNode in childNodes)
            {
                childNode.ClearConnectionsForClipboard();
            }
        }
    }
}
