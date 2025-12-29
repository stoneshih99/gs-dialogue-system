using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 在舞台中央顯示打字機效果文字的獨立呈現器。
    /// <para><b>重要設定：</b>為了讓文字置中並從中間向兩側展開，請在 TextMeshProUGUI 元件的 Inspector 中將 Alignment 設為 Center。</para>
    /// </summary>
    public class StageTextPresenter : BaseTextPresenter
    {
        [Header("容器")]
        [Tooltip("包含所有文字 UI 元素的根物件。")]
        [SerializeField] private GameObject container;

        protected override void Awake()
        {
            base.Awake();
            if (container == null) container = gameObject;
            container.SetActive(false);
        }

        /// <summary>
        /// 以打字機效果顯示一條訊息。
        /// </summary>
        /// <param name="message">要顯示的訊息。</param>
        /// <param name="interval">每個字元出現的間隔時間（秒）。</param>
        public void ShowMessage(string message, float interval)
        {
            if (string.IsNullOrEmpty(message))
            {
                Hide();
                return;
            }
            container.SetActive(true);
            
            // 將間隔時間轉換為每秒字元數
            float charsPerSecond = interval > 0 ? 1f / interval : 0;
            base.ShowText(message, charsPerSecond);
        }

        public void Hide()
        {
            base.Clear();
            if (container != null) container.SetActive(false);
        }
    }
}
