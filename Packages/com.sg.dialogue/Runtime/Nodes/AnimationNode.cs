using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        
        [Tooltip("是否等待動畫播放完畢才繼續下一個節點。\n如果動畫是無限循環 (Loops = -1)，請務必設為 False，否則對話會卡住。")]
        public bool waitForCompletion = false;
        
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
        /// 處理節點的邏輯。
        /// </summary>
        public override async UniTask Process(DialogueController controller)
        {
            // 在 ParallelNode 或其他直接呼叫 Process 的情境下，
            // 我們需要主動呼叫 VisualManager 來播放動畫。
            if (controller != null && controller.VisualManager != null)
            {
                await controller.VisualManager.PlayAnimations(this);
            }
        }

        public override void ClearConnectionsForClipboard()
        {
            nextNodeId = null;
        }
    }
}
