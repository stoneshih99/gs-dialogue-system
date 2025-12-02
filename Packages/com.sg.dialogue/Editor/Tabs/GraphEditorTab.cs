#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// GraphEditorTab 是一個 VisualElement，用於在編輯器中顯示和編輯對話圖。
    /// 它包含了對話圖視圖、資產選擇器、保存按鈕、語言選擇和本地化同步功能。
    /// </summary>
    public class GraphEditorTab : VisualElement
    {
        /// <summary>
        /// 當對話圖被選中時觸發的事件。
        /// </summary>
        public Action<DialogueGraph> OnGraphSelected;
        /// <summary>
        /// 當本地化表格被選中時觸發的事件。
        /// </summary>
        public Action<LocalizationTable> OnTableSelected;
        /// <summary>
        /// 當全域對話狀態資產被選中時觸發的事件。
        /// </summary>
        public Action<DialogueStateAsset> OnStateSelected;

        private DialogueGraphView _graphView; // 對話圖的視覺化視圖
        private ObjectField _graphField; // 用於選擇對話圖資產的欄位
        private ObjectField _stateField; // 用於選擇全域對話狀態資產的欄位
        private VisualElement _breadcrumbContainer; // 用於顯示導航路徑的容器
        
        private DialogueGraph _graph; // 當前編輯的對話圖數據
        private LocalizationTable _table; // 當前選中的本地化表格
        private DialogueStateAsset _state; // 當前選中的全域對話狀態資產
        private EnumField _languageField; // 語言選擇下拉選單
        private LocalizationLanguage _currentLanguage = LocalizationLanguage.ZhTw; // 當前選中的語言

        /// <summary>
        /// 構造函數，初始化 UI 佈局和功能。
        /// </summary>
        public GraphEditorTab()
        {
            style.flexGrow = 1; // 讓分頁佔滿可用空間
            style.flexDirection = FlexDirection.Column; // 設定為垂直佈局

            var header = new Toolbar(); // 頂部工具列
            header.style.paddingLeft = 6;
            header.style.paddingRight = 6;

            // 對話圖資產選擇欄位
            _graphField = new ObjectField("Graph") { objectType = typeof(DialogueGraph), allowSceneObjects = false, style = { minWidth = 250 } };
            header.Add(_graphField);

            // 全域狀態資產選擇欄位
            _stateField = new ObjectField("Global State") { objectType = typeof(DialogueStateAsset), allowSceneObjects = false, style = { minWidth = 250 } };
            header.Add(_stateField);

            // 保存按鈕，用於保存對話圖資產
            header.Add(new Button(() => { if (_graph != null) { _graphView?.SyncPositionsToAsset(); EditorUtility.SetDirty(_graph); AssetDatabase.SaveAssets(); } }) { text = "Save" });
            // 框選所有節點按鈕
            header.Add(new Button(() => _graphView?.FrameGraph()) { text = "Frame All", tooltip = "Focus on the start node or frame all nodes in the view." });

            // 語言選擇下拉選單
            _languageField = new EnumField("Lang", _currentLanguage) { style = { minWidth = 120 } };
            _languageField.RegisterValueChangedCallback(evt => _currentLanguage = (LocalizationLanguage)evt.newValue);
            header.Add(_languageField);

            // 同步本地化文本到圖表按鈕
            header.Add(new Button(SyncLocalizedTextFromTableToGraph) { text = "Sync Loc → Text", tooltip = "使用目前選取的 LocalizationTable，依照語言下拉選擇的語言，將每個 TextNode 的 text 與 speakerName 同步為該語言版本，方便在 Graph 分頁預覽多語系。" });
            
            Add(header);

            // 導航容器
            _breadcrumbContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingLeft = 6, height = 20, backgroundColor = new Color(0.2f, 0.2f, 0.2f) } };
            Add(_breadcrumbContainer);

            // 對話圖視圖
            _graphView = new DialogueGraphView { style = { flexGrow = 1 } };
            _graphView.OnNavigationChanged = UpdateBreadcrumbs; // 註冊導航改變事件的回調
            Add(_graphView);
        }

        /// <summary>
        /// 更新導航顯示。
        /// </summary>
        /// <param name="navigationStack">當前的導航堆疊。</param>
        private void UpdateBreadcrumbs(Stack<object> navigationStack)
        {
            _breadcrumbContainer.Clear(); // 清空現有航顯示
            var stackArray = navigationStack.Reverse().ToArray(); // 反轉堆疊以正確顯示順序
            for (int i = 0; i < stackArray.Length; i++)
            {
                var container = stackArray[i];
                string name = "Unknown";
                if (container is DialogueGraph g) name = g.name; // 如果是對話圖，顯示其名稱
                else if (container is SequenceNode s) name = s.sequenceName; // 調整回使用 sequenceName
                else if (container is ParallelNode p) name = p.parallelName; // 調整回使用 parallelName
                
                if (i > 0) _breadcrumbContainer.Add(new Label(" > ")); // 添加分隔符

                if (i < stackArray.Length - 1) // 如果不是最後一個元素，則為可點擊的按鈕
                {
                    var button = new Button(() =>
                    {
                        // 點擊按鈕時，導航回該層級
                        while (_graphView.NavigationStack.Peek() != container)
                        {
                            _graphView.NavigateBack();
                        }
                    }) { text = name };
                    _breadcrumbContainer.Add(button);
                }
                else // 最後一個元素只是標籤
                {
                    _breadcrumbContainer.Add(new Label(name));
                }
            }
        }

        /// <summary>
        /// 設定當前編輯的對話圖。
        /// </summary>
        /// <param name="graph">要設定的對話圖。</param>
        public void SetGraph(DialogueGraph graph)
        {
            _graph = graph;
            // 解除註冊並重新註冊回調，以避免重複觸發
            _graphField.UnregisterValueChangedCallback(OnGraphFieldValueChanged);
            _graphField.SetValueWithoutNotify(graph);
            _graphField.RegisterValueChangedCallback(OnGraphFieldValueChanged);
            _graphView.PopulateView(graph); // 填充對話圖視圖
        }
        
        /// <summary>
        /// 設定當前選中的本地化表格。
        /// </summary>
        /// <param name="table">要設定的本地化表格。</param>
        public void SetTable(LocalizationTable table)
        {
            _table = table;
        }

        /// <summary>
        /// 設定當前選中的全域對話狀態資產。
        /// </summary>
        /// <param name="state">要設定的狀態資產。</param>
        public void SetState(DialogueStateAsset state)
        {
            _state = state;
            // 解除註冊並重新註冊回調，以避免重複觸發
            _stateField.UnregisterValueChangedCallback(OnStateFieldValueChanged);
            _stateField.SetValueWithoutNotify(state);
            _stateField.RegisterValueChangedCallback(OnStateFieldValueChanged);
            _graphView.SetGlobalState(state); // 設定對話圖視圖的全域狀態
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
        /// 全域狀態 ObjectField 值改變時的回調。
        /// </summary>
        /// <param name="evt">改變事件。</param>
        private void OnStateFieldValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            OnStateSelected?.Invoke(evt.newValue as DialogueStateAsset);
        }

        /// <summary>
        /// 保存對話圖視圖的變換狀態（位置和縮放）。
        /// </summary>
        public void SaveGraphViewState()
        {
            _graphView?.SaveViewTransform();
        }

        /// <summary>
        /// 使用目前選取的 LocalizationTable，依照語言下拉選擇的語言，
        /// 將每個 TextNode 的 text 與 speakerName 同步為該語言版本，方便在 Graph 分頁預覽多語系。
        /// </summary>
        private void SyncLocalizedTextFromTableToGraph()
        {
            if (_graph == null) { EditorUtility.DisplayDialog("Sync Localization", "請先指定一個 DialogueGraph。", "OK"); return; }
            if (_table == null) { EditorUtility.DisplayDialog("Sync Localization", "請先在 Localization 或 Table 分頁選擇 LocalizationTable。", "OK"); return; }

            _table.BuildLookup(); // 確保本地化表格的查找表已建立
            int updatedCount = 0;

            // 遍歷對話圖中的所有 TextNode
            foreach (var node in _graph.AllNodes.OfType<Nodes.TextNode>())
            {
                if (string.IsNullOrEmpty(node.textKey)) continue; // 如果沒有 textKey，則跳過

                var entry = _table.GetEntry(node.textKey); // 從本地化表格中獲取條目
                if (entry == null) continue; // 如果找不到條目，則跳過

                string textValue, speakerValue;
                // 根據當前選中的語言獲取對應的文本和說話者
                switch (_currentLanguage)
                {
                    case LocalizationLanguage.JaJp:
                        textValue = string.IsNullOrEmpty(entry.jaJP) ? entry.zhTW : entry.jaJP;
                        speakerValue = string.IsNullOrEmpty(entry.speakerJaJP) ? entry.speakerZhTW : entry.speakerJaJP;
                        break;
                    case LocalizationLanguage.EnUs:
                        textValue = string.IsNullOrEmpty(entry.enUS) ? entry.zhTW : entry.enUS;
                        speakerValue = string.IsNullOrEmpty(entry.speakerEnUS) ? entry.speakerZhTW : entry.speakerEnUS;
                        break;
                    default: // 預設為繁體中文
                        textValue = entry.zhTW;
                        speakerValue = entry.speakerZhTW;
                        break;
                }

                // 如果文本或說話者有改變，則更新並計數
                if (!string.IsNullOrEmpty(textValue) && node.text != textValue) { node.text = textValue; updatedCount++; }
                if (!string.IsNullOrEmpty(speakerValue) && node.speakerName != speakerValue) { node.speakerName = speakerValue; updatedCount++; }
            }

            if (updatedCount > 0) { EditorUtility.SetDirty(_graph); AssetDatabase.SaveAssets(); } // 如果有更新，則保存對話圖
            _graphView?.PopulateView(_graph); // 重新填充視圖以顯示更新後的文本
            EditorUtility.DisplayDialog("Sync Localization", updatedCount > 0 ? $"已依 {_currentLanguage} 從 LocalizationTable 同步 {updatedCount} 筆文字/說話者到 Graph。" : "沒有找到可以同步的文字或說話者。", "OK");
        }
    }
}
#endif
