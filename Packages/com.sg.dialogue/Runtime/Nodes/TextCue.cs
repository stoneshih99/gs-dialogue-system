using System;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// TextCue (文本提示) 定義了一個在對話文本顯示過程中，於指定字元索引觸發的事件。
    /// 這可以用於在特定的詞或句子出現時，同步播放音效、觸發動畫、或執行其他遊戲邏輯。
    /// </summary>
    [Serializable]
    public class TextCue
    {
        [Tooltip("在第幾個字元索引時觸發事件（從 0 開始計算，對應 TextNode.text 的索引）。")]
        public int charIndex;
        
        [Tooltip("當打字機效果顯示到此字元索引時，要觸發的 UnityEvent。")]
        public UnityEvent onTrigger;
        
        [Tooltip("如果同一節點被多次顯示（例如在一個迴圈中），此提示是否只觸發一次。如果為 true，則只在第一次顯示時觸發。")]
        public bool triggerOnce = true;
    }
}
