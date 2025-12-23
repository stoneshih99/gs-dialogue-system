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
        // /// <summary>
        // /// 要執行的音訊動作類型（例如：播放BGM、停止BGM、播放SFX）。
        // /// </summary>
        // [Header("音訊動作類型")]
        // public AudioActionType actionType;
        //
        // [Header("音訊名稱")]
        // public string soundName;
        //
        // [Header("是否循環播放")]
        // public bool loop;
        //
        // [Header("淡入或淡出持續時間（秒）")]
        // [Tooltip("如果設定為 -1，表示使用音訊管理器的預設值。")]
        // public float fadeDuration = -1f;
        
        [Header("音訊請求")]
        public AudioRequest request;
    }
}
