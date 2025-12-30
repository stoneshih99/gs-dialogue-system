using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 控制 UGUI 的螢幕效果，例如灰階、畫面閃爍和背景模糊。
    /// </summary>
    public class ScreenEffectController : MonoBehaviour
    {
        [Header("灰階效果")]
        [Tooltip("用於灰階效果的 Image 遮罩")]
        [SerializeField] private Image grayscaleMask;
        [Tooltip("灰階效果的目標 Alpha 值")]
        [Range(0f, 1f)]
        [SerializeField] private float targetGrayscaleAlpha = 0.8f;

        [Header("閃爍效果")]
        [Tooltip("用於畫面閃爍效果的 Image 遮罩")]
        [SerializeField] private Image flashMask;

        [Header("背景模糊效果")]
        [Tooltip("帶有模糊材質的背景 Image 組件")]
        [SerializeField] private Image backgroundBlurImage;

        private Material _blurMaterialInstance;

        private void Awake()
        {
            if (grayscaleMask != null)
            {
                var initialGrayscaleColor = grayscaleMask.color;
                initialGrayscaleColor.a = 0f;
                grayscaleMask.color = initialGrayscaleColor;
            }
            if (flashMask != null)
            {
                var initialFlashColor = flashMask.color;
                initialFlashColor.a = 0f;
                flashMask.color = initialFlashColor;
            }
            if (backgroundBlurImage != null)
            {
                // 創建材質實例以避免修改專案中的原始材質
                _blurMaterialInstance = Instantiate(backgroundBlurImage.material);
                backgroundBlurImage.material = _blurMaterialInstance;
                _blurMaterialInstance.SetFloat("_BlurSize", 0f);
            }
        }

        /// <summary>
        /// 啟用灰階效果。
        /// </summary>
        public async UniTask EnableGrayscale(float duration)
        {
            if (grayscaleMask == null) return;
            await FadeMaskRoutine(grayscaleMask, grayscaleMask.color, targetGrayscaleAlpha, duration);
        }

        /// <summary>
        /// 禁用灰階效果。
        /// </summary>
        public async UniTask DisableGrayscale(float duration)
        {
            if (grayscaleMask == null) return;
            await FadeMaskRoutine(grayscaleMask, grayscaleMask.color, 0f, duration);
        }

        /// <summary>
        /// 執行畫面閃爍效果。
        /// </summary>
        public async UniTask ExecuteFlash(float duration, Color color, float intensity)
        {
            if (flashMask == null) return;
            float fadeInDuration = duration * 0.5f;
            float fadeOutDuration = duration * 0.5f;
            await FadeMaskRoutine(flashMask, color, intensity, fadeInDuration);
            await FadeMaskRoutine(flashMask, color, 0f, fadeOutDuration);
        }

        /// <summary>
        /// 啟用背景模糊效果。
        /// </summary>
        public async UniTask EnableBlur(float duration, float blurAmount)
        {
            if (_blurMaterialInstance == null) return;
            await FadeBlurRoutine(blurAmount, duration);
        }

        /// <summary>
        /// 禁用背景模糊效果。
        /// </summary>
        public async UniTask DisableBlur(float duration)
        {
            if (_blurMaterialInstance == null) return;
            await FadeBlurRoutine(0f, duration);
        }

        private async UniTask FadeMaskRoutine(Image mask, Color baseColor, float targetAlpha, float duration)
        {
            float startAlpha = mask.color.a;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                baseColor.a = newAlpha;
                mask.color = baseColor;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            baseColor.a = targetAlpha;
            mask.color = baseColor;
        }

        private async UniTask FadeBlurRoutine(float targetBlur, float duration)
        {
            float startBlur = _blurMaterialInstance.GetFloat("_BlurSize");
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float newBlur = Mathf.Lerp(startBlur, targetBlur, time / duration);
                _blurMaterialInstance.SetFloat("_BlurSize", newBlur);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            _blurMaterialInstance.SetFloat("_BlurSize", targetBlur);
        }
    }
}
