using System;
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
        /// <param name="onProgress">載入進度回調 (0.0 ~ 1.0)，可用於顯示 Loading 畫面。</param>
        public static async UniTask PreloadAsync(IEnumerable<string> keys, Action<float> onProgress = null)
        {
            if (_provider == null || keys == null) return;
            await _provider.PreloadAsync(keys, onProgress);
        }

        /// <summary>
        /// 解析資源：優先透過 Bridge 以 key 載入，否則 fallback 到直接引用。
        /// 統一供各 Manager 使用，避免重複程式碼。
        /// </summary>
        /// <typeparam name="T">資源類型。</typeparam>
        /// <param name="resourceKey">資源 key（可為 null 或空）。</param>
        /// <param name="directReference">節點上的直接引用（fallback 用）。</param>
        /// <param name="loadedKeys">已載入 key 的追蹤列表，成功載入時會加入。</param>
        /// <returns>解析後的資源。</returns>
        public static async UniTask<T> ResolveAsset<T>(string resourceKey, T directReference, List<string> loadedKeys = null) where T : UnityEngine.Object
        {
            if (!string.IsNullOrEmpty(resourceKey) && HasProvider)
            {
                var asset = await LoadAsync<T>(resourceKey);
                if (asset != null)
                {
                    loadedKeys?.Add(resourceKey);
                    return asset;
                }
                Debug.LogWarning($"[DialogueResourceBridge] 無法載入資源 '{resourceKey}'，使用直接引用。");
            }
            return directReference;
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
