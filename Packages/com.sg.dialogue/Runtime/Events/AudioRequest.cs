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

        public AudioActionType ActionType;
        public string SoundName;
        public bool Loop;
        public float FadeDuration;

        public AudioRequest(AudioActionType actionType, string soundName, bool loop, float fadeDuration)
        {
            ActionType = actionType;
            SoundName = soundName;
            Loop = loop;
            FadeDuration = fadeDuration;
        }
        
        public AudioRequest() {}
    }
}
