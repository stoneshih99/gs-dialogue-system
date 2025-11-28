using System;
using System.Collections;
using SG.Dialogue.Events;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// GameEventNode 是一個遊戲事件節點，用於在對話流程中觸發一個全域的 GameEvent (ScriptableObject)。
    /// </summary>
    [Serializable]
    public class GameEventNode : DialogueNodeBase
    {
        [Tooltip("要觸發的 GameEvent 資產。")]
        public GameEvent GameEvent;

        [Tooltip("要發送的事件請求，包含事件名稱等資訊。")]
        public GameRequest request;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        public override IEnumerator Process(DialogueController controller)
        {
            if (GameEvent != null)
            {
                Debug.Log($"[對話] 觸發遊戲事件: {GameEvent.name}，請求: {request.EventName}");
                GameEvent.Raise(request);
            }
            else
            {
                Debug.LogWarning($"遊戲事件節點 '{nodeId}' 沒有指派任何 GameEvent。");
            }
            yield break;
        }

        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
    }
}
