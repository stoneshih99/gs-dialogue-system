using System;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// GameEvent 是一個基於 ScriptableObject 的事件系統。
    /// 它允許不同遊戲物件之間進行通信，而無需直接引用彼此，從而降低耦合度。
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "SG/Dialogue/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private Action<GameRequest> _onRaised;

        /// <summary>
        /// 觸發此事件，並傳遞一個音訊請求給所有監聽者。
        /// </summary>
        /// <param name="request">包含音訊播放資料的請求。</param>
        public void Raise(GameRequest request) => _onRaised?.Invoke(request);

        /// <summary>
        /// 註冊一個監聽者到此事件。
        /// </summary>
        /// <param name="listener">要註冊的 Action，它接受一個 AudioRequest 參數。</param>
        public void RegisterListener(Action<GameRequest> listener) => _onRaised += listener;

        /// <summary>
        /// 從此事件中解除註冊一個監聽者。
        /// </summary>
        /// <param name="listener">要解除註冊的 Action。</param>
        public void UnregisterListener(Action<GameRequest> listener) => _onRaised -= listener;
    }
}
