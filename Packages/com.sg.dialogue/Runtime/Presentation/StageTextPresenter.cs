using System.Collections;
using TMPro;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 在舞台中央顯示打字機效果文字的獨立呈現器。
    /// <para><b>重要設定：</b>為了讓文字置中並從中間向兩側展開，請在 TextMeshProUGUI 元件的 Inspector 中將 Alignment 設為 Center。</para>
    /// </summary>
    public class StageTextPresenter : MonoBehaviour
    {
        [Header("UI 參考")]
        [Tooltip("包含所有文字 UI 元素的根物件，方便統一顯示/隱藏。")]
        [SerializeField] private GameObject container;

        [Tooltip("用於顯示文字的 TextMeshProUGUI 元件。")]
        [SerializeField] private TextMeshProUGUI textLabel;

        private Coroutine _typewriterCoroutine;

        private float _typingSpeed;
        
        /// <summary>
        /// 查詢打字機效果是否正在進行中。
        /// </summary>
        public bool IsTyping => _typewriterCoroutine != null;

        private void Awake()
        {
            if (container == null)
            {
                container = gameObject; // 如果未指定，則使用自身作為容器
            }
            if (textLabel == null)
            {
                textLabel = GetComponentInChildren<TextMeshProUGUI>();
            }
            container.SetActive(false); // 初始狀態為隱藏
        }

        /// <summary>
        /// 以打字機效果顯示一條訊息。
        /// </summary>
        /// <param name="message">要顯示的最終文字內容。</param>
        /// <param name="speed">每個字出現的間隔時間（秒）。</param>
        public void ShowMessage(string message, float speed)
        {
            _typingSpeed = speed;
            if (string.IsNullOrEmpty(message))
            {
                Hide();
                return;
            }

            container.SetActive(true);
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(message));
        }

        /// <summary>
        /// 隱藏文字容器。
        /// </summary>
        public void Hide()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            if (container != null)
            {
                container.SetActive(false);
            }
        }

        /// <summary>
        /// 執行打字機效果的協程。
        /// 使用 maxVisibleCharacters 屬性來實現從中間展開的打字效果。
        /// </summary>
        private IEnumerator TypewriterCoroutine(string message)
        {
            // 1. 先設定好完整文字
            textLabel.text = message;
            // 2. 強制更新幾何，確保 textInfo 是最新的
            textLabel.ForceMeshUpdate();
            
            int totalVisibleCharacters = textLabel.textInfo.characterCount;
            int visibleCount = 0;

            while (visibleCount <= totalVisibleCharacters)
            {
                textLabel.maxVisibleCharacters = visibleCount;
                visibleCount++;
                yield return new WaitForSeconds(_typingSpeed);
            }

            _typewriterCoroutine = null;
        }
    }
}
