#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// DialogueLocalizationWindow 是主要的對話編輯器視窗，整合了對話圖編輯、本地化工具、本地化表格編輯和對話模擬器。
    /// </summary>
    public class DialogueLocalizationWindow : EditorWindow
    {
        // 用於在 EditorPrefs 中保存上次選取資產的 GUID 鍵
        private const string LastGraphGuidKey = "DialogueLocalizationWindow.LastGraphGuid";
        private const string LastTableGuidKey = "DialogueLocalizationWindow.LastTableGuid";
        private const string LastStateGuidKey = "DialogueLocalizationWindow.LastStateGuid";

        // 各個功能分頁的實例
        private GraphEditorTab _graphTab;
        private LocalizationToolsTab _locTab;
        private TableEditorTab _tableTab;
        private SimulatorTab _simulatorTab;

        // 視窗開啟時載入的初始資產
        private DialogueGraph _initialGraph;
        private LocalizationTable _initialTable;
        private DialogueStateAsset _initialState;

        /// <summary>
        /// 在 Unity 編輯器菜單中添加一個項目，用於打開對話圖與本地化視窗。
        /// </summary>
        [MenuItem("SG/Dialogue/Graph + Localization Window")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<DialogueLocalizationWindow>(); // 獲取或創建視窗實例
            wnd.titleContent = new GUIContent("Dialogue Graph & Loc"); // 設定視窗標題
            wnd.minSize = new Vector2(900, 600); // 設定視窗最小尺寸
            wnd.Show(); // 顯示視窗
        }

        /// <summary>
        /// 顯示視窗並聚焦。
        /// </summary>
        /// <returns>視窗實例。</returns>
        public static DialogueLocalizationWindow ShowWindowAndFocus()
        {
            var wnd = ShowWindowAndGet();
            wnd.Focus(); // 聚焦到視窗
            return wnd;
        }
        
        /// <summary>
        /// 顯示視窗並返回其實例。
        /// </summary>
        /// <returns>視窗實例。</returns>
        public static DialogueLocalizationWindow ShowWindowAndGet()
        {
            var wnd = GetWindow<DialogueLocalizationWindow>();
            wnd.titleContent = new GUIContent("Dialogue Graph & Loc");
            wnd.minSize = new Vector2(900, 600);
            wnd.Show();
            return wnd;
        }

        /// <summary>
        /// 視窗啟用時調用。
        /// </summary>
        private void OnEnable()
        {
            LoadLastSelection(); // 載入上次選取的資產
            CreateGUI(); // 創建視窗的 UI
        }

        /// <summary>
        /// 視窗禁用時調用。
        /// </summary>
        private void OnDisable()
        {
            _graphTab?.SaveGraphViewState(); // 保存對話圖視圖的狀態
        }

        /// <summary>
        /// 設定當前編輯的對話圖。
        /// </summary>
        /// <param name="graph">要設定的對話圖。</param>
        public void SetGraph(DialogueGraph graph)
        {
            _initialGraph = graph;
            // 將對話圖設定到各個相關分頁
            if (_graphTab != null) _graphTab.SetGraph(graph);
            if (_locTab != null) _locTab.SetGraph(graph);
            if (_simulatorTab != null) _simulatorTab.SetGraph(graph);
            UpdateWindowTitle(); // 更新視窗標題

            // 將對話圖的 GUID 保存到 EditorPrefs
            string guid = graph != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(graph)) : null;
            EditorPrefs.SetString(LastGraphGuidKey, guid);
        }

        /// <summary>
        /// 設定當前編輯的本地化表格。
        /// </summary>
        /// <param name="table">要設定的本地化表格。</param>
        public void SetTable(LocalizationTable table)
        {
            _initialTable = table;
            // 將本地化表格設定到各個相關分頁
            if (_graphTab != null) _graphTab.SetTable(table);
            if (_locTab != null) _locTab.SetTable(table);
            if (_tableTab != null) _tableTab.SetTable(table);

            // 將本地化表格的 GUID 保存到 EditorPrefs
            string guid = table != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(table)) : null;
            EditorPrefs.SetString(LastTableGuidKey, guid);
        }

        /// <summary>
        /// 設定當前使用的全域對話狀態資產。
        /// </summary>
        /// <param name="state">要設定的全域對話狀態資產。</param>
        public void SetState(DialogueStateAsset state)
        {
            _initialState = state;
            // 將狀態資產設定到各個相關分頁
            if (_graphTab != null) _graphTab.SetState(state);
            if (_simulatorTab != null) _simulatorTab.SetState(state);

            // 將狀態資產的 GUID 保存到 EditorPrefs
            string guid = state != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(state)) : null;
            EditorPrefs.SetString(LastStateGuidKey, guid);
        }

        /// <summary>
        /// 創建視窗的 UI 佈局和各個分頁。
        /// </summary>
        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear(); // 清除現有元素
            root.style.flexDirection = FlexDirection.Column; // 設定為垂直佈局

            // 創建主工具列，包含各個分頁按鈕
            var mainToolbar = new Toolbar();
            mainToolbar.Add(new ToolbarButton(() => SwitchTab(0)) { text = "Graph", style = { minWidth = 90 } });
            mainToolbar.Add(new ToolbarButton(() => SwitchTab(1)) { text = "Localization", style = { minWidth = 120 } });
            mainToolbar.Add(new ToolbarButton(() => SwitchTab(2)) { text = "Table", style = { minWidth = 90 } });
            mainToolbar.Add(new ToolbarButton(() => SwitchTab(3)) { text = "Simulator", style = { minWidth = 90 } });
            root.Add(mainToolbar);

            // 實例化各個分頁
            _graphTab = new GraphEditorTab();
            _locTab = new LocalizationToolsTab();
            _tableTab = new TableEditorTab();
            _simulatorTab = new SimulatorTab();

            // 設定分頁之間的回調，以便同步選取的資產
            _graphTab.OnGraphSelected = SetGraph;
            _graphTab.OnTableSelected = SetTable;
            _graphTab.OnStateSelected = SetState;
            _locTab.OnGraphSelected = SetGraph;
            _locTab.OnTableSelected = SetTable;
            _tableTab.OnTableSelected = SetTable;

            // 將分頁添加到根元素
            root.Add(_graphTab);
            root.Add(_locTab);
            root.Add(_tableTab);
            root.Add(_simulatorTab);
            
            // 設定初始資產
            SetGraph(_initialGraph);
            SetTable(_initialTable);
            SetState(_initialState);

            SwitchTab(0); // 預設顯示 Graph 分頁
        }

        /// <summary>
        /// 從 EditorPrefs 載入上次選取的資產。
        /// </summary>
        private void LoadLastSelection()
        {
            string graphGuid = EditorPrefs.GetString(LastGraphGuidKey, null);
            _initialGraph = !string.IsNullOrEmpty(graphGuid) ? AssetDatabase.LoadAssetAtPath<DialogueGraph>(AssetDatabase.GUIDToAssetPath(graphGuid)) : null;

            string tableGuid = EditorPrefs.GetString(LastTableGuidKey, null);
            _initialTable = !string.IsNullOrEmpty(tableGuid) ? AssetDatabase.LoadAssetAtPath<LocalizationTable>(AssetDatabase.GUIDToAssetPath(tableGuid)) : null;

            string stateGuid = EditorPrefs.GetString(LastStateGuidKey, null);
            _initialState = !string.IsNullOrEmpty(stateGuid) ? AssetDatabase.LoadAssetAtPath<DialogueStateAsset>(AssetDatabase.GUIDToAssetPath(stateGuid)) : null;
        }

        /// <summary>
        /// 切換顯示的分頁。
        /// </summary>
        /// <param name="index">要顯示的分頁索引 (0: Graph, 1: Localization, 2: Table, 3: Simulator)。</param>
        private void SwitchTab(int index)
        {
            if (_graphTab == null || _locTab == null || _tableTab == null || _simulatorTab == null) return;
            // 根據索引設定分頁的顯示樣式
            _graphTab.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _locTab.style.display = index == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            _tableTab.style.display = index == 2 ? DisplayStyle.Flex : DisplayStyle.None;
            _simulatorTab.style.display = index == 3 ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        /// <summary>
        /// 更新視窗的標題，顯示當前編輯的對話圖名稱。
        /// </summary>
        private void UpdateWindowTitle()
        {
            titleContent = new GUIContent(_initialGraph != null ? $"Dialogue - {_initialGraph.name}" : "Dialogue Graph & Loc");
        }
        
        /// <summary>
        /// 用於 JSON 序列化的對話圖本地化數據結構。
        /// </summary>
        [Serializable]
        public class GraphLocalizationJson
        {
            public string graphName; // 對話圖名稱
            public List<GraphLocalizationNodeJson> nodes; // 節點本地化數據列表
        }

        /// <summary>
        /// 用於 JSON 序列化的單個節點本地化數據結構。
        /// </summary>
        [Serializable]
        public class GraphLocalizationNodeJson
        {
            public string nodeId; // 節點 ID
            public string key; // 本地化鍵
            public string speaker; // 說話者名稱
            public string zhTW; // 繁體中文內容
            public string jaJP; // 日文內容
            public string enUS; // 英文內容
        }

        /// <summary>
        /// 用於 JSON 序列化的本地化表格數據結構。
        /// </summary>
        [Serializable]
        public class LocalizationTableJson
        {
            public List<LocalizationTableEntryJson> entries; // 表格條目列表
        }

        /// <summary>
        /// 用於 JSON 序列化的單個本地化表格條目數據結構。
        /// </summary>
        [Serializable]
        public class LocalizationTableEntryJson
        {
            public string key; // 本地化鍵
            public string zhTW; // 繁體中文內容
            public string jaJP; // 日文內容
            public string enUS; // 英文內容
            public string speakerZhTW; // 繁體中文說話者
            public string speakerJaJP; // 日文說話者
            public string speakerEnUS; // 英文說話者
        }
    }
}
#endif
