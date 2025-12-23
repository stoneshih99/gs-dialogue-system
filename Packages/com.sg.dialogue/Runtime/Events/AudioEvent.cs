using SG.Dialogue.Enums;
using UnityEngine;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// AudioEvent 是一個帶有參數的 ScriptableObject 事件，專門用於傳遞音訊請求 (AudioRequest)。
    /// 它作為對話系統和外部音訊系統之間的事件通道，允許對話節點發出音訊請求，而無需直接引用音訊管理器。
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioEvent", menuName = "SG/Dialogue/Events/Audio Event")]
    public class AudioEvent : BaseEventChannel<AudioRequest>
    {
        [Header("註解說明")]
        public string description;
        [Header("音訊請求")]
        public AudioRequest request;
    }
}
