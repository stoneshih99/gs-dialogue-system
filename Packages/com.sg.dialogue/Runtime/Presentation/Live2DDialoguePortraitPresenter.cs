#if LIVE2D_KIT_AVAILABLE
using Live2DActorKit.Core;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Animation;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// Live2DDialoguePortraitPresenter 是一個使用 Live2D Actor 來呈現對話立繪的類別。
    /// 它實現了 IDialoguePortraitPresenter 介面，提供了顯示、隱藏和動畫化 Live2D 立繪的功能。
    /// </summary>
    [RequireComponent(typeof(LitMotionPlayer))]
    public class Live2DDialoguePortraitPresenter : MonoBehaviour, IDialoguePortraitPresenter
    {
        [Tooltip("用於顯示 Live2D 動畫的 ILive2DActor。")]
        [SerializeField] private MonoBehaviour live2DActorComponent;

        private ILive2DActor _live2DActor;
        private LitMotionPlayer _motionPlayer;
        private CancellationTokenSource _fadeCts;

        public float Alpha
        {
            get => _live2DActor != null ? _live2DActor.GetOpacity() : 0f;
            set
            {
                if (_live2DActor != null)
                {
                    _live2DActor.SetOpacity(value);
                }
            }
        }

        private void Awake()
        {
            if (live2DActorComponent is ILive2DActor actor)
            {
                _live2DActor = actor;
            }
            else
            {
                _live2DActor = GetComponent<ILive2DActor>();
            }
            _motionPlayer = GetComponent<LitMotionPlayer>();
        }

        private void OnDestroy()
        {
            CancelFade();
        }

        public UniTask ShowSprite(Sprite sprite, float fadeDuration)
        {
            Debug.LogWarning("Live2DDialoguePortraitPresenter does not support Sprites. Hiding portrait.");
            HideImmediate();
            return UniTask.CompletedTask;
        }

        public UniTask ShowSpine(SpinePortraitConfig config, float fadeDuration)
        {
            Debug.LogWarning("Live2DDialoguePortraitPresenter does not support Spine. Hiding portrait.");
            HideImmediate();
            return UniTask.CompletedTask;
        }

        public UniTask ShowSpriteSheet(string animationName, float fadeDuration)
        {
            Debug.LogWarning("Live2DDialoguePortraitPresenter does not support Sprite Sheets. Hiding portrait.");
            HideImmediate();
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 顯示一個 Live2D 立繪，並在指定的持續時間內淡入。
        /// </summary>
        /// <param name="config">Live2D 立繪的設定。</param>
        /// <param name="fadeDuration">淡入持續時間（秒）。</param>
        public UniTask ShowLive2D(Live2DPortraitConfig config, float fadeDuration)
        {
            if (config == null) { HideImmediate(); return UniTask.CompletedTask; }
            if (_live2DActor == null) { Debug.LogWarning("Live2DActor 未設定，無法顯示 Live2D 立繪。"); return UniTask.CompletedTask; }

            transform.localScale = new Vector3(config.scaleX, 1, 1);

            if (!string.IsNullOrEmpty(config.expression))
            {
                _live2DActor.SetExpression(config.expression);
            }

            if (!string.IsNullOrEmpty(config.enterAnimation))
            {
                _live2DActor.PlayMotion(config.enterAnimation, config.loop);
            }

            // Live2D does not have a direct equivalent of queued animation, so we will ignore it for now.

            CancelFade();
            gameObject.SetActive(true);
            _live2DActor.SetOpacity(0f);
            
            _fadeCts = new CancellationTokenSource();
            return FadeTo(1f, fadeDuration, _fadeCts.Token);
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
            if (_live2DActor != null)
            {
                _live2DActor.SetOpacity(0f);
                _live2DActor.StopMotion();
            }
            gameObject.SetActive(false);
        }

        public void PlayMotion(MotionData data)
        {
            if (_motionPlayer != null)
            {
                _motionPlayer.Play(data);
            }
            else
            {
                Debug.LogWarning("Live2DDialoguePortraitPresenter: LitMotionPlayer component not found.", this);
            }
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (_live2DActor == null) return;
            Color targetColor = isHighlighted ? Color.white : Color.gray;
            _live2DActor.SetColor(targetColor);
        }

        public async UniTask Flicker(float duration, float frequency, float minAlpha)
        {
            if (_live2DActor == null) return;

            var token = this.GetCancellationTokenOnDestroy();
            float time = 0;
            float originalAlpha = _live2DActor.GetOpacity();

            while (time < duration)
            {
                if (token.IsCancellationRequested) return;
                
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)));
                _live2DActor.SetOpacity(alpha);
                time += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _live2DActor.SetOpacity(originalAlpha);
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
            if (_live2DActor == null) return;

            gameObject.SetActive(true);
            float startAlpha = _live2DActor.GetOpacity();

            if (duration <= 0f)
            {
                _live2DActor.SetOpacity(targetAlpha);
                if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
                return;
            }

            float t = 0f;
            while (t < duration)
            {
                if (token.IsCancellationRequested) return;
                
                t += Time.deltaTime;
                _live2DActor.SetOpacity(Mathf.Lerp(startAlpha, targetAlpha, t / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _live2DActor.SetOpacity(targetAlpha);
            if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
        }
    }
}
#endif
