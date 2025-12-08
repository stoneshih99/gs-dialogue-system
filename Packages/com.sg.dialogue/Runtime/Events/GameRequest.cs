using System;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// GameRequest 是一個類別，用於封裝一次遊戲事件請求所需的所有資料。
    /// </summary>
    [Serializable]
    public class GameRequest : IEventRequest
    {
        /// <summary>
        /// 要觸發的事件的名稱。
        /// </summary>
        public string EventName { get; set; }
    }
}
