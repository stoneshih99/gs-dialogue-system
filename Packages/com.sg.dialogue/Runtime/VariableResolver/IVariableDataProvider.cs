namespace SG.Dialogue.VariableResolver
{
    /// <summary>
    /// 定義了一個變數資料提供者的合約。
    /// 任何想要向對話系統提供動態變數的類別都可以實現此介面。
    /// </summary>
    public interface IVariableDataProvider
    {
        /// <summary>
        /// 嘗試根據提供的鍵（變數名稱）獲取對應的值。
        /// </summary>
        /// <param name="key">要查詢的變數名稱（不含大括號）。</param>
        /// <param name="value">如果找到，則為輸出的變數值。</param>
        /// <returns>如果此提供者成功處理了這個鍵，則返回 true；否則返回 false。</returns>
        bool TryGetValue(string key, out string value);
    }
}
