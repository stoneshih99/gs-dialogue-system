using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// Live2D 立繪的設定。
    /// </summary>
    [CreateAssetMenu(fileName = "Live2DPortraitConfig", menuName = "SG/Dialogue/Live2D Portrait Config")]
    public class Live2DPortraitConfig : ScriptableObject
    {
        [Tooltip("進入動畫的動作 ID。")]
        public string enterAnimation;

        [Tooltip("進入動畫是否循環。")]
        public bool loop;

        [Tooltip("佇列動畫的動作 ID。")]
        public string queuedAnimation;

        [Tooltip("佇列動畫的延遲時間。")]
        public float queuedAnimationDelay;

        [Tooltip("表情 ID。")]
        public string expression;

        [Tooltip("水平縮放（用於翻轉）。")]
        public float scaleX = 1f;
    }
}
