using System;
using System.Collections.Generic;
using SG.Dialogue.Events;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue.Managers
{
    public abstract class BaseEventManager<TChannel, TRequest, TMapping, TResponse> : MonoBehaviour
        where TChannel : BaseEventChannel<TRequest>
        where TRequest : class, IEventRequest
        where TMapping : struct
        where TResponse : UnityEvent<TRequest>
    {
        [Header("事件通道")]
        [Tooltip("要監聽的事件通道。")]
        [SerializeField] protected TChannel eventChannel;

        [Header("事件映射")]
        [Tooltip("設定事件名稱與對應的回應列表。")]
        [SerializeField] protected List<TMapping> eventMappings;

        private Dictionary<string, TResponse> _eventDictionary;

        protected abstract string GetEventName(TMapping mapping);
        protected abstract TResponse GetResponse(TMapping mapping);
        
        protected virtual void Awake()
        {
            _eventDictionary = new Dictionary<string, TResponse>();
            if (eventMappings == null) return;
            
            foreach (var mapping in eventMappings)
            {
                string eventName = GetEventName(mapping);
                if (!string.IsNullOrEmpty(eventName) && !_eventDictionary.ContainsKey(eventName))
                {
                    _eventDictionary.Add(eventName, GetResponse(mapping));
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (eventChannel != null)
            {
                eventChannel.RegisterListener(OnRequest);
            }
        }

        protected virtual void OnDisable()
        {
            if (eventChannel != null)
            {
                eventChannel.UnregisterListener(OnRequest);
            }
        }

        private void OnRequest(TRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.EventName)) return;

            Debug.Log($"[{GetType().Name}] 收到事件請求: {request.EventName}");

            if (_eventDictionary.TryGetValue(request.EventName, out TResponse response))
            {
                response?.Invoke(request);
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] 找不到針對事件 '{request.EventName}' 的回應設定。");
            }
        }
    }
}
