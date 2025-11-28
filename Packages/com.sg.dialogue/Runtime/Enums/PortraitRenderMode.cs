using System;

namespace SG.Dialogue.Enums
{
    /// <summary>
    /// 定義角色立繪的渲染模式，以支援不同類型的視覺資源。
    /// </summary>
    [Serializable]
    public enum PortraitRenderMode
    {
        /// <summary>
        /// 不顯示任何立繪。
        /// </summary>
        None = 0,
        /// <summary>
        /// 使用靜態的 Sprite 圖片作為立繪。
        /// </summary>
        Sprite = 1,
        /// <summary>
        /// 使用 Spine 骨骼動畫作為立繪。
        /// </summary>
        Spine = 2,
        /// <summary>
        /// 使用 Live2D 模型作為立繪。
        /// </summary>
        Live2D = 3
    }
}
