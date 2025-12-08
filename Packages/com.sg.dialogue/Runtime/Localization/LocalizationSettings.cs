using UnityEngine;

namespace SG.Dialogue
{
    /// <summary>
    /// LocalizationSettings 是一個 MonoBehaviour，用於在場景中設定當前語言和 LocalizationTable。
    /// 它會在 Awake 時將設定應用到靜態的 LocalizationManager，並提供運行時切換語言的方法。
    /// </summary>
    public class LocalizationSettings : MonoBehaviour
    {
        [Header("Localization Table")]
        [Tooltip("要設定給 LocalizationManager 使用的 LocalizationTable 資產。")]
        public LocalizationTable table;

        [Header("Language")]
        [Tooltip("當前遊戲使用的語言。對應繁體中文 (ZhTw)、日文 (JaJp) 或英文 (EnUs)。")]
        public LocalizationLanguage language = LocalizationLanguage.ZhTw;

        private void Awake()
        {
            ApplySettings(); // 在遊戲物件啟用時應用設定
        }

        /// <summary>
        /// 將 Inspector 中設定的 LocalizationTable 和語言應用到靜態的 LocalizationManager。
        /// </summary>
        public void ApplySettings()
        {
            if (table != null)
            {
                LocalizationManager.Table = table; // 設定 LocalizationManager 的表格
            }

            LocalizationManager.CurrentLanguageEnum = language; // 設定 LocalizationManager 的當前語言
        }

        /// <summary>
        /// 根據提供的語言代碼切換當前語言，並應用設定。
        /// 此方法可以綁定到 UI 按鈕等，實現運行時語言切換。
        /// </summary>
        /// <param name="languageCode">語言的字符串代碼，例如 "ja-JP", "en-US", "zh-TW"。</param>
        public void SetLanguage(string languageCode)
        {
            switch (languageCode)
            {
                case "ja-JP":
                    language = LocalizationLanguage.JaJp;
                    break;
                case "en-US":
                    language = LocalizationLanguage.EnUs;
                    break;
                case "zh-TW":
                default:
                    language = LocalizationLanguage.ZhTw;
                    break;
            }

            ApplySettings(); // 應用新的語言設定
        }
    }
}
