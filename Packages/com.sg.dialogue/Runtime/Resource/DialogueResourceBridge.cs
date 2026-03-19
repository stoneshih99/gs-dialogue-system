using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SG.Dialogue.Resource
{
    /// <summary>
    /// 對話資源橋接器，作為對話系統與外部資源管理方案之間的靜態存取點。
    /// 遊戲專案在初始化時應呼叫 SetProvider() 註冊自己的 IDialogueResourceProvider 實作。
    /// </summary>
    public static class DialogueResourceBridge
    {
        private static IDialogueResourceProvider _provider;

        /// <summary>
        /// 是否已註冊資源提供者。
        /// </summary>
        public static bool HasProvider => _provider != null;

        /// <summary>
        /// 註冊資源提供者。應在對話系統啟動前呼叫。
        /// </summary>
        /// <param name="provider">實作了 IDialogueResourceProvider 的實例。</param>
        public static void SetProvider(IDialogueResourceProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// 移除已註冊的資源提供者。
        /// </summary>
        public static void ClearProvider()
        {
            _provider = null;
        }

        /// <summary>
        /// 透過已註冊的 Provider 非同步載入資源。
        /// 如果沒有註冊 Provider 或 key 為空，返回 null。
        /// </summary>
        /// <typeparam name="T">資源類型。</typeparam>
        /// <param name="key">資源 key。</param>
        /// <returns>載入完成的資源，或 null。</returns>
        public static async UniTask<T> LoadAsync<T>(string key) where T : UnityEngine.Object
        {
            if (_provider == null || string.IsNullOrEmpty(key))
                return null;

            return await _provider.LoadAsync<T>(key);
        }

        /// <summary>
        /// 批次預載入多個資源 key。在對話開始前呼叫，避免對話中逐一載入造成掉幀。
        /// </summary>
        /// <param name="keys">要預載入的資源 key 列表。</param>
        public static async UniTask PreloadAsync(IEnumerable<string> keys)
        {
            if (_provider == null || keys == null) return;
            await _provider.PreloadAsync(keys);
        }

        /// <summary>
        /// 釋放指定 key 的資源。
        /// </summary>
        /// <param name="key">資源 key。</param>
        public static void Release(string key)
        {
            if (_provider == null || string.IsNullOrEmpty(key)) return;
            _provider.Release(key);
        }

        /// <summary>
        /// 釋放所有已載入的資源。
        /// </summary>
        public static void ReleaseAll()
        {
            _provider?.ReleaseAll();
        }
    }
}
