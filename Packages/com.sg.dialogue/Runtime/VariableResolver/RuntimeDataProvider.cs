using System;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Dialogue.VariableResolver
{
    /// <summary>
    /// 一個在執行時提供動態資料的範例類別。
    /// 它實現了 IVariableDataProvider 介面，並向 PlayerVariableResolver 註冊自己。
    /// 這是擴充自訂對話變數的核心位置。
    /// </summary>
    public class RuntimeDataProvider : MonoBehaviour, IVariableDataProvider
    {
        // 依賴注入，讓這個提供者可以找到解析器
        [Tooltip("場景中的變數解析器。通常與 DialogueController 在同一個物件上。")]
        [SerializeField] private PlayerVariableResolver resolver;

        // 使用 Func<string> 來延遲值的計算，確保每次獲取時都是最新的
        private readonly Dictionary<string, Func<string>> _dataMappings = new Dictionary<string, Func<string>>();

        private void Awake()
        {
            // --- 在此處設定所有您想提供的變數 ---
            // Key 是在對話中使用的名稱，Value 是一個返回目前值的函式。
            
            _dataMappings["PlayerName"] = () => PlayerProfile.PlayerName;
            _dataMappings["PlayerLevel"] = () => "15"; // 範例：直接返回一個寫死的字串
            _dataMappings["SystemTime"] = () => DateTime.Now.ToShortTimeString(); // 範例：返回目前系統時間
            
            // 如果沒有在 Inspector 中指定 resolver，嘗試在父物件或子物件中尋找
            if (resolver == null)
            {
                resolver = GetComponentInParent<PlayerVariableResolver>();
            }
            if (resolver == null)
            {
                resolver = GetComponentInChildren<PlayerVariableResolver>();
            }
        }

        private void OnEnable()
        {
            // 向解析器註冊自己
            resolver?.RegisterProvider(this);
        }

        private void OnDisable()
        {
            // 從解析器中取消註冊，防止記憶體洩漏
            resolver?.UnregisterProvider(this);
        }

        /// <summary>
        /// 實現介面的方法。
        /// </summary>
        public bool TryGetValue(string key, out string value)
        {
            // 嘗試在我們的資料字典中查找對應的鍵
            if (_dataMappings.TryGetValue(key, out Func<string> valueFunc))
            {
                // 如果找到了，執行對應的函式來獲取最新的值
                value = valueFunc();
                return true;
            }

            // 如果沒找到，設定 value 為 null 並返回 false
            value = null;
            return false;
        }
    }
}
