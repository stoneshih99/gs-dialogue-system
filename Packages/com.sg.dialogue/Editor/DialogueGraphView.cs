#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 對話系統的圖形化編輯器視圖 (GraphView)。
    /// <para>
    /// 此類別負責將 <see cref="DialogueGraph"/> 資料模型視覺化，並處理使用者的互動操作
    /// (如：拖曳節點、連接線段、縮放視圖、導覽子圖等)。
    /// </para>
    /// <para>
    /// 架構設計：
    /// 1. **資料驅動**：視圖狀態完全依賴於 <see cref="DialogueGraph"/> 與 <see cref="SerializedObject"/>。
    /// 2. **導覽堆疊**：支援巢狀子圖 (Sequence/Parallel)，透過 Stack 管理當前顯示層級。
    /// 3. **解耦渲染**：透過 <see cref="NodeHandlerRegistry"/> 將不同類型節點的渲染邏輯分離。
    /// </para>
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        #region Events & Constants

        /// <summary>
        /// 當導覽堆疊變更時觸發 (例如：進入子圖或返回上一層)。
        /// UI 層可監聽此事件來更新麵包屑 (Breadcrumbs) 導覽列。
        /// </summary>
        public Action<Stack<object>> OnNavigationChanged;

        /// <summary>
        /// 用於在 EditorPrefs 儲存視圖狀態 (位置/縮放) 的 Key 前綴。
        /// </summary>
        private const string ViewTransformKeyPrefix = "DialogueGraphView.ViewTransform.";
        
        #endregion

        #region Fields

        // --- Data (資料模型) ---
        
        /// <summary>
        /// 當前編輯的對話圖資料資產。
        /// </summary>
        public DialogueGraph Graph => _graph;
        private DialogueGraph _graph;

        /// <summary>
        /// 全域狀態資產，用於條件節點 (ConditionNode) 存取變數。
        /// </summary>
        public DialogueStateAsset GlobalState => _globalState;
        private DialogueStateAsset _globalState;

        // --- Navigation (導覽狀態) ---
        
        /// <summary>
        /// 導覽堆疊，用於管理子圖層級。
        /// Stack 底部通常是 Root Graph，頂部是當前顯示的容器 (SequenceNode/ParallelNode)。
        /// </summary>
        public Stack<object> NavigationStack => _navigationStack;
        private readonly Stack<object> _navigationStack = new();

        // --- Visual Elements Cache (視覺元件快取) ---
        
        /// <summary>
        /// 節點 ID 對應到視覺元素 (Node View) 的查找表，用於快速存取與更新。
        /// </summary>
        private readonly Dictionary<string, DialogueNodeElement> _nodeViews = new();
        
        // --- State Flags (狀態標記) ---
        
        /// <summary>
        /// 標記目前是否正在程式化填充視圖。
        /// 用於防止在重建圖表時觸發不必要的 <see cref="OnGraphViewChanged"/> 事件。
        /// </summary>
        private bool _isPopulating;
        
        /// <summary>
        /// 當前正在執行 (Runtime) 並高亮的節點。
        /// </summary>
        private DialogueNodeElement _executingNode;

        // --- Configuration (設定) ---
        
        private readonly Vector2 _defaultNodeSize = new Vector2(200, 150);

        // --- Handlers (邏輯處理器) ---
        
        private readonly NodeClipboardHandler _clipboardHandler;
        private readonly GraphConnectionHandler _connectionHandler;

        // --- SubGraph Elements (子圖特殊節點) ---
        
        /// <summary>
        /// 當進入 SequenceNode 子圖時，顯示的虛擬起始節點。
        /// </summary>
        public SequenceStartNodeElement SequenceStartNode { get; private set; }
        
        /// <summary>
        /// 當進入 ParallelNode 子圖時，顯示的虛擬分支起始節點。
        /// </summary>
        public ParallelBranchStartNodeElement ParallelStartNode { get; private set; }

        #endregion

        #region Initialization

        public DialogueGraphView()
        {
            SetupGraphView();
            
            // 初始化輔助處理器
            _clipboardHandler = new NodeClipboardHandler(this);
            _connectionHandler = new GraphConnectionHandler(this);

            // 註冊生命週期事件，確保正確釋放資源
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            
            // 監聽 Unity 的 Undo/Redo，以便在復原時刷新視圖
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        /// <summary>
        /// 設定 GraphView 的基本互動功能與外觀。
        /// </summary>
        private void SetupGraphView()
        {
            // 設定縮放範圍
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            
            // 加入操作器 (Manipulators)
            this.AddManipulator(new ContentDragger());      // 拖曳畫布
            this.AddManipulator(new SelectionDragger());    // 拖曳選取物件
            this.AddManipulator(new RectangleSelector());   // 框選
            this.AddManipulator(new ClickSelector());       // 點選
            
            // 加入網格背景
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            style.flexGrow = 1f; // 填滿父容器
            
            // 監聽圖表變更 (新增/刪除/移動)
            graphViewChanged += OnGraphViewChanged;
        }

        #endregion

        #region Lifecycle & Event Handling

        private void OnAttachToPanel(AttachToPanelEvent evt) => RegisterGraphEvents();
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterGraphEvents();
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        /// <summary>
        /// 註冊 Runtime 事件監聽 (節點進入、對話結束)。
        /// </summary>
        private void RegisterGraphEvents()
        {
            if (_graph == null) return;
            _graph.onNodeEntered.AddListener(OnNodeEntered);
            _graph.onDialogueEnded.AddListener(OnDialogueEnded);
        }

        /// <summary>
        /// 移除 Runtime 事件監聽。
        /// </summary>
        private void UnregisterGraphEvents()
        {
            if (_graph == null) return;
            _graph.onNodeEntered.RemoveListener(OnNodeEntered);
            _graph.onDialogueEnded.RemoveListener(OnDialogueEnded);
            OnDialogueEnded(); // 確保狀態被清除
        }

        /// <summary>
        /// Runtime: 當節點被執行時，更新視覺高亮。
        /// </summary>
        private void OnNodeEntered(string nodeId)
        {
            // 取消上一個節點的高亮
            if (_executingNode != null)
            {
                _executingNode.SetExecutionState(false);
                _executingNode = null;
            }

            // 高亮當前節點
            if (_nodeViews.TryGetValue(nodeId, out var currentNodeView))
            {
                _executingNode = currentNodeView;
                _executingNode.SetExecutionState(true);
            }
        }

        /// <summary>
        /// Runtime: 對話結束時清除所有高亮。
        /// </summary>
        private void OnDialogueEnded()
        {
            if (_executingNode != null)
            {
                _executingNode.SetExecutionState(false);
                _executingNode = null;
            }
        }

        /// <summary>
        /// 當 Undo/Redo 發生時，強制重新繪製圖表以同步資料。
        /// </summary>
        private void OnUndoRedo()
        {
            if (_graph) PopulateView(_graph);
        }

        #endregion

        #region Graph Population (Core Logic)

        /// <summary>
        /// 填充視圖的入口方法。
        /// <para>此方法會重置導覽堆疊並從根節點開始繪製。</para>
        /// </summary>
        /// <param name="graph">要顯示的對話圖資料。</param>
        public void PopulateView(DialogueGraph graph)
        {
            _isPopulating = true;
            try
            {
                UnregisterGraphEvents(); // 切換圖表前先移除舊的監聽

                _graph = graph;
                _navigationStack.Clear();
                if (graph) _navigationStack.Push(graph);

                RegisterGraphEvents(); // 註冊新圖表的監聽
                PopulateFromCurrentNavigation();
            }
            finally
            {
                _isPopulating = false;
            }
        }

        /// <summary>
        /// 根據目前的導覽堆疊 (Navigation Stack) 重建整個圖表視圖。
        /// <para>流程：清理 -> 建立子圖節點 -> 建立資料節點 -> 建立連線 -> 更新視圖狀態。</para>
        /// </summary>
        private void PopulateFromCurrentNavigation()
        {
            _isPopulating = true;
            try
            {
                // 1. 清理現有元素
                ClearGraph();

                if (!_graph || _navigationStack.Count == 0) return;
                
                var currentContainer = _navigationStack.Peek();
                if (currentContainer == null) return;

                // 2. 建立子圖的虛擬起始節點 (若當前在 Sequence 或 Parallel 內部)
                CreateSubGraphStartNodes(currentContainer);

                // 3. 獲取並建立所有資料節點
                var nodesToDisplay = GetNodesFromContainer(currentContainer);
                if (nodesToDisplay == null) return;

                _graph.BuildLookup(); // 重建 ID 查找表
                CreateNodes(currentContainer, nodesToDisplay);

                // 4. 建立節點間的連線 (Edges)
                CreateEdges(currentContainer, nodesToDisplay);
                
                // 5. 收尾：更新視圖位置、麵包屑與起始節點標記
                FinalizeGraphPopulation();
            }
            finally
            {
                _isPopulating = false;
            }
        }

        private void ClearGraph()
        {
            // 移除所有 GraphElement (Node, Edge, etc.)
            DeleteElements(graphElements.ToList());
            _nodeViews.Clear();
            SequenceStartNode = null;
            ParallelStartNode = null;
            _executingNode = null;
        }

        private void CreateSubGraphStartNodes(object currentContainer)
        {
            if (currentContainer is SequenceNode seqNode) CreateSequenceStartNode(seqNode);
            else if (currentContainer is ParallelNode parNode) CreateParallelStartNode(parNode);
        }

        /// <summary>
        /// 遍歷資料節點並建立對應的視覺元素。
        /// </summary>
        private void CreateNodes(object currentContainer, List<DialogueNodeBase> nodesToDisplay)
        {
            // 為了支援 Undo/Redo 與 Inspector 修改，我們使用 SerializedObject
            var serializedGraph = new SerializedObject(_graph);
            var nodesProperty = FindNodesProperty(serializedGraph, currentContainer);

            for (int i = 0; i < nodesToDisplay.Count; i++)
            {
                var nodeData = nodesToDisplay[i];
                var nodeProperty = nodesProperty?.GetArrayElementAtIndex(i);
                CreateAndRegisterNode(nodeData, nodeProperty);
            }
        }

        /// <summary>
        /// 重建所有連線 (Edges)。
        /// </summary>
        private void CreateEdges(object currentContainer, List<DialogueNodeBase> nodesToDisplay)
        {
            // 連接一般節點之間的線
            foreach (var nodeData in nodesToDisplay)
            {
                if (!_nodeViews.TryGetValue(nodeData.nodeId, out var sourceView)) continue;
                ConnectPortsForNode(sourceView, nodeData);
            }

            // 連接子圖虛擬起始節點到第一個實際節點
            if (SequenceStartNode != null && currentContainer is SequenceNode seqNodeData)
            {
                var inputPort = TryGetInputPort(seqNodeData.startNodeId);
                if (inputPort != null) ConnectPorts(SequenceStartNode.OutputPort, inputPort);
            }
            else if (ParallelStartNode != null && currentContainer is ParallelNode parNodeData)
            {
                for (int i = 0; i < parNodeData.branchStartNodeIds.Count; i++)
                {
                    var inputPort = TryGetInputPort(parNodeData.branchStartNodeIds[i]);
                    if (inputPort != null && i < ParallelStartNode.BranchPorts.Count)
                    {
                        ConnectPorts(ParallelStartNode.BranchPorts[i], inputPort);
                    }
                }
            }
        }

        private void FinalizeGraphPopulation()
        {
            ResetView(); // 恢復上次的視圖位置
            OnNavigationChanged?.Invoke(_navigationStack); // 通知 UI 更新
            UpdateStartNodeVisuals(); // 標記起始節點
        }

        #endregion

        #region Node Creation & Management

        /// <summary>
        /// 建立單一節點的視覺元素並註冊到視圖中。
        /// </summary>
        /// <param name="node">節點資料。</param>
        /// <param name="nodeProperty">節點的 SerializedProperty (用於 Inspector 綁定)。</param>
        public DialogueNodeElement CreateAndRegisterNode(DialogueNodeBase node, SerializedProperty nodeProperty)
        {
            // 使用 Factory Pattern (NodeHandler) 根據節點類型建立對應的 View
            if (!NodeHandlerRegistry.Handlers.TryGetValue(node.GetType(), out var handler)) return null;

            var element = handler.CreateNodeElement(node, this, nodeProperty, () => RecordUndo("Modify Node"));
            if (element == null) return null;

            // 設定位置與初始化
            element.SetPosition(new Rect(_graph.GetNodePosition(node.nodeId), _defaultNodeSize));
            element.Initialize(this, nodeProperty);
            
            // 設定刪除回調：當使用者在圖表中刪除節點時觸發
            element.OnDelete = () => DeleteNode(node, element);

            AddElement(element);
            _nodeViews[node.nodeId] = element;
            
            return element;
        }

        private void DeleteNode(DialogueNodeBase node, DialogueNodeElement element)
        {
            RecordUndo("Delete Node");
            var container = _navigationStack.Peek();
            GetNodesFromContainer(container)?.Remove(node); // 從資料層移除
            _graph.RemoveNodePosition(node.nodeId);       // 移除位置資訊
            _nodeViews.Remove(node.nodeId);               // 從快取移除
            EditorUtility.SetDirty(_graph);               // 標記資產已修改
        }

        /// <summary>
        /// 處理 GraphView 的變更事件 (由 Unity 內部觸發)。
        /// 包括：建立連線、移除元素、移動元素。
        /// </summary>
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isPopulating || _graph == null) return change;

            // 處理新連線
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate) _connectionHandler.HandleEdgeConnection(edge);
            }

            // 處理移除元素
            if (change.elementsToRemove != null)
            {
                foreach (var el in change.elementsToRemove)
                {
                    if (el is Edge edge) _connectionHandler.HandleEdgeDisconnection(edge);
                    else if (el is DialogueNodeElement nodeElement) nodeElement.OnDelete?.Invoke();
                }
            }

            // 處理移動元素 (同步位置到資料)
            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                RecordUndo("Move Nodes");
                SyncPositionsToAsset();
            }

            return change;
        }

        #endregion

        #region Navigation & View Control

        /// <summary>
        /// 進入容器節點 (子圖)。
        /// </summary>
        public void EnterContainerNode(DialogueNodeBase containerNode)
        {
            if (containerNode is SequenceNode || containerNode is ParallelNode)
            {
                _navigationStack.Push(containerNode);
                PopulateFromCurrentNavigation();
            }
        }

        /// <summary>
        /// 返回上一層圖表。
        /// </summary>
        public void NavigateBack()
        {
            if (_navigationStack.Count > 1)
            {
                _navigationStack.Pop();
                PopulateFromCurrentNavigation();
            }
        }

        /// <summary>
        /// 儲存當前視圖的變換資訊 (位置、縮放) 到 EditorPrefs。
        /// </summary>
        public void SaveViewTransform()
        {
            if (_graph == null) return;
            string key = GetViewTransformKey();
            string value = $"{viewTransform.position.x},{viewTransform.position.y},{viewTransform.scale.x}";
            EditorPrefs.SetString(key, value);
        }

        /// <summary>
        /// 嘗試載入並套用視圖變換資訊。
        /// </summary>
        private bool TryLoadViewTransform()
        {
            if (_graph == null) return false;
            string key = GetViewTransformKey();
            string value = EditorPrefs.GetString(key);
            if (string.IsNullOrEmpty(value)) return false;

            var parts = value.Split(',');
            if (parts.Length == 3 && 
                float.TryParse(parts[0], out float x) && 
                float.TryParse(parts[1], out float y) && 
                float.TryParse(parts[2], out float scale))
            {
                UpdateViewTransform(new Vector3(x, y, viewTransform.position.z), new Vector3(scale, scale, viewTransform.scale.z));
                return true;
            }
            return false;
        }

        private void ResetView()
        {
            if (!TryLoadViewTransform()) FrameGraph();
        }

        /// <summary>
        /// 將視圖聚焦到圖表內容。
        /// </summary>
        public void FrameGraph()
        {
            if (_graph == null || _navigationStack.Count == 0) return;
            var container = _navigationStack.Peek();
            
            // 根據容器類型決定聚焦策略
            if (container is DialogueGraph graph)
            {
                if (!string.IsNullOrEmpty(graph.startNodeId) && _nodeViews.TryGetValue(graph.startNodeId, out var startNodeElement)) 
                    FrameSelectionOrAll(startNodeElement);
                else 
                    FrameAllOrReset();
            }
            else if (container is SequenceNode) FrameSelectionOrAll(SequenceStartNode);
            else if (container is ParallelNode) FrameSelectionOrAll(ParallelStartNode);
            else FrameAllOrReset();
        }

        private void FrameSelectionOrAll(GraphElement elementToSelect)
        {
            if (elementToSelect != null)
            {
                ClearSelection();
                AddToSelection(elementToSelect);
                FrameSelection();
            }
            else FrameAllOrReset();
        }

        private void FrameAllOrReset()
        {
            if (nodes.Any()) FrameAll();
            else UpdateViewTransform(Vector3.zero, Vector3.one);
        }

        /// <summary>
        /// 取得視圖狀態儲存的 Key。每個子圖層級都有獨立的 Key。
        /// </summary>
        private string GetViewTransformKey()
        {
            if (_graph == null) return null;
            string path = AssetDatabase.GetAssetPath(_graph);
            string context = "root";
            if (_navigationStack.Count > 1 && _navigationStack.Peek() is DialogueNodeBase node) context = node.nodeId;
            return $"{ViewTransformKeyPrefix}{path}_{context}";
        }

        #endregion

        #region Context Menu

        /// <summary>
        /// 建立右鍵選單。
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            var mousePos = contentViewContainer.WorldToLocal(evt.mousePosition);
            
            // 複製/貼上
            if (selection.Any(s => s is DialogueNodeElement)) 
                evt.menu.AppendAction("Copy", _ => _clipboardHandler.CopySelectionToClipboard());
            
            evt.menu.AppendAction("Paste", _ => _clipboardHandler.PasteFromClipboard(mousePos), DropdownMenuAction.Status.Normal);
            
            if (selection.Any(s => s is DialogueNodeElement) || _clipboardHandler.HasClipboardContent()) 
                evt.menu.AppendSeparator();
            
            bool inSubGraph = _navigationStack.Count > 1;

            // 動態生成節點創建選項
            foreach (var handler in NodeHandlerRegistry.Handlers.Values)
            {
                // 在子圖中禁止創建新的子圖容器 (避免過度巢狀)
                if (inSubGraph && (handler.CreateNodeData() is SequenceNode || handler.CreateNodeData() is ParallelNode)) continue;
                evt.menu.AppendAction(handler.MenuName, _ => CreateAndAddNode(handler.CreateNodeData(), mousePos, handler));
            }
        }

        private void CreateAndAddNode(DialogueNodeBase node, Vector2 pos, INodeHandler handler)
        {
            if (_graph == null) return;
            
            RecordUndo("Create Node");

            var container = _navigationStack.Peek();
            var targetList = GetNodesFromContainer(container);
            if (targetList == null) return;

            _graph.BuildLookup(); 
            node.nodeId = _clipboardHandler.GenerateUniqueNodeId(handler.GetPrefix());
            
            targetList.Add(node); 
            _graph.SetNodePosition(node.nodeId, pos);

            // 獲取新節點的 SerializedProperty
            var serializedGraph = new SerializedObject(_graph);
            var nodesProperty = FindNodesProperty(serializedGraph, container);
            var newNodeProperty = nodesProperty?.GetArrayElementAtIndex(targetList.Count - 1);
            
            CreateAndRegisterNode(node, newNodeProperty); 
            
            EditorUtility.SetDirty(_graph);
        }

        #endregion

        #region Helpers & Utilities

        /// <summary>
        /// 設定全域變數狀態，並通知相關節點更新 UI (如 ConditionNode 的下拉選單)。
        /// </summary>
        public void SetGlobalState(DialogueStateAsset state)
        {
            _globalState = state;
            foreach (var nodeView in _nodeViews.Values.OfType<ConditionNodeElement>())
            {
                nodeView.UpdateDropdowns(_globalState);
            }
        }

        /// <summary>
        /// 設定圖表的起始節點。
        /// </summary>
        public void SetStartNode(string nodeId)
        {
            if (_graph == null) return;
            RecordUndo("Set Start Node");
            
            // 更新舊起始節點視覺
            if (!string.IsNullOrEmpty(_graph.startNodeId) && _nodeViews.TryGetValue(_graph.startNodeId, out var oldStart)) 
                oldStart.SetIsStartNode(false);
            
            _graph.startNodeId = nodeId;
            
            // 更新新起始節點視覺
            if (!string.IsNullOrEmpty(_graph.startNodeId) && _nodeViews.TryGetValue(_graph.startNodeId, out var newStart)) 
                newStart.SetIsStartNode(true);
            
            EditorUtility.SetDirty(_graph);
        }
        
        /// <summary>
        /// 根據容器物件取得其包含的節點列表。
        /// </summary>
        public List<DialogueNodeBase> GetNodesFromContainer(object container)
        {
            return container switch
            {
                DialogueGraph graph => graph.AllNodes,
                SequenceNode sequence => sequence.childNodes,
                ParallelNode parallel => parallel.childNodes,
                _ => null
            };
        }

        /// <summary>
        /// 記錄 Unity Undo 操作。
        /// </summary>
        public void RecordUndo(string undoName)
        {
            if (_graph != null)
            {
                Undo.RecordObject(_graph, undoName);
                EditorUtility.SetDirty(_graph);
            }
        }

        /// <summary>
        /// 將所有節點的視覺位置同步回資料資產。
        /// </summary>
        public void SyncPositionsToAsset()
        {
            if (_graph == null) return;
            foreach (var node in nodes.OfType<DialogueNodeElement>())
            {
                _graph.SetNodePosition(node.NodeId, node.GetPosition().position);
            }
        }

        private Port TryGetInputPort(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            return _nodeViews.TryGetValue(nodeId, out var element) ? element.InputPort : null;
        }

        /// <summary>
        /// 尋找特定容器節點在 SerializedObject 中的屬性路徑。
        /// 用於支援 Inspector 的即時修改。
        /// </summary>
        private SerializedProperty FindNodesProperty(SerializedObject serializedGraph, object container)
        {
            if (container is DialogueGraph) return serializedGraph.FindProperty("AllNodes");
            if (container is DialogueNodeBase containerNode)
            {
                string path = FindPropertyPath(_graph.AllNodes, "AllNodes", containerNode);
                if (!string.IsNullOrEmpty(path))
                {
                    var containerProperty = serializedGraph.FindProperty(path);
                    return containerProperty?.FindPropertyRelative("childNodes");
                }
            }
            return null;
        }

        /// <summary>
        /// 遞迴搜尋節點屬性路徑。
        /// </summary>
        private string FindPropertyPath(List<DialogueNodeBase> nodes, string currentPath, DialogueNodeBase targetNode)
        {
            if (nodes == null) return null;
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.nodeId == targetNode.nodeId) return $"{currentPath}.Array.data[{i}]";
                
                if (node is SequenceNode seqNode)
                {
                    string foundPath = FindPropertyPath(seqNode.childNodes, $"{currentPath}.Array.data[{i}].childNodes", targetNode);
                    if (foundPath != null) return foundPath;
                }
                else if (node is ParallelNode parNode)
                {
                    string foundPath = FindPropertyPath(parNode.childNodes, $"{currentPath}.Array.data[{i}].childNodes", targetNode);
                    if (foundPath != null) return foundPath;
                }
            }
            return null;
        }

        private void UpdateStartNodeVisuals()
        {
            foreach (var nodeView in _nodeViews.Values)
            {
                nodeView.SetIsStartNode(nodeView.NodeId == _graph.startNodeId);
            }
        }

        /// <summary>
        /// 定義哪些 Port 可以互相連接。
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(port => 
                startPort != port && 
                startPort.node != port.node && 
                startPort.direction != port.direction
            ).ToList();
        }

        #endregion

        #region SubGraph & Connection Helpers

        private void CreateSequenceStartNode(SequenceNode seqNode)
        {
            SequenceStartNode = new SequenceStartNodeElement();
            AddElement(SequenceStartNode);
        }

        private void CreateParallelStartNode(ParallelNode parNode)
        {
            ParallelStartNode = new ParallelBranchStartNodeElement();
            ParallelStartNode.BuildPorts(parNode.branchStartNodeIds);
            ParallelStartNode.OnBranchesChanged = () =>
            {
                RecordUndo("Modify Parallel Branches");
                // 同步資料與視覺
                while (parNode.branchStartNodeIds.Count < ParallelStartNode.BranchPorts.Count) parNode.branchStartNodeIds.Add(null);
                while (parNode.branchStartNodeIds.Count > ParallelStartNode.BranchPorts.Count) parNode.branchStartNodeIds.RemoveAt(parNode.branchStartNodeIds.Count - 1);
            };
            AddElement(ParallelStartNode);
        }

        private void ConnectPortsForNode(DialogueNodeElement sourceView, DialogueNodeBase nodeData)
        {
            if (NodeHandlerRegistry.Handlers.TryGetValue(nodeData.GetType(), out var handler))
            {
                handler.ConnectPorts(sourceView, nodeData, TryGetInputPort, ConnectPorts);
            }
        }

        private void ConnectPorts(Port output, Port input)
        {
            if (output != null && input != null)
            {
                AddElement(output.ConnectTo(input));
            }
        }

        #endregion
    }
}
#endif