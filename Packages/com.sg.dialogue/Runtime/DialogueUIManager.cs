using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SG.Dialogue.Animation;
using SG.Dialogue.Conditions;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using SG.Dialogue.Nodes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Dialogue.UI
{
    /// <summary>
    /// DialogueUIManager 負責管理對話系統的 UI 顯示，包括文本、說話者、選項和打字機效果。
    /// </summary>
    public class DialogueUIManager : MonoBehaviour
    {
        [Header("UI 介面參考")]
        [Tooltip("對話 UI 的根面板")]
        [SerializeField] private GameObject rootPanel;
        [Tooltip("顯示說話者名稱的 TextMeshProUGUI")]
        [SerializeField] private TextMeshProUGUI speakerLabel;
        [Tooltip("顯示對話文本的 TextMeshProUGUI")]
        [SerializeField] private TextMeshProUGUI bodyLabel;
        [Tooltip("前進到下一句對話的按鈕")]
        [SerializeField] private Button nextButton;
        [Tooltip("跳過對話的按鈕")]
        [SerializeField] private Button skipButton;
        [Tooltip("全螢幕點擊前進的透明按鈕")]
        [SerializeField] private Button fullscreenAdvanceButton;

        [Header("選項")]
        [Tooltip("選項按鈕的根容器")]
        [SerializeField] private RectTransform choicesRoot;
        [Tooltip("選項按鈕的 Prefab")]
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("打字機效果")]
        [Tooltip("每秒顯示的字元數（打字機效果）")]
        [SerializeField] private float charsPerSecond = 30f;
        [Tooltip("是否啟用打字機效果")]
        [SerializeField] private bool enableTypewriter = true;

        [Header("打字機音效")]
        [Tooltip("是否啟用打字機音效")]
        [SerializeField] private bool enableTypewriterSound = true;
        [Tooltip("每隔幾個有效字元播放一次音效")]
        [SerializeField] private int soundInterval = 3;
        [Tooltip("用於觸發打字音效的事件通道")]
        [SerializeField] private AudioEvent typewriterAudioEvent;
        [Tooltip("要播放的打字音效")]
        [SerializeField] private AudioClip typewriterSfx;

        [Header("行為")]
        [Tooltip("是否使用 CanvasGroup 來隱藏/顯示根面板（影響互動性）")]
        [SerializeField] private bool hideRootWithCanvasGroup = true;
        [Tooltip("是否允許點擊螢幕任意位置來前進對話")]
        [SerializeField] private bool clickAnywhereToAdvance = true;

        /// <summary>
        /// 當請求前進到下一句對話時觸發。
        /// </summary>
        public event Action OnAdvanceRequested;
        /// <summary>
        /// 當選擇一個選項時觸發。
        /// </summary>
        public event Action<DialogueChoice> OnChoiceSelected;
        /// <summary>
        /// 當打字機效果完成時觸發。
        /// </summary>
        public event Action OnTypingCompleted;
        /// <summary>
        /// 當請求跳過對話時觸發。
        /// </summary>
        public event Action OnSkipRequested;

        private DialogueTextAnimator _textAnimator; // 文本動畫器
        private Coroutine _typingRoutine; // 打字機協程
        private bool _isTyping; // 是否正在打字
        private List<TextCue> _currentCues; // 當前文本的所有提示點
        private int _nextCueIndex; // 下一個要觸發的提示點索引

        // 打字機協程的共享變數
        private int _soundCharacterCount; // 用於計算音效間隔的字元計數
        private int _visibleCharIndex; // 當前可見的字元索引

        /// <summary>
        /// 當前是否正在進行打字機效果。
        /// </summary>
        public bool IsTyping => _isTyping;

        private void Awake()
        {
            if (bodyLabel != null)
            {
                _textAnimator = bodyLabel.GetComponent<DialogueTextAnimator>();
                if (_textAnimator == null)
                {
                    _textAnimator = bodyLabel.gameObject.AddComponent<DialogueTextAnimator>();
                }
            }
            
            if (nextButton != null) nextButton.onClick.AddListener(HandleNextClick);
            if (skipButton != null) skipButton.onClick.AddListener(() => OnSkipRequested?.Invoke());
            if (fullscreenAdvanceButton != null) fullscreenAdvanceButton.onClick.AddListener(HandleFullscreenClick);
            SetPanelVisibility(false); // 初始隱藏面板
        }

        /// <summary>
        /// 處理「下一步」按鈕的點擊事件。
        /// </summary>
        private void HandleNextClick()
        {
            if (_isTyping) CompleteTyping(); // 如果正在打字，則立即完成
            else OnAdvanceRequested?.Invoke(); // 否則請求前進到下一句
        }

        /// <summary>
        /// 處理全螢幕點擊事件。
        /// </summary>
        private void HandleFullscreenClick()
        {
            if (clickAnywhereToAdvance) HandleNextClick();
        }

        /// <summary>
        /// 設定對話面板的可見性。
        /// </summary>
        /// <param name="visible">是否可見。</param>
        public void SetPanelVisibility(bool visible)
        {
            if (rootPanel == null) return;
            if (hideRootWithCanvasGroup)
            {
                var cg = rootPanel.GetComponent<CanvasGroup>() ?? rootPanel.AddComponent<CanvasGroup>();
                cg.alpha = visible ? 1f : 0f;
                cg.interactable = visible;
                cg.blocksRaycasts = visible;
            }
            else
            {
                rootPanel.SetActive(visible);
            }
        }

        /// <summary>
        /// 設定跳過按鈕的可見性。
        /// </summary>
        /// <param name="visible">是否可見。</param>
        public void SetSkipButtonVisibility(bool visible)
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 顯示指定的文本節點內容。
        /// </summary>
        /// <param name="node">文本節點。</param>
        /// <param name="text">要顯示的文本。</param>
        public void ShowText(TextNode node, string text)
        {
            if (speakerLabel != null)
            {
                bool hasSpeaker = !string.IsNullOrEmpty(node.speakerName);
                speakerLabel.gameObject.SetActive(hasSpeaker);
                if (hasSpeaker)
                {
                    speakerLabel.text = node.speakerName;
                }
            }

            _currentCues = (node.textCues != null && node.textCues.Count > 0) 
                ? new List<TextCue>(node.textCues) 
                : null;
            _currentCues?.Sort((a, b) => a.charIndex.CompareTo(b.charIndex));
            _nextCueIndex = 0;

            if (_typingRoutine != null) StopCoroutine(_typingRoutine);

            if (_textAnimator != null)
            {
                _textAnimator.Animate(text);
                if (enableTypewriter && gameObject.activeInHierarchy)
                {
                    _typingRoutine = StartCoroutine(TypewriterRoutine(bodyLabel.text));
                }
                else
                {
                    CompleteTyping();
                }
            }
            else
            {
                if (bodyLabel != null) bodyLabel.text = text;
                _isTyping = false;
                TriggerRemainingCues(text?.Length ?? 0);
                OnTypingCompleted?.Invoke();
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(!string.IsNullOrEmpty(node.nextNodeId));
            }
            ClearChoices();
        }

        /// <summary>
        /// 顯示指定的選項節點內容。
        /// </summary>
        /// <param name="node">選項節點。</param>
        /// <param name="conditionChecker">用於檢查選項條件的函式。</param>
        public void ShowChoices(ChoiceNode node, Func<Condition, bool> conditionChecker)
        {
            ClearChoices();
            if (choicesRoot == null || choiceButtonPrefab == null || node?.choices == null) return;
            
            choicesRoot.gameObject.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(false);
            if (skipButton != null) skipButton.gameObject.SetActive(false);

            foreach (var choice in node.choices)
            {
                if (choice.condition != null && !conditionChecker(choice.condition)) continue;
                var go = Instantiate(choiceButtonPrefab, choicesRoot);
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = choice.text;
                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    var selectedChoice = choice;
                    btn.onClick.AddListener(() => OnChoiceSelected?.Invoke(selectedChoice));
                }
            }
        }

        /// <summary>
        /// 清除所有選項按鈕。
        /// </summary>
        public void ClearChoices()
        {
            if (choicesRoot == null) return;
            choicesRoot.gameObject.SetActive(false);
            for (int i = choicesRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(choicesRoot.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 立即完成打字機效果。
        /// </summary>
        private void CompleteTyping()
        {
            if (!_isTyping && _typingRoutine == null) return;
            _isTyping = false;
            if (_typingRoutine != null) { StopCoroutine(_typingRoutine); _typingRoutine = null; }
            
            if (bodyLabel != null)
            {
                bodyLabel.maxVisibleCharacters = int.MaxValue;
                TriggerRemainingCues(bodyLabel.textInfo.characterCount);
            }
            OnTypingCompleted?.Invoke();
        }

        /// <summary>
        /// 執行打字機效果的協程。
        /// </summary>
        /// <param name="originalText">原始文本。</param>
        private IEnumerator TypewriterRoutine(string originalText)
        {
            _isTyping = true;
            if (bodyLabel == null)
            {
                _isTyping = false;
                yield break;
            }

            bodyLabel.maxVisibleCharacters = 0;
            _nextCueIndex = 0;
            yield return new WaitForEndOfFrame();

            float baseDelay = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;
            float currentDelay = baseDelay;
            _soundCharacterCount = 0;
            _visibleCharIndex = 0;

            for (int i = 0; i < originalText.Length; i++)
            {
                if (!_isTyping) yield break;

                // 檢查 Rich Text 標籤
                if (originalText[i] == '<')
                {
                    // 嘗試解析速度標籤 <speed=...></speed>
                    Match speedMatch = Regex.Match(originalText.Substring(i), @"<speed=([0-9\.]+)>(.*?)<\/speed>");
                    if (speedMatch.Success && speedMatch.Index == 0)
                    {
                        float speedMultiplier = float.Parse(speedMatch.Groups[1].Value);
                        string content = speedMatch.Groups[2].Value;
                        
                        // 處理標籤內的文本
                        currentDelay = baseDelay / speedMultiplier;
                        for (int j = 0; j < content.Length; j++)
                        {
                            yield return ProcessCharacter(content[j], currentDelay);
                        }
                        
                        // 恢復預設速度並跳過已處理的文本
                        currentDelay = baseDelay;
                        i += speedMatch.Length - 1;
                        continue;
                    }
                    
                    // 跳過其他 Rich Text 標籤
                    Match tagMatch = Regex.Match(originalText.Substring(i), @"<.*?>");
                    if (tagMatch.Success && tagMatch.Index == 0)
                    {
                        i += tagMatch.Length - 1;
                        continue;
                    }
                }

                // 處理普通字元
                yield return ProcessCharacter(originalText[i], currentDelay);
            }

            _isTyping = false;
            _typingRoutine = null;
            TriggerRemainingCues(_visibleCharIndex);
            OnTypingCompleted?.Invoke();
        }

        /// <summary>
        /// 處理單個字元的顯示和音效。
        /// </summary>
        /// <param name="character">要處理的字元。</param>
        /// <param name="delay">顯示下一個字元前的延遲。</param>
        private IEnumerator ProcessCharacter(char character, float delay)
        {
            bodyLabel.maxVisibleCharacters = _visibleCharIndex + 1;
            TriggerCuesUpToIndex(_visibleCharIndex);

            if (!char.IsWhiteSpace(character) && !char.IsPunctuation(character))
            {
                _soundCharacterCount++;
                if (_soundCharacterCount % soundInterval == 0)
                {
                    PlayTypewriterSound();
                }
            }
            
            _visibleCharIndex++;
            if (delay > 0f) yield return new WaitForSeconds(delay);
        }

        /// <summary>
        /// 播放打字機音效。
        /// </summary>
        private void PlayTypewriterSound()
        {
            if (enableTypewriterSound && typewriterAudioEvent != null && typewriterSfx != null)
            {
                var request = new AudioRequest
                {
                    ActionType = AudioActionType.PlaySFX,
                    Clip = typewriterSfx
                };
                typewriterAudioEvent.Raise(request);
            }
        }

        /// <summary>
        /// 觸發直到指定字元索引的所有提示點。
        /// </summary>
        /// <param name="charIndex">當前的字元索引。</param>
        private void TriggerCuesUpToIndex(int charIndex)
        {
            if (_currentCues == null) return;
            while (_nextCueIndex < _currentCues.Count && _currentCues[_nextCueIndex].charIndex <= charIndex)
            {
                _currentCues[_nextCueIndex]?.onTrigger?.Invoke();
                _nextCueIndex++;
            }
        }

        /// <summary>
        /// 觸發所有剩餘的提示點。
        /// </summary>
        /// <param name="totalLength">文本總長度。</param>
        private void TriggerRemainingCues(int totalLength)
        {
            if (_currentCues == null) return;
            while (_nextCueIndex < _currentCues.Count)
            {
                var cue = _currentCues[_nextCueIndex];
                if (cue?.onTrigger != null && cue.charIndex < totalLength) cue.onTrigger.Invoke();
                _nextCueIndex++;
            }
        }
    }
}
