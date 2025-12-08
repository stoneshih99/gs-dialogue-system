using System;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Dialogue.VariableResolver
{
    public class DialoguePlayerDataResolver : MonoBehaviour, IVariableDataProvider
    {
        
        // 依賴注入，讓這個提供者可以找到解析器
        [Tooltip("場景中的變數解析器。通常與 DialogueController 在同一個物件上。")]
        [SerializeField] private PlayerVariableResolver resolver;

        // 使用 Func<string> 來延遲值的計算，確保每次獲取時都是最新的
        private readonly Dictionary<string, Func<string>> _dataMappings = new Dictionary<string, Func<string>>();

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
        /// 新增一個資料映射。 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddDataMapping(string key, string value)
        {
            _dataMappings[key] = () => value;
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
                Debug.LogFormat("找到變數 Key='{0}'，Value='{1}'", key, value);
                return true;
            }

            // 如果沒找到，設定 value 為 null 並返回 false
            value = null;
            return false;
        }
    }
}