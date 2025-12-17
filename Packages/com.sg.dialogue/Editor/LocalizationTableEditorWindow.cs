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
    /// 獨立的多國語言編輯器視窗：
    /// - 允許選取 LocalizationTable 資產。
    /// - 提供搜尋、檢視、編輯 zh-TW/ja-JP/en-US 語言內容與說話者欄位。
    /// - 支援新增、複製、刪除本地化條目。
    /// - 提供保存功能，以及 JSON 格式的匯入/匯出。
    /// </summary>
    public class LocalizationTableEditorWindow : EditorWindow
    {
        private ObjectField _tableField; // 用於選擇 LocalizationTable 資產的欄位
        private LocalizationTable _table; // 當前編輯的 LocalizationTable 資產

        private TextField _searchField; // 搜尋輸入框
        private Label _countLabel; // 顯示條目數量的標籤
        private ListView _listView; // 顯示本地化條目列表的視圖
        private List<LocalizationTable.Entry> _filtered = new List<LocalizationTable.Entry>(); // 經過篩選的條目列表
        private LocalizationTable.Entry _selected; // 當前選中的本地化條目

        // 詳細資訊面板的輸入欄位
        private TextField _keyField;
        private TextField _zhField;
        private TextField _jaField;
        private TextField _enField;
        private TextField _speakerZhField;
        private TextField _speakerJaField;
        private TextField _speakerEnField;

        /// <summary>
        /// 在 Unity 編輯器菜單中添加一個項目，用於打開本地化表格編輯器。
        /// </summary>
        [MenuItem("SG Framework/Dialogue/Localization Table Editor")]
        public static void Open()
        {
            var wnd = GetWindow<LocalizationTableEditorWindow>(); // 獲取或創建視窗實例
            wnd.titleContent = new GUIContent("Localization Table Editor"); // 設定視窗標題
            wnd.minSize = new Vector2(800, 520); // 設定視窗最小尺寸
            wnd.Show(); // 顯示視窗
        }

        /// <summary>
        /// 打開本地化表格編輯器並載入指定的 LocalizationTable。
        /// </summary>
        /// <param name="table">要載入的 LocalizationTable 資產。</param>
        /// <returns>視窗實例。</returns>
        public static LocalizationTableEditorWindow OpenWith(LocalizationTable table)
        {
            var wnd = GetWindow<LocalizationTableEditorWindow>();
            wnd.titleContent = new GUIContent("Localization Table Editor");
            wnd.minSize = new Vector2(800, 520);
            wnd.Show();
            wnd.Focus(); // 聚焦到視窗
            wnd.SetTable(table); // 設定表格
            return wnd;
        }

        /// <summary>
        /// 設定當前編輯的 LocalizationTable。
        /// </summary>
        /// <param name="table">要設定的 LocalizationTable 資產。</param>
        public void SetTable(LocalizationTable table)
        {
            _table = table;
            if (_tableField != null) _tableField.SetValueWithoutNotify(table); // 更新 ObjectField 的顯示
            RebuildList(); // 重新構建列表視圖
        }

        /// <summary>
        /// 創建視窗的 UI 佈局。
        /// </summary>
        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear(); // 清除現有元素
            root.style.flexDirection = FlexDirection.Column; // 設定為垂直佈局

            // 頂部工具列
            var top = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingTop = 4, paddingBottom = 4,
                    paddingLeft = 6, paddingRight = 6
                }
            };
            _tableField = new ObjectField("Localization Table")
            {
                objectType = typeof(LocalizationTable), // 只能選擇 LocalizationTable 類型的資產
                allowSceneObjects = false, // 不允許選擇場景物件
                style = { minWidth = 260, marginRight = 6 }
            };
            _tableField.RegisterValueChangedCallback(evt => // 註冊值改變回調
            {
                _table = evt.newValue as LocalizationTable; // 更新當前表格
                RebuildList(); // 重新構建列表
            });
            top.Add(_tableField);

            _searchField = new TextField("Search") { style = { flexGrow = 1f, marginRight = 6 } };
            _searchField.RegisterValueChangedCallback(_ => RebuildList()); // 搜尋框內容改變時重新構建列表
            top.Add(_searchField);

            _countLabel = new Label("0 items") { style = { marginRight = 6 } };
            top.Add(_countLabel);

            // 功能按鈕
            var addBtn = new Button(AddEntry) { text = "+ Add" };
            var dupBtn = new Button(DuplicateEntry) { text = "Duplicate" };
            var delBtn = new Button(DeleteEntry) { text = "Delete" };
            var saveBtn = new Button(SaveTable) { text = "Save" };
            top.Add(addBtn);
            top.Add(dupBtn);
            top.Add(delBtn);
            top.Add(saveBtn);

            var exportBtn = new Button(ExportJson) { text = "Export JSON" };
            var importBtn = new Button(ImportJson) { text = "Import JSON" };
            top.Add(exportBtn);
            top.Add(importBtn);

            root.Add(top);

            // 分割視圖 (左側列表，右側詳細資訊)
            var split = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, flexGrow = 1f, paddingLeft = 6, paddingRight = 6,
                    paddingBottom = 6
                }
            };

            // 左側列表視圖
            _listView = new ListView
            {
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight, // 動態高度虛擬化
                selectionType = SelectionType.Single, // 單選
                style = { flexBasis = 300, flexGrow = 0f, flexShrink = 0f } // 固定寬度
            };
            _listView.makeItem = () => new Label(); // 創建列表項的 UI
            _listView.bindItem = (ve, i) => // 綁定列表項的數據
            {
                var lbl = (Label)ve;
                if (i < 0 || i >= _filtered.Count)
                {
                    lbl.text = string.Empty;
                    return;
                }

                var e = _filtered[i];
                var preview = e.zhTW;
                if (!string.IsNullOrEmpty(preview))
                {
                    preview = preview.Replace("\n", " "); // 替換換行符
                    if (preview.Length > 30) preview = preview.Substring(0, 30) + "…"; // 截斷預覽文字
                }

                lbl.text = string.IsNullOrEmpty(e.key) ? "<no key>" : e.key; // 顯示 Key
                if (!string.IsNullOrEmpty(preview)) lbl.text += $"  ·  {preview}"; // 顯示預覽文字
            };
            _listView.selectionChanged += OnSelectionChanged; // 註冊選中項改變回調
            split.Add(_listView);

            // 右側詳細資訊面板
            var detail = new ScrollView { style = { flexGrow = 1f, paddingLeft = 8 } };
            _keyField = new TextField("Key");
            _keyField.RegisterValueChangedCallback(evt => // 註冊 Key 改變回調
            {
                if (_selected == null) return;
                var newKey = evt.newValue?.Trim() ?? string.Empty;
                if (newKey == _selected.key) return;
                if (!IsKeyUnique(newKey, _selected)) // 檢查 Key 是否重複
                {
                    EditorUtility.DisplayDialog("Key 重複", $"Key '{newKey}' 已存在，請使用其他 key。", "OK");
                    _keyField.SetValueWithoutNotify(_selected.key); // 恢復舊的 Key
                    return;
                }

                _selected.key = newKey;
                MarkDirty(); // 標記資產為 Dirty
                RebuildList(keepSelection: true); // 重新構建列表並保持選中
            });
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

            var spHeader = new Label("Speaker (說話者)")
                { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6 } };
            detail.Add(spHeader);

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
            root.Add(split);

            // 初始設定
            if (_table != null) _tableField.SetValueWithoutNotify(_table);
            RebuildList();
        }

        /// <summary>
        /// 當列表選中項改變時調用，更新詳細資訊面板的顯示。
        /// </summary>
        /// <param name="objs">選中的物件列表。</param>
        private void OnSelectionChanged(IEnumerable<object> objs)
        {
            var entry = objs != null ? objs.OfType<LocalizationTable.Entry>().FirstOrDefault() : null;
            _selected = entry;

            // 如果沒有選中項，則清空詳細資訊面板
            if (_selected == null)
            {
                _keyField?.SetValueWithoutNotify(string.Empty);
                _zhField?.SetValueWithoutNotify(string.Empty);
                _jaField?.SetValueWithoutNotify(string.Empty);
                _enField?.SetValueWithoutNotify(string.Empty);
                _speakerZhField?.SetValueWithoutNotify(string.Empty);
                _speakerJaField?.SetValueWithoutNotify(string.Empty);
                _speakerEnField?.SetValueWithoutNotify(string.Empty);
                return;
            }

            // 填充詳細資訊面板
            _keyField?.SetValueWithoutNotify(_selected.key ?? string.Empty);
            _zhField?.SetValueWithoutNotify(_selected.zhTW ?? string.Empty);
            _jaField?.SetValueWithoutNotify(_selected.jaJP ?? string.Empty);
            _enField?.SetValueWithoutNotify(_selected.enUS ?? string.Empty);
            _speakerZhField?.SetValueWithoutNotify(_selected.speakerZhTW ?? string.Empty);
            _speakerJaField?.SetValueWithoutNotify(_selected.speakerJaJP ?? string.Empty);
            _speakerEnField?.SetValueWithoutNotify(_selected.speakerEnUS ?? string.Empty);
        }

        /// <summary>
        /// 重新構建列表視圖，應用搜尋篩選和排序。
        /// </summary>
        /// <param name="keepSelection">是否嘗試保持當前選中項。</param>
        private void RebuildList(bool keepSelection = false)
        {
            if (_listView == null) return;
            int prevIndex = keepSelection ? _listView.selectedIndex : -1;
            var prevObj = keepSelection && prevIndex >= 0 && prevIndex < _filtered.Count ? _filtered[prevIndex] : null;

            if (_table == null)
            {
                _filtered = new List<LocalizationTable.Entry>();
                _listView.itemsSource = _filtered;
                _listView.Rebuild();
                _countLabel.text = "No table selected";
                OnSelectionChanged(null);
                return;
            }

            var q = _searchField != null ? (_searchField.value ?? string.Empty).Trim() : string.Empty;
            IEnumerable<LocalizationTable.Entry> seq = _table.entries;
            if (!string.IsNullOrEmpty(q))
            {
                // 根據搜尋關鍵字篩選條目
                seq = seq.Where(e =>
                    (!string.IsNullOrEmpty(e.key) && e.key.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(e.zhTW) && e.zhTW.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(e.jaJP) && e.jaJP.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(e.enUS) && e.enUS.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }

            _filtered = seq.OrderBy(e => e.key).ToList(); // 排序並更新篩選列表
            _listView.itemsSource = _filtered;
            // 移除非法的負值 fixedItemHeight 設定，改由 DynamicHeight 自行處理高度以避免 ArgumentOutOfRangeException。
            // 若想改回固定高度，請改用正值，例如：_listView.fixedItemHeight = 18f;
            _listView.Rebuild(); // 重新繪製列表
            _countLabel.text = _filtered.Count + " items"; // 更新條目數量顯示

            // 處理選中項的恢復邏輯
            if (keepSelection && prevObj != null)
            {
                int newIndex = _filtered.IndexOf(prevObj);
                if (newIndex >= 0)
                {
                    _listView.SetSelection(newIndex);
                    OnSelectionChanged(new[] { prevObj });
                }
                else if (_filtered.Count > 0)
                {
                    _listView.SetSelection(0);
                    OnSelectionChanged(new[] { _filtered[0] });
                }
                else
                {
                    _listView.ClearSelection();
                    OnSelectionChanged(null);
                }
            }
            else
            {
                if (_filtered.Count > 0)
                {
                    _listView.SetSelection(0);
                    OnSelectionChanged(new[] { _filtered[0] });
                }
                else
                {
                    _listView.ClearSelection();
                    OnSelectionChanged(null);
                }
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
            var idx = _filtered.IndexOf(e);
            if (idx >= 0) _listView.SetSelection(idx); // 選中新添加的條目
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
                zhTW = _selected.zhTW,
                jaJP = _selected.jaJP,
                enUS = _selected.enUS,
                speakerZhTW = _selected.speakerZhTW,
                speakerJaJP = _selected.speakerJaJP,
                speakerEnUS = _selected.speakerEnUS
            };
            _table.entries.Add(e);
            MarkDirty();
            RebuildList();
            var idx = _filtered.IndexOf(e);
            if (idx >= 0) _listView.SetSelection(idx);
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
            if (_table == null)
            {
                EditorUtility.DisplayDialog("Export JSON", "請先選擇 LocalizationTable。", "OK");
                return;
            }

            string defaultFolder = "Assets/Dialogue/Localization";
            if (!Directory.Exists(defaultFolder))
            {
                Directory.CreateDirectory(defaultFolder); // 如果資料夾不存在則創建
            }

            // 打開保存檔案對話框
            string path = EditorUtility.SaveFilePanel("Export Localization JSON", defaultFolder, _table.name + ".json",
                "json");
            if (string.IsNullOrEmpty(path)) return;

            // 構建要匯出的 JSON 數據結構
            var export = new DialogueLocalizationWindow.LocalizationTableJson
            {
                entries = new List<DialogueLocalizationWindow.LocalizationTableEntryJson>()
            };

            foreach (var e in _table.entries)
            {
                if (string.IsNullOrEmpty(e.key)) continue;
                export.entries.Add(new DialogueLocalizationWindow.LocalizationTableEntryJson
                {
                    key = e.key,
                    zhTW = e.zhTW,
                    jaJP = e.jaJP,
                    enUS = e.enUS,
                    speakerZhTW = e.speakerZhTW,
                    speakerJaJP = e.speakerJaJP,
                    speakerEnUS = e.speakerEnUS
                });
            }

            var json = JsonUtility.ToJson(export, true); // 將數據序列化為 JSON 字符串
            File.WriteAllText(path, json, new UTF8Encoding(true)); // 寫入檔案
            EditorUtility.RevealInFinder(path); // 在檔案管理器中顯示檔案
        }

        /// <summary>
        /// 從 JSON 檔案匯入數據到當前 LocalizationTable。
        /// </summary>
        private void ImportJson()
        {
            if (_table == null)
            {
                EditorUtility.DisplayDialog("Import JSON", "請先選擇 LocalizationTable。", "OK");
                return;
            }

            string defaultFolder = "Assets/Dialogue/Localization";
            // 打開檔案對話框
            string path = EditorUtility.OpenFilePanel("Import Localization JSON", defaultFolder, "json");
            if (string.IsNullOrEmpty(path)) return;

            var json = File.ReadAllText(path, Encoding.UTF8); // 讀取 JSON 檔案內容
            var data = JsonUtility.FromJson<DialogueLocalizationWindow.LocalizationTableJson>(json); // 反序列化 JSON 數據
            if (data == null || data.entries == null)
            {
                EditorUtility.DisplayDialog("Import Localization JSON", "檔案沒有有效資料", "OK");
                return;
            }

            // 建立現有條目的查找字典
            var dict = new Dictionary<string, LocalizationTable.Entry>();
            foreach (var e in _table.entries)
            {
                if (!string.IsNullOrEmpty(e.key) && !dict.ContainsKey(e.key))
                    dict.Add(e.key, e);
            }

            int updateCount = 0;
            int addCount = 0;

            // 遍歷匯入的條目，更新或新增
            foreach (var item in data.entries)
            {
                if (string.IsNullOrEmpty(item.key)) continue;

                if (!dict.TryGetValue(item.key, out var entry))
                {
                    entry = new LocalizationTable.Entry { key = item.key };
                    _table.entries.Add(entry);
                    dict.Add(item.key, entry);
                    addCount++;
                }
                else
                {
                    updateCount++;
                }

                // 更新條目內容
                entry.zhTW = item.zhTW;
                entry.jaJP = item.jaJP;
                entry.enUS = item.enUS;
                entry.speakerZhTW = item.speakerZhTW;
                entry.speakerJaJP = item.speakerJaJP;
                entry.speakerEnUS = item.speakerEnUS;
            }

            EditorUtility.SetDirty(_table); // 標記資產為 Dirty
            AssetDatabase.SaveAssets(); // 保存資產

            EditorUtility.DisplayDialog("Import Localization JSON",
                $"匯入完成\n更新：{updateCount} 筆\n新增：{addCount} 筆",
                "OK");

            RebuildList(keepSelection: true); // 重新構建列表並保持選中
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
                candidate = baseKey + "_" + i;
                i++;
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
            if (_table == null || string.IsNullOrEmpty(key)) return true;
            foreach (var e in _table.entries)
            {
                if (e != null && e != except && e.key == key) return false;
            }

            return true;
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
