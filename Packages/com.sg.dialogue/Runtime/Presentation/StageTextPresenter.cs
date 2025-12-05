using System.Collections;
using TMPro;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 在舞台中央顯示打字機效果文字的獨立呈現器。
    /// </summary>
    public class StageTextPresenter : MonoBehaviour
    {
        [Header("UI 參考")]
        [Tooltip("包含所有文字 UI 元素的根物件，方便統一顯示/隱藏。")]
        [SerializeField] private GameObject container;

        [Tooltip("用於顯示文字的 TextMeshProUGUI 元件。")]
        [SerializeField] private TextMeshProUGUI textLabel;

        // [Header("打字機效果")]
        // [Tooltip("每個字元出現的間隔時間（秒）。")]
        // [SerializeField] public float typingSpeed = 0.05f;

        private Coroutine _typewriterCoroutine;

        private float _typingSpeed;

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
        /// <param name="speed">打字機速度</param>
        public void ShowMessage(string message, float speed)
        {
            _typingSpeed = speed;
            if (string.IsNullOrEmpty(message))
            {
                Hide();
                return;
            }

            Debug.LogFormat("Showing stage text message: {0}", message);
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
            container.SetActive(false);
        }

        /// <summary>
        /// 執行打字機效果的協程。
        /// </summary>
        private IEnumerator TypewriterCoroutine(string message)
        {
            textLabel.text = "";
            foreach (char letter in message)
            {
                textLabel.text += letter;
                yield return new WaitForSeconds(_typingSpeed);
            }
            _typewriterCoroutine = null;
        }
    }
}
