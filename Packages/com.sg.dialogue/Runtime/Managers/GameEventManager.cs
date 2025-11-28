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
    public class GameEventManager : MonoBehaviour
    {
        [Header("事件通道")]
        [Tooltip("要監聽的 GameEvent 事件通道。")]
        [SerializeField] private GameEvent gameEventChannel;

        [Header("事件映射")]
        [Tooltip("設定事件名稱與對應的回應。")]
        [SerializeField] private List<GameEventMapping> eventMappings;

        // 用於快速查找的字典
        private Dictionary<string, UnityEvent<GameRequest>> _eventDictionary;

        private void Awake()
        {
            // 將 Inspector 中的列表轉換為字典，以提高執行時的查找效率。
            _eventDictionary = new Dictionary<string, UnityEvent<GameRequest>>();
            foreach (var mapping in eventMappings)
            {
                if (!string.IsNullOrEmpty(mapping.EventName) && !_eventDictionary.ContainsKey(mapping.EventName))
                {
                    _eventDictionary.Add(mapping.EventName, mapping.Response);
                }
            }
        }

        private void OnEnable()
        {
            if (gameEventChannel != null)
            {
                gameEventChannel.RegisterListener(OnGameRequest);
            }
        }

        private void OnDisable()
        {
            if (gameEventChannel != null)
            {
                gameEventChannel.UnregisterListener(OnGameRequest);
            }
        }

        /// <summary>
        /// 接收到事件請求時的回呼。
        /// </summary>
        private void OnGameRequest(GameRequest request)
        {
            Debug.Log($"[GameEventManager] 收到事件請求: {request.EventName}");

            // 嘗試從字典中找到對應的 UnityEvent 並觸發它。
            if (_eventDictionary.TryGetValue(request.EventName, out UnityEvent<GameRequest> response))
            {
                response?.Invoke(request);
            }
            else
            {
                Debug.LogWarning($"[GameEventManager] 找不到針對事件 '{request.EventName}' 的回應設定。");
            }
        }
    }
}
