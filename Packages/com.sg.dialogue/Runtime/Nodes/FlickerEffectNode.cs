using System;
using System.Collections;
using SG.Dialogue.Enums;
using SG.Dialogue.Presentation;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// FlickerEffectNode 是一個閃爍特效節點，用於讓指定的背景或角色產生快速的明暗閃爍效果。
    /// 這常用於表現角色消失、訊號不穩或靈異現象等。
    /// </summary>
    [Serializable]
    public class FlickerEffectNode : DialogueNodeBase
    {
        /// <summary>
        /// 定義閃爍效果的目標類型。
        /// </summary>
        public enum TargetType { Background, Character }

        [Header("目標設定")]
        [Tooltip("閃爍效果的目標類型。")]
        public TargetType Target = TargetType.Background;

        [Tooltip("當目標是『背景』時，要閃爍的背景圖層索引。")]
        public int BackgroundLayerIndex = 0;

        [Tooltip("當目標是『角色』時，要閃爍的角色位置。")]
        public CharacterPosition CharacterPosition = CharacterPosition.Center;

        [Header("閃爍參數")]
        [Tooltip("整個閃爍效果的總持續時間（秒）。")]
        public float Duration = 1f;

        [Tooltip("閃爍的頻率（每秒閃爍次數）。")]
        public float Frequency = 10f;

        [Tooltip("閃爍時的最低透明度（Alpha 值，範圍 0 到 1）。1 表示不變，0 表示完全透明。")]
        [Range(0f, 1f)]
        public float MinAlpha = 0.2f;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理閃爍特效節點的核心邏輯。
        /// 它會呼叫視覺管理器來執行閃爍效果，並等待其完成。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個協程迭代器，用於等待特效完成。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            // 呼叫視覺管理器來處理這個特效，並等待其完成
            yield return controller.VisualManager.ExecuteFlickerEffect(this);
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
    }
}
