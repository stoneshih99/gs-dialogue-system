using System;
using System.Collections;
using SG.Dialogue.Presentation;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// BlurEffectNode 是一個背景模糊特效節點，用於啟用或禁用畫面的模糊效果。
    /// 這常用於凸顯前景 UI 或營造回憶、夢境等氛圍。
    /// </summary>
    [Serializable]
    public class BlurEffectNode : DialogueNodeBase
    {
        /// <summary>
        /// 定義特效要執行的動作類型。
        /// </summary>
        public enum ActionType { Enable, Disable }

        [Header("模糊參數")]
        [Tooltip("要執行的動作類型：啟用或禁用。")]
        public ActionType Action = ActionType.Enable;

        [Tooltip("效果過渡的持續時間（秒）。")]
        public float Duration = 1f;

        [Tooltip("模糊的程度（範圍 0 到 0.01）。僅在『啟用』時有效。")]
        [Range(0f, 0.01f)]
        public float BlurAmount = 0.005f;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理背景模糊特效節點的核心邏輯。
        /// 它會尋找場景中的 ScreenEffectController 並呼叫對應的方法來啟用或禁用模糊效果。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個協程迭代器，用於等待特效動畫完成。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            var effectController = UnityEngine.Object.FindObjectOfType<ScreenEffectController>();
            if (effectController == null)
            {
                Debug.LogWarning("場景中找不到 ScreenEffectController，無法執行背景模糊特效。");
                yield break;
            }

            if (Action == ActionType.Enable)
            {
                yield return effectController.EnableBlur(Duration, BlurAmount);
            }
            else
            {
                yield return effectController.DisableBlur(Duration);
            }
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
