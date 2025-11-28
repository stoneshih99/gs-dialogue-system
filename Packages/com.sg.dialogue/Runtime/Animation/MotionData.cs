using System;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace SG.Dialogue.Animation
{
    /// <summary>
    /// MotionData 儲存了單一 LitMotion 動畫的所有參數，用於定義一個可配置的動畫效果。
    /// </summary>
    [Serializable]
    public class MotionData
    {
        [Tooltip("要執行的動畫目標屬性，例如位置、旋轉、縮放或透明度。")]
        public MotionTargetProperty TargetProperty = MotionTargetProperty.Position;

        [Tooltip("動畫的目標值。對於 Vector3 屬性，這是最終值；對於 Alpha，通常使用 X 分量。")]
        public Vector3 EndValue = Vector3.zero;

        [Tooltip("動畫持續時間（秒）。")]
        public float Duration = 0.5f;

        [Tooltip("動畫的緩和曲線（Easing Function），決定動畫的速度變化。")]
        public Ease Ease = Ease.OutQuad;

        [Tooltip("動畫的循環類型，例如不循環、重新開始或來回播放。")]
        public MotionLoopType LoopType = MotionLoopType.None;

        [Tooltip("動畫的循環次數。0 表示不循環，-1 表示無限循環。")]
        public int Loops = 0;

        [Tooltip("動畫開始前的延遲時間（秒）。")]
        public float Delay = 0f;

        [Tooltip("如果為 true，則 EndValue 將被視為相對於當前值的變化量（例如，目標位置 = 當前位置 + EndValue）。")]
        public bool IsRelative = false;
    }
}
