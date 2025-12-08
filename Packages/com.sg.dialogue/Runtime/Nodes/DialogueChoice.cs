using System;
using System.Collections.Generic;
using SG.Dialogue.Conditions;
using SG.Dialogue.Variables;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// DialogueChoice 定義了一個對話選項的所有數據。
    /// 它包含了選項的顯示文本、選擇後要跳轉到的下一個節點、
    /// 顯示此選項所需的條件，以及選擇此選項後將發生的變數變更和觸發的事件。
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("此選項在 UI 上顯示的文本內容。")]
        [TextArea] public string text;
        
        [Tooltip("選擇此選項後，對話流程將前往的下一個節點的 ID。")]
        public string nextNodeId;
        
        [Tooltip("顯示此選項所需滿足的條件。如果條件不滿足，這個選項將不會顯示給玩家。如果沒有設定條件，則永遠顯示。")]
        public Condition condition = new Condition();
        
        [Tooltip("當玩家選擇此選項後，將會應用的變數變更列表。")]
        public List<VariableChange> variableChanges = new List<VariableChange>();
        
        [Tooltip("當玩家選擇此選項時觸發的 UnityEvent。這個事件會在變數變更之後、跳轉到下一個節點之前被觸發。")]
        public UnityEvent onSelected;
    }
}
