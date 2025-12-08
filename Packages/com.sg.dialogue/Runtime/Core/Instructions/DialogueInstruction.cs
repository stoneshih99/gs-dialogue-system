namespace SG.Dialogue.Core.Instructions
{
    /// <summary>
    /// 所有對話指令的抽象基底類別。
    /// 節點的 Process 方法會 yield return 這些指令的實例，
    /// 告訴 DialogueController 下一步該做什麼，例如等待使用者輸入、跳轉到另一個節點或結束對話。
    /// 這是一個標記類別，主要用於類型識別。
    /// </summary>
    public abstract class DialogueInstruction { }
}
