using System;
using System.Collections.Generic;
using SG.Dialogue.Events;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue.Managers
{
    
    /// <summary>
    /// 用於在 Inspector 中設定事件名稱與回應的配對。
    /// </summary>
    [Serializable]
    public struct AudioEventMapping
    {
        public string EventName;
        public UnityEvent<AudioRequest> Response;
    }
    
    /// <summary>
    /// DialogueAudioEventManager 負責管理對話系統中的音訊播放，包括背景音樂 (BGM) 和音效 (SFX)。
    /// 它透過監聽 AudioEvent 來回應音訊請求，實現了與對話節點的解耦。
    /// </summary>
    public class DialogueAudioEventManager : BaseEventManager<AudioEvent, AudioRequest, AudioEventMapping, UnityEvent<AudioRequest>>
    {
        protected override string GetEventName(AudioEventMapping mapping) => mapping.EventName;
        protected override UnityEvent<AudioRequest> GetResponse(AudioEventMapping mapping) => mapping.Response;
    }
}
