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
    /// DialogueGraphView 負責在編輯器中顯示和操作對話圖。
    /// 它繼承自 Unity 的 GraphView，提供了節點的佈輯、連接、縮放、拖曳等功能。
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        public Action<Stack<object>> OnNavigationChanged;

        private const string ViewTransformKeyPrefix = "DialogueGraphView.ViewTransform.";
        private DialogueGraph _graph;
        public DialogueGraph Graph => _graph;
        private DialogueStateAsset _globalState;
        public DialogueStateAsset GlobalState => _globalState;
        
        private readonly Vector2 defaultNodeSize = new Vector2(200, 150);
        private readonly Dictionary<string, DialogueNodeElement> _nodeViews = new();
        private readonly Stack<object> _navigationStack = new();
        public Stack<object> NavigationStack => _navigationStack;
        
        private bool _isPopulating;

        // 子圖起始節點的引用
        public SequenceStartNodeElement SequenceStartNode { get; private set; }
        public ParallelBranchStartNodeElement ParallelStartNode { get; private set; }

        private NodeClipboardHandler _clipboardHandler;
        private GraphConnectionHandler _connectionHandler;
        
        // 用於追蹤執行狀態的欄位
        private DialogueNodeElement _executingNode;

        public DialogueGraphView()
        {
            this.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());
            this.graphViewChanged += OnGraphViewChanged;

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            style.flexGrow = 1f;

            Undo.undoRedoPerformed += OnUndoRedo;
            
            _clipboardHandler = new NodeClipboardHandler(this);
            _connectionHandler = new GraphConnectionHandler(this);

            // 註冊面板事件，用於管理事件監聽器的生命週期
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        // 不再使用解構函式，改用 DetachFromPanelEvent
        // ~DialogueGraphView()
        // {
        //     Undo.undoRedoPerformed -= OnUndoRedo;
        // }

        #region Event Handling

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // 當 View 被加入到 UI 中時，開始監聽圖表事件
            RegisterGraphEvents();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // 當 View 從 UI 中移除時，停止監聽圖表事件，防止記憶體洩漏
            UnregisterGraphEvents();
            Undo.undoRedoPerformed -= OnUndoRedo; // 也在此處移除 Undo/Redo 的監聽
        }

        private void RegisterGraphEvents()
        {
            if (_graph == null) return;
            _graph.onNodeEntered.AddListener(OnNodeEntered);
            _graph.onDialogueEnded.AddListener(OnDialogueEnded);
        }

        private void UnregisterGraphEvents()
        {
            if (_graph == null) return;
            _graph.onNodeEntered.RemoveListener(OnNodeEntered);
            _graph.onDialogueEnded.RemoveListener(OnDialogueEnded);
            
            // 清除執行狀態
            OnDialogueEnded();
        }

        /// <summary>
        /// 當對話執行進入一個新節點時呼叫。
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
        /// 當對話結束時呼叫。
        /// </summary>
        private void OnDialogueEnded()
        {
            // 清除所有執行高亮
            if (_executingNode != null)
            {
                _executingNode.SetExecutionState(false);
                _executingNode = null;
            }
        }

        #endregion

        private void OnUndoRedo()
        {
            if (_graph)
            {
                PopulateView(_graph);
            }
        }
        
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        public void SetGlobalState(DialogueStateAsset state)
        {
            _globalState = state;
            foreach (var nodeView in _nodeViews.Values.OfType<ConditionNodeElement>())
            {
                nodeView.UpdateDropdowns(_globalState);
            }
        }

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

                PopulateFromCurrentNavigation();
            }
            finally
            {
                _isPopulating = false;
            }
        }

        private void PopulateFromCurrentNavigation()
        {
            _isPopulating = true;
            try
            {
                DeleteElements(graphElements.ToList());
                _nodeViews.Clear();
                SequenceStartNode = null;
                ParallelStartNode = null;
                _executingNode = null; // 重置執行節點

                if (!_graph || _navigationStack.Count == 0) return;
                
                var currentContainer = _navigationStack.Peek();
                if (currentContainer == null) return;

                if (currentContainer is SequenceNode seqNode) CreateSequenceStartNode(seqNode);
                else if (currentContainer is ParallelNode parNode) CreateParallelStartNode(parNode);
                
                List<DialogueNodeBase> nodesToDisplay = GetNodesFromContainer(currentContainer);
                if (nodesToDisplay == null) return;

                _graph.BuildLookup();

                var serializedGraph = new SerializedObject(_graph);
                var nodesProperty = FindNodesProperty(serializedGraph, currentContainer);

                for (int i = 0; i < nodesToDisplay.Count; i++)
                {
                    var nodeData = nodesToDisplay[i];
                    var nodeProperty = nodesProperty?.GetArrayElementAtIndex(i);
                    CreateAndRegisterNode(nodeData, nodeProperty);
                }

                foreach (var nodeData in nodesToDisplay)
                {
                    if (!_nodeViews.TryGetValue(nodeData.nodeId, out var sourceView)) continue;
                    ConnectPortsForNode(sourceView, nodeData);
                }

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
                
                ResetView();
                OnNavigationChanged?.Invoke(_navigationStack);
                UpdateStartNodeVisuals();
            }
            finally
            {
                _isPopulating = false;
            }
        }

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
        
        public List<DialogueNodeBase> GetNodesFromContainer(object container)
        {
            if (container is DialogueGraph graph) return graph.AllNodes;
            if (container is SequenceNode sequence) return sequence.childNodes;
            if (container is ParallelNode parallel) return parallel.childNodes;
            return null;
        }

        public void EnterContainerNode(DialogueNodeBase containerNode)
        {
            if (containerNode is SequenceNode || containerNode is ParallelNode)
            {
                _navigationStack.Push(containerNode);
                PopulateFromCurrentNavigation();
            }
        }

        public void NavigateBack()
        {
            if (_navigationStack.Count > 1)
            {
                _navigationStack.Pop();
                PopulateFromCurrentNavigation();
            }
        }

        private void ConnectPorts(Port output, Port input)
        {
            if (output != null && input != null) AddElement(output.ConnectTo(input));
        }

        public void SaveViewTransform()
        {
            if (_graph == null) return;
            string key = GetViewTransformKey();
            string value = $"{viewTransform.position.x},{viewTransform.position.y},{viewTransform.scale.x}";
            EditorPrefs.SetString(key, value);
        }

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

        private void ResetView()
        {
            if (!TryLoadViewTransform()) FrameGraph();
        }
        
        private string GetViewTransformKey()
        {
            if (_graph == null) return null;
            string path = AssetDatabase.GetAssetPath(_graph);
            string context = "root";
            if (_navigationStack.Count > 1 && _navigationStack.Peek() is DialogueNodeBase node) context = node.nodeId;
            return $"{ViewTransformKeyPrefix}{path}_{context}";
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isPopulating || _graph == null) return change;

            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate) _connectionHandler.HandleEdgeConnection(edge);
            }

            if (change.elementsToRemove != null)
            {
                foreach (var el in change.elementsToRemove)
                {
                    if (el is Edge edge) _connectionHandler.HandleEdgeDisconnection(edge);
                    else if (el is DialogueNodeElement nodeElement) nodeElement.OnDelete?.Invoke();
                }
            }

            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                RecordUndo("Move Nodes");
                SyncPositionsToAsset();
            }

            return change;
        }

        public void SyncPositionsToAsset()
        {
            if (_graph == null) return;
            foreach (var node in nodes.OfType<DialogueNodeElement>())
            {
                _graph.SetNodePosition(node.NodeId, node.GetPosition().position);
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

            var serializedGraph = new SerializedObject(_graph);
            var nodesProperty = FindNodesProperty(serializedGraph, container);
            var newNodeProperty = nodesProperty?.GetArrayElementAtIndex(targetList.Count - 1);
            
            var element = CreateAndRegisterNode(node, newNodeProperty); 
            
            EditorUtility.SetDirty(_graph);
        }

        public void SetStartNode(string nodeId)
        {
            if (_graph == null) return;
            RecordUndo("Set Start Node");
            if (!string.IsNullOrEmpty(_graph.startNodeId) && _nodeViews.TryGetValue(_graph.startNodeId, out var oldStartNodeElement)) oldStartNodeElement.SetIsStartNode(false);
            _graph.startNodeId = nodeId;
            if (!string.IsNullOrEmpty(_graph.startNodeId) && _nodeViews.TryGetValue(_graph.startNodeId, out var newStartNodeElement)) newStartNodeElement.SetIsStartNode(true);
            EditorUtility.SetDirty(_graph);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            var mousePos = contentViewContainer.WorldToLocal(evt.mousePosition);
            
            if (selection.Any(s => s is DialogueNodeElement)) evt.menu.AppendAction("Copy", action => _clipboardHandler.CopySelectionToClipboard());
            evt.menu.AppendAction("Paste", action => _clipboardHandler.PasteFromClipboard(mousePos), DropdownMenuAction.Status.Normal);
            if (selection.Any(s => s is DialogueNodeElement) || _clipboardHandler.HasClipboardContent()) evt.menu.AppendSeparator();
            
            bool inSubGraph = _navigationStack.Count > 1;

            foreach (var handler in NodeHandlerRegistry.Handlers.Values)
            {
                if (inSubGraph && (handler.CreateNodeData() is SequenceNode || handler.CreateNodeData() is ParallelNode)) continue;
                evt.menu.AppendAction(handler.MenuName, _ => { CreateAndAddNode(handler.CreateNodeData(), mousePos, handler); });
            }
        }

        public DialogueNodeElement CreateAndRegisterNode(DialogueNodeBase node, SerializedProperty nodeProperty)
        {
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
                    _nodeViews[node.nodeId] = element;
                }
                return element;
            }
            return null;
        }

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

        public void RecordUndo(string undoName)
        {
            if (_graph != null)
            {
                Undo.RecordObject(_graph, undoName);
                EditorUtility.SetDirty(_graph);
            }
        }

        private Port TryGetInputPort(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            if (_nodeViews.TryGetValue(nodeId, out var element)) return element.InputPort;
            return null;
        }

        private Port GetOutputPort(string nodeId, string portName = "Next")
        {
            if (string.IsNullOrEmpty(nodeId) || !_nodeViews.TryGetValue(nodeId, out var element)) return null;
            if (NodeHandlerRegistry.Handlers.TryGetValue(element.NodeData.GetType(), out var handler)) return handler.GetOutputPort(element, portName);
            return null;
        }

        private void UpdateStartNodeVisuals()
        {
            foreach (var nodeView in _nodeViews.Values)
            {
                nodeView.SetIsStartNode(nodeView.NodeId == _graph.startNodeId);
            }
        }
    }
}
#endif
