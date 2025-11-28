using System;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Dialogue.Conditions
{
    /// <summary>
    /// Condition 類別定義了一個組合條件，它包含多個整數條件和布林條件。
    /// 只有當所有子條件都同時滿足時（AND 邏輯），整個組合條件才返回 true。
    /// </summary>
    [Serializable]
    public class Condition
    {
        [Tooltip("整數變數的條件列表。")]
        public List<IntCondition> intConditions = new List<IntCondition>();
        [Tooltip("布林變數的條件列表。")]
        public List<BoolCondition> boolConditions = new List<BoolCondition>();

        /// <summary>
        /// 檢查此組合條件是否滿足。
        /// </summary>
        /// <param name="controller">對話控制器，用於獲取當前對話狀態中的變數值。</param>
        /// <returns>如果所有子條件都滿足則返回 true，否則返回 false。</returns>
        public bool Check(DialogueController controller)
        {
            if (controller == null) return false;
            
            var localState = controller.LocalState; // 獲取本地對話狀態
            var globalState = controller.GlobalState; // 獲取全域對話狀態

            // 檢查所有整數條件
            foreach (var ic in intConditions)
            {
                int current;
                // 優先從本地變數取值，如果沒有，再從全域變數取值
                if (localState.HasInt(ic.variableName))
                {
                    current = localState.GetInt(ic.variableName);
                }
                else if (globalState != null && globalState.HasInt(ic.variableName))
                {
                    current = globalState.GetInt(ic.variableName);
                }
                else
                {
                    // 如果本地和全域狀態中都找不到該變數，則視為 0
                    current = 0;
                }

                // 根據指定的比較方式進行判斷，只要有一個不滿足，就立刻返回 false
                switch (ic.comparison)
                {
                    case Comparison.Equal:          if (current != ic.value) return false; break;
                    case Comparison.NotEqual:       if (current == ic.value) return false; break;
                    case Comparison.Greater:        if (current <= ic.value) return false; break;
                    case Comparison.Less:           if (current >= ic.value) return false; break;
                    case Comparison.GreaterOrEqual: if (current < ic.value)  return false; break;
                    case Comparison.LessOrEqual:    if (current > ic.value)  return false; break;
                }
            }

            // 檢查所有布林條件
            foreach (var bc in boolConditions)
            {
                bool current;
                // 優先從本地變數取值，如果沒有，再從全域變數取值
                if (localState.HasBool(bc.variableName))
                {
                    current = localState.GetBool(bc.variableName);
                }
                else if (globalState != null && globalState.HasBool(bc.variableName))
                {
                    current = globalState.GetBool(bc.variableName);
                }
                else
                {
                    // 如果本地和全域狀態中都找不到該變數，則視為 false
                    current = false;
                }

                // 只要有一個變數的當前值與要求的布林值不符，就立刻返回 false
                if (current != bc.requiredValue) return false;
            }
            
            // 如果所有條件都檢查通過，才返回 true
            return true;
        }
    }
}
