using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue.Variables
{
    /// <summary>
    /// VariableChange 定義了一個對話變數的變更操作。
    /// 這個類別被設計為可以在 Unity 的 Inspector 中方便地進行設定，
    /// 用於 TextNode、ChoiceNode 等節點中，以在對話過程中改變遊戲狀態。
    /// </summary>
    [Serializable]
    public class VariableChange
    {
        /// <summary>
        /// 定義變數的類型。
        /// </summary>
        public enum VarType { Int, Bool }

        [Tooltip("要變更的變數類型（整數或布林）。")]
        public VarType type;

        [Tooltip("要變更的變數名稱。這個名稱應該與 DialogueStateAsset 或 DialogueState 中的變數名稱相符。")]
        public string variableName;

        [Header("整數 (Int) 設定")]
        [Tooltip("如果變數類型是 Int，這是要增加（正數）或減少（負數）的值。")]
        public int intDelta;

        [Header("布林 (Bool) 設定")]
        [Tooltip("如果變數類型是 Bool 且『直接設定布林值』為 true，這將是變數被設定成的目標值。")]
        public bool boolValue;

        [Tooltip("如果變數類型是 Bool，此選項決定如何修改變數：\n" +
                 " - true: 直接將變數設定為上方的 boolValue。\n" +
                 " - false: 將變數的目前值取反（true 變 false，false 變 true）。")]
        public bool setBool;
    }
}
