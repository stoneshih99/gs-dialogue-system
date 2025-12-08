using System.Collections;
using SG.Dialogue.Core.Instructions;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// 這是所有顯示文字類型節點的抽象基底類別。
    /// 它統一處理了文字準備（本地化、變數格式化）和流程控制（自動前進、等待輸入）的邏輯。
    /// 子類別只需要實作如何「顯示」文字的核心行為。
    /// </summary>
    public abstract class BaseTextNode : DialogueNodeBase
    {
        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        [Header("延遲設定")]
        [Tooltip("打字機效果完成後，進入下一步驟（等待輸入或自動前進）前的額外延遲時間。")]
        public float postTypingDelay = 0.3f;

        /// <summary>
        /// 取得此節點要顯示的主要文字內容。
        /// </summary>
        protected abstract string Text { get; }

        /// <summary>
        /// 取得此節點用於本地化的 Key。
        /// </summary>
        protected abstract string TextKey { get; }

        /// <summary>
        /// 模板方法：定義了處理文字節點的完整演算法。
        /// 流程：準備文字 -> 顯示文字（由子類別定義） -> 處理後續流程。
        /// </summary>
        public sealed override IEnumerator Process(DialogueController controller)
        {
            // 1. 準備要顯示的文字 (通用邏輯)
            string rawText = !string.IsNullOrEmpty(TextKey) ? LocalizationManager.GetText(TextKey) : Text;
            if (string.IsNullOrEmpty(rawText)) rawText = Text; // Fallback
            string formattedText = controller.FormatString(rawText);

            // 2. 顯示文字 (由子類別實現)
            yield return controller.StartCoroutine(DoShowText(controller, formattedText));

            // 3. 處理打字後延遲 (通用邏輯)
            if (postTypingDelay > 0)
            {
                yield return new WaitForSeconds(postTypingDelay);
            }

            // 4. 處理自動前進或等待輸入 (通用邏輯)
            bool advance = false;
            float delay = 0f;

            switch (controller.autoAdvanceOverride)
            {
                case AutoAdvanceMode.ForceEnable:
                    advance = true;
                    delay = controller.forcedAutoAdvanceDelay;
                    break;
                case AutoAdvanceMode.ForceDisable:
                    advance = false;
                    break;
                case AutoAdvanceMode.Default:
                    if (controller.CurrentGraph != null && controller.CurrentGraph.autoAdvanceEnabled)
                    {
                        advance = true;
                        delay = controller.AutoAdvanceDelay;
                    }
                    break;
            }

            if (advance)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                // 如果是 TextNode，等待輸入是合理的。
                // 如果是 StageTextNode，它也需要一個機制來前進。
                // 因此，將 WaitForUserInput 作為預設行為是合適的。
                yield return new WaitForUserInput();
            }
        }

        /// <summary>
        /// 子類別必須覆寫此方法，以定義如何具體地顯示文字。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <param name="formattedText">已經過本地化和變數格式化處理的最終文字。</param>
        protected abstract IEnumerator DoShowText(DialogueController controller, string formattedText);

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
