using System.Collections.Generic;
using UnityEngine;

namespace SG.Dialogue
{
    /// <summary>
    /// DialogueStateAsset 是一個 ScriptableObject，用於儲存對話變數（整數、布林值和字串）。
    /// 這可以用於需要跨對話、跨場景持久化的全域狀態，例如玩家的好感度、任務進度等。
    /// 變數在編輯器中設定初始值，在運行時可以被修改。
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueState", menuName = "SG/Dialogue/Dialogue State Asset")]
    public class DialogueStateAsset : ScriptableObject
    {
        /// <summary>
        /// 用於在編輯器中序列化整數變數鍵值對的結構。
        /// </summary>
        [System.Serializable]
        public class IntPair { public string key; public int value; }
        /// <summary>
        /// 用於在編輯器中序列化布林變數鍵值對的結構。
        /// </summary>
        [System.Serializable]
        public class BoolPair { public string key; public bool value; }
        /// <summary>
        /// 用於在編輯器中序列化字串變數鍵值對的結構。
        /// </summary>
        [System.Serializable]
        public class StringPair { public string key; public string value; }

        [Tooltip("初始整數變數列表，在編輯器中設定。")]
        [SerializeField] private List<IntPair> initialInts = new List<IntPair>();
        [Tooltip("初始布林變數列表，在編輯器中設定。")]
        [SerializeField] private List<BoolPair> initialBools = new List<BoolPair>();
        [Tooltip("初始字串變數列表，在編輯器中設定。")]
        [SerializeField] private List<StringPair> initialStrings = new List<StringPair>();

        /// <summary>
        /// 獲取在編輯器中設定的初始整數變數的唯讀列表。
        /// </summary>
        public IReadOnlyList<IntPair> InitialInts => initialInts;
        /// <summary>
        /// 獲取在編輯器中設定的初始布林變數的唯讀列表。
        /// </summary>
        public IReadOnlyList<BoolPair> InitialBools => initialBools;
        /// <summary>
        /// 獲取在編輯器中設定的初始字串變數的唯讀列表。
        /// </summary>
        public IReadOnlyList<StringPair> InitialStrings => initialStrings;

        // 運行時的變數儲存在字典中，以提供快速的查找效能。
        private readonly Dictionary<string, int> _runtimeInts = new Dictionary<string, int>();
        private readonly Dictionary<string, bool> _runtimeBools = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> _runtimeStrings = new Dictionary<string, string>();

        private void OnEnable()
        {
            // 當 ScriptableObject 被載入時（例如遊戲開始時），初始化運行時變數。
            Initialize();
        }

        /// <summary>
        /// 初始化運行時變數字典，將在編輯器中設定的初始值複製進去。
        /// 這會重設所有變數為其初始狀態。
        /// </summary>
        public void Initialize()
        {
            _runtimeInts.Clear();
            foreach (var pair in initialInts) _runtimeInts[pair.key] = pair.value;
            
            _runtimeBools.Clear();
            foreach (var pair in initialBools) _runtimeBools[pair.key] = pair.value;

            _runtimeStrings.Clear();
            foreach (var pair in initialStrings) _runtimeStrings[pair.key] = pair.value;
        }

        // --- 整數 (Int) 操作 ---
        public bool HasInt(string name) => _runtimeInts.ContainsKey(name);
        public int GetInt(string name) => _runtimeInts.TryGetValue(name, out var v) ? v : 0;
        public void SetInt(string name, int value) => _runtimeInts[name] = value;
        public void AddInt(string name, int delta) => _runtimeInts[name] = GetInt(name) + delta;

        // --- 布林 (Bool) 操作 ---
        public bool HasBool(string name) => _runtimeBools.ContainsKey(name);
        public bool GetBool(string name) => _runtimeBools.TryGetValue(name, out var v) && v;
        public void SetBool(string name, bool value) => _runtimeBools[name] = value;
        public void ToggleBool(string name) => _runtimeBools[name] = !GetBool(name);

        // --- 字串 (String) 操作 ---
        public bool HasString(string name) => _runtimeStrings.ContainsKey(name);
        public string GetString(string name) => _runtimeStrings.TryGetValue(name, out var v) ? v : null;
        public void SetString(string name, string value) => _runtimeStrings[name] = value;

        // --- 匯出/匯入 (用於存檔/讀檔) ---
        public List<IntPair> ExportInts()
        {
            var list = new List<IntPair>();
            foreach (var kv in _runtimeInts) list.Add(new IntPair { key = kv.Key, value = kv.Value });
            return list;
        }
        public List<BoolPair> ExportBools()
        {
            var list = new List<BoolPair>();
            foreach (var kv in _runtimeBools) list.Add(new BoolPair { key = kv.Key, value = kv.Value });
            return list;
        }
        public List<StringPair> ExportStrings()
        {
            var list = new List<StringPair>();
            foreach (var kv in _runtimeStrings) list.Add(new StringPair { key = kv.Key, value = kv.Value });
            return list;
        }

        public void ImportInts(List<IntPair> list)
        {
            _runtimeInts.Clear();
            if (list != null) foreach (var p in list) _runtimeInts[p.key] = p.value;
        }
        public void ImportBools(List<BoolPair> list)
        {
            _runtimeBools.Clear();
            if (list != null) foreach (var p in list) _runtimeBools[p.key] = p.value;
        }
        public void ImportStrings(List<StringPair> list)
        {
            _runtimeStrings.Clear();
            if (list != null) foreach (var p in list) _runtimeStrings[p.key] = p.value;
        }
    }
}
