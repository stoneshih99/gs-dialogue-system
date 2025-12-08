using System;
using System.Collections;
using SG.Dialogue.Presentation;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// FlashEffectNode 是一個畫面閃爍特效節點，用於讓整個畫面快速閃爍指定的顏色（例如閃白、閃紅）。
    /// 這常用於表現衝擊、爆炸或場景切換等效果。
    /// </summary>
    [Serializable]
    public class FlashEffectNode : DialogueNodeBase
    {
        [Header("閃爍參數")]
        [Tooltip("閃爍的顏色。")]
        public Color FlashColor = Color.white;

        [Tooltip("整個閃爍效果的總持續時間（秒）。效果會從開始到中間點達到最高亮度，再從中間點回到透明。")]
        public float Duration = 0.3f;

        [Tooltip("閃爍的最高亮度（Alpha 值，範圍 0 到 1）。")]
        [Range(0f, 1f)]
        public float Intensity = 1f;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理畫面閃爍特效節點的核心邏輯。
        /// 它會尋找場景中的 ScreenEffectController 並呼叫對應的方法來執行閃爍效果。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個協程迭代器，用於等待特效完成。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            var effectController = UnityEngine.Object.FindObjectOfType<ScreenEffectController>();
            if (effectController == null)
            {
                Debug.LogWarning("場景中找不到 ScreenEffectController，無法執行畫面閃爍特效。");
                yield break;
            }

            // 呼叫 ScreenEffectController 來執行閃爍效果，並等待其完成
            yield return effectController.ExecuteFlash(Duration, FlashColor, Intensity);
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }

        public override void ClearConnectionsForClipboard()
        {
            nextNodeId = null;
        }
    }
}
