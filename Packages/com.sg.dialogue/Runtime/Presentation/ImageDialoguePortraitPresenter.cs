using System.Threading;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Animation;
using SG.Dialogue.Utils;
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
        private LitMotionPlayer _motionPlayer;
        private CancellationTokenSource _fadeCts;

        public float Alpha
        {
            get => targetImage != null ? targetImage.color.a : 0f;
            set
            {
                if (targetImage != null)
                {
                    var c = targetImage.color;
                    c.a = value;
                    targetImage.color = c;
                }
            }
        }

        private void Awake()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            _motionPlayer = GetComponent<LitMotionPlayer>();
        }

        private void OnDestroy()
        {
            CancelFade();
        }

        public UniTask ShowSprite(Sprite sprite, float fadeDuration)
        {
            if (targetImage == null) return UniTask.CompletedTask;
            
            CancelFade();

            targetImage.sprite = sprite;
            targetImage.enabled = sprite != null;
            
            _fadeCts = new CancellationTokenSource();
            return UIAnimationUtils.FadeImage(targetImage, 1f, fadeDuration, _fadeCts.Token);
        }

        public UniTask ShowSpine(SpinePortraitConfig config, float fadeDuration)
        {
            Debug.LogWarning("ImageDialoguePortraitPresenter does not support Spine. Hiding portrait.");
            HideImmediate();
            return UniTask.CompletedTask;
        }

        public UniTask ShowSpriteSheet(string animationName, float fadeDuration)
        {
            Debug.LogWarning("ImageDialoguePortraitPresenter does not support Sprite Sheets. Hiding portrait.");
            HideImmediate();
            return UniTask.CompletedTask;
        }

        public UniTask Hide(float fadeDuration)
        {
            if (targetImage == null) return UniTask.CompletedTask;
            
            CancelFade();
            _fadeCts = new CancellationTokenSource();
            var task = UIAnimationUtils.FadeImage(targetImage, 0f, fadeDuration, _fadeCts.Token);
            task.ContinueWith(() =>
            {
                if (targetImage != null)
                {
                    targetImage.enabled = false;
                    targetImage.sprite = null;
                }
            });
            return task;
        }

        public void HideImmediate()
        {
            if (targetImage == null) return;
            CancelFade();
            targetImage.enabled = false;
            targetImage.sprite = null;
        }

        public void PlayMotion(MotionData data)
        {
            if (_motionPlayer != null) _motionPlayer.Play(data);
            else Debug.LogWarning("ImageDialoguePortraitPresenter: LitMotionPlayer component not found.", this);
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (targetImage == null) return;
            Color targetColor = isHighlighted ? Color.white : Color.gray;
            targetColor.a = targetImage.color.a;
            targetImage.color = targetColor;
        }

        public async UniTask Flicker(float duration, float frequency, float minAlpha)
        {
            if (targetImage == null) return;

            var token = this.GetCancellationTokenOnDestroy();
            float time = 0;
            float originalAlpha = targetImage.color.a;
            Color color = targetImage.color;

            while (time < duration)
            {
                if (token.IsCancellationRequested) return;
                
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)));
                color.a = alpha;
                targetImage.color = color;
                time += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            color.a = originalAlpha;
            targetImage.color = color;
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
    }
}
