using System;
using System.Collections;
using SG.Dialogue.Core.Instructions;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// WaitNode 是一個等待節點，它會在繼續執行下一個節點之前，暫停指定的秒數。
    /// 這對於控制對話的節奏、等待動畫或特效播放完畢非常有用。
    /// </summary>
    [Serializable]
    public class WaitNode : DialogueNodeBase
    {
        [Tooltip("要等待的時間（秒）。")]
        public float WaitTime = 1f;

        [Header("流程控制")]
        [Tooltip("等待結束後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理等待節點的核心邏輯。
        /// 它會返回一個 WaitForSeconds 指令，讓對話控制器的主協程等待指定的時間。
        /// </summary>
        /// <param name="controller">對話總控制器（在此節點中未使用）。</param>
        /// <returns>一個 WaitForSeconds 實例。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            // 返回一個 WaitForSeconds 指令，DialogueController 會 yield return 這個指令，
            // 從而使對話流程暫停指定的時間。
            yield return new WaitForSeconds(WaitTime);
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此等待節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
    }
}
