using System;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// 定義遊戲事件參數的類型。
    /// </summary>
    public enum GameEventParameterType
    {
        String,
        Int,
        Float,
        Bool
    }

    /// <summary>
    /// 代表一個遊戲事件的參數。
    /// </summary>
    [Serializable]
    public class GameEventParameter
    {
        [Tooltip("參數名稱")]
        public string name;

        [Tooltip("參數類型")]
        public GameEventParameterType type;

        [Tooltip("字串值")]
        public string stringValue;

        [Tooltip("整數值")]
        public int intValue;

        [Tooltip("浮點數值")]
        public float floatValue;

        [Tooltip("布林值")]
        public bool boolValue;

        public object GetValue()
        {
            switch (type)
            {
                case GameEventParameterType.String: return stringValue;
                case GameEventParameterType.Int: return intValue;
                case GameEventParameterType.Float: return floatValue;
                case GameEventParameterType.Bool: return boolValue;
                default: return null;
            }
        }
    }

    /// <summary>
    /// GameRequest 是一個類別，用於封裝一次遊戲事件請求所需的所有資料。
    /// </summary>
    [Serializable]
    public class GameRequest : IEventRequest
    {
        /// <summary>
        /// 要觸發的事件的名稱。
        /// </summary>
        [Tooltip("事件名稱")]
        public string EventName;

        /// <summary>
        /// 事件參數列表。
        /// </summary>
        [Tooltip("事件參數列表")]
        public List<GameEventParameter> Parameters = new List<GameEventParameter>();

        // 顯式實作介面屬性
        string IEventRequest.EventName => EventName;
        
        /// <summary>
        /// 獲取指定名稱的參數值。
        /// </summary>
        public T GetParameter<T>(string paramName)
        {
            var param = Parameters.Find(p => p.name == paramName);
            if (param == null) return default;

            object val = param.GetValue();
            if (val is T tVal) return tVal;
            
            try { return (T)Convert.ChangeType(val, typeof(T)); }
            catch { return default; }
        }
    }
}
