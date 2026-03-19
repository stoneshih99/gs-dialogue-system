using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Nodes;
using SG.Dialogue.Resource;
using SG.Dialogue.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 專門管理背景圖片的切換和轉場。
    /// </summary>
    public class BackgroundManager : MonoBehaviour
    {
        [Header("背景")]
        [SerializeField] private List<Image> backgroundImages;

        private readonly List<CancellationTokenSource> _backgroundFadeCts = new List<CancellationTokenSource>();
        private readonly List<string> _loadedResourceKeys = new List<string>();

        private void Awake()
        {
            for (int i = 0; i < backgroundImages.Count; i++)
            {
                _backgroundFadeCts.Add(null);
            }
        }

        private void OnDestroy()
        {
            foreach (var cts in _backgroundFadeCts)
            {
                cts?.Cancel();
                cts?.Dispose();
            }
            _backgroundFadeCts.Clear();
        }

        public async UniTask ProcessSetBackground(SetBackgroundNode node)
        {
            float bgFadeTime = node.backgroundFadeOverride;
            var layerIndex = node.spriteIndex;

            if (node.useBlackScreen && backgroundImages.Count > layerIndex && backgroundImages[layerIndex] != null)
            {
                CancelFade(layerIndex);
                var cts = new CancellationTokenSource();
                _backgroundFadeCts[layerIndex] = cts;

                await UIAnimationUtils.FadeImage(backgroundImages[layerIndex], 0f, bgFadeTime, cts.Token);

                if (node.blackScreenDuration > 0)
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(node.blackScreenDuration), ignoreTimeScale: false, cancellationToken: this.GetCancellationTokenOnDestroy());
                }
            }

            // 透過 Bridge 延遲載入背景 Sprite，或 fallback 到直接引用
            var sprite = await ResolveAsset<Sprite>(node.backgroundSpriteKey, node.backgroundSprite);
            await UpdateBackground(layerIndex, sprite, node.clearBackground, bgFadeTime);
        }

        public Image GetBackgroundImage(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < backgroundImages.Count)
            {
                return backgroundImages[layerIndex];
            }
            return null;
        }

        private async UniTask UpdateBackground(int layerIndex, Sprite sprite, bool clear, float duration)
        {
            if (layerIndex < 0 || layerIndex >= backgroundImages.Count || backgroundImages[layerIndex] == null) return;
            
            Image targetImage = backgroundImages[layerIndex];
            if (!targetImage.gameObject.activeSelf)
            {
                targetImage.gameObject.SetActive(true);
            }
            
            CancelFade(layerIndex);
            var cts = new CancellationTokenSource();
            _backgroundFadeCts[layerIndex] = cts;

            if (clear)
            {
                await UIAnimationUtils.FadeImage(targetImage, 0f, duration, cts.Token);
            }
            if (sprite != null)
            {
                targetImage.sprite = sprite;
                await UIAnimationUtils.FadeImage(targetImage, 1f, duration, cts.Token);
            }
        }

        private void CancelFade(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < _backgroundFadeCts.Count)
            {
                if (_backgroundFadeCts[layerIndex] != null)
                {
                    _backgroundFadeCts[layerIndex].Cancel();
                    _backgroundFadeCts[layerIndex].Dispose();
                    _backgroundFadeCts[layerIndex] = null;
                }
            }
        }

        /// <summary>
        /// 解析資源：優先透過 Bridge 以 key 載入，否則 fallback 到直接引用。
        /// </summary>
        private async UniTask<T> ResolveAsset<T>(string resourceKey, T directReference) where T : UnityEngine.Object
        {
            if (!string.IsNullOrEmpty(resourceKey) && DialogueResourceBridge.HasProvider)
            {
                var asset = await DialogueResourceBridge.LoadAsync<T>(resourceKey);
                if (asset != null)
                {
                    _loadedResourceKeys.Add(resourceKey);
                    return asset;
                }
                Debug.LogWarning($"[BackgroundManager] 無法透過 Bridge 載入資源 '{resourceKey}'，嘗試使用直接引用。");
            }
            return directReference;
        }

        /// <summary>
        /// 釋放所有透過 Bridge 載入的資源。應在對話結束時呼叫。
        /// </summary>
        public void ReleaseAllResources()
        {
            foreach (var key in _loadedResourceKeys)
            {
                DialogueResourceBridge.Release(key);
            }
            _loadedResourceKeys.Clear();
        }
    }
}
