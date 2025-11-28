#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// TableEditorTab 是一個 VisualElement，提供了一個自包含的 UI 組件，用於編輯 LocalizationTable 資產。
    /// 它允許用戶選擇表格、搜尋、添加、複製、刪除條目，並進行 JSON 格式的匯入匯出。
    /// </summary>
    public class TableEditorTab : VisualElement
    {
        /// <summary>
        /// 當本地化表格被選中時觸發的事件。
        /// </summary>
        public Action<LocalizationTable> OnTableSelected;

        private LocalizationTable _table; // 當前編輯的 LocalizationTable 資產

        // UI 元素引用
        private readonly ObjectField _tableField; // 用於選擇 LocalizationTable 資產的欄位
        private readonly TextField _searchField; // 搜尋輸入框
        private readonly Label _countLabel; // 顯示條目數量的標籤
        private readonly ListView _listView; // 顯示本地化條目列表的視圖
        private readonly List<LocalizationTable.Entry> _filtered = new List<LocalizationTable.Entry>(); // 經過篩選的條目列表
        private LocalizationTable.Entry _selected; // 當前選中的本地化條目

        // 詳細資訊面板的輸入欄位
        private readonly TextField _keyField;
        private readonly TextField _zhField;
        private readonly TextField _jaField;
        private readonly TextField _enField;
        private readonly TextField _speakerZhField;
        private readonly TextField _speakerJaField;
        private readonly TextField _speakerEnField;

        // 預設的本地化文件夾路徑
        private const string DefaultLocalizationFolder = "Assets/Dialogue/Localization";

        /// <summary>
        /// 構造函數，初始化 UI 佈局和功能。
        /// </summary>
        public TableEditorTab()
        {
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;
            style.paddingLeft = 6;
            style.paddingRight = 6;
            style.paddingTop = 4;
            style.paddingBottom = 4;

            // 頂部工具列
            var top = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingBottom = 4 } };
            _tableField = new ObjectField("Localization Table") { objectType = typeof(LocalizationTable), allowSceneObjects = false, style = { minWidth = 260, marginRight = 6 } };
            top.Add(_tableField);

            _searchField = new TextField("Search") { style = { flexGrow = 1f, marginRight = 6 }, tooltip = "搜尋 Key 或任何語言內容" };
            _searchField.RegisterValueChangedCallback(_ => RebuildList()); // 搜尋框內容改變時重新構建列表
            top.Add(_searchField);

            _countLabel = new Label("0 items") { style = { marginRight = 6 } };
            top.Add(_countLabel);

            // 功能按鈕
            top.Add(new Button(AddEntry) { text = "+ Add" });
            top.Add(new Button(DuplicateEntry) { text = "Duplicate" });
            top.Add(new Button(DeleteEntry) { text = "Delete" });
            top.Add(new Button(SaveTable) { text = "Save" });
            top.Add(new Button(ExportJson) { text = "Export JSON" });
            top.Add(new Button(ImportJson) { text = "Import JSON" });
            Add(top);

            // 分割視圖 (左側列表，右側詳細資訊)
            var split = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1f } };
            
            // 左側：列表視圖
            _listView = new ListView { virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight, selectionType = SelectionType.Single, style = { flexBasis = 300, flexGrow = 0f, flexShrink = 0f } };
            _listView.makeItem = () => new Label(); // 創建列表項的 UI
            _listView.bindItem = (ve, i) => // 綁定列表項的數據
            {
                var lbl = (Label)ve;
                if (i < 0 || i >= _filtered.Count) { lbl.text = string.Empty; return; }
                var e = _filtered[i];
                var preview = e.zhTW?.Replace("\n", " ");
                if (preview?.Length > 30) preview = preview.Substring(0, 30) + "…";
                lbl.text = $"{(string.IsNullOrEmpty(e.key) ? "<no key>" : e.key)}\n<color=#888>{preview ?? ""}</color>";
            };
            _listView.selectionChanged += OnSelectionChanged; // 註冊選中項改變回調
            split.Add(_listView);

            // 右側：詳細資訊面板
            var detail = new ScrollView { style = { flexGrow = 1f, paddingLeft = 8 } };
            _keyField = new TextField("Key");
            _keyField.RegisterValueChangedCallback(evt => { if (_selected == null) return; _selected.key = evt.newValue; MarkDirty(); RebuildList(true); });
            detail.Add(_keyField);

            _zhField = new TextField("zh-TW") { multiline = true };
            _zhField.RegisterValueChangedCallback(evt => { if (_selected != null) { _selected.zhTW = evt.newValue; MarkDirty(); } });
            detail.Add(_zhField);

            _jaField = new TextField("ja-JP") { multiline = true };
            _jaField.RegisterValueChangedCallback(evt => { if (_selected != null) { _selected.jaJP = evt.newValue; MarkDirty(); } });
            detail.Add(_jaField);

            _enField = new TextField("en-US") { multiline = true };
            _enField.RegisterValueChangedCallback(evt => { if (_selected != null) { _selected.enUS = evt.newValue; MarkDirty(); } });
            detail.Add(_enField);

            detail.Add(new Label("Speaker (說話者)") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6 } });
            _speakerZhField = new TextField("Speaker zh-TW");
            _speakerZhField.RegisterValueChangedCallback(evt => { if (_selected != null) { _selected.speakerZhTW = evt.newValue; MarkDirty(); } });
            detail.Add(_speakerZhField);

            _speakerJaField = new TextField("Speaker ja-JP");
            _speakerJaField.RegisterValueChangedCallback(evt => { if (_selected != null) { _selected.speakerJaJP = evt.newValue; MarkDirty(); } });
            detail.Add(_speakerJaField);

            _speakerEnField = new TextField("Speaker en-US");
            _speakerEnField.RegisterValueChangedCallback(evt => { if (_selected != null) { _selected.speakerEnUS = evt.newValue; MarkDirty(); } });
            detail.Add(_speakerEnField);

            split.Add(detail);
            Add(split);

            RebuildList(); // 初始構建列表
        }

        /// <summary>
        /// 設定當前編輯的 LocalizationTable。
        /// </summary>
        /// <param name="table">要設定的 LocalizationTable 資產。</param>
        public void SetTable(LocalizationTable table)
        {
            _table = table;
            // 解除註冊並重新註冊回調，以避免重複觸發
            _tableField.UnregisterValueChangedCallback(OnTableFieldValueChanged);
            _tableField.SetValueWithoutNotify(table);
            _tableField.RegisterValueChangedCallback(OnTableFieldValueChanged);
            RebuildList(); // 重新構建列表視圖
        }

        /// <summary>
        /// 本地化表格 ObjectField 值改變時的回調。
        /// </summary>
        /// <param name="evt">改變事件。</param>
        private void OnTableFieldValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            OnTableSelected?.Invoke(evt.newValue as LocalizationTable);
        }

        /// <summary>
        /// 當列表選中項改變時調用，更新詳細資訊面板的顯示。
        /// </summary>
        /// <param name="objs">選中的物件列表。</param>
        private void OnSelectionChanged(IEnumerable<object> objs)
        {
            _selected = objs?.FirstOrDefault() as LocalizationTable.Entry;

            // 如果沒有選中項，則清空詳細資訊面板
            if (_selected == null)
            {
                _keyField.SetValueWithoutNotify(string.Empty);
                _zhField.SetValueWithoutNotify(string.Empty);
                _jaField.SetValueWithoutNotify(string.Empty);
                _enField.SetValueWithoutNotify(string.Empty);
                _speakerZhField.SetValueWithoutNotify(string.Empty);
                _speakerJaField.SetValueWithoutNotify(string.Empty);
                _speakerEnField.SetValueWithoutNotify(string.Empty);
                return;
            }

            // 填充詳細資訊面板
            _keyField.SetValueWithoutNotify(_selected.key ?? string.Empty);
            _zhField.SetValueWithoutNotify(_selected.zhTW ?? string.Empty);
            _jaField.SetValueWithoutNotify(_selected.jaJP ?? string.Empty);
            _enField.SetValueWithoutNotify(_selected.enUS ?? string.Empty);
            _speakerZhField.SetValueWithoutNotify(_selected.speakerZhTW ?? string.Empty);
            _speakerJaField.SetValueWithoutNotify(_selected.speakerJaJP ?? string.Empty);
            _speakerEnField.SetValueWithoutNotify(_selected.speakerEnUS ?? string.Empty);
        }

        /// <summary>
        /// 重新構建列表視圖，應用搜尋篩選和排序。
        /// </summary>
        /// <param name="keepSelection">是否嘗試保持當前選中項。</param>
        private void RebuildList(bool keepSelection = false)
        {
            if (_listView == null) return;
            var prevSelected = keepSelection ? _selected : null;

            _filtered.Clear();
            if (_table != null)
            {
                var q = _searchField?.value?.Trim() ?? string.Empty;
                IEnumerable<LocalizationTable.Entry> seq = _table.entries;
                if (!string.IsNullOrEmpty(q))
                {
                    // 根據搜尋關鍵字篩選條目
                    seq = seq.Where(e =>
                        (e.key?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (e.zhTW?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (e.jaJP?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (e.enUS?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
                }
                _filtered.AddRange(seq.OrderBy(e => e.key)); // 排序並更新篩選列表
            }

            _listView.itemsSource = _filtered;
            _listView.Rebuild(); // 重新繪製列表
            _countLabel.text = $"{_filtered.Count} items"; // 更新條目數量顯示

            // 處理選中項的恢復邏輯
            if (prevSelected != null && _filtered.Contains(prevSelected))
            {
                _listView.SetSelection(_filtered.IndexOf(prevSelected));
            }
            else if (_filtered.Count > 0)
            {
                _listView.SetSelection(0);
            }
            else
            {
                OnSelectionChanged(null);
            }
        }

        /// <summary>
        /// 添加一個新的本地化條目。
        /// </summary>
        private void AddEntry()
        {
            if (_table == null) return;
            var e = new LocalizationTable.Entry { key = GenerateUniqueKey("NEW_KEY") }; // 生成唯一 Key
            _table.entries.Add(e); // 添加到表格數據中
            MarkDirty(); // 標記資產為 Dirty
            RebuildList(); // 重新構建列表
            _listView.SetSelection(_filtered.IndexOf(e)); // 選中新添加的條目
        }

        /// <summary>
        /// 複製當前選中的本地化條目。
        /// </summary>
        private void DuplicateEntry()
        {
            if (_table == null || _selected == null) return;
            var e = new LocalizationTable.Entry // 創建一個新的條目並複製內容
            {
                key = GenerateUniqueKey(_selected.key + "_copy"),
                zhTW = _selected.zhTW, jaJP = _selected.jaJP, enUS = _selected.enUS,
                speakerZhTW = _selected.speakerZhTW, speakerJaJP = _selected.speakerJaJP, speakerEnUS = _selected.speakerEnUS
            };
            _table.entries.Add(e);
            MarkDirty();
            RebuildList();
            _listView.SetSelection(_filtered.IndexOf(e));
        }

        /// <summary>
        /// 刪除當前選中的本地化條目。
        /// </summary>
        private void DeleteEntry()
        {
            if (_table == null || _selected == null) return;
            // 彈出確認對話框
            if (!EditorUtility.DisplayDialog("刪除條目", $"確定要刪除 key '{_selected.key}' 嗎？", "刪除", "取消")) return;
            _table.entries.Remove(_selected); // 從表格數據中移除
            _selected = null; // 清空選中項
            MarkDirty();
            RebuildList();
        }

        /// <summary>
        /// 保存當前編輯的 LocalizationTable 資產。
        /// </summary>
        private void SaveTable()
        {
            if (_table == null) return;
            EditorUtility.SetDirty(_table); // 標記資產為 Dirty
            AssetDatabase.SaveAssets(); // 保存資產
            EditorUtility.DisplayDialog("保存完成", "LocalizationTable 已保存。", "OK");
        }

        /// <summary>
        /// 將當前 LocalizationTable 匯出為 JSON 檔案。
        /// </summary>
        private void ExportJson()
        {
            if (_table == null) { EditorUtility.DisplayDialog("Export JSON", "請先選擇 LocalizationTable。", "OK"); return; }
            // 打開保存檔案對話框
            string path = EditorUtility.SaveFilePanel("Export Localization JSON", DefaultLocalizationFolder, $"{_table.name}.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            // 構建要匯出的 JSON 數據結構
            var export = new DialogueLocalizationWindow.LocalizationTableJson { entries = new List<DialogueLocalizationWindow.LocalizationTableEntryJson>() };
            foreach (var e in _table.entries.Where(entry => !string.IsNullOrEmpty(entry.key)))
            {
                export.entries.Add(new DialogueLocalizationWindow.LocalizationTableEntryJson
                {
                    key = e.key, zhTW = e.zhTW, jaJP = e.jaJP, enUS = e.enUS,
                    speakerZhTW = e.speakerZhTW, speakerJaJP = e.speakerJaJP, speakerEnUS = e.speakerEnUS
                });
            }
            File.WriteAllText(path, JsonUtility.ToJson(export, true), new UTF8Encoding(true)); // 寫入檔案
            EditorUtility.RevealInFinder(path); // 在檔案管理器中顯示檔案
        }

        /// <summary>
        /// 從 JSON 檔案匯入數據到當前 LocalizationTable。
        /// </summary>
        private void ImportJson()
        {
            if (_table == null) { EditorUtility.DisplayDialog("Import JSON", "請先選擇 LocalizationTable。", "OK"); return; }
            // 打開檔案對話框
            string path = EditorUtility.OpenFilePanel("Import Localization JSON", DefaultLocalizationFolder, "json");
            if (string.IsNullOrEmpty(path)) return;

            var data = JsonUtility.FromJson<DialogueLocalizationWindow.LocalizationTableJson>(File.ReadAllText(path, Encoding.UTF8));
            if (data?.entries == null) { EditorUtility.DisplayDialog("Import Localization JSON", "檔案沒有有效資料", "OK"); return; }

            // 建立現有條目的查找字典
            var dict = _table.entries.Where(e => !string.IsNullOrEmpty(e.key)).ToDictionary(e => e.key);
            int updateCount = 0, addCount = 0;

            // 遍歷匯入的條目，更新或新增
            foreach (var item in data.entries.Where(i => !string.IsNullOrEmpty(i.key)))
            {
                if (dict.TryGetValue(item.key, out var entry))
                {
                    updateCount++;
                }
                else
                {
                    entry = new LocalizationTable.Entry { key = item.key };
                    _table.entries.Add(entry);
                    addCount++;
                }
                // 更新條目內容
                entry.zhTW = item.zhTW; entry.jaJP = item.jaJP; entry.enUS = item.enUS;
                entry.speakerZhTW = item.speakerZhTW; entry.speakerJaJP = item.speakerJaJP; entry.speakerEnUS = item.speakerEnUS;
            }
            EditorUtility.SetDirty(_table); // 標記資產為 Dirty
            AssetDatabase.SaveAssets(); // 保存資產
            EditorUtility.DisplayDialog("Import Localization JSON", $"匯入完成\n更新：{updateCount} 筆\n新增：{addCount} 筆", "OK");
            RebuildList(true); // 重新構建列表並保持選中
        }

        /// <summary>
        /// 生成一個唯一的 Key。
        /// </summary>
        /// <param name="baseKey">基礎 Key 名稱。</param>
        /// <returns>唯一的 Key。</returns>
        private string GenerateUniqueKey(string baseKey)
        {
            if (string.IsNullOrEmpty(baseKey)) baseKey = "KEY";
            string candidate = baseKey;
            int i = 1;
            while (!IsKeyUnique(candidate, null)) // 循環直到生成唯一的 Key
            {
                candidate = $"{baseKey}_{i++}";
            }
            return candidate;
        }

        /// <summary>
        /// 檢查給定的 Key 是否唯一。
        /// </summary>
        /// <param name="key">要檢查的 Key。</param>
        /// <param name="except">要排除的條目（在編輯自身 Key 時使用）。</param>
        /// <returns>如果 Key 唯一則為 true，否則為 false。</returns>
        private bool IsKeyUnique(string key, LocalizationTable.Entry except)
        {
            return _table == null || string.IsNullOrEmpty(key) || !_table.entries.Any(e => e != null && e != except && e.key == key);
        }

        /// <summary>
        /// 標記當前編輯的 LocalizationTable 資產為 Dirty。
        /// </summary>
        private void MarkDirty()
        {
            if (_table != null) EditorUtility.SetDirty(_table);
        }
    }
}
#endif
