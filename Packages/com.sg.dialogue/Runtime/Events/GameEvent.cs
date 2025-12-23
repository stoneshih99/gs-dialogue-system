using UnityEngine;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// GameEvent 是一個純粹的事件通道。
    /// 具體的事件請求資料 (GameRequest) 由發送者 (GameEventNode) 決定並傳遞。
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "SG/Dialogue/Events/Game Event")]
    public class GameEvent : BaseEventChannel<GameRequest>
    {
        [TextArea]
        public string description;
    }
}
