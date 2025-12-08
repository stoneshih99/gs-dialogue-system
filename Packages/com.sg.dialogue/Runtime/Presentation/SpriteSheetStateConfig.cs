using System;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// NewSpriteSheetStateConfig 描述了 Sprite Sheet 動畫立繪顯示所需的數據。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpriteSheetStateConfig", menuName = "SG/Dialogue/Sprite Sheet State Config")]
    public class SpriteSheetStateConfig : ScriptableObject
    {
        [Tooltip("動畫的名稱，用於識別。")]
        public string animationName = "default";

        [Tooltip("組成動畫的所有 Sprite 影格。")]
        public Sprite[] frames;

    }
}
