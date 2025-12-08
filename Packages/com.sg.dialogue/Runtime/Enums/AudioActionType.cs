using System;

namespace SG.Dialogue.Enums
{
    /// <summary>
    /// 定義音訊節點 (PlayAudioNode) 可以執行的動作類型。
    /// </summary>
    [Serializable]
    public enum AudioActionType
    {
        /// <summary>
        /// 播放背景音樂 (BGM)。
        /// </summary>
        PlayBGM,
        /// <summary>
        /// 停止背景音樂 (BGM)。
        /// </summary>
        StopBGM,
        /// <summary>
        /// 播放一次性的音效 (SFX)。
        /// </summary>
        PlaySFX
    }
}
