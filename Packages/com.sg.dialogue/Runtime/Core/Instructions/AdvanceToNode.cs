namespace SG.Dialogue.Core.Instructions
{
    /// <summary>
    /// 一個對話指令，用於明確地指示 DialogueController 前進到下一個指定的節點。
    /// 當節點的處理邏輯需要跳轉到一個非預設的節點時（例如，條件判斷的結果），就會使用此指令。
    /// </summary>
    public class AdvanceToNode : DialogueInstruction
    {
        /// <summary>
        /// 要前進到的下一個節點的 ID。
        /// </summary>
        public string NextNodeId { get; }

        /// <summary>
        /// 建立一個 AdvanceToNode 指令。
        /// </summary>
        /// <param name="nextNodeId">目標節點的 ID。</param>
        public AdvanceToNode(string nextNodeId)
        {
            NextNodeId = nextNodeId;
        }
    }
}
