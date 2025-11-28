using System;

namespace SG.Dialogue.Conditions
{
    /// <summary>
    /// 定義了用於變數比較的運算符類型。
    /// </summary>
    [Serializable]
    public enum Comparison
    {
        /// <summary>
        /// 等於。
        /// </summary>
        Equal,
        /// <summary>
        /// 不等於。
        /// </summary>
        NotEqual,
        /// <summary>
        /// 大於。
        /// </summary>
        Greater,
        /// <summary>
        /// 小於。
        /// </summary>
        Less,
        /// <summary>
        /// 大於或等於。
        /// </summary>
        GreaterOrEqual,
        /// <summary>
        /// 小於或等於。
        /// </summary>
        LessOrEqual
    }
}
