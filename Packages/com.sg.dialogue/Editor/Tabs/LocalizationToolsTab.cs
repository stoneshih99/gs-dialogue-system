#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// LocalizationToolsTab 是一個 VisualElement，提供了與對話圖和本地化表格相關的工具功能。
    /// 這些工具包括圖表驗證、本地化數據的匯入匯出，以及本地化鍵的同步。
    /// </summary>
    public class LocalizationToolsTab : VisualElement
    {
        /// <summary>
        /// 當對話圖被選中時觸發的事件。
        /// </summary>
        public Action<DialogueGraph> OnGraphSelected;
        /// <summary>
        /// 當本地化表格被選中時觸發的事件。
        /// </summary>
        public Action<LocalizationTable> OnTableSelected;

        private DialogueGraph _graph; // 當前選中的對話圖
        private LocalizationTable _table; // 當前選中的本地化表格

        private readonly ObjectField _graphField; // 用於選擇對話圖的 ObjectField
        private readonly ObjectField _tableField; // 用於選擇本地化表格的 ObjectField
        
        // 預設的本地化文件夾路徑和預設表格資產路徑
        private const string DefaultLocalizationFolder = "Assets/Dialogue/Localization";
        private const string DefaultTablePath = "Assets/Dialogue/DialogueLocalizationTable.asset";

        /// <summary>
        /// 構造函數，初始化 UI 佈局和功能按鈕。
        /// </summary>
        public LocalizationToolsTab()
        {
            style.flexGrow = 1;
            style.paddingLeft = 6;
            style.paddingRight = 6;
            style.paddingTop = 4;
            style.paddingBottom = 4;

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            Add(row);

            // 左側：對話圖工具箱
            var graphBox = new Box { style = { flexGrow = 1, marginRight = 4 } };
            graphBox.Add(new Label("Graph Tools") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            _graphField = new ObjectField("Dialogue Graph") { objectType = typeof(DialogueGraph), allowSceneObjects = false };
            _graphField.RegisterValueChangedCallback(evt => OnGraphSelected?.Invoke(evt.newValue as DialogueGraph));
            graphBox.Add(_graphField);
            
            var graphButtonsRow1 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            graphButtonsRow1.Add(new Button(ValidateCurrentGraph) { text = "Validate Graph", tooltip = "檢查當前圖表是否有懸空連線、孤島節點等錯誤。" });
            graphBox.Add(graphButtonsRow1);
            
            var graphButtonsRow2 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            graphButtonsRow2.Add(new Button(ExportGraphLocalizationJson) { text = "Export Loc (JSON)", tooltip = "將目前 DialogueGraph 的本地化資料匯出為 JSON 檔案。" });
            graphButtonsRow2.Add(new Button(ImportGraphLocalizationJson) { text = "Import Loc (JSON)", tooltip = "從 JSON 檔匯入本地化資料，更新 DialogueGraph 的文字與說話者，並可選擇是否更新 LocalizationTable。" });
            graphBox.Add(graphButtonsRow2);
            row.Add(graphBox);

            // 右側：本地化表格工具箱
            var tableBox = new Box { style = { flexGrow = 1, marginLeft = 4 } };
            tableBox.Add(new Label("Localization Table Tools") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            _tableField = new ObjectField("Localization Table") { objectType = typeof(LocalizationTable), allowSceneObjects = false };
            _tableField.RegisterValueChangedCallback(evt => OnTableSelected?.Invoke(evt.newValue as LocalizationTable));
            tableBox.Add(_tableField);

            var tableButtonsRow1 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            tableButtonsRow1.Add(new Button(SyncKeysFromGraphToTable) { text = "Sync Keys From Graph", tooltip = "從選取的 DialogueGraph 收集 key，並同步到目前選取的 LocalizationTable 中。" });
            tableBox.Add(tableButtonsRow1);

            var tableButtonsRow2 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            tableButtonsRow2.Add(new Button(ExportTableLocalizationJson) { text = "Export Table JSON", tooltip = "將 LocalizationTable 匯出為 JSON 檔案。" });
            tableButtonsRow2.Add(new Button(ImportTableLocalizationJson) { text = "Import Table JSON", tooltip = "從 JSON 檔匯入 LocalizationTable 內容。" });
            tableButtonsRow2.Add(new Button(SyncKeysFromGraphAndExportTableJson) { text = "Sync + Export JSON", tooltip = "一鍵同步選取的 DialogueGraph 的 key 到當前 LocalizationTable，並立刻匯出 JSON。" });
            tableBox.Add(tableButtonsRow2);
            row.Add(tableBox);
        }

        /// <summary>
        /// 驗證當前選中的對話圖。
        /// </summary>
        private void ValidateCurrentGraph()
        {
            if (_graph == null)
            {
                EditorUtility.DisplayDialog("Validation", "Please select a DialogueGraph to validate.", "OK");
                return;
            }

            var issues = DialogueGraphValidator.Validate(_graph); // 呼叫驗證器進行驗證

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Result", "Graph is valid! No issues found.", "OK");
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Found {issues.Count} issue(s) in '{_graph.name}':");
                sb.AppendLine();
                foreach (var issue in issues)
                {
                    sb.AppendLine($"- {issue}");
                }
                
                Debug.LogWarning(sb.ToString()); // 在 Console 中顯示警告
                EditorUtility.DisplayDialog("Validation Result", "Issues found! Check the Console for details.", "OK");
            }
        }

        /// <summary>
        /// 設定當前選中的對話圖。
        /// </summary>
        /// <param name="graph">要設定的對話圖。</param>
        public void SetGraph(DialogueGraph graph)
        {
            _graph = graph;
            // 解除註冊並重新註冊回調，以避免重複觸發
            _graphField.UnregisterValueChangedCallback(OnGraphFieldValueChanged);
            _graphField.SetValueWithoutNotify(graph);
            _graphField.RegisterValueChangedCallback(OnGraphFieldValueChanged);
        }

        /// <summary>
        /// 設定當前選中的本地化表格。
        /// </summary>
        /// <param name="table">要設定的本地化表格。</param>
        public void SetTable(LocalizationTable table)
        {
            _table = table;
            // 解除註冊並重新註冊回調，以避免重複觸發
            _tableField.UnregisterValueChangedCallback(OnTableFieldValueChanged);
            _tableField.SetValueWithoutNotify(table);
            _tableField.RegisterValueChangedCallback(OnTableFieldValueChanged);
        }
        
        /// <summary>
        /// 對話圖 ObjectField 值改變時的回調。
        /// </summary>
        /// <param name="evt">改變事件。</param>
        private void OnGraphFieldValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            OnGraphSelected?.Invoke(evt.newValue as DialogueGraph);
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
        /// 將當前對話圖的本地化數據匯出為 JSON 檔案。
        /// </summary>
        private void ExportGraphLocalizationJson()
        {
            if (_graph == null) { EditorUtility.DisplayDialog("Export Loc (Graph JSON)", "請先指定一個 DialogueGraph。", "OK"); return; }
            
            var textNodes = _graph.AllNodes.OfType<TextNode>().ToList();

            // 為沒有 textKey 的 TextNode 自動生成 textKey
            foreach (var node in textNodes.Where(n => string.IsNullOrEmpty(n.textKey)))
            {
                node.textKey = $"{_graph.name}_{node.nodeId}";
                EditorUtility.SetDirty(_graph);
            }

            // 打開保存檔案對話框
            var path = EditorUtility.SaveFilePanel("Export Dialogue Localization (JSON)", DefaultLocalizationFolder, $"{_graph.name}_Loc", "json");
            if (string.IsNullOrEmpty(path)) return;

            // 構建匯出數據
            var export = new DialogueLocalizationWindow.GraphLocalizationJson { graphName = _graph.name, nodes = new List<DialogueLocalizationWindow.GraphLocalizationNodeJson>() };
            foreach (var node in textNodes)
            {
                export.nodes.Add(new DialogueLocalizationWindow.GraphLocalizationNodeJson { nodeId = node.nodeId, key = node.textKey, speaker = node.speakerName, zhTW = node.text });
            }
            File.WriteAllText(path, JsonUtility.ToJson(export, true), new UTF8Encoding(true)); // 寫入 JSON 檔案
            EditorUtility.RevealInFinder(path); // 在檔案管理器中顯示
        }

        /// <summary>
        /// 從 JSON 檔案匯入本地化數據，更新 DialogueGraph 的文字與說話者，並可選擇是否更新 LocalizationTable。
        /// </summary>
        private void ImportGraphLocalizationJson()
        {
            if (_graph == null) { EditorUtility.DisplayDialog("Import Loc (Graph JSON)", "請先指定一個 DialogueGraph。", "OK"); return; }
            var path = EditorUtility.OpenFilePanel("Import Dialogue Localization (JSON)", DefaultLocalizationFolder, "json");
            if (string.IsNullOrEmpty(path)) return;

            var table = _table;
            // 如果沒有選中表格，則詢問是否同時更新 LocalizationTable
            if (table == null && EditorUtility.DisplayDialog("Import Loc (Graph JSON)", "是否同時更新 LocalizationTable？", "選擇表", "只更新 Graph"))
            {
                var tableAssetPath = EditorUtility.OpenFilePanel("Select LocalizationTable asset", "Assets", "asset");
                if (!string.IsNullOrEmpty(tableAssetPath))
                {
                    table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(tableAssetPath.Replace(Application.dataPath, "Assets"));
                    OnTableSelected?.Invoke(table); // 更新選中的表格
                }
            }

            var data = JsonUtility.FromJson<DialogueLocalizationWindow.GraphLocalizationJson>(File.ReadAllText(path, Encoding.UTF8));
            if (data?.nodes == null) { Debug.LogWarning("[Localization] JSON is empty or invalid."); return; }

            // 建立節點和表格條目的查找字典
            var keyToNode = _graph.AllNodes.OfType<TextNode>().Where(n => !string.IsNullOrEmpty(n.textKey)).ToDictionary(n => n.textKey);
            var tableLookup = table?.entries.Where(e => !string.IsNullOrEmpty(e.key)).ToDictionary(e => e.key);

            foreach (var item in data.nodes.Where(i => !string.IsNullOrEmpty(i.key)))
            {
                // 更新對話圖中的 TextNode
                if (keyToNode.TryGetValue(item.key, out var node))
                {
                    if (!string.IsNullOrEmpty(item.zhTW)) node.text = item.zhTW;
                    if (!string.IsNullOrEmpty(item.speaker)) node.speakerName = item.speaker;
                }

                // 更新 LocalizationTable
                if (table != null && tableLookup != null)
                {
                    if (!tableLookup.TryGetValue(item.key, out var entry))
                    {
                        entry = new LocalizationTable.Entry { key = item.key };
                        table.entries.Add(entry);
                        tableLookup[item.key] = entry;
                    }
                    if (!string.IsNullOrEmpty(item.zhTW)) entry.zhTW = item.zhTW;
                    if (!string.IsNullOrEmpty(item.jaJP)) entry.jaJP = item.jaJP;
                    if (!string.IsNullOrEmpty(item.enUS)) entry.enUS = item.enUS;
                    if (!string.IsNullOrEmpty(item.speaker)) entry.speakerZhTW = item.speaker;
                }
            }
            EditorUtility.SetDirty(_graph); // 標記對話圖為 Dirty
            if (table != null) EditorUtility.SetDirty(table); // 標記表格為 Dirty
            AssetDatabase.SaveAssets(); // 保存資產
        }

        /// <summary>
        /// 創建或選取預設的 LocalizationTable 資產。
        /// </summary>
        private void CreateOrSelectDefaultTable()
        {
            var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(DefaultTablePath);
            if (table == null)
            {
                var dir = Path.GetDirectoryName(DefaultTablePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir); // 創建目錄
                table = ScriptableObject.CreateInstance<LocalizationTable>(); // 創建新的表格資產
                AssetDatabase.CreateAsset(table, DefaultTablePath); // 保存資產
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Localization Table", $"已建立預設 LocalizationTable：{DefaultTablePath}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Localization Table", "已選取既有預設 LocalizationTable。", "OK");
            }
            OnTableSelected?.Invoke(table); // 更新選中的表格
            Selection.activeObject = table; // 在項目視窗中選中表格
            EditorGUIUtility.PingObject(table); // 閃爍表格資產
        }

        /// <summary>
        /// 從選取的 DialogueGraph 收集本地化鍵，並同步到目前選取的 LocalizationTable 中。
        /// 注意：此操作會先清空 LocalizationTable 中的所有現有資料。
        /// </summary>
        private void SyncKeysFromGraphToTable()
        {
            if (_table == null) { EditorUtility.DisplayDialog("Sync Keys", "請先選擇 LocalizationTable。", "OK"); return; }
            if (_graph == null) { EditorUtility.DisplayDialog("Sync Keys", "請先選擇 DialogueGraph。", "OK"); return; }

            if (!EditorUtility.DisplayDialog("確認同步", $"即將清空 '{_table.name}' 的所有內容，並從 '{_graph.name}' 同步新的 key。\n\n確定要繼續嗎？", "確定同步", "取消"))
            {
                return;
            }

            bool fillFromGraph = EditorUtility.DisplayDialog("Sync Keys", "要不要用目前 Graph 節點上的文字，初始化新 key 的 zh-TW 內容？", "是，填入目前文字", "否，全部留空");

            _table.entries.Clear();

            var allKeysFromGraph = new HashSet<string>();
            var defaultZhFromGraph = fillFromGraph ? new Dictionary<string, string>() : null;
            
            bool graphDirty = false;
            CollectKeysRecursively(_graph.AllNodes, ref graphDirty, allKeysFromGraph, defaultZhFromGraph);

            if (graphDirty) EditorUtility.SetDirty(_graph);

            int added = 0;
            foreach (var key in allKeysFromGraph.Where(k => !string.IsNullOrEmpty(k)))
            {
                var entry = new LocalizationTable.Entry { key = key };
                if (fillFromGraph && defaultZhFromGraph != null && defaultZhFromGraph.TryGetValue(key, out var text))
                {
                    entry.zhTW = text;
                }
                else
                {
                    entry.zhTW = "";
                }
                _table.entries.Add(entry);
                added++;
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(_table);
                AssetDatabase.SaveAssets();
            }
            EditorUtility.DisplayDialog("Sync Keys", $"同步完成！\n\n'{_table.name}' 已被清空，並從 '{_graph.name}' 新增了 {added} 筆 key。", "OK");
        }

        private void CollectKeysRecursively(List<DialogueNodeBase> nodes, ref bool graphDirty, HashSet<string> allKeys, Dictionary<string, string> defaultTexts)
        {
            if (nodes == null) return;

            string graphIdSafe = SanitizeKey(string.IsNullOrEmpty(_graph.graphId) ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(_graph)) : _graph.graphId);

            foreach (var node in nodes)
            {
                if (node is TextNode textNode)
                {
                    if (string.IsNullOrEmpty(textNode.textKey))
                    {
                        textNode.textKey = $"{_graph.name}_{textNode.nodeId}";
                        graphDirty = true;
                    }
                    allKeys.Add(textNode.textKey);
                    if (defaultTexts != null && !defaultTexts.ContainsKey(textNode.textKey))
                    {
                        defaultTexts[textNode.textKey] = textNode.text ?? "";
                    }
                }
                else if (node is StageTextNode stageTextNode)
                {
                    if (string.IsNullOrEmpty(stageTextNode.messageKey))
                    {
                        stageTextNode.messageKey = $"{_graph.name}_{stageTextNode.nodeId}";
                        graphDirty = true;
                    }
                    allKeys.Add(stageTextNode.messageKey);
                    if (defaultTexts != null && !defaultTexts.ContainsKey(stageTextNode.messageKey))
                    {
                        defaultTexts[stageTextNode.messageKey] = stageTextNode.message ?? "";
                    }
                }
                else if (node is ChoiceNode choiceNode)
                {
                    string nodeIdSafe = SanitizeKey(string.IsNullOrEmpty(choiceNode.nodeId) ? "NODE" : choiceNode.nodeId);
                    for (int i = 0; i < choiceNode.choices.Count; i++)
                    {
                        string key = $"{graphIdSafe}_CHOICE_{nodeIdSafe}_{i}";
                        allKeys.Add(key);
                        if (defaultTexts != null && !defaultTexts.ContainsKey(key))
                        {
                            defaultTexts[key] = choiceNode.choices[i].text ?? "";
                        }
                    }
                }
                else if (node is SequenceNode sequenceNode)
                {
                    CollectKeysRecursively(sequenceNode.childNodes, ref graphDirty, allKeys, defaultTexts);
                }
                else if (node is ParallelNode parallelNode)
                {
                    CollectKeysRecursively(parallelNode.childNodes, ref graphDirty, allKeys, defaultTexts);
                }
            }
        }

        /// <summary>
        /// 將當前 LocalizationTable 匯出為 JSON 檔案。
        /// </summary>
        private void ExportTableLocalizationJson()
        {
            if (_table == null) { EditorUtility.DisplayDialog("Export Table JSON", "請先選擇 LocalizationTable。", "OK"); return; }
            var path = EditorUtility.SaveFilePanel("Export Localization JSON", DefaultLocalizationFolder, $"{_table.name}.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            var export = new DialogueLocalizationWindow.LocalizationTableJson { entries = new List<DialogueLocalizationWindow.LocalizationTableEntryJson>() };
            foreach (var e in _table.entries.Where(entry => !string.IsNullOrEmpty(entry.key)))
            {
                export.entries.Add(new DialogueLocalizationWindow.LocalizationTableEntryJson { key = e.key, zhTW = e.zhTW, jaJP = e.jaJP, enUS = e.enUS, speakerZhTW = e.speakerZhTW, speakerJaJP = e.speakerJaJP, speakerEnUS = e.speakerEnUS });
            }
            File.WriteAllText(path, JsonUtility.ToJson(export, true), new UTF8Encoding(true));
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>
        /// 從 JSON 檔案匯入數據到當前 LocalizationTable。
        /// </summary>
        private void ImportTableLocalizationJson()
        {
            if (_table == null) { EditorUtility.DisplayDialog("Import Table JSON", "請先選擇 LocalizationTable。", "OK"); return; }
            var path = EditorUtility.OpenFilePanel("Import Localization JSON", DefaultLocalizationFolder, "json");
            if (string.IsNullOrEmpty(path)) return;

            var data = JsonUtility.FromJson<DialogueLocalizationWindow.LocalizationTableJson>(File.ReadAllText(path, Encoding.UTF8));
            if (data?.entries == null) { EditorUtility.DisplayDialog("Import Table JSON", "檔案沒有有效資料", "OK"); return; }

            var dict = _table.entries.Where(e => !string.IsNullOrEmpty(e.key)).ToDictionary(e => e.key);
            int updateCount = 0, addCount = 0;
            foreach (var item in data.entries.Where(i => !string.IsNullOrEmpty(i.key)))
            {
                if (dict.TryGetValue(item.key, out var entry)) { updateCount++; }
                else { entry = new LocalizationTable.Entry { key = item.key }; _table.entries.Add(entry); addCount++; }
                entry.zhTW = item.zhTW; entry.jaJP = item.jaJP; entry.enUS = item.enUS;
                entry.speakerZhTW = item.speakerZhTW; entry.speakerJaJP = item.speakerJaJP; entry.speakerEnUS = item.speakerEnUS;
            }
            EditorUtility.SetDirty(_table);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Import Table JSON", $"匯入完成\n更新：{updateCount} 筆\n新增：{addCount} 筆", "OK");
        }

        /// <summary>
        /// 一鍵同步選取的 DialogueGraph 的鍵到當前 LocalizationTable，並立刻匯出 JSON。
        /// </summary>
        private void SyncKeysFromGraphAndExportTableJson()
        {
            if (_table == null) { EditorUtility.DisplayDialog("Sync + Export JSON", "請先選擇 LocalizationTable。", "OK"); return; }
            if (_graph == null) { EditorUtility.DisplayDialog("Sync + Export JSON", "請先選擇 DialogueGraph。", "OK"); return; }
            SyncKeysFromGraphToTable(); // 同步鍵
            ExportTableLocalizationJson(); // 匯出 JSON
        }

        /// <summary>
        /// 清理輸入字符串，使其適合作為 Key（只保留字母、數字和下劃線）。
        /// </summary>
        /// <param name="input">原始字符串。</param>
        /// <returns>清理後的字符串。</returns>
        private static string SanitizeKey(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var sb = new StringBuilder(input.Length);
            foreach (var ch in input) { sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_'); }
            return sb.ToString();
        }
    }
}
#endif
