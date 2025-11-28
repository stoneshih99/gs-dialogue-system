using System;
using UnityEngine.Events;

namespace SG.Dialogue.Events
{
    /// <summary>
    /// StringEvent 是一個可序列化的 UnityEvent，它接受一個字串作為參數。
    /// 這使得我們可以在 Inspector 中方便地設定需要接收字串參數的回呼函式。
    /// 它主要用於對話圖層級的事件，例如 onNodeEntered 或 onNodeExited，用於傳遞節點 ID。
    /// </summary>
    [Serializable]
    public class StringEvent : UnityEvent<string> {}
}
