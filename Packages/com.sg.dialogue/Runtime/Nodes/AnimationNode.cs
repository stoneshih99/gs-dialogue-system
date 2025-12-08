using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Animation;
using SG.Dialogue.Enums;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// AnimationNode 用於在對話中觸發 LitMotion 動畫。
    /// 它可以指定要對哪個位置的角色（左、中、右）播放一或多個動畫。
    /// </summary>
    public class AnimationNode : DialogueNodeBase
    {
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。如果留空，對話將在此節點後結束。")]
        public string nextNodeId;

        [Tooltip("動畫要作用在哪個位置的角色上（例如：左、中、右）。")]
        public CharacterPosition targetAnimationPosition;
        
        [Tooltip("此節點進入時要觸發的 LitMotion 動畫列表。")]
        public List<MotionData> motions = new List<MotionData>();

        /// <summary>
        /// 覆寫基底類別的方法，返回此節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }

        /// <summary>
        /// 處理節點的邏輯。對於 AnimationNode，主要邏輯由 DialogueController 特殊處理，
        /// 此處僅為滿足抽象類別的要求。
        /// </summary>
        public override IEnumerator Process(DialogueController controller)
        {
            // AnimationNode 的主要邏輯在 DialogueController.ProcessNodeCoroutine 中處理，
            // 以便直接控制協程的執行流程。
            // 此處返回 null 或空的迭代器。
            yield break;
        }

        public override void ClearConnectionsForClipboard()
        {
            nextNodeId = null;
        }
    }
}
