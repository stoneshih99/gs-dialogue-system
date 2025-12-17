#if SPINE_KIT_AVAILABLE
using System.Collections;
using SG.Dialogue.Animation;
using Spine.Unity;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// SpineUiDialoguePortraitPresenter 是一個使用 Spine SkeletonGraphic (for Unity UI) 組件來呈現對話立繪的類別。
    /// 它實現了 IDialoguePortraitPresenter 介面，提供了顯示、隱藏和動畫化 Spine UI 立繪的功能。
    /// </summary>
    [RequireComponent(typeof(LitMotionPlayer))]
    public class SpineUiDialoguePortraitPresenter : MonoBehaviour, IDialoguePortraitPresenter
    {
        [Tooltip("用於顯示 Spine 動畫的 SkeletonGraphic 組件。")]
        [SerializeField] private SkeletonGraphic skeletonGraphic;
        [Tooltip("用於控制淡入淡出的 CanvasGroup 組件。")]
        [SerializeField] private CanvasGroup canvasGroup;

        private LitMotionPlayer _motionPlayer; // 用於播放動畫的 LitMotionPlayer
        private Coroutine _fadeRoutine; // 當前的淡入淡出協程

        private void Awake()
        {
            // 如果 canvasGroup 未設定，則嘗試從當前 GameObject 獲取或添加
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            _motionPlayer = GetComponent<LitMotionPlayer>();
        }

        /// <summary>
        /// 此呈現器不支援 Sprite，因此呼叫此方法會立即隱藏立繪。
        /// </summary>
        /// <param name="sprite">要顯示的 Sprite。</param>
        /// <param name="fadeDuration">淡入持續時間（秒）。</param>
        public void ShowSprite(Sprite sprite, float fadeDuration)
        {
            Debug.LogWarning("SpineUiDialoguePortraitPresenter does not support Sprites. Hiding portrait.");
            HideImmediate();
        }

        /// <summary>
        /// 顯示一個 Spine UI 立繪，並在指定的持續時間內淡入。
        /// </summary>
        /// <param name="config">Spine 立繪的設定。</param>
        /// <param name="fadeDuration">淡入持續時間（秒）。</param>
        public void ShowSpine(SpinePortraitConfig config, float fadeDuration)
        {
            if (config == null ) { HideImmediate(); return; }
            if (skeletonGraphic == null) { Debug.LogWarning("SkeletonGraphic 未設定，無法顯示 Spine UI 立繪。"); return; }

            // 根據設定初始化 Spine 動畫
            skeletonGraphic.initialSkinName = config.skin;
            skeletonGraphic.Initialize(overwrite: true);
            skeletonGraphic.Skeleton.SetSlotsToSetupPose();
            skeletonGraphic.Skeleton.ScaleX = config.scaleX; // 設定水平縮放（翻轉）

            if (!string.IsNullOrEmpty(config.enterAnimation))
            {
                skeletonGraphic.AnimationState.SetAnimation(0, config.enterAnimation, config.loop); // 設定進入動畫
            }

            if (!string.IsNullOrEmpty(config.queuedAnimation))
            {
                skeletonGraphic.AnimationState.AddAnimation(0, config.queuedAnimation, config.loop, config.queuedAnimationDelay); // 添加佇列動畫
            }

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine); // 停止之前的淡入淡出協程
            gameObject.SetActive(true); // 啟用 GameObject
            _fadeRoutine = StartCoroutine(FadeTo(1f, fadeDuration)); // 啟動淡入協程
        }

        /// <summary>
        /// 此呈現器不支援 Sprite Sheet 動畫，因此呼叫此方法會立即隱藏立繪。
        /// </summary>
        /// <param name="animationName">動畫名稱。</param>
        /// <param name="fadeDuration">淡入持續時間（秒）。</param>
        public void ShowSpriteSheet(string animationName, float fadeDuration)
        {
            Debug.LogWarning("SpineUiDialoguePortraitPresenter does not support Sprite Sheets. Hiding portrait.");
            HideImmediate();
        }

        /// <summary>
        /// 在指定的持續時間內淡出並隱藏立繪。
        /// </summary>
        /// <param name="fadeDuration">淡出持續時間（秒）。</param>
        public void Hide(float fadeDuration)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeTo(0f, fadeDuration)); // 啟動淡出協程
        }

        /// <summary>
        /// 立即隱藏立繪，沒有淡出效果。
        /// </summary>
        public void HideImmediate()
        {
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (skeletonGraphic != null && skeletonGraphic.AnimationState != null) skeletonGraphic.AnimationState.ClearTracks(); // 清除動畫軌道
            gameObject.SetActive(false); // 禁用 GameObject
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
                Debug.LogWarning("SpineUiDialoguePortraitPresenter: LitMotionPlayer component not found.", this);
            }
        }

        /// <summary>
        /// 設定立繪的高亮狀態。
        /// 當 isHighlighted 為 true 時，立繪應顯示為正常狀態（白色）；
        /// 當 isHighlighted 為 false 時，立繪應顯示為非高亮狀態（灰色）。
        /// </summary>
        /// <param name="isHighlighted">是否高亮。</param>
        public void SetHighlight(bool isHighlighted)
        {
            if (skeletonGraphic == null || skeletonGraphic.Skeleton == null) return;
            // 保持原有的透明度，只改變 RGB 值
            Color targetColor = isHighlighted ? Color.white : Color.gray; // 高亮為白色，非高亮為灰色
            skeletonGraphic.Skeleton.SetColor(targetColor);
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
            if (canvasGroup == null) yield break;

            float time = 0;
            float originalAlpha = canvasGroup.alpha;

            while (time < duration)
            {
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)));
                canvasGroup.alpha = alpha;
                time += Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = originalAlpha; // 恢復原始透明度
        }

        /// <summary>
        /// 淡入淡出協程。
        /// </summary>
        /// <param name="targetAlpha">目標 Alpha 值。</param>
        /// <param name="duration">持續時間。</param>
        /// <returns>協程。</returns>
        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (canvasGroup == null) yield break;
            gameObject.SetActive(true);
            float startAlpha = canvasGroup.alpha;
            if (duration <= 0f) // 如果持續時間為 0，則立即設定
            {
                canvasGroup.alpha = targetAlpha;
                if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
                _fadeRoutine = null;
                yield break;
            }

            float t = 0f;
            while (t < duration) // 漸變 Alpha
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha; // 確保最終 Alpha 值正確
            if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false); // 如果是淡出，則禁用 GameObject
            _fadeRoutine = null;
        }
    }
}
#endif