using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// SpinePortraitConfig 描述了 Spine 立繪顯示所需的數據，包括模型 Prefab、Skin、動畫設定等。
    /// </summary>
    [System.Serializable]
    public class SpinePortraitConfig
    {
        [Tooltip("Spine 角色模型的 Prefab (應包含 SkeletonAnimation 或 SkeletonGraphic)，" +
                 "用於實例化 Spine 立繪。")]
        public GameObject modelPrefab;
        
        [Tooltip("指定要使用的 Skin 名稱。如果留空，則使用 Spine 模型的預設 Skin。")]
        public string skin;
        
        [Tooltip("立繪進場時播放的動畫名稱。")]
        public string enterAnimation = "idle";
        
        [Tooltip("進場動畫是否循環播放。")]
        public bool loop = true;
        
        [Tooltip("如果需要額外顯示動畫，可指定一個佇列動畫名稱。此動畫將在 enterAnimation 之後播放。")]
        public string queuedAnimation;
        
        [Tooltip("佇列動畫開始前的延遲時間（秒）。")]
        public float queuedAnimationDelay;
        
        [Tooltip("Spine 模型的 X 軸縮放比例。設定為 -1 可以水平翻轉立繪。")]
        public float scaleX = 1f;
    }
}
