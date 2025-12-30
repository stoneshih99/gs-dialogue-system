using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Dialogue.Utils
{
    /// <summary>
    /// 提供通用的 UI 動畫 UniTask。
    /// </summary>
    public static class UIAnimationUtils
    {
        /// <summary>
        /// 對 Image 執行淡入淡出。
        /// </summary>
        public static async UniTask FadeImage(Image image, float targetAlpha, float duration, CancellationToken cancellationToken = default)
        {
            if (image == null) return;

            Color startColor = image.color;
            float startAlpha = startColor.a;
            float startTime = Time.unscaledTime;

            if (duration <= 0f)
            {
                startColor.a = targetAlpha;
                image.color = startColor;
                return;
            }

            while (Time.unscaledTime < startTime + duration)
            {
                // 檢查取消請求
                if (cancellationToken.IsCancellationRequested) return;
                if (image == null) return;

                float t = (Time.unscaledTime - startTime) / duration;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
                
                startColor.a = newAlpha;
                image.color = startColor;
                
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (image != null)
            {
                startColor.a = targetAlpha;
                image.color = startColor;
            }
        }

        /// <summary>
        /// 對 CanvasGroup 執行閃爍效果。
        /// </summary>
        public static async UniTask Flicker(CanvasGroup cg, float duration, float frequency, float minAlpha, CancellationToken cancellationToken = default)
        {
            if (cg == null) return;

            float time = 0;
            float originalAlpha = cg.alpha;

            while (time < duration)
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (cg == null) return;

                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)));
                cg.alpha = alpha;
                time += Time.deltaTime; // Flicker 通常受遊戲時間影響
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (cg != null) cg.alpha = originalAlpha;
        }
    }
}
