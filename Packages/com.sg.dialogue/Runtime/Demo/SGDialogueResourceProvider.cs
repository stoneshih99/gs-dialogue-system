using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Resource;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SG.Dialogue
{
    /// <summary>
    /// 基於 Unity Addressables + UniTask 的資源提供者實作。
    /// 掛載到場景中的 GameObject 上，並拖進 UI_FavorDisplay 的 resourceProvider 欄位即可使用。
    /// 節點中的 resourceKey 對應 Addressables 中的 Address。
    /// </summary>
    public class SGDialogueResourceProvider : DialogueResourceProviderComponent
    {
        // 使用非泛型 AsyncOperationHandle 統一儲存不同資源類型的 handle
        private readonly Dictionary<string, AsyncOperationHandle> _loadedHandles = new();

        public override async UniTask<T> LoadAsync<T>(string key)
        {
            // 如果已經載入過，直接返回快取
            if (_loadedHandles.TryGetValue(key, out var existingHandle))
            {
                if (existingHandle.Status == AsyncOperationStatus.Succeeded && existingHandle.Result is T cached)
                    return cached;
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            var result = await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 轉為非泛型 handle 存入快取
                _loadedHandles[key] = handle;
                return result;
            }

            Debug.LogWarning($"[SGDialogueResourceProvider] 載入失敗: {key}");
            Addressables.Release(handle);
            return null;
        }

        public override void Release(string key)
        {
            if (_loadedHandles.TryGetValue(key, out var handle))
            {
                Addressables.Release(handle);
                _loadedHandles.Remove(key);
            }
        }

        public override void ReleaseAll()
        {
            foreach (var handle in _loadedHandles.Values)
            {
                Addressables.Release(handle);
            }
            _loadedHandles.Clear();
        }

    }
}
