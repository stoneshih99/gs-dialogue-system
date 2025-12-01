using System.Collections;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// SetBackgroundNode 用於控制對話中的背景視覺效果。
    /// </summary>
    public class SetBackgroundNode : DialogueNodeBase
    {
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        [Tooltip("要設定的背景圖片。如果留空，則只會執行清除或黑屏操作。")]
        public Sprite backgroundSprite;

        [Tooltip("是否在設定新背景前先清除目前的背景。")]
        public bool clearBackground;

        [Header("黑屏效果")]
        [Tooltip("是否啟用黑屏過渡效果。")]
        public bool useBlackScreen;
        [Tooltip("黑屏持續的時間（秒）。")]
        public float blackScreenDuration = 1f;

        [Header("淡入淡出覆寫")]
        [Tooltip("是否覆寫預設的背景淡入淡出時間。")]
        public bool overrideBackgroundFade;
        [Tooltip("自訂的背景淡入淡出時間（秒）。")]
        public float backgroundFadeOverride = 0.3f;

        public override string GetNextNodeId()
        {
            return nextNodeId;
        }

        public override IEnumerator Process(DialogueController controller)
        {
            // 主要邏輯由 DialogueController 處理
            yield break;
        }

        public override void ClearConnectionsForClipboard()
        {
            nextNodeId = null;
        }

        public override void ClearUnityReferencesForClipboard()
        {
            ClearAllUnityObjectFields();
        }
    }
}
