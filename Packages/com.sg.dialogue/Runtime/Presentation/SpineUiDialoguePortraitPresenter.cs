#if SPINE_KIT_AVAILABLE
using System.Threading;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Animation;
using SG.Dialogue.Utils;
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

        private LitMotionPlayer _motionPlayer;
        private CancellationTokenSource _fadeCts;

        public float Alpha
        {
            get => canvasGroup != null ? canvasGroup.alpha : 0f;
            set
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = value;
                }
            }
        }

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            _motionPlayer = GetComponent<LitMotionPlayer>();
        }

        private void OnDestroy()
        {
            CancelFade();
        }

        public UniTask ShowSprite(Sprite sprite, float fadeDuration)
        {
            Debug.LogWarning("SpineUiDialoguePortraitPresenter does not support Sprites. Hiding portrait.");
            HideImmediate();
            return UniTask.CompletedTask;
        }

        public UniTask ShowSpine(SpinePortraitConfig config, float fadeDuration)
        {
            if (config == null ) { HideImmediate(); return UniTask.CompletedTask; }
            if (skeletonGraphic == null) { Debug.LogWarning("SkeletonGraphic 未設定，無法顯示 Spine UI 立繪。"); return UniTask.CompletedTask; }

            skeletonGraphic.initialSkinName = config.skin;
            skeletonGraphic.Initialize(overwrite: true);
            skeletonGraphic.Skeleton.SetSlotsToSetupPose();
            skeletonGraphic.Skeleton.ScaleX = config.scaleX;

            if (!string.IsNullOrEmpty(config.enterAnimation))
            {
                skeletonGraphic.AnimationState.SetAnimation(0, config.enterAnimation, config.loop);
            }

            if (!string.IsNullOrEmpty(config.queuedAnimation))
            {
                skeletonGraphic.AnimationState.AddAnimation(0, config.queuedAnimation, config.loop, config.queuedAnimationDelay);
            }

            CancelFade();
            gameObject.SetActive(true);
            
            _fadeCts = new CancellationTokenSource();
            // 確保初始 Alpha 為 0
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            
            return FadeTo(1f, fadeDuration, _fadeCts.Token);
        }

        public UniTask ShowSpriteSheet(string animationName, float fadeDuration)
        {
            Debug.LogWarning("SpineUiDialoguePortraitPresenter does not support Sprite Sheets. Hiding portrait.");
            HideImmediate();
            return UniTask.CompletedTask;
        }

        public UniTask Hide(float fadeDuration)
        {
            CancelFade();
            _fadeCts = new CancellationTokenSource();
            return FadeTo(0f, fadeDuration, _fadeCts.Token);
        }

        public void HideImmediate()
        {
            CancelFade();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (skeletonGraphic != null && skeletonGraphic.AnimationState != null) skeletonGraphic.AnimationState.ClearTracks();
            gameObject.SetActive(false);
        }

        public async UniTask PlayMotion(MotionData data)
        {
            if (_motionPlayer != null) await _motionPlayer.Play(data);
            else Debug.LogWarning("SpineUiDialoguePortraitPresenter: LitMotionPlayer component not found.", this);
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (skeletonGraphic == null || skeletonGraphic.Skeleton == null) return;
            Color targetColor = isHighlighted ? Color.white : Color.gray;
            skeletonGraphic.Skeleton.SetColor(targetColor);
        }

        public UniTask Flicker(float duration, float frequency, float minAlpha)
        {
            if (canvasGroup == null) return UniTask.CompletedTask;
            var token = this.GetCancellationTokenOnDestroy();
            return UIAnimationUtils.Flicker(canvasGroup, duration, frequency, minAlpha, token);
        }

        private void CancelFade()
        {
            if (_fadeCts != null)
            {
                _fadeCts.Cancel();
                _fadeCts.Dispose();
                _fadeCts = null;
            }
        }

        private async UniTask FadeTo(float targetAlpha, float duration, CancellationToken token)
        {
            if (canvasGroup == null) return;
            
            gameObject.SetActive(true);
            float startAlpha = canvasGroup.alpha;

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
                return;
            }

            float startTime = Time.unscaledTime;
            while (Time.unscaledTime < startTime + duration)
            {
                if (token.IsCancellationRequested) return;
                
                float t = (Time.unscaledTime - startTime) / duration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            canvasGroup.alpha = targetAlpha;
            if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
        }
    }
}
#endif
