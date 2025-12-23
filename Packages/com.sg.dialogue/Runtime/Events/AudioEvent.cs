using UnityEngine;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// AudioEvent 是一個純粹的事件通道，不包含具體音訊資料。
    /// 所有資料都由發送者 (PlayAudioNode) 透過 AudioRequest 傳遞。
    /// </summary>
    [CreateAssetMenu(fileName = "AudioChannel", menuName = "SG/Dialogue/Events/Audio Event Channel")]
    public class AudioEvent : BaseEventChannel<AudioRequest>
    {
        [TextArea]
        public string description;
    }
}
