using System;
using System.Collections;
using SG.Dialogue.Enums;
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
        public TargetType target = TargetType.Background;

        [Tooltip("當目標是『背景』時，要閃爍的背景圖層索引。")]
        public int backgroundLayerIndex;

        [Tooltip("當目標是『角色』時，要閃爍的角色位置。")]
        public CharacterPosition characterPosition = CharacterPosition.Center;

        [Header("閃爍參數")]
        [Tooltip("整個閃爍效果的總持續時間（秒）。")]
        public float duration = 1f;

        [Tooltip("閃爍的頻率（每秒閃爍次數）。")]
        public float frequency = 10f;

        [Tooltip("閃爍時的最低透明度（Alpha 值，範圍 0 到 1）。1 表示不變，0 表示完全透明。")]
        [Range(0f, 1f)]
        public float minAlpha = 0.2f;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        public override IEnumerator Process(DialogueController controller)
        {
            // 主要邏輯由 DialogueController 處理
            yield break;
        }

        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
    }
}
