using System.Collections.Generic;

namespace SG.Dialogue
{
    /// <summary>
    /// DialogueStateAsset 用於儲存對話變數（整數、布林值和字串）。
    /// 可直接以 new DialogueStateAsset() 建立實體。
    /// 可用於需要跨對話、跨場景持久化的全域狀態，例如玩家的好感度、任務進度等。
    /// </summary>
    public class DialogueStateAsset
    {
        [System.Serializable] public class IntPair { public string key; public int value; }
        [System.Serializable] public class BoolPair { public string key; public bool value; }
        [System.Serializable] public class StringPair { public string key; public string value; }

        private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
        private readonly Dictionary<string, bool> _bools = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        /// <summary>清空所有運行時變數。</summary>
        public void Clear()
        {
            _ints.Clear();
            _bools.Clear();
            _strings.Clear();
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

        // --- 匯出/匯入 (用於存檔/讀檔) ---
        public List<IntPair> ExportInts()
        {
            var list = new List<IntPair>();
            foreach (var kv in _ints) list.Add(new IntPair { key = kv.Key, value = kv.Value });
            return list;
        }
        public List<BoolPair> ExportBools()
        {
            var list = new List<BoolPair>();
            foreach (var kv in _bools) list.Add(new BoolPair { key = kv.Key, value = kv.Value });
            return list;
        }
        public List<StringPair> ExportStrings()
        {
            var list = new List<StringPair>();
            foreach (var kv in _strings) list.Add(new StringPair { key = kv.Key, value = kv.Value });
            return list;
        }

        public void ImportInts(List<IntPair> list)
        {
            _ints.Clear();
            if (list != null) foreach (var p in list) _ints[p.key] = p.value;
        }
        public void ImportBools(List<BoolPair> list)
        {
            _bools.Clear();
            if (list != null) foreach (var p in list) _bools[p.key] = p.value;
        }
        public void ImportStrings(List<StringPair> list)
        {
            _strings.Clear();
            if (list != null) foreach (var p in list) _strings[p.key] = p.value;
        }
    }
}
