using System.Collections;
using SG.Dialogue.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// ImageDialoguePortraitPresenter 是一個使用 Unity UI Image 組件來呈現對話立繪的類別。
    /// 它實現了 IDialoguePortraitPresenter 介面，提供了顯示、隱藏和動畫化 Sprite 立繪的功能。
    /// </summary>
    [RequireComponent(typeof(LitMotionPlayer))]
    public class ImageDialoguePortraitPresenter : MonoBehaviour, IDialoguePortraitPresenter
    {
        [Tooltip("用於顯示立繪的 Image 組件。")]
        [SerializeField] private Image targetImage;
        private LitMotionPlayer _motionPlayer; // 用於播放動畫的 LitMotionPlayer
        private Coroutine _fadeRoutine; // 當前的淡入淡出協程

        private void Awake()
        {
            // 如果 targetImage 未設定，則嘗試從當前 GameObject 獲取
            if (targetImage == null) targetImage = GetComponent<Image>();
            _motionPlayer = GetComponent<LitMotionPlayer>();
        }

        /// <summary>
        /// 顯示一個 Sprite 立繪，並在指定的持續時間內淡入。
        /// </summary>
        /// <param name="sprite">要顯示的 Sprite。</param>
        /// <param name="fadeDuration">淡入持續時間（秒）。</param>
        public void ShowSprite(Sprite sprite, float fadeDuration)
        {
            if (targetImage == null) return;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine); // 停止之前的淡入淡出協程

            if (fadeDuration <= 0f) // 如果持續時間為 0，則立即顯示
            {
                targetImage.sprite = sprite;
                targetImage.enabled = sprite != null;
                if (targetImage.enabled)
                {
                    var c = targetImage.color;
                    c.a = 1f;
                    targetImage.color = c;
                }
                return;
            }
            _fadeRoutine = StartCoroutine(FadeInRoutine(sprite, fadeDuration)); // 啟動淡入協程
        }

        /// <summary>
        /// 此呈現器不支援 Spine，因此呼叫此方法會立即隱藏立繪。
        /// </summary>
        /// <param name="config">Spine 立繪的設定。</param>
        /// <param name="fadeDuration">淡入持續時間（秒）。</param>
        public void ShowSpine(SpinePortraitConfig config, float fadeDuration)
        {
            HideImmediate();
        }

        /// <summary>
        /// 在指定的持續時間內淡出並隱藏立繪。
        /// </summary>
        /// <param name="fadeDuration">淡出持續時間（秒）。</param>
        public void Hide(float fadeDuration)
        {
            if (targetImage == null) return;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);

            if (fadeDuration <= 0f) // 如果持續時間為 0，則立即隱藏
            {
                targetImage.enabled = false;
                targetImage.sprite = null;
                return;
            }
            _fadeRoutine = StartCoroutine(FadeOutRoutine(fadeDuration)); // 啟動淡出協程
        }

        /// <summary>
        /// 立即隱藏立繪，沒有淡出效果。
        /// </summary>
        public void HideImmediate()
        {
            if (targetImage == null) return;
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
            targetImage.enabled = false;
            targetImage.sprite = null;
        }

        /// <summary>
        /// 播放一個指定的 LitMotion 動畫。
        /// </summary>
        /// <param name="data">包含動畫參數的 MotionData 實例。</param>
        public void PlayMotion(MotionData data)
        {
            if (_motionPlayer != null)
            {
                _motionPlayer.Play(data);
            }
            else
            {
                Debug.LogWarning("ImageDialoguePortraitPresenter: LitMotionPlayer component not found.", this);
            }
        }

        /// <summary>
        /// 設定立繪的高亮狀態。
        /// 當 isHighlighted 為 true 時，立繪應顯示為正常狀態（例如全彩）；
        /// 當 isHighlighted 為 false 時，立繪應顯示為非高亮狀態（例如灰階或半透明）。
        /// </summary>
        /// <param name="isHighlighted">是否高亮。</param>
        public void SetHighlight(bool isHighlighted)
        {
            if (targetImage == null) return;
            // 保持原有的透明度，只改變 RGB 值
            Color targetColor = isHighlighted ? Color.white : Color.gray; // 高亮為白色，非高亮為灰色
            targetColor.a = targetImage.color.a; // 保留當前透明度
            targetImage.color = targetColor;
        }

        /// <summary>
        /// 執行閃爍效果。
        /// </summary>
        /// <param name="duration">總持續時間。</param>
        /// <param name="frequency">閃爍頻率。</param>
        /// <param name="minAlpha">閃爍時的最低透明度。</param>
        /// <returns>一個協程，用於等待閃爍效果完成。</returns>
        public IEnumerator Flicker(float duration, float frequency, float minAlpha)
        {
            if (targetImage == null) yield break;

            float time = 0;
            float originalAlpha = targetImage.color.a;
            Color color = targetImage.color;

            while (time < duration)
            {
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)));
                color.a = alpha;
                targetImage.color = color;
                time += Time.deltaTime;
                yield return null;
            }

            color.a = originalAlpha; // 恢復原始透明度
            targetImage.color = color;
        }

        /// <summary>
        /// 淡入協程。
        /// </summary>
        /// <param name="sprite">要顯示的 Sprite。</param>
        /// <param name="duration">淡入持續時間。</param>
        /// <returns>協程。</returns>
        private IEnumerator FadeInRoutine(Sprite sprite, float duration)
        {
            targetImage.sprite = sprite;
            targetImage.enabled = sprite != null;
            float t = 0f;
            var color = targetImage.color;
            float start = 0f;
            float end = sprite == null ? 0f : 1f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / duration);
                color.a = Mathf.Lerp(start, end, lerp);
                targetImage.color = color;
                yield return null;
            }
            color.a = end;
            targetImage.color = color;
            _fadeRoutine = null;
        }

        /// <summary>
        /// 淡出協程。
        /// </summary>
        /// <param name="duration">淡出持續時間。</param>
        /// <returns>協程。</returns>
        private IEnumerator FadeOutRoutine(float duration)
        {
            float t = 0f;
            var color = targetImage.color;
            float start = color.a;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / duration);
                color.a = Mathf.Lerp(start, 0f, lerp);
                targetImage.color = color;
                yield return null;
            }
            color.a = 0f;
            targetImage.color = color;
            targetImage.enabled = false;
            targetImage.sprite = null;
            _fadeRoutine = null;
        }
    }
}
