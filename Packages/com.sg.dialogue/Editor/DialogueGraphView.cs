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
    /// DialogueGraphView 是對話圖的視覺化編輯器核心。
    /// 它繼承自 Unity 的 GraphView，提供了節點的佈局、連接、縮放、拖曳等所有互動功能。
    /// 這個類別負責將 DialogueGraph ScriptableObject 的資料模型轉換為使用者可以互動的視覺元素。
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        /// <summary>
        /// 當導覽堆疊（例如，進入或退出子圖）發生變化時觸發的事件。
        /// </summary>
        public Action<Stack<object>> OnNavigationChanged;

        /// <summary>
        /// 用於在 EditorPrefs 中儲存視圖變換（位置、縮放）的鍵值前綴。
        /// </summary>
        private const string ViewTransformKeyPrefix = "DialogueGraphView.ViewTransform.";
        
        /// <summary>
        /// 目前正在編輯的對話圖資料資產。
        /// </summary>
        public DialogueGraph Graph => _graph;
        private DialogueGraph _graph;

        /// <summary>
        /// 全域狀態資產，主要用於條件節點的變數選擇。
        /// </summary>
        public DialogueStateAsset GlobalState => _globalState;
        private DialogueStateAsset _globalState;
        
        private readonly Vector2 defaultNodeSize = new Vector2(200, 150);
        
        /// <summary>
        /// 節點 ID 到其視覺元素（DialogueNodeElement）的快速查找字典。
        /// </summary>
        private readonly Dictionary<string, DialogueNodeElement> _nodeViews = new();
        
        /// <summary>
        /// 導覽堆疊，用於處理子圖（如 SequenceNode, ParallelNode）的進入和退出。
        /// 堆疊頂部是目前顯示的容器（DialogueGraph、SequenceNode 或 ParallelNode）。
        /// </summary>
        public Stack<object> NavigationStack => _navigationStack;
        private readonly Stack<object> _navigationStack = new();
        
        /// <summary>
        /// 一個標記，用於防止在程式化填充視圖時觸發 OnGraphViewChanged 事件。
        /// </summary>
        private bool _isPopulating;

        // --- 子圖相關節點的引用 ---
        public SequenceStartNodeElement SequenceStartNode { get; private set; }
        public ParallelBranchStartNodeElement ParallelStartNode { get; private set; }

        // --- 處理器 ---
        private readonly NodeClipboardHandler _clipboardHandler;
        private readonly GraphConnectionHandler _connectionHandler;
        
        /// <summary>
        /// 用於追蹤目前正在執行並高亮的節點。
        /// </summary>
        private DialogueNodeElement _executingNode;

        public DialogueGraphView()
        {
            // --- 初始化 GraphView 的基本功能 ---
            // 設定縮放範圍
            this.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            // 添加內容拖曳、選取、框選等操作器
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());
            // 監聽圖形變更事件
            this.graphViewChanged += OnGraphViewChanged;

            // 添加網格背景
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            style.flexGrow = 1f; // 讓視圖填滿父容器

            // 監聽 Unity 的撤銷/重做操作，以便在操作後刷新視圖
            Undo.undoRedoPerformed += OnUndoRedo;
            
            // 初始化各種處理器
            _clipboardHandler = new NodeClipboardHandler(this);
            _connectionHandler = new GraphConnectionHandler(this);

            // 註冊面板事件，用於管理事件監聽器的生命週期
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        #region Event Handling & Lifecycle
        
        /// <summary>
        /// 當此視圖被附加到 UI 面板時呼叫。
        /// </summary>
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // 當 View 被加入到 UI 中時，開始監聽圖表事件
            RegisterGraphEvents();
        }

        /// <summary>
        /// 當此視圖從 UI 面板上分離時呼叫。
        /// </summary>
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // 當 View 從 UI 中移除時，停止監聽圖表事件，防止記憶體洩漏
            UnregisterGraphEvents();
            Undo.undoRedoPerformed -= OnUndoRedo; // 也在此處移除 Undo/Redo 的監聽
        }

        /// <summary>
        /// 註冊監聽目前圖表的執行時期事件。
        /// </summary>
        private void RegisterGraphEvents()
        {
            if (_graph == null) return;
            _graph.onNodeEntered.AddListener(OnNodeEntered);
            _graph.onDialogueEnded.AddListener(OnDialogueEnded);
        }

        /// <summary>
        /// 取消監聽目前圖表的執行時期事件。
        /// </summary>
        private void UnregisterGraphEvents()
        {
            if (_graph == null) return;
            _graph.onNodeEntered.RemoveListener(OnNodeEntered);
            _graph.onDialogueEnded.RemoveListener(OnDialogueEnded);
            
            // 清除執行狀態
            OnDialogueEnded();
        }

        /// <summary>
        /// 當對話執行進入一個新節點時呼叫，用於更新節點高亮。
        /// </summary>
        private void OnNodeEntered(string nodeId)
        {
            // 清除上一個執行節點的高亮
            if (_executingNode != null)
            {
                _executingNode.SetExecutionState(false);
                _executingNode = null;
            }

            // 找到並高亮目前節點
            if (_nodeViews.TryGetValue(nodeId, out var currentNodeView))
            {
                _executingNode = currentNodeView;
                _executingNode.SetExecutionState(true);
            }
        }

        /// <summary>
        /// 當對話結束時呼叫，用於清除所有高亮。
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
        /// 當執行撤銷或重做時，重新填充視圖以反映資料變更。
        /// </summary>
        private void OnUndoRedo()
        {
            if (_graph)
            {
                PopulateView(_graph);
            }
        }
        
        #endregion

        #region Population & Drawing

        /// <summary>
        /// 填充視圖的入口方法。它會設定新的圖表資料，並觸發繪製流程。
        /// </summary>
        /// <param name="graph">要顯示的 DialogueGraph 資產。</param>
        public void PopulateView(DialogueGraph graph)
        {
            _isPopulating = true;
            try
            {
                // 在更換圖表前，先取消監聽舊圖表的事件
                UnregisterGraphEvents();

                _graph = graph;
                _navigationStack.Clear();
                if (graph)
                {
                    _navigationStack.Push(graph);
                }

                // 註冊新圖表的事件
                RegisterGraphEvents();

                // 從導覽堆疊的頂部開始填充
                PopulateFromCurrentNavigation();
            }
            finally
            {
                _isPopulating = false;
            }
        }

        /// <summary>
        /// 根據目前導覽堆疊的狀態，清除並重新繪製整個圖形視圖。
        /// </summary>
        private void PopulateFromCurrentNavigation()
        {
            _isPopulating = true;
            try
            {
                // --- 清理工作 ---
                DeleteElements(graphElements.ToList());
                _nodeViews.Clear();
                SequenceStartNode = null;
                ParallelStartNode = null;
                _executingNode = null; // 重置執行節點

                if (!_graph || _navigationStack.Count == 0) return;
                
                var currentContainer = _navigationStack.Peek();
                if (currentContainer == null) return;

                // --- 繪製節點 ---
                // 如果在子圖中，創建對應的虛擬起始節點
                if (currentContainer is SequenceNode seqNode) CreateSequenceStartNode(seqNode);
                else if (currentContainer is ParallelNode parNode) CreateParallelStartNode(parNode);
                
                List<DialogueNodeBase> nodesToDisplay = GetNodesFromContainer(currentContainer);
                if (nodesToDisplay == null) return;

                _graph.BuildLookup(); // 確保節點查找字典是最新的

                // 為了與 Inspector 互動，我們需要使用 SerializedObject
                var serializedGraph = new SerializedObject(_graph);
                var nodesProperty = FindNodesProperty(serializedGraph, currentContainer);

                // 遍歷資料並創建視覺元素
                for (int i = 0; i < nodesToDisplay.Count; i++)
                {
                    var nodeData = nodesToDisplay[i];
                    var nodeProperty = nodesProperty?.GetArrayElementAtIndex(i);
                    CreateAndRegisterNode(nodeData, nodeProperty);
                }

                // --- 繪製連線 ---
                foreach (var nodeData in nodesToDisplay)
                {
                    if (!_nodeViews.TryGetValue(nodeData.nodeId, out var sourceView)) continue;
                    ConnectPortsForNode(sourceView, nodeData);
                }

                // 連接子圖起始節點的輸出埠
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
                
                // --- 收尾工作 ---
                ResetView(); // 恢復視圖的位置和縮放
                OnNavigationChanged?.Invoke(_navigationStack); // 通知外部 UI 更新導覽麵包屑
                UpdateStartNodeVisuals(); // 更新起始節點的視覺標記
            }
            finally
            {
                _isPopulating = false;
            }
        }
        
        /// <summary>
        /// 根據節點資料創建對應的視覺元素，並將其註冊到視圖中。
        /// </summary>
        public DialogueNodeElement CreateAndRegisterNode(DialogueNodeBase node, SerializedProperty nodeProperty)
        {
            // 使用 NodeHandlerRegistry 來解耦節點類型和其視覺元素的創建
            if (NodeHandlerRegistry.Handlers.TryGetValue(node.GetType(), out var handler))
            {
                var element = handler.CreateNodeElement(node, this, nodeProperty, () => RecordUndo("Modify Node"));
                if (element != null)
                {
                    element.SetPosition(new Rect(_graph.GetNodePosition(node.nodeId), defaultNodeSize));
                    element.Initialize(this, nodeProperty); 
                    element.OnDelete = () => 
                    {
                        RecordUndo("Delete Node");
                        var container = _navigationStack.Peek();
                        GetNodesFromContainer(container)?.Remove(node);
                        _graph.RemoveNodePosition(node.nodeId);
                        _nodeViews.Remove(node.nodeId);
                        EditorUtility.SetDirty(_graph);
                    };
                    AddElement(element);
                    _nodeViews[node.nodeId] = element; // 註冊到字典中以便快速查找
                }
                return element;
            }
            return null;
        }

        /// <summary>
        /// 處理使用者在圖形視圖中進行的各種變更（如創建/刪除邊、移動元素）。
        /// </summary>
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isPopulating || _graph == null) return change;

            // 處理新創建的連線
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate) _connectionHandler.HandleEdgeConnection(edge);
            }

            // 處理被移除的元素
            if (change.elementsToRemove != null)
            {
                foreach (var el in change.elementsToRemove)
                {
                    if (el is Edge edge) _connectionHandler.HandleEdgeDisconnection(edge);
                    else if (el is DialogueNodeElement nodeElement) nodeElement.OnDelete?.Invoke();
                }
            }

            // 處理被移動的元素
            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                RecordUndo("Move Nodes");
                SyncPositionsToAsset();
            }

            return change;
        }
        
        #endregion

        #region Navigation & View
        
        /// <summary>
        /// 進入一個容器節點（子圖）。
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
        /// 從子圖返回上一層。
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
        /// 儲存目前的視圖變換（位置和縮放）到 EditorPrefs。
        /// </summary>
        public void SaveViewTransform()
        {
            if (_graph == null) return;
            string key = GetViewTransformKey();
            string value = $"{viewTransform.position.x},{viewTransform.position.y},{viewTransform.scale.x}";
            EditorPrefs.SetString(key, value);
        }

        /// <summary>
        /// 嘗試從 EditorPrefs 載入並應用視圖變換。
        /// </summary>
        private bool TryLoadViewTransform()
        {
            if (_graph == null) return false;
            string key = GetViewTransformKey();
            string value = EditorPrefs.GetString(key);
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split(',');
                if (parts.Length == 3 && float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y) && float.TryParse(parts[2], out float scale))
                {
                    UpdateViewTransform(new Vector3(x, y, viewTransform.position.z), new Vector3(scale, scale, viewTransform.scale.z));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 將視圖聚焦到圖的起始節點或整個圖。
        /// </summary>
        public void FrameGraph()
        {
            if (_graph == null || _navigationStack.Count == 0) return;
            var container = _navigationStack.Peek();
            
            if (container is DialogueGraph graph)
            {
                if (!string.IsNullOrEmpty(graph.startNodeId) && _nodeViews.TryGetValue(graph.startNodeId, out var startNodeElement)) FrameSelectionOrAll(startNodeElement);
                else FrameAllOrReset();
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
        /// 重置視圖，優先載入已儲存的視圖狀態，否則聚焦到圖上。
        /// </summary>
        private void ResetView()
        {
            if (!TryLoadViewTransform()) FrameGraph();
        }
        
        #endregion

        #region Context Menu & Node Creation

        /// <summary>
        /// 建立右鍵上下文菜單。
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            var mousePos = contentViewContainer.WorldToLocal(evt.mousePosition);
            
            // 添加複製/貼上選項
            if (selection.Any(s => s is DialogueNodeElement)) evt.menu.AppendAction("Copy", action => _clipboardHandler.CopySelectionToClipboard());
            evt.menu.AppendAction("Paste", action => _clipboardHandler.PasteFromClipboard(mousePos), DropdownMenuAction.Status.Normal);
            if (selection.Any(s => s is DialogueNodeElement) || _clipboardHandler.HasClipboardContent()) evt.menu.AppendSeparator();
            
            bool inSubGraph = _navigationStack.Count > 1;

            // 根據 NodeHandlerRegistry 動態生成創建節點的菜單項
            foreach (var handler in NodeHandlerRegistry.Handlers.Values)
            {
                // 在子圖中不允許創建新的子圖節點
                if (inSubGraph && (handler.CreateNodeData() is SequenceNode || handler.CreateNodeData() is ParallelNode)) continue;
                evt.menu.AppendAction(handler.MenuName, _ => { CreateAndAddNode(handler.CreateNodeData(), mousePos, handler); });
            }
        }

        /// <summary>
        /// 處理節點創建的邏輯。
        /// </summary>
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

            var serializedGraph = new SerializedObject(_graph);
            var nodesProperty = FindNodesProperty(serializedGraph, container);
            var newNodeProperty = nodesProperty?.GetArrayElementAtIndex(targetList.Count - 1);
            
            CreateAndRegisterNode(node, newNodeProperty); 
            
            EditorUtility.SetDirty(_graph);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// 設定全域狀態資產，並通知相關節點更新其 UI。
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
        /// 設定當前圖的起始節點。
        /// </summary>
        public void SetStartNode(string nodeId)
        {
            if (_graph == null) return;
            RecordUndo("Set Start Node");
            // 更新舊起始節點的視覺
            if (!string.IsNullOrEmpty(_graph.startNodeId) && _nodeViews.TryGetValue(_graph.startNodeId, out var oldStartNodeElement)) oldStartNodeElement.SetIsStartNode(false);
            // 更新資料
            _graph.startNodeId = nodeId;
            // 更新新起始節點的視覺
            if (!string.IsNullOrEmpty(_graph.startNodeId) && _nodeViews.TryGetValue(_graph.startNodeId, out var newStartNodeElement)) newStartNodeElement.SetIsStartNode(true);
            EditorUtility.SetDirty(_graph);
        }
        
        /// <summary>
        /// 根據容器物件獲取其包含的節點列表。
        /// </summary>
        public List<DialogueNodeBase> GetNodesFromContainer(object container)
        {
            if (container is DialogueGraph graph) return graph.AllNodes;
            if (container is SequenceNode sequence) return sequence.childNodes;
            if (container is ParallelNode parallel) return parallel.childNodes;
            return null;
        }

        /// <summary>
        /// 記錄一步可撤銷的操作。
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
        /// 將所有節點的視覺位置同步到底層的資料資產中。
        /// </summary>
        public void SyncPositionsToAsset()
        {
            if (_graph == null) return;
            foreach (var node in nodes.OfType<DialogueNodeElement>())
            {
                _graph.SetNodePosition(node.NodeId, node.GetPosition().position);
            }
        }

        /// <summary>
        /// 根據節點 ID 嘗試獲取其輸入埠。
        /// </summary>
        private Port TryGetInputPort(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            if (_nodeViews.TryGetValue(nodeId, out var element)) return element.InputPort;
            return null;
        }

        /// <summary>
        /// 根據 SerializedObject 和容器物件，找到對應的節點列表屬性。
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
        /// 遞迴查找目標節點在 SerializedObject 中的屬性路徑。
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

        /// <summary>
        /// 更新所有可見節點的起始節點視覺標記。
        /// </summary>
        private void UpdateStartNodeVisuals()
        {
            foreach (var nodeView in _nodeViews.Values)
            {
                nodeView.SetIsStartNode(nodeView.NodeId == _graph.startNodeId);
            }
        }

        // --- 以下方法是為了與舊程式碼兼容或提供簡化的介面 ---
        private void CreateSequenceStartNode(SequenceNode seqNode) { SequenceStartNode = new SequenceStartNodeElement(); AddElement(SequenceStartNode); }
        private void CreateParallelStartNode(ParallelNode parNode) { ParallelStartNode = new ParallelBranchStartNodeElement(); ParallelStartNode.BuildPorts(parNode.branchStartNodeIds); ParallelStartNode.OnBranchesChanged = () => { RecordUndo("Modify Parallel Branches"); while (parNode.branchStartNodeIds.Count < ParallelStartNode.BranchPorts.Count) parNode.branchStartNodeIds.Add(null); while (parNode.branchStartNodeIds.Count > ParallelStartNode.BranchPorts.Count) parNode.branchStartNodeIds.RemoveAt(parNode.branchStartNodeIds.Count - 1); }; AddElement(ParallelStartNode); }
        private void ConnectPortsForNode(DialogueNodeElement sourceView, DialogueNodeBase nodeData) { if (NodeHandlerRegistry.Handlers.TryGetValue(nodeData.GetType(), out var handler)) handler.ConnectPorts(sourceView, nodeData, TryGetInputPort, ConnectPorts); }
        private void ConnectPorts(Port output, Port input) { if (output != null && input != null) AddElement(output.ConnectTo(input)); }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) { var compatiblePorts = new List<Port>(); ports.ForEach(port => { if (startPort != port && startPort.node != port.node && startPort.direction != port.direction) compatiblePorts.Add(port); }); return compatiblePorts; }
        private string GetViewTransformKey() { if (_graph == null) return null; string path = AssetDatabase.GetAssetPath(_graph); string context = "root"; if (_navigationStack.Count > 1 && _navigationStack.Peek() is DialogueNodeBase node) context = node.nodeId; return $"{ViewTransformKeyPrefix}{path}_{context}"; }
        private Port GetOutputPort(string nodeId, string portName = "Next") { if (string.IsNullOrEmpty(nodeId) || !_nodeViews.TryGetValue(nodeId, out var element)) return null; if (NodeHandlerRegistry.Handlers.TryGetValue(element.NodeData.GetType(), out var handler)) return handler.GetOutputPort(element, portName); return null; }

        #endregion
    }
}
#endif
