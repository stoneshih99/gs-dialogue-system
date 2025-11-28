namespace SG.Dialogue.Animation
{
    /// <summary>
    /// 定義可以被 LitMotion 補間動畫庫控制的目標屬性。
    /// </summary>
    public enum MotionTargetProperty
    {
        /// <summary>
        /// 目標物件的位置。
        /// </summary>
        Position,
        /// <summary>
        /// 目標物件的旋轉。
        /// </summary>
        Rotation,
        /// <summary>
        /// 目標物件的縮放。
        /// </summary>
        Scale,
        /// <summary>
        /// 目標物件的透明度（通常作用於 CanvasGroup）。
        /// </summary>
        Alpha
    }

    /// <summary>
    /// 定義動畫的循環類型。
    /// </summary>
    public enum MotionLoopType
    {
        /// <summary>
        /// 動畫不循環，只播放一次。
        /// </summary>
        None,
        /// <summary>
        /// 動畫每次循環都從頭重新播放。
        /// </summary>
        Restart,
        /// <summary>
        /// 動畫來回播放（例如，從 A 到 B，再從 B 到 A）。
        /// </summary>
        Yoyo
    }
}
