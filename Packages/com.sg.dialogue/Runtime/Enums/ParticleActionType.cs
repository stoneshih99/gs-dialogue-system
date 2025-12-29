using System;

namespace SG.Dialogue.Enums
{
    [Serializable]
    public enum ParticleActionType
    {
        /// <summary>
        /// 播放粒子（生成並播放）
        /// </summary>
        Play,
        /// <summary>
        /// 停止粒子（銷毀或隱藏）
        /// </summary>
        Stop
    }
}
