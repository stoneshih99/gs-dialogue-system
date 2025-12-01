using System;
using System.Collections;
using SG.Dialogue.Presentation;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// ScreenEffectNode 是一個螢幕特效節點的基底類別，用於控制全螢幕效果。
    /// 這個基底類別目前主要用於控制灰階效果，但可以被擴充以支援更多特效。
    /// </summary>
    [Serializable]
    public class ScreenEffectNode : DialogueNodeBase
    {
        /// <summary>
        /// 定義特效要執行的動作類型。
        /// </summary>
        public enum ActionType { Enable, Disable }

        [Tooltip("要執行的動作類型：啟用或禁用。")]
        public ActionType Action = ActionType.Enable;

        [Tooltip("效果過渡的持續時間（秒）。")]
        public float Duration = 1f;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理螢幕特效節點的核心邏輯。
        /// 它會尋找場景中的 ScreenEffectController 並呼叫對應的方法來啟用或禁用特效。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個協程迭代器，用於等待特效動畫完成。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            var effectController = UnityEngine.Object.FindObjectOfType<ScreenEffectController>();
            if (effectController == null)
            {
                Debug.LogWarning("場景中找不到 ScreenEffectController，無法執行螢幕特效。");
                yield break;
            }

            // 根據動作類型，呼叫 ScreenEffectController 的方法
            // 注意：目前的實作是硬編碼為灰階 (Grayscale)。
            // 若要擴充，可以考慮在此節點新增一個 EffectType 枚舉，並在 Process 中根據類型呼叫不同的方法。
            if (Action == ActionType.Enable)
            {
                yield return effectController.EnableGrayscale(Duration);
            }
            else
            {
                yield return effectController.DisableGrayscale(Duration);
            }
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此螢幕特效節點的下一個節點 ID。
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
