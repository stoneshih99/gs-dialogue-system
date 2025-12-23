using System;
using SG.Dialogue.Enums;
using UnityEngine;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// AudioRequest 是一個類別，用於封裝一次音訊播放請求所需的所有資料。
    /// 它作為參數透過 AudioEvent 傳遞。
    /// </summary>
    [Serializable]
    public class AudioRequest : IEventRequest
    {
        public string EventName => "AudioEvent";

        /// <summary>
        /// 要執行的音訊動作類型（例如：播放BGM、停止BGM、播放SFX）。
        /// </summary>
        [Header("音訊動作類型")]
        public AudioActionType actionType;
        
        [Header("音訊名稱")]
        public string soundName;
        
        [Header("是否循環播放")]
        public bool loop;
        
        [Header("淡入或淡出持續時間（秒）")]
        [Tooltip("如果設定為 -1，表示使用音訊管理器的預設值。")]
        public float fadeDuration = -1f;
    }
}
