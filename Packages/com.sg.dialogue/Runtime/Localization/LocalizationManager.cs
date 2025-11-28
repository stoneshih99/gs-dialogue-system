using UnityEngine;

namespace SG.Dialogue
{
    /// <summary>
    /// LocalizationManager 是一個靜態的多國語系存取點。
    /// 它負責管理當前選定的語言和所使用的 LocalizationTable，並提供根據 Key 獲取對應文本的方法。
    /// 為了兼容性，它同時提供了字符串和枚舉類型的語言設定介面。
    /// </summary>
    public static class LocalizationManager
    {
        /// <summary>
        /// 當前語言的字符串代碼，例如 "zh-TW", "ja-JP", "en-US"。
        /// （保留此屬性以兼容舊程式碼或需要字符串表示的場景）
        /// </summary>
        public static string CurrentLanguage = "zh-TW";

        private static LocalizationLanguage _currentLanguageEnum = LocalizationLanguage.ZhTw;
        /// <summary>
        /// 以枚舉類型表示的當前語言。推薦在新程式碼中使用此屬性。
        /// 設定此屬性會自動更新 CurrentLanguage 字符串代碼。
        /// </summary>
        public static LocalizationLanguage CurrentLanguageEnum
        {
            get => _currentLanguageEnum;
            set
            {
                _currentLanguageEnum = value;
                // 將枚舉值轉換為字符串代碼，供 LocalizationTable.Get 方法使用
                switch (_currentLanguageEnum)
                {
                    case LocalizationLanguage.JaJp:
                        CurrentLanguage = "ja-JP";
                        break;
                    case LocalizationLanguage.EnUs:
                        CurrentLanguage = "en-US";
                        break;
                    case LocalizationLanguage.ZhTw:
                    default:
                        CurrentLanguage = "zh-TW";
                        break;
                }
            }
        }

        /// <summary>
        /// 指向目前使用的 LocalizationTable 資產。
        /// 可以在遊戲啟動場景中通過 LocalizationSettings 組件進行設定。
        /// </summary>
        public static LocalizationTable Table;

        /// <summary>
        /// 根據 Key 獲取當前語言的本地化文本。
        /// </summary>
        /// <param name="key">本地化條目的 Key。</param>
        /// <returns>對應的本地化文本，如果 Table 為空或找不到 Key 則返回 null。</returns>
        public static string GetText(string key)
        {
            if (Table == null)
            {
                Debug.LogWarning($"LocalizationManager: Table is null, key={key}");
                return null;
            }
            return Table.Get(key, CurrentLanguage);
        }
        
        /// <summary>
        /// 根據 Key 獲取當前語言的本地化文本，並提供一個備用值。
        /// </summary>
        /// <param name="key">本地化條目的 Key。</param>
        /// <param name="fallback">如果找不到本地化文本時返回的備用字符串。</param>
        /// <returns>對應的本地化文本，如果找不到則返回 fallback。</returns>
        public static string GetText(string key, string fallback = "")
        {
            // 這是原始程式碼中的一個模擬實現。
            // 實際應用中，應呼叫 GetText(string key) 並處理其返回的 null 或空字符串。
            if (!string.IsNullOrEmpty(key))
            {
                string localizedText = GetText(key);
                if (localizedText != null) return localizedText;
            }
            return fallback;
        }
    }

    /// <summary>
    /// 定義對話系統支援的語言類型。
    /// </summary>
    public enum LocalizationLanguage
    {
        /// <summary>
        /// 繁體中文。
        /// </summary>
        ZhTw,
        /// <summary>
        /// 日文。
        /// </summary>
        JaJp,
        /// <summary>
        /// 英文。
        /// </summary>
        EnUs
    }
}
