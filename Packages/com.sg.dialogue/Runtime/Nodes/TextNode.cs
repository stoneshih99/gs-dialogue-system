using System;
using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Core.Instructions;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using SG.Dialogue.Variables;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue.Nodes
{
    [Serializable]
    public class TextNode : DialogueNodeBase
    {
        [Header("事件通道")]
        [Tooltip("用於發出音訊請求的事件通道。")]
        public AudioEvent AudioEvent;

        [Tooltip("說話者的名稱。支援使用 {variableName} 的格式來插入變數。")]
        public string speakerName;
        [Tooltip("對話的文本內容。同樣支援 {variableName} 格式的變數。")]
        [TextArea(2, 5)] public string text;
        
        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。如果留空，對話將在此節點後結束。")]
        public string nextNodeId;

        [Header("打斷設定")]
        [Tooltip("如果勾選，這句對話可以在顯示期間被一個遊戲事件打斷。")]
        public bool IsInterruptible;
        [Tooltip("可以打斷此對話的 GameEvent 資產。當這個事件被觸發時，對話將立即跳轉到下方的『打斷後節點』。")]
        public GameEvent InterruptEvent;
        [Tooltip("當此對話被 InterruptEvent 打斷後，要跳轉到的目標節點 ID。")]
        public string InterruptNextNodeId;

        [Header("本地化")]
        [Tooltip("此句對話的本地化 Key。用於從本地化表格中查找對應的多國語言文本。如果留空，則直接使用上方的 text 欄位作為原文。")]
        public string textKey;

        [Header("文本提示 (Text Cues)")]
        [Tooltip("在台詞打字機效果過程中觸發的事件標記。當文本顯示到指定的字元索引時，會觸發對應的 UnityEvent。")]
        public List<TextCue> textCues = new List<TextCue>();
        
        [Header("變數與事件")]
        [Tooltip("當進入此節點時，要變更的變數列表。")]
        public List<VariableChange> variableChanges = new List<VariableChange>();
        [Tooltip("當進入此節點時觸發的 UnityEvent。")]
        public UnityEvent onEnter;
        [Tooltip("當退出此節點時觸發的 UnityEvent。")]
        public UnityEvent onExit;

        [Header("自動前進覆寫")]
        [Tooltip("是否覆寫全域的自動前進設定。如果啟用，將使用下方定義的延遲時間。")]
        public bool overrideAutoAdvance;
        [Tooltip("此節點的自動前進延遲時間（秒）。僅在『覆寫自動前進』為 true 時生效。")]
        public float autoAdvanceDelay = 1.2f;

        public override IEnumerator Process(DialogueController controller)
        {
            string formattedSpeaker = controller.FormatString(speakerName);

            string rawText = !string.IsNullOrEmpty(textKey) ? LocalizationManager.GetText(textKey) : text;
            if (string.IsNullOrEmpty(rawText)) rawText = text;
            string formattedText = controller.FormatString(rawText);
            
            var displayNode = (TextNode)this.MemberwiseClone();
            displayNode.speakerName = formattedSpeaker;
            
            controller.VisualManager.UpdateFromTextNode(displayNode); 
            
            // 等待打字機效果完成
            yield return controller.UiManager.ShowText(displayNode, formattedText);
            
            // 根據自動前進設定決定等待方式
            if (controller.CurrentGraph != null && controller.CurrentGraph.autoAdvanceEnabled)
            {
                float delay = overrideAutoAdvance ? autoAdvanceDelay : controller.AutoAdvanceDelay;
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return new WaitForUserInput();
            }
        }

        public override string GetNextNodeId()
        {
            return nextNodeId;
        }

        public override void ClearConnectionsForClipboard()
        {
            nextNodeId = null;
            InterruptNextNodeId = null;
        }

        public override void ClearUnityReferencesForClipboard()
        {
            AudioEvent = null;
            InterruptEvent = null;
            // UnityEvent 內部可能包含對 Unity 物件的引用，但通常不需要在剪貼簿操作時清除其內部細節
            // 如果需要，可以考慮遍歷 GetPersistentEventCount() 並清除
        }
    }
}
