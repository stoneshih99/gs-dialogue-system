using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Animation;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 一個 IDialoguePortraitPresenter 的實作，用於顯示和播放 Sprite Sheet 動畫。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(LitMotionPlayer))]
    public class SpriteSheetDialoguePortraitPresenter : MonoBehaviour, IDialoguePortraitPresenter
    {
        [Tooltip("Sprite Sheet 動畫的播放速度（每秒影格數）。")]
        [SerializeField] public int fps = 60;
        [Tooltip("動畫是否循環播放。")]
        [SerializeField] public bool loop = true;
        private SpriteRenderer _portraitSprite;
        private LitMotionPlayer _motionPlayer;

        [SerializeField] private List<SpriteSheetStateConfig> stateAnimations;
        
        private CancellationTokenSource _animationCts;
        private CancellationTokenSource _fadeCts;

        public float Alpha
        {
            get => _portraitSprite != null ? _portraitSprite.color.a : 0f;
            set
            {
                if (_portraitSprite != null)
                {
                    var c = _portraitSprite.color;
                    c.a = value;
                    _portraitSprite.color = c;
                }
            }
        }

        private void Awake()
        {
            if (_portraitSprite == null)
            {
                _portraitSprite = GetComponent<SpriteRenderer>();
            }
            _motionPlayer = GetComponent<LitMotionPlayer>();
            
            // 確保初始狀態正確，但不強制設為 0，以免覆蓋編輯器設定或初始狀態
            if (_portraitSprite != null)
            {
                _portraitSprite.enabled = false;
                // 移除強制設為 0 的邏輯，讓 Show 方法來控制
                // var c = _portraitSprite.color;
                // c.a = 0f;
                // _portraitSprite.color = c;
            }
        }

        private void OnDestroy()
        {
            CancelFade();
            StopAnimation();
        }

        public UniTask ShowSprite(Sprite sprite, float fadeDuration)
        {
            StopAnimation();
            if (_portraitSprite == null) return UniTask.CompletedTask;
            
            _portraitSprite.sprite = sprite;
            _portraitSprite.enabled = true;
            
            CancelFade();
            _fadeCts = new CancellationTokenSource();
            
            // 如果是第一次顯示，確保 Alpha 從 0 開始（如果需要淡入）
            if (fadeDuration > 0 && !_portraitSprite.gameObject.activeSelf)
            {
                 var c = _portraitSprite.color;
                 c.a = 0f;
                 _portraitSprite.color = c;
            }
            
            return FadeTo(1f, fadeDuration, _fadeCts.Token);
        }

        public UniTask ShowSpine(SpinePortraitConfig config, float fadeDuration)
        {
            Debug.LogWarning("SpriteSheetDialoguePortraitPresenter does not support Spine.");
            HideImmediate();
            return UniTask.CompletedTask;
        }

        public UniTask ShowSpriteSheet(string spriteSheetAnimationName, float fadeDuration)
        {
            if (string.IsNullOrEmpty(spriteSheetAnimationName)) return UniTask.CompletedTask;
            
            StopAnimation();
            if (_portraitSprite == null) return UniTask.CompletedTask;

            _portraitSprite.enabled = true;
            var config = FindAnimationByName(spriteSheetAnimationName);
            
            _animationCts = new CancellationTokenSource();
            PlaySpriteSheetAnimation(config, _animationCts.Token).Forget();
            
            CancelFade();
            _fadeCts = new CancellationTokenSource();
            
            // 只有在物件原本未激活或 Alpha 為 0 時，才強制從 0 開始淡入
            // 這樣可以避免已經顯示的角色在切換動畫時閃爍
            if (!_portraitSprite.gameObject.activeSelf || _portraitSprite.color.a == 0)
            {
                var c = _portraitSprite.color;
                c.a = 0f;
                _portraitSprite.color = c;
            }
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
            StopAnimation();
            CancelFade();
            if (_portraitSprite != null)
            {
                _portraitSprite.enabled = false;
                var c = _portraitSprite.color;
                c.a = 0f;
                _portraitSprite.color = c;
            }
        }

        public async UniTask PlayMotion(MotionData data)
        {
            if (_motionPlayer != null) await _motionPlayer.Play(data);
            else Debug.LogWarning("SpriteSheetDialoguePortraitPresenter: LitMotionPlayer component not found.", this);
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (_portraitSprite == null) return;
            
            Color targetColor = isHighlighted ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            Color currentColor = _portraitSprite.color;
            targetColor.a = currentColor.a;
            _portraitSprite.color = targetColor;
        }

        public async UniTask Flicker(float duration, float frequency, float minAlpha)
        {
            if (_portraitSprite == null) return;

            var token = this.GetCancellationTokenOnDestroy();
            float time = 0;
            float originalAlpha = _portraitSprite.color.a;

            while (time < duration)
            {
                if (token.IsCancellationRequested) return;
                
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI * 2)));
                var color = _portraitSprite.color;
                color.a = alpha;
                _portraitSprite.color = color;
                time += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            var finalColor = _portraitSprite.color;
            finalColor.a = originalAlpha;
            _portraitSprite.color = finalColor;
        }

        private async UniTaskVoid PlaySpriteSheetAnimation(SpriteSheetStateConfig config, CancellationToken token)
        {
            if (config == null || config.frames == null || config.frames.Length == 0)
            {
                return;
            }

            float frameDuration = 1f / fps;
            int frameIndex = 0;

            while (true)
            {
                if (token.IsCancellationRequested) return;
                
                _portraitSprite.sprite = config.frames[frameIndex];
                await UniTask.Delay(TimeSpan.FromSeconds(frameDuration), cancellationToken: token);

                frameIndex++;
                if (frameIndex >= config.frames.Length)
                {
                    if (loop)
                    {
                        frameIndex = 0;
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
        
        private SpriteSheetStateConfig FindAnimationByName(string animationName)
        {
            return stateAnimations.Find(config => config.animationName == animationName);
        }

        private void StopAnimation()
        {
            if (_animationCts != null)
            {
                _animationCts.Cancel();
                _animationCts.Dispose();
                _animationCts = null;
            }
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
            if (_portraitSprite == null) return;

            float startAlpha = _portraitSprite.color.a;
            float startTime = Time.unscaledTime;

            if (duration <= 0f)
            {
                Color c = _portraitSprite.color;
                c.a = targetAlpha;
                _portraitSprite.color = c;
            }
            else
            {
                while (Time.unscaledTime < startTime + duration)
                {
                    if (token.IsCancellationRequested) return;
                    
                    float t = (Time.unscaledTime - startTime) / duration;
                    float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
                    
                    Color c = _portraitSprite.color;
                    c.a = newAlpha;
                    _portraitSprite.color = c;
                    
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
                
                Color finalColor = _portraitSprite.color;
                finalColor.a = targetAlpha;
                _portraitSprite.color = finalColor;
            }

            if (Mathf.Approximately(targetAlpha, 0f))
            {
                StopAnimation();
                _portraitSprite.enabled = false;
            }
        }
    }
}
