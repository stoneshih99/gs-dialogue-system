using System.Collections;
using Live2DActorKit.Core;
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
        private Coroutine _fadeRoutine;

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

        public void ShowSprite(Sprite sprite, float fadeDuration)
        {
            Debug.LogWarning("Live2DDialoguePortraitPresenter does not support Sprites. Hiding portrait.");
            HideImmediate();
        }

        public void ShowSpine(SpinePortraitConfig config, float fadeDuration)
        {
            Debug.LogWarning("Live2DDialoguePortraitPresenter does not support Spine. Hiding portrait.");
            HideImmediate();
        }

        public void ShowSpriteSheet(string animationName, float fadeDuration)
        {
            Debug.LogWarning("Live2DDialoguePortraitPresenter does not support Sprite Sheets. Hiding portrait.");
            HideImmediate();
        }

        /// <summary>
        /// 顯示一個 Live2D 立繪，並在指定的持續時間內淡入。
        /// </summary>
        /// <param name="config">Live2D 立繪的設定。</param>
        /// <param name="fadeDuration">淡入持續時間（秒）。</param>
        public void ShowLive2D(Live2DPortraitConfig config, float fadeDuration)
        {
            if (config == null) { HideImmediate(); return; }
            if (_live2DActor == null) { Debug.LogWarning("Live2DActor 未設定，無法顯示 Live2D 立繪。"); return; }

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

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            gameObject.SetActive(true);
            _live2DActor.SetOpacity(0f);
            _fadeRoutine = StartCoroutine(FadeTo(1f, fadeDuration));
        }

        public void Hide(float fadeDuration)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeTo(0f, fadeDuration));
        }

        public void HideImmediate()
        {
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
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

        public IEnumerator Flicker(float duration, float frequency, float minAlpha)
        {
            if (_live2DActor == null) yield break;

            float time = 0;
            float originalAlpha = _live2DActor.GetOpacity();

            while (time < duration)
            {
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)));
                _live2DActor.SetOpacity(alpha);
                time += Time.deltaTime;
                yield return null;
            }

            _live2DActor.SetOpacity(originalAlpha);
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (_live2DActor == null) yield break;

            gameObject.SetActive(true);
            float startAlpha = _live2DActor.GetOpacity();

            if (duration <= 0f)
            {
                _live2DActor.SetOpacity(targetAlpha);
                if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
                _fadeRoutine = null;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _live2DActor.SetOpacity(Mathf.Lerp(startAlpha, targetAlpha, t / duration));
                yield return null;
            }

            _live2DActor.SetOpacity(targetAlpha);
            if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
            _fadeRoutine = null;
        }
    }
}
