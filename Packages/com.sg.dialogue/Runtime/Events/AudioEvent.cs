using System;
using UnityEngine;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// AudioEvent 是一個帶有參數的 ScriptableObject 事件，專門用於傳遞音訊請求 (AudioRequest)。
    /// 它作為對話系統和外部音訊系統之間的事件通道，允許對話節點發出音訊請求，而無需直接引用音訊管理器。
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioEvent", menuName = "SG/Dialogue/Events/Audio Event")]
    public class AudioEvent : ScriptableObject
    {
        private Action<AudioRequest> _onRaised;

        /// <summary>
        /// 觸發此事件，並傳遞一個音訊請求給所有監聽者。
        /// </summary>
        /// <param name="request">包含音訊播放資料的請求。</param>
        public void Raise(AudioRequest request) => _onRaised?.Invoke(request);

        /// <summary>
        /// 註冊一個監聽者到此事件。
        /// </summary>
        /// <param name="listener">要註冊的 Action，它接受一個 AudioRequest 參數。</param>
        public void RegisterListener(Action<AudioRequest> listener) => _onRaised += listener;

        /// <summary>
        /// 從此事件中解除註冊一個監聽者。
        /// </summary>
        /// <param name="listener">要解除註冊的 Action。</param>
        public void UnregisterListener(Action<AudioRequest> listener) => _onRaised -= listener;
    }
}
