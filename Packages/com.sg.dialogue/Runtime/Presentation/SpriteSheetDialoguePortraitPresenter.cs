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

        private void Awake()
        {
            if (_portraitSprite == null)
            {
                _portraitSprite = GetComponent<SpriteRenderer>();
            }
            _portraitSprite.enabled = false; // 初始時隱藏
        }

        public void ShowSprite(Sprite sprite, float fadeDuration)
        {
            StopAnimation();
            _portraitSprite.sprite = sprite;
            _portraitSprite.enabled = true;
            // 可以在這裡加入淡入效果
        }

        public void ShowSpine(SpinePortraitConfig config, float fadeDuration)
        {
            // 這個 Presenter 不支援 Spine，所以忽略。
            Debug.LogWarning("SpriteSheetDialoguePortraitPresenter does not support Spine.");
        }

        public void ShowSpriteSheet(string spriteSheetAnimationName, float fadeDuration)
        {
            if (string.IsNullOrEmpty(spriteSheetAnimationName))
            {
                return;
            }
            StopAnimation();
            _portraitSprite.enabled = true;
            var config = FindAnimationByName(spriteSheetAnimationName);
            _animationCoroutine = StartCoroutine(PlaySpriteSheetAnimation(config));
            // 可以在這裡加入淡入效果
        }

        public void Hide(float fadeDuration)
        {
            StopAnimation();
            // 可以在這裡加入淡出效果
            _portraitSprite.enabled = false;
        }

        public void HideImmediate()
        {
            StopAnimation();
            _portraitSprite.enabled = false;
        }

        public void PlayMotion(MotionData data)
        {
            // 可以根據需要實作 LitMotion 動畫
        }

        public void SetHighlight(bool isHighlighted)
        {
            _portraitSprite.color = isHighlighted ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        public IEnumerator Flicker(float duration, float frequency, float minAlpha)
        {
            // 實作閃爍效果
            float time = 0;
            while (time < duration)
            {
                float alpha = Mathf.Lerp(minAlpha, 1f, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI * 2)));
                var color = _portraitSprite.color;
                color.a = alpha;
                _portraitSprite.color = color;
                time += Time.deltaTime;
                yield return null;
            }
            var finalColor = _portraitSprite.color;
            finalColor.a = 1f;
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
    }
}
