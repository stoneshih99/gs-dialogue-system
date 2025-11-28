using System.Collections.Generic;
using static SG.Dialogue.DialogueStateAsset;

namespace SG.Dialogue.Variables
{
    /// <summary>
    /// DialogueState 是一個運行時類別，用於儲存單次對話會話中的臨時變數（本地變數）。
    /// 這些變數在每次對話開始時被清空，並且只在該次對話的生命週期內有效。
    /// 它管理整數、布林和字串類型的變數。
    /// </summary>
    public class DialogueState
    {
        private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
        private readonly Dictionary<string, bool> _bools = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        /// <summary>
        /// 清空所有儲存的本地變數。通常在每次新對話開始時呼叫。
        /// </summary>
        public void Clear()
        {
            _ints.Clear();
            _bools.Clear();
            _strings.Clear();
        }

        /// <summary>
        /// 從另一個 DialogueState 實例複製所有變數。
        /// </summary>
        /// <param name="other">要從中複製變數的 DialogueState 實例。</param>
        public void CopyFrom(DialogueState other)
        {
            Clear(); // 先清空當前狀態
            if (other == null) return;
            foreach (var kvp in other._ints) _ints[kvp.Key] = kvp.Value;
            foreach (var kvp in other._bools) _bools[kvp.Key] = kvp.Value;
            foreach (var kvp in other._strings) _strings[kvp.Key] = kvp.Value;
        }

        // --- 整數 (Int) 操作 ---
        public bool HasInt(string name) => _ints.ContainsKey(name);
        public int GetInt(string name) => _ints.TryGetValue(name, out var v) ? v : 0;
        public void SetInt(string name, int value) => _ints[name] = value;
        public void AddInt(string name, int delta) => _ints[name] = GetInt(name) + delta;

        // --- 布林 (Bool) 操作 ---
        public bool HasBool(string name) => _bools.ContainsKey(name);
        public bool GetBool(string name) => _bools.TryGetValue(name, out var v) && v;
        public void SetBool(string name, bool value) => _bools[name] = value;
        public void ToggleBool(string name) => _bools[name] = !GetBool(name);

        // --- 字串 (String) 操作 ---
        public bool HasString(string name) => _strings.ContainsKey(name);
        public string GetString(string name) => _strings.TryGetValue(name, out var v) ? v : null;
        public void SetString(string name, string value) => _strings[name] = value;

        // --- 匯出/匯入 (主要用於存檔/讀檔系統) ---
        
        /// <summary>
        /// 匯出當前所有整數變數為一個可序列化的列表。
        /// </summary>
        public List<IntPair> ExportInts()
        {
            var list = new List<IntPair>();
            foreach (var kv in _ints) list.Add(new IntPair { key = kv.Key, value = kv.Value });
            return list;
        }
        
        /// <summary>
        /// 匯出當前所有布林變數為一個可序列化的列表。
        /// </summary>
        public List<BoolPair> ExportBools()
        {
            var list = new List<BoolPair>();
            foreach (var kv in _bools) list.Add(new BoolPair { key = kv.Key, value = kv.Value });
            return list;
        }
        
        /// <summary>
        /// 匯出當前所有字串變數為一個可序列化的列表。
        /// </summary>
        public List<StringPair> ExportStrings()
        {
            var list = new List<StringPair>();
            foreach (var kv in _strings) list.Add(new StringPair { key = kv.Key, value = kv.Value });
            return list;
        }

        /// <summary>
        /// 從一個可序列化的列表匯入整數變數，會覆蓋現有變數。
        /// </summary>
        public void ImportInts(List<IntPair> list)
        {
            _ints.Clear();
            if (list != null) foreach (var p in list) _ints[p.key] = p.value;
        }
        
        /// <summary>
        /// 從一個可序列化的列表匯入布林變數，會覆蓋現有變數。
        /// </summary>
        public void ImportBools(List<BoolPair> list)
        {
            _bools.Clear();
            if (list != null) foreach (var p in list) _bools[p.key] = p.value;
        }
        
        /// <summary>
        /// 從一個可序列化的列表匯入字串變數，會覆蓋現有變數。
        /// </summary>
        public void ImportStrings(List<StringPair> list)
        {
            _strings.Clear();
            if (list != null) foreach (var p in list) _strings[p.key] = p.value;
        }
    }
}
