namespace SG.Dialogue.Core.Instructions
{
    /// <summary>
    /// 一個對話指令，用於指示 DialogueController 結束當前的對話流程。
    /// 當對話到達一個終點時，對應的節點（例如 EndNode）會返回此指令。
    /// </summary>
    public class EndDialogue : DialogueInstruction { }
}
