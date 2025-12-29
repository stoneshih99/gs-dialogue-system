using SG.Dialogue.Events;
using UnityEngine;

namespace SG.Dialogue
{
    public class DialogueEventBridge :MonoBehaviour
    {
        [Tooltip("要監聽的對話音訊事件通道。")] [SerializeField]
        private GameEvent[] gameEvents;

        private void OnEnable()
        {
            foreach (var e in gameEvents)
            {
                if (e != null)
                {
                    e.RegisterListener(OnReceived);
                }
            }
        }

        private void OnDisable()
        {
            foreach (var e in gameEvents)
            {
                if (e != null)
                {
                    e.UnregisterListener(OnReceived);
                }
            }
        }

        private void OnReceived(GameRequest request)
        {
            Debug.LogFormat("DialogueEventBridge: 收到遊戲事件請求，事件名稱：{0}", request.EventName);
        }        
    }
}