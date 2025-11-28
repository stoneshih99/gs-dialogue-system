using System;
using UnityEngine;

namespace SG.Dialogue.Conditions
{
    /// <summary>
    /// IntCondition 定義了一個針對整數 (integer) 變數的單一條件。
    /// </summary>
    [Serializable]
    public class IntCondition
    {
        [Tooltip("要比較的整數變數的名稱。")]
        public string variableName;
        
        [Tooltip("變數的當前值與目標值之間的比較方式（例如：等於、大於、小於等）。")]
        public Comparison comparison;
        
        [Tooltip("要與變數值進行比較的目標值。")]
        public int value;
    }
}
