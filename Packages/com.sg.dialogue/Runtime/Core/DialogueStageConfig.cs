using System;
using System.Collections.Generic;
using SG.Dialogue.Conditions;
using UnityEngine;

namespace SG.Dialogue
{
    /// <summary>
    /// DialogueStageConfig 是一個 ScriptableObject，用於根據當前的遊戲狀態（透過條件檢查）來動態選擇要執行的 DialogueGraph。
    /// 這對於實現多階段的對話、根據玩家進度或選擇顯示不同對話內容的場景非常有用。
    /// 例如，一個 NPC 在任務完成前後可能會說不同的話。
    /// </summary>
    [CreateAssetMenu(menuName = "SG/Dialogue/Stage Config")]
    public class DialogueStageConfig : ScriptableObject
    {
        /// <summary>
        /// StageEntry 定義了一個「階段」，包含一個觸發條件和對應的對話圖。
        /// </summary>
        [Serializable]
        public class StageEntry
        {
            [Tooltip("當此條件滿足時，將使用下方的對話圖。")]
            public Condition condition = new Condition();
            [Tooltip("與此階段條件相關聯的對話圖。")]
            public DialogueGraph graph;
        }
        
        [Tooltip("依序檢查的階段列表。系統會從上到下檢查，並使用第一個符合條件的階段。")]
        public List<StageEntry> stages = new List<StageEntry>();
        
        [Tooltip("如果上方所有階段的條件都不符合，則會使用這個備用的對話圖。")]
        public DialogueGraph fallbackGraph;

        /// <summary>
        /// 根據當前 DialogueController 的狀態（本地和全域變數），取得第一個符合條件的 DialogueGraph。
        /// 如果沒有任何階段的條件符合，則返回備用的 fallbackGraph。
        /// </summary>
        /// <param name="controller">當前的 DialogueController 實例，用於檢查條件中使用的變數。</param>
        /// <returns>符合條件的 DialogueGraph；如果都無，則返回 fallbackGraph。</returns>
        public DialogueGraph GetGraph(DialogueController controller)
        {
            foreach (var s in stages)
            {
                // 如果階段條目中沒有設定對話圖，則跳過此階段
                if (s.graph == null) continue;
                
                // 如果條件為空（視為永遠滿足）或條件檢查通過，則返回此階段的對話圖
                if (s.condition == null || s.condition.Check(controller))
                {
                    return s.graph; 
                }
            }
            // 如果所有階段的條件都不符合，則返回備用對話圖
            return fallbackGraph;
        }
    }
}
