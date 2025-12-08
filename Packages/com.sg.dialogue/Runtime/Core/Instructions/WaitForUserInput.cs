namespace SG.Dialogue.Core.Instructions
{
    /// <summary>
    /// 一個對話指令，用於指示 DialogueController 暫停執行，直到接收到使用者的輸入。
    /// 這通常在顯示一句對話文本或一組選項後使用，等待玩家點擊「下一步」按鈕或選擇一個選項。
    /// </summary>
    public class WaitForUserInput : DialogueInstruction { }
}
