using UnityEngine;
using TMPro;

namespace SG.Dialogue.Profiles
{
    [CreateAssetMenu(fileName = "NewDialogueStyle", menuName = "SG/Dialogue/Style Profile")]
    public class DialogueStyleProfile : ScriptableObject
    {
        [Header("Panel Settings")]
        [Tooltip("對話框背景圖片")]
        public Sprite panelBackground;
        [Tooltip("對話框顏色濾鏡")]
        public Color panelColor = Color.white;

        [Header("Name Label Settings")]
        [Tooltip("名字標籤背景圖片")]
        public Sprite nameLabelBackground;
        [Tooltip("名字標籤顏色濾鏡")]
        public Color nameLabelColor = Color.white;
        [Tooltip("名字文字顏色")]
        public Color nameTextColor = Color.black;
        [Tooltip("名字文字字體 (Optional)")]
        public TMP_FontAsset nameTextFont;

        [Header("Content Text Settings")]
        [Tooltip("內容文字顏色")]
        public Color contentTextColor = Color.white;
        [Tooltip("內容文字字體 (Optional)")]
        public TMP_FontAsset contentTextFont;
        [Tooltip("文字大小 (0 = 使用預設)")]
        public float contentTextSize = 0;
    }
}
