using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SG.Dialogue.Resource
{
    /// <summary>
    /// 對話系統的資源載入介面。
    /// 遊戲專案應實作此介面來橋接具體的資源管理方案（如 Addressables、AssetBundle 等）。
    /// </summary>
    public interface IDialogueResourceProvider
    {
        /// <summary>
        /// 非同步載入指定 key 的資源。
        /// 實作應自行處理快取邏輯（同一 key 重複載入時應返回已快取的資源）。
        /// </summary>
        /// <typeparam name="T">資源類型（Sprite, GameObject, AudioClip 等）。</typeparam>
        /// <param name="key">資源的唯一識別字串。</param>
        /// <returns>載入完成的資源實例。</returns>
        UniTask<T> LoadAsync<T>(string key) where T : UnityEngine.Object;

        /// <summary>
        /// 批次預載入多個資源。在對話開始前呼叫，將所有需要的資源一次載入到快取中，
        /// 避免在對話進行時逐一載入造成的瞬間掉幀。
        /// 預設實作會逐一呼叫 LoadAsync，子類別可覆寫以實作更高效的批次載入。
        /// </summary>
        /// <param name="keys">要預載入的資源 key 列表。</param>
        /// <param name="onProgress">載入進度回調 (0.0 ~ 1.0)，可用於顯示 Loading 畫面。傳入 null 則不回報進度。</param>
        UniTask PreloadAsync(IEnumerable<string> keys, Action<float> onProgress = null);

        /// <summary>
        /// 釋放指定 key 的已載入資源。
        /// </summary>
        /// <param name="key">資源的唯一識別字串。</param>
        void Release(string key);

        /// <summary>
        /// 釋放所有已載入的資源。通常在對話結束時呼叫。
        /// </summary>
        void ReleaseAll();
    }
}
