#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// DialogueLocalizationWindow 是主要的對話編輯器視窗，整合了對話圖編輯、本地化工具、本地化表格編輯和對話模擬器。
    /// </summary>
    public class DialogueLocalizationWindow : EditorWindow
    {
        // 用於在 EditorPrefs 中保存資產 GUID 組的鍵。
        // 這個鍵儲存了一個 JSON 字符串，其中包含了多組 Graph, Table, 和 State 的 GUID 對應關係。
        private const string AssetGuidGroupsKey = "DialogueLocalizationWindow.AssetGuidGroups";

        /// <summary>
        /// 用於序列化的資產 GUID 組。
        /// </summary>
        [Serializable]
        private class AssetGuidGroup
        {
            public string GraphGuid;
            public string TableGuid;
            public string StateGuid;
        }

        /// <summary>
        /// 用於序列化 AssetGuidGroup 列表的輔助類別。
        /// </summary>
        [Serializable]
        private class AssetGuidGroupList
        {
            public List<AssetGuidGroup> Groups = new List<AssetGuidGroup>();
        }

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

            string graphGuid = graph != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(graph)) : null;

            // 如果傳入的 graph 是 null，清除當前 session 的 table 和 state
            if (string.IsNullOrEmpty(graphGuid))
            {
                SetTable(null);
                SetState(null);
                return;
            }

            var list = LoadGuidGroups();
            var existingGroup = list.Groups.FirstOrDefault(g => g.GraphGuid == graphGuid);

            if (existingGroup != null)
            {
                // 如果已存在，將其移到列表末尾，表示最近使用
                list.Groups.Remove(existingGroup);
                list.Groups.Add(existingGroup);
            }
            else
            {
                // 如果不存在，創建新的一組
                var newGroup = new AssetGuidGroup { GraphGuid = graphGuid };
                list.Groups.Add(newGroup);
            }
            
            SaveGuidGroups(list);
            
            // 當 graph 改變時，自動載入與其關聯的 table 和 state
            LoadAssociatedAssets();
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

            // 如果沒有 graph，就無法建立關聯
            if (_initialGraph == null) return;
            string graphGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_initialGraph));
            if (string.IsNullOrEmpty(graphGuid)) return;

            string tableGuid = table != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(table)) : null;

            var list = LoadGuidGroups();
            var groupToUpdate = list.Groups.FirstOrDefault(g => g.GraphGuid == graphGuid);

            if (groupToUpdate != null)
            {
                // 只有在 GUID 不同的時候才儲存，避免不必要的寫入
                if (groupToUpdate.TableGuid != tableGuid)
                {
                    groupToUpdate.TableGuid = tableGuid;
                    SaveGuidGroups(list);
                }
            }
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

            // 如果沒有 graph，就無法建立關聯
            if (_initialGraph == null) return;
            string graphGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_initialGraph));
            if (string.IsNullOrEmpty(graphGuid)) return;

            string stateGuid = state != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(state)) : null;

            var list = LoadGuidGroups();
            var groupToUpdate = list.Groups.FirstOrDefault(g => g.GraphGuid == graphGuid);

            if (groupToUpdate != null)
            {
                // 只有在 GUID 不同的時候才儲存，避免不必要的寫入
                if (groupToUpdate.StateGuid != stateGuid)
                {
                    groupToUpdate.StateGuid = stateGuid;
                    SaveGuidGroups(list);
                }
            }
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
        /// 從 EditorPrefs 載入上次選取的資產組。
        /// </summary>
        private void LoadLastSelection()
        {
            var list = LoadGuidGroups();
            if (list.Groups.Count == 0) return;

            // 載入最近使用的一組
            var lastGroup = list.Groups.Last();

            string graphGuid = lastGroup.GraphGuid;
            _initialGraph = !string.IsNullOrEmpty(graphGuid) ? AssetDatabase.LoadAssetAtPath<DialogueGraph>(AssetDatabase.GUIDToAssetPath(graphGuid)) : null;

            string tableGuid = lastGroup.TableGuid;
            _initialTable = !string.IsNullOrEmpty(tableGuid) ? AssetDatabase.LoadAssetAtPath<LocalizationTable>(AssetDatabase.GUIDToAssetPath(tableGuid)) : null;

            string stateGuid = lastGroup.StateGuid;
            _initialState = !string.IsNullOrEmpty(stateGuid) ? AssetDatabase.LoadAssetAtPath<DialogueStateAsset>(AssetDatabase.GUIDToAssetPath(stateGuid)) : null;
        }

        /// <summary>
        /// 從 EditorPrefs 載入並反序列化資產 GUID 組列表。
        /// </summary>
        private AssetGuidGroupList LoadGuidGroups()
        {
            string json = EditorPrefs.GetString(AssetGuidGroupsKey, "{}");
            AssetGuidGroupList list = JsonUtility.FromJson<AssetGuidGroupList>(json);
            // JsonUtility 在反序列化空 JSON 時可能會返回 null，或 list.Groups 為 null，需要做防呆處理
            if (list == null)
            {
                list = new AssetGuidGroupList();
            }
            if (list.Groups == null)
            {
                list.Groups = new List<AssetGuidGroup>();
            }
            return list;
        }

        /// <summary>
        /// 序列化並保存資產 GUID 組列表到 EditorPrefs。
        /// </summary>
        private void SaveGuidGroups(AssetGuidGroupList list)
        {
            // 限制列表的最大長度，避免無限增長
            const int maxGroups = 20;
            if (list.Groups.Count > maxGroups)
            {
                list.Groups.RemoveRange(0, list.Groups.Count - maxGroups);
            }
            
            string json = JsonUtility.ToJson(list);
            EditorPrefs.SetString(AssetGuidGroupsKey, json);
        }

        /// <summary>
        /// 根據當前的 Graph，載入與其關聯的 Table 和 State。
        /// </summary>
        private void LoadAssociatedAssets()
        {
            if (_initialGraph == null) return;
            string graphGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_initialGraph));
            if (string.IsNullOrEmpty(graphGuid)) return;

            var list = LoadGuidGroups();
            var group = list.Groups.FirstOrDefault(g => g.GraphGuid == graphGuid);

            if (group != null)
            {
                string tableGuid = group.TableGuid;
                var table = !string.IsNullOrEmpty(tableGuid) ? AssetDatabase.LoadAssetAtPath<LocalizationTable>(AssetDatabase.GUIDToAssetPath(tableGuid)) : null;
                SetTable(table);

                string stateGuid = group.StateGuid;
                var state = !string.IsNullOrEmpty(stateGuid) ? AssetDatabase.LoadAssetAtPath<DialogueStateAsset>(AssetDatabase.GUIDToAssetPath(stateGuid)) : null;
                SetState(state);
            }
            else
            {
                // 如果找不到關聯的群組 (例如，在創建新 Graph 後)，則清空 Table 和 State
                SetTable(null);
                SetState(null);
            }
        }

        /// <summary>
        /// 切換顯示的分頁。
        /// </summary>
        /// <param name="index">要顯示的分頁索引 (0: Graph, 1: Localization, 2: Table, 3: Simulator)。</param>
        private void SwitchTab(int index)
        {
            if (_graphTab == null || _locTab == null || _tableTab == null || _simulatorTab == null) return;
            
            // [核心修改] 當切換到 Table 分頁時，觸發其刷新
            if (index == 2)
            {
                _tableTab.Refresh();
            }
            
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
