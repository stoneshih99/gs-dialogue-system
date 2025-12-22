using System;

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

        public AudioEvent audioEvent;
        
        public AudioRequest(AudioEvent e)
        {
            audioEvent = e;
        }
    }
}
