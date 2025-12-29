using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Dialogue.Utils
{
    /// <summary>
    /// 提供通用的 UI 動畫協程。
    /// </summary>
    public static class UIAnimationUtils
    {
        /// <summary>
        /// 對 Image 執行淡入淡出。
        /// </summary>
        public static IEnumerator FadeImage(Image image, float targetAlpha, float duration)
        {
            if (image == null) yield break;

            Color startColor = image.color;
            float startAlpha = startColor.a;
            float startTime = Time.unscaledTime;

            if (duration <= 0f)
            {
                startColor.a = targetAlpha;
                image.color = startColor;
                yield break;
            }

            while (Time.unscaledTime < startTime + duration)
            {
                float t = (Time.unscaledTime - startTime) / duration;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
                
                startColor.a = newAlpha;
                image.color = startColor;
                
                yield return null;
            }

            startColor.a = targetAlpha;
            image.color = startColor;
        }

        /// <summary>
        /// 對 CanvasGroup 執行閃爍效果。
        /// </summary>
        public static IEnumerator Flicker(CanvasGroup cg, float duration, float frequency, float minAlpha)
        {
            if (cg == null) yield break;

            float time = 0;
            float originalAlpha = cg.alpha;

            while (time < duration)
            {
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)));
                cg.alpha = alpha;
                time += Time.deltaTime; // Flicker 通常受遊戲時間影響
                yield return null;
            }

            cg.alpha = originalAlpha;
        }
    }
}
