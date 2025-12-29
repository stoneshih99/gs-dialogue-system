using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Animation;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 一個 IDialoguePortraitPresenter 的實作，用於顯示和播放 Sprite Sheet 動畫。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteSheetDialoguePortraitPresenter : MonoBehaviour, IDialoguePortraitPresenter
    {
        [Tooltip("Sprite Sheet 動畫的播放速度（每秒影格數）。")]
        [SerializeField] public int fps = 60;
        [Tooltip("動畫是否循環播放。")]
        [SerializeField] public bool loop = true;
        private SpriteRenderer _portraitSprite;

        [SerializeField] private List<SpriteSheetStateConfig> stateAnimations;
        
        private Coroutine _animationCoroutine;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            if (_portraitSprite == null)
            {
                _portraitSprite = GetComponent<SpriteRenderer>();
            }
            // 確保初始狀態正確
            if (_portraitSprite != null)
            {
                _portraitSprite.enabled = false;
                var c = _portraitSprite.color;
                c.a = 0f;
                _portraitSprite.color = c;
            }
        }

        public void ShowSprite(Sprite sprite, float fadeDuration)
        {
            StopAnimation();
            if (_portraitSprite == null) return;
            
            _portraitSprite.sprite = sprite;
            _portraitSprite.enabled = true;
            
            // 啟動淡入
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeTo(1f, fadeDuration));
        }

        public void ShowSpine(SpinePortraitConfig config, float fadeDuration)
        {
            Debug.LogWarning("SpriteSheetDialoguePortraitPresenter does not support Spine.");
            HideImmediate();
        }

        public void ShowSpriteSheet(string spriteSheetAnimationName, float fadeDuration)
        {
            if (string.IsNullOrEmpty(spriteSheetAnimationName)) return;
            
            StopAnimation();
            if (_portraitSprite == null) return;

            _portraitSprite.enabled = true;
            var config = FindAnimationByName(spriteSheetAnimationName);
            _animationCoroutine = StartCoroutine(PlaySpriteSheetAnimation(config));
            
            // 啟動淡入
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            // 如果是剛開始顯示，確保 Alpha 為 0
            if (!_portraitSprite.gameObject.activeSelf || _portraitSprite.color.a == 0)
            {
                var c = _portraitSprite.color;
                c.a = 0f;
                _portraitSprite.color = c;
            }
            _fadeRoutine = StartCoroutine(FadeTo(1f, fadeDuration));
        }

        public void Hide(float fadeDuration)
        {
            // 保持動畫播放直到完全消失，這樣看起來更自然
            // StopAnimation(); 
            
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeTo(0f, fadeDuration));
        }

        public void HideImmediate()
        {
            StopAnimation();
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
            if (_portraitSprite != null)
            {
                _portraitSprite.enabled = false;
                var c = _portraitSprite.color;
                c.a = 0f;
                _portraitSprite.color = c;
            }
        }

        public void PlayMotion(MotionData data)
        {
            // 可以根據需要實作 LitMotion 動畫
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (_portraitSprite == null) return;
            
            // 這裡需要注意：直接設定顏色會覆蓋 Alpha。
            // 我們應該只改變 RGB，保留當前的 Alpha。
            Color targetColor = isHighlighted ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            Color currentColor = _portraitSprite.color;
            targetColor.a = currentColor.a; // 保留當前 Alpha
            _portraitSprite.color = targetColor;
        }

        public IEnumerator Flicker(float duration, float frequency, float minAlpha)
        {
            if (_portraitSprite == null) yield break;

            float time = 0;
            float originalAlpha = _portraitSprite.color.a;

            while (time < duration)
            {
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI * 2)));
                var color = _portraitSprite.color;
                color.a = alpha;
                _portraitSprite.color = color;
                time += Time.deltaTime;
                yield return null;
            }
            var finalColor = _portraitSprite.color;
            finalColor.a = originalAlpha;
            _portraitSprite.color = finalColor;
        }

        private IEnumerator PlaySpriteSheetAnimation(SpriteSheetStateConfig config)
        {
            if (config == null || config.frames == null || config.frames.Length == 0)
            {
                yield break;
            }

            float frameDuration = 1f / fps;
            int frameIndex = 0;

            while (true)
            {
                _portraitSprite.sprite = config.frames[frameIndex];
                yield return new WaitForSeconds(frameDuration);

                frameIndex++;
                if (frameIndex >= config.frames.Length)
                {
                    if (loop)
                    {
                        frameIndex = 0;
                    }
                    else
                    {
                        yield break; // 動畫結束
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
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (_portraitSprite == null) yield break;

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
                    float t = (Time.unscaledTime - startTime) / duration;
                    float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
                    
                    Color c = _portraitSprite.color;
                    c.a = newAlpha;
                    _portraitSprite.color = c;
                    
                    yield return null;
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
            
            _fadeRoutine = null;
        }
    }
}
