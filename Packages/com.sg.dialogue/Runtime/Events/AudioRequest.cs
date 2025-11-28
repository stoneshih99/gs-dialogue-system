using System;
using SG.Dialogue.Enums;
using UnityEngine;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// AudioRequest 是一個結構 (struct)，用於封裝一次音訊播放請求所需的所有資料。
    /// 它作為參數透過 AudioEvent 傳遞。
    /// </summary>
    [Serializable]
    public struct AudioRequest
    {
        /// <summary>
        /// 要執行的音訊動作類型（例如：播放BGM、停止BGM、播放SFX）。
        /// </summary>
        public AudioActionType ActionType;
        
        /// <summary>
        /// 要播放的音訊片段 (AudioClip)。
        /// </summary>
        public AudioClip Clip;
        
        /// <summary>
        /// 是否循環播放（主要用於 BGM）。
        /// </summary>
        public bool Loop;
        
        /// <summary>
        /// 淡入或淡出的持續時間（秒）。
        /// 如果設定為 -1，表示使用音訊管理器的預設值。
        /// </summary>
        public float FadeDuration;

        public override string ToString()
        {
            return $"ActionType: {ActionType}, Clip: {Clip}, Loop: {Loop}, FadeDuration: {FadeDuration}";
        }
    }
}
