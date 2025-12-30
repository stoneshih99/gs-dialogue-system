#if SPINE_KIT_AVAILABLE
using System.Threading;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Animation;
using Spine.Unity;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// SpineDialoguePortraitPresenter 是一個使用 Spine SkeletonAnimation 組件來呈現對話立繪的類別。
    /// 它實現了 IDialoguePortraitPresenter 介面，提供了顯示、隱藏和動畫化 Spine 立繪的功能。
    /// </summary>
    [RequireComponent(typeof(LitMotionPlayer))]
    public class SpineDialoguePortraitPresenter : MonoBehaviour, IDialoguePortraitPresenter
    {
        [Tooltip("用於顯示 Spine 動畫的 SkeletonAnimation 組件。")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        private LitMotionPlayer _motionPlayer;
        private CancellationTokenSource _fadeCts;

        private void Awake()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }
            _motionPlayer = GetComponent<LitMotionPlayer>();
        }

        private void OnDestroy()
        {
            CancelFade();
        }

        public UniTask ShowSprite(Sprite sprite, float fadeDuration)
        {
            Debug.LogWarning("SpineDialoguePortraitPresenter does not support Sprites. Hiding portrait.");
            HideImmediate();
            return UniTask.CompletedTask;
        }

        public UniTask ShowSpine(SpinePortraitConfig config, float fadeDuration)
        {
            if (config == null ) { HideImmediate(); return UniTask.CompletedTask; }
            if (skeletonAnimation == null) { Debug.LogWarning("SkeletonAnimation 未設定，無法顯示 Spine 立繪。"); return UniTask.CompletedTask; }

            skeletonAnimation.Initialize(overwrite: true);
            skeletonAnimation.Skeleton.SetSlotsToSetupPose();
            skeletonAnimation.Skeleton.ScaleX = config.scaleX;

            if (!string.IsNullOrEmpty(config.skin))
            {
                skeletonAnimation.Skeleton.SetSkin(config.skin);
            }

            if (!string.IsNullOrEmpty(config.enterAnimation))
            {
                skeletonAnimation.AnimationState.SetAnimation(0, config.enterAnimation, config.loop);
            }

            if (!string.IsNullOrEmpty(config.queuedAnimation))
            {
                skeletonAnimation.AnimationState.AddAnimation(0, config.queuedAnimation, config.loop, config.queuedAnimationDelay);
            }

            CancelFade();
            gameObject.SetActive(true);
            skeletonAnimation.skeleton.A = 0f;
            
            _fadeCts = new CancellationTokenSource();
            return FadeTo(1f, fadeDuration, _fadeCts.Token);
        }

        public UniTask ShowSpriteSheet(string animationName, float fadeDuration)
        {
            Debug.LogWarning("SpineDialoguePortraitPresenter does not support Sprite Sheets. Hiding portrait.");
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
            if (skeletonAnimation != null && skeletonAnimation.skeleton != null)
            {
                skeletonAnimation.skeleton.A = 0f;
                if(skeletonAnimation.AnimationState != null) skeletonAnimation.AnimationState.ClearTracks();
            }
            gameObject.SetActive(false);
        }

        public void PlayMotion(MotionData data)
        {
            if (_motionPlayer != null) _motionPlayer.Play(data);
            else Debug.LogWarning("SpineDialoguePortraitPresenter: LitMotionPlayer component not found.", this);
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (skeletonAnimation == null || skeletonAnimation.skeleton == null) return;
            Color targetColor = isHighlighted ? Color.white : Color.gray;
            skeletonAnimation.skeleton.SetColor(targetColor);
        }

        public async UniTask Flicker(float duration, float frequency, float minAlpha)
        {
            if (skeletonAnimation == null || skeletonAnimation.skeleton == null) return;

            var token = this.GetCancellationTokenOnDestroy();
            float time = 0;
            float originalAlpha = skeletonAnimation.skeleton.A;

            while (time < duration)
            {
                if (token.IsCancellationRequested) return;
                
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)));
                skeletonAnimation.skeleton.A = alpha;
                time += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            skeletonAnimation.skeleton.A = originalAlpha;
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
            if (skeletonAnimation == null || skeletonAnimation.skeleton == null) return;
            
            gameObject.SetActive(true);
            float startAlpha = skeletonAnimation.skeleton.A;

            if (duration <= 0f)
            {
                skeletonAnimation.skeleton.A = targetAlpha;
                if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
                return;
            }

            float startTime = Time.unscaledTime;
            while (Time.unscaledTime < startTime + duration)
            {
                if (token.IsCancellationRequested) return;
                
                float t = (Time.unscaledTime - startTime) / duration;
                skeletonAnimation.skeleton.A = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            skeletonAnimation.skeleton.A = targetAlpha;
            if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
        }
    }
}
#endif
