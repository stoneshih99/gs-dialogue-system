using UnityEngine;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// GameEvent 是一個基於 ScriptableObject 的事件系統。
    /// 它允許不同遊戲物件之間進行通信，而無需直接引用彼此，從而降低耦合度。
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "SG/Dialogue/Events/Game Event")]
    public class GameEvent : BaseEventChannel<GameRequest>
    {
    }
}
