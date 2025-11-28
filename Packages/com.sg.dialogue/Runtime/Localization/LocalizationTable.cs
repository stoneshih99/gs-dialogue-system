using System;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Dialogue
{
    /// <summary>
    /// LocalizationTable 是一個 ScriptableObject，用於儲存多語言文本和說話者名稱的對應關係。
    /// 它提供了一個基於 Key 的查找機制，以獲取不同語言的本地化內容。
    /// </summary>
    [CreateAssetMenu(menuName = "SG/Dialogue/Localization Table", fileName = "DialogueLocalizationTable")]
    public class LocalizationTable : ScriptableObject
    {
        /// <summary>
        /// Entry 定義了本地化表格中的一個條目，包含不同語言的文本和說話者名稱。
        /// </summary>
        [Serializable]
        public class Entry
        {
            [Tooltip("本地化條目的唯一 Key。")]
            public string key;
            [Tooltip("繁體中文文本內容。")]
            [TextArea] public string zhTW;
            [Tooltip("日文文本內容。")]
            [TextArea] public string jaJP;
            [Tooltip("英文文本內容。")]
            [TextArea] public string enUS;

            [Header("Speaker Localization")]
            [Tooltip("繁體中文說話者名稱。")]
            public string speakerZhTW;
            [Tooltip("日文說話者名稱。")]
            public string speakerJaJP;
            [Tooltip("英文說話者名稱。")]
            public string speakerEnUS;
        }

        [Tooltip("本地化條目的列表。")]
        [SerializeField]
        public List<Entry> entries = new List<Entry>();

        private Dictionary<string, Entry> _lookup; // Key 到 Entry 的查找字典

        /// <summary>
        /// 建立 Key 到 Entry 的查找字典，以提高查詢效率。
        /// </summary>
        public void BuildLookup()
        {
            if (_lookup != null) return; // 如果已建立，則直接返回
            _lookup = new Dictionary<string, Entry>();
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.key))
                {
                    _lookup[e.key] = e;
                }
            }
        }

        /// <summary>
        /// 根據 Key 和語言代碼獲取本地化文本內容。
        /// </summary>
        /// <param name="key">本地化條目的 Key。</param>
        /// <param name="languageCode">語言代碼（例如 "zh-TW", "ja-JP", "en-US"）。</param>
        /// <returns>對應語言的文本內容，如果找不到則為 null。</returns>
        public string Get(string key, string languageCode)
        {
            var entry = GetEntry(key);
            if (entry == null) return null;

            switch (languageCode)
            {
                case "ja-JP": return string.IsNullOrEmpty(entry.jaJP) ? entry.zhTW : entry.jaJP; // 如果日文為空，則回退到繁體中文
                case "en-US": return string.IsNullOrEmpty(entry.enUS) ? entry.zhTW : entry.enUS; // 如果英文為空，則回退到繁體中文
                case "zh-TW":
                default:
                    return entry.zhTW; // 預設返回繁體中文
            }
        }

        /// <summary>
        /// 根據 Key 獲取本地化條目。
        /// </summary>
        /// <param name="key">本地化條目的 Key。</param>
        /// <returns>對應的 Entry 實例，如果找不到則為 null。</returns>
        public Entry GetEntry(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_lookup == null) BuildLookup(); // 如果查找表未建立，則先建立
            _lookup.TryGetValue(key, out var e);
            return e;
        }
    }
}
