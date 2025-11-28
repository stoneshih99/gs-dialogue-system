using System;

namespace SG.Dialogue.Enums
{
    /// <summary>
    /// 定義角色動作節點 (CharacterActionNode) 可以執行的動作類型。
    /// </summary>
    [Serializable]
    public enum CharacterActionType
    {
        /// <summary>
        /// 讓角色在指定位置登場，或改變該位置上現有角色的立繪。
        /// </summary>
        Enter,
        /// <summary>
        /// 讓指定位置的角色退場。
        /// </summary>
        Exit,
        // 未來可擴充其他動作，例如：
        // Move,      // 將角色從一個位置移動到另一個位置
        // ChangeExpression, // 改變表情（如果使用支援多表情的立繪系統）
    }
}
