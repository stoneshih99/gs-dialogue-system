using System;
using System.Collections.Generic;
using SG.Dialogue.Conditions;
using SG.Dialogue.Nodes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SG.Dialogue.Presentation;

namespace SG.Dialogue.UI
{
    /// <summary>
    /// DialogueUIManager 負責管理對話系統的 UI 顯示，包括說話者、選項和面板可見性。
    /// 文字顯示邏輯已委派給 DialogueTextPresenter。
    /// </summary>
    public class DialogueUIManager : MonoBehaviour
    {
        [Header("UI 介面參考")]
        [Tooltip("對話 UI 的根面板")]
        [SerializeField] private GameObject rootPanel;
        [Tooltip("顯示說話者名稱的 TextMeshProUGUI")]
        [SerializeField] private TextMeshProUGUI speakerLabel;
        [Tooltip("對話文本呈現器")]
        [SerializeField] private DialogueTextPresenter dialogueTextPresenter;

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

        /// <summary>
        /// 當前是否正在進行打字機效果。
        /// </summary>
        public bool IsTyping => dialogueTextPresenter != null && dialogueTextPresenter.IsTyping;
        
        private void Awake()
        {
            if (dialogueTextPresenter == null)
            {
                dialogueTextPresenter = GetComponentInChildren<DialogueTextPresenter>();
            }
            if (dialogueTextPresenter != null)
            {
                dialogueTextPresenter.OnTypingCompleted += HandleTypingCompleted;
            }
            
            if (nextButton != null) nextButton.onClick.AddListener(HandleNextClick);
            if (skipButton != null) skipButton.onClick.AddListener(() => OnSkipRequested?.Invoke());
            if (fullscreenAdvanceButton != null) fullscreenAdvanceButton.onClick.AddListener(HandleFullscreenClick);
            SetPanelVisibility(false);
        }

        private void OnDestroy()
        {
            if (dialogueTextPresenter != null)
            {
                dialogueTextPresenter.OnTypingCompleted -= HandleTypingCompleted;
            }
        }

        private void HandleTypingCompleted()
        {
            OnTypingCompleted?.Invoke();
        }

        private void HandleNextClick()
        {
            if (IsTyping)
            {
                dialogueTextPresenter.SkipTypewriter();
            }
            else
            {
                OnAdvanceRequested?.Invoke();
            }
        }

        private void HandleFullscreenClick()
        {
            if (clickAnywhereToAdvance) HandleNextClick();
        }


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

        public void SetSkipButtonVisibility(bool visible)
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(visible);
            }
        }

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

            if (dialogueTextPresenter != null)
            {
                dialogueTextPresenter.ShowText(text, node.textCues);
            }
            else
            {
                Debug.LogWarning("DialogueTextPresenter not assigned or found in DialogueUIManager.");
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(!string.IsNullOrEmpty(node.nextNodeId));
            }
            ClearChoices();
        }

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

        public void ClearChoices()
        {
            if (choicesRoot == null) return;
            choicesRoot.gameObject.SetActive(false);
            for (int i = choicesRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(choicesRoot.GetChild(i).gameObject);
            }
        }

        public void CompleteTyping()
        {
            if (dialogueTextPresenter != null)
            {
                dialogueTextPresenter.SkipTypewriter();
            }
        }
    }
}
