using System;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Animation;
using TMPro;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 文字呈現器的基底類別，封裝了通用的打字機效果邏輯 (UniTask 版本)。
    /// </summary>
    public abstract class BaseTextPresenter : MonoBehaviour
    {
        [Header("UI 參考")]
        [Tooltip("用於顯示內容的 TextMeshProUGUI 元件。")]
        [SerializeField] protected TextMeshProUGUI textLabel;

        [Header("打字機設定")]
        [Tooltip("每秒顯示的字元數。")]
        [SerializeField] protected float charsPerSecond = 30f;
        [Tooltip("是否啟用打字機效果。")]
        [SerializeField] protected bool enableTypewriter = true;

        /// <summary>
        /// 當打字機效果完成時觸發。
        /// </summary>
        public event Action OnTypingCompleted;

        protected DialogueTextAnimator textAnimator;
        protected bool isTyping;
        
        // 用於取消打字機任務
        protected CancellationTokenSource _typingCts;
        
        // 打字機狀態
        protected int visibleCharIndex;

        public bool IsTyping => isTyping;

        public void SetTextColor(Color color)
        {
            if (textLabel != null) textLabel.color = color;
        }

        public void SetFont(TMP_FontAsset font)
        {
            if (textLabel != null && font != null) textLabel.font = font;
        }
        
        public void SetFontSize(float size)
        {
            if (textLabel != null && size > 0) textLabel.fontSize = size;
        }

        protected virtual void Awake()
        {
            if (textLabel == null)
            {
                textLabel = GetComponent<TextMeshProUGUI>();
            }
            if (textLabel != null)
            {
                textAnimator = textLabel.GetComponent<DialogueTextAnimator>();
                if (textAnimator == null)
                {
                    textAnimator = textLabel.gameObject.AddComponent<DialogueTextAnimator>();
                }
            }
        }

        protected virtual void OnDestroy()
        {
            CancelTyping();
        }

        /// <summary>
        /// 顯示文字。
        /// </summary>
        /// <param name="text">要顯示的文字。</param>
        /// <param name="speedOverride">可選的速度覆寫（<=0 使用預設值）。</param>
        public virtual void ShowText(string text, float speedOverride = -1f)
        {
            if (textLabel == null) return;

            CancelTyping();

            // 處理文字動畫
            if (textAnimator != null)
            {
                textAnimator.Animate(text);
            }
            else
            {
                textLabel.text = text;
            }

            float speed = speedOverride > 0 ? speedOverride : charsPerSecond;

            if (enableTypewriter && gameObject.activeInHierarchy && speed > 0)
            {
                _typingCts = new CancellationTokenSource();
                TypewriterRoutine(textLabel.text, speed, _typingCts.Token).Forget();
            }
            else
            {
                // 立即顯示
                textLabel.maxVisibleCharacters = int.MaxValue;
                isTyping = false;
                OnTypingCompleteInternal();
            }
        }

        /// <summary>
        /// 跳過打字機效果。
        /// </summary>
        public virtual void SkipTypewriter()
        {
            if (!isTyping) return;

            CancelTyping();
            isTyping = false;

            if (textLabel != null)
            {
                textLabel.maxVisibleCharacters = int.MaxValue;
            }
            OnTypingCompleteInternal();
        }

        public virtual void Clear()
        {
            if (textLabel != null) textLabel.text = "";
            isTyping = false;
            CancelTyping();
        }

        protected void CancelTyping()
        {
            if (_typingCts != null)
            {
                _typingCts.Cancel();
                _typingCts.Dispose();
                _typingCts = null;
            }
        }

        protected virtual async UniTaskVoid TypewriterRoutine(string originalText, float speed, CancellationToken token)
        {
            isTyping = true;
            textLabel.maxVisibleCharacters = 0;
            visibleCharIndex = 0;

            await UniTask.Yield(PlayerLoopTiming.Update, token); // 等待一幀以確保 TMPro 更新

            float baseDelay = 1f / speed;
            float currentDelay = baseDelay;

            for (int i = 0; i < originalText.Length; i++)
            {
                if (token.IsCancellationRequested) return;

                // 處理 Rich Text 標籤 (包含 <speed>)
                if (originalText[i] == '<')
                {
                    // 檢查 <speed> 標籤
                    Match speedMatch = Regex.Match(originalText.Substring(i), @"<speed=([0-9\.]+)>(.*?)<\/speed>");
                    if (speedMatch.Success && speedMatch.Index == 0)
                    {
                        float multiplier = float.Parse(speedMatch.Groups[1].Value);
                        string content = speedMatch.Groups[2].Value;
                        currentDelay = baseDelay / multiplier;

                        for (int j = 0; j < content.Length; j++)
                        {
                            await ProcessCharacter(content[j], currentDelay, token);
                        }
                        
                        currentDelay = baseDelay;
                        i += speedMatch.Length - 1;
                        continue;
                    }

                    // 跳過其他標籤
                    Match tagMatch = Regex.Match(originalText.Substring(i), @"<.*?>");
                    if (tagMatch.Success && tagMatch.Index == 0)
                    {
                        i += tagMatch.Length - 1;
                        continue;
                    }
                }

                await ProcessCharacter(originalText[i], currentDelay, token);
            }

            isTyping = false;
            _typingCts = null; // 任務自然結束，不需要 Cancel
            OnTypingCompleteInternal();
        }

        protected virtual async UniTask ProcessCharacter(char character, float delay, CancellationToken token)
        {
            textLabel.maxVisibleCharacters = visibleCharIndex + 1;
            
            // 讓子類別處理額外邏輯（如音效、Cues）
            OnCharacterTyped(character, visibleCharIndex);

            visibleCharIndex++;
            if (delay > 0f)
            {
                // 使用 Delay，並傳入 cancellationToken
                await UniTask.Delay(TimeSpan.FromSeconds(delay), ignoreTimeScale: false, cancellationToken: token);
            }
        }

        /// <summary>
        /// 當一個字元被顯示出來時呼叫。子類別可覆寫此方法來添加音效或觸發事件。
        /// </summary>
        protected virtual void OnCharacterTyped(char character, int index) { }

        /// <summary>
        /// 當打字機完成（或被跳過）時呼叫。
        /// </summary>
        protected virtual void OnTypingCompleteInternal()
        {
            OnTypingCompleted?.Invoke();
        }
    }
}
