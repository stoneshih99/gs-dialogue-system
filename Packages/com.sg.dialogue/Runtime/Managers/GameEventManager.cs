using System.Collections.Generic;
using SG.Dialogue.Events;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue.Managers
{
    /// <summary>
    /// 用於在 Inspector 中設定事件名稱與回應的配對。
    /// </summary>
    [System.Serializable]
    public struct GameEventMapping
    {
        [Tooltip("要回應的事件名稱，必須與 GameEventNode 中請求的 EventName 完全匹配。")]
        public string EventName;
        [Tooltip("當接收到對應的事件名稱時，要觸發的 UnityEvent。")]
        public UnityEvent<GameRequest> Response;
    }

    /// <summary>
    /// GameEventManager 是一個中央事件管理器。
    /// 它獨自監聽一個 GameEvent 通道，並根據收到的請求中的事件名稱，
    /// 觸發在 Inspector 中設定好的對應 UnityEvent。
    /// 這種模式統一了事件處理架構，使其與 DialogueAudioManager 保持一致。
    /// </summary>
    public class GameEventManager : BaseEventManager<GameEvent, GameRequest, GameEventMapping, UnityEvent<GameRequest>>
    {
        protected override string GetEventName(GameEventMapping mapping) => mapping.EventName;
        protected override UnityEvent<GameRequest> GetResponse(GameEventMapping mapping) => mapping.Response;
    }
}
