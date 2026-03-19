using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SG.Dialogue.Resource
{
    /// <summary>
    /// IDialogueResourceProvider 的 MonoBehaviour 包裝基底類別。
    /// 繼承此類別並實作資源載入邏輯，即可在 Inspector 中拖曳指定給需要資源提供者的組件。
    ///
    /// 使用範例：
    /// 1. 建立一個繼承此類別的子類別（例如 AddressableResourceProvider）。
    /// 2. 覆寫 LoadAsync、Release、ReleaseAll 方法。
    /// 3. 將該組件掛載到場景中的 GameObject 上。
    /// 4. 在遊戲初始化時呼叫 DialogueResourceBridge.SetProvider(myProvider)。
    /// </summary>
    public abstract class DialogueResourceProviderComponent : MonoBehaviour, IDialogueResourceProvider
    {
        public abstract UniTask<T> LoadAsync<T>(string key) where T : UnityEngine.Object;
        public abstract void Release(string key);
        public abstract void ReleaseAll();

        /// <summary>
        /// 批次預載入。預設並行載入所有資源並回報進度。子類別可覆寫以使用更高效的批次 API。
        /// </summary>
        public virtual async UniTask PreloadAsync(IEnumerable<string> keys, Action<float> onProgress = null)
        {
            var keyList = keys.ToList();
            if (keyList.Count == 0)
            {
                onProgress?.Invoke(1f);
                return;
            }

            int completed = 0;
            int total = keyList.Count;

            var tasks = new List<UniTask>();
            foreach (var key in keyList)
            {
                tasks.Add(LoadAndReport(key));
            }
            await UniTask.WhenAll(tasks);
            onProgress?.Invoke(1f);
            return;

            async UniTask LoadAndReport(string k)
            {
                await LoadAsync<UnityEngine.Object>(k);
                completed++;
                onProgress?.Invoke((float)completed / total);
            }
        }
    }
}
