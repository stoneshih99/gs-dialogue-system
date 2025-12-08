using System;
using UnityEngine;

namespace SG.Dialogue.Conditions
{
    /// <summary>
    /// BoolCondition 定義了一個針對布林變數的單一條件。
    /// </summary>
    [Serializable]
    public class BoolCondition
    {
        [Tooltip("要比較的布林變數的名稱。")]
        public string variableName;
        [Tooltip("布林變數必須滿足的值（true 或 false）。")]
        public bool requiredValue;
    }
}
