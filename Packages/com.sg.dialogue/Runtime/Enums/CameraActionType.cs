using System;

namespace SG.Dialogue.Enums
{
    /// <summary>
    /// 定義攝影機控制節點 (CameraControlNode) 可以執行的動作類型。
    /// </summary>
    [Serializable]
    public enum CameraActionType
    {
        /// <summary>
        /// 震動攝影機，常用於表現衝擊或緊張感。
        /// </summary>
        Shake,
        /// <summary>
        /// 縮放攝影機視野（改變正交攝影機的 Orthographic Size）。
        /// </summary>
        Zoom,
        /// <summary>
        /// 將攝影機平移到一個指定的世界座標位置。
        /// </summary>
        Pan,
        /// <summary>
        /// 將攝影機聚焦在一個指定的 Transform 物件上，並跟隨它。
        /// </summary>
        FocusOnTarget
    }
}
