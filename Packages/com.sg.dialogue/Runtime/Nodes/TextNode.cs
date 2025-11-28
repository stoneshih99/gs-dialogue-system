using System;
using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Animation;
using SG.Dialogue.Core.Instructions;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using SG.Dialogue.Variables;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// TextNode 是對話系統中最核心的節點之一，用於顯示一段對話文本。
    /// 它包含了說話者、文本內容，並支援流程控制、打斷、本地化、文本提示、動畫、音效、變數變更和事件等多種功能。
    /// </summary>
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
        
        [Header("動畫")]
        [Tooltip("此節點進入時要觸發的 LitMotion 動畫列表。")]
        public List<MotionData> motions = new List<MotionData>();
        [Tooltip("動畫要作用在哪個位置的角色上（例如：左、中、右）。")]
        public CharacterPosition targetAnimationPosition = CharacterPosition.Center;

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

        /// <summary>
        /// 處理文字節點的核心邏輯。
        /// 它會格式化文本、更新視覺與 UI 管理器，最後返回一個等待使用者輸入的指令。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個包含對話指令的協程迭代器。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            // 格式化說話者名稱，替換其中的變數
            string formattedSpeaker = controller.FormatString(speakerName);

            // 根據 textKey 從本地化管理器獲取文本，如果 key 為空或找不到，則使用節點上預設的 text
            string rawText = !string.IsNullOrEmpty(textKey) ? LocalizationManager.GetText(textKey) : text;
            if (string.IsNullOrEmpty(rawText)) rawText = text;
            // 格式化最終的文本內容，替換其中的變數
            string formattedText = controller.FormatString(rawText);
            
            // 創建一個臨時的節點副本，包含格式化後的資料，以傳遞給 UI 和視覺管理器
            // 這樣做可以避免修改原始的 ScriptableObject 資料
            var displayNode = (TextNode)this.MemberwiseClone();
            displayNode.speakerName = formattedSpeaker;
            
            // 更新各個管理器
            controller.VisualManager.UpdateFromTextNode(displayNode); 
            controller.UiManager.ShowText(displayNode, formattedText);
            
            // 返回指令，告訴控制器在此暫停，等待使用者輸入（例如點擊按鈕）
            yield return new WaitForUserInput();
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此文字節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
    }
}
