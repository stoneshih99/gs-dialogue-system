#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// DialogueGraphView 的複製 / 貼上處理器。
    /// - 支援 Ctrl/Cmd + C / V 針對 DialogueNodeElement 做複製與貼上。
    /// - 複製時會深拷貝 NodeData，並呼叫各 node 的 ClearConnectionsForClipboard / ClearUnityReferencesForClipboard。
    /// - 貼上時會為每個節點產新的 nodeId，加入 Graph，並建立對應的視覺節點。
    /// </summary>
    public class NodeClipboardHandler
    {
        private readonly DialogueGraphView _graphView;
        private string _clipboard;
        private Vector2 _lastMousePosition;

        public NodeClipboardHandler(DialogueGraphView graphView)
        {
            _graphView = graphView;
            _graphView.RegisterCallback<KeyDownEvent>(OnKeyDown);
            _graphView.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            _lastMousePosition = _graphView.contentViewContainer.WorldToLocal(evt.mousePosition);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            // 只在 Ctrl/Cmd 情境下處理
            if (!evt.actionKey) return;

            // 焦點在文字輸入時不攔截
            if (evt.target is TextField || evt.target is TextElement) return;

            if (evt.keyCode == KeyCode.C)
            {
                CopySelectionToClipboard();
                evt.StopImmediatePropagation();
            }
            else if (evt.keyCode == KeyCode.V)
            {
                PasteFromClipboard(_lastMousePosition);
                evt.StopImmediatePropagation();
            }
        }

        /// <summary>
        /// 複製當前選取的 DialogueNodeElement 到內部剪貼簿。
        /// </summary>
        public void CopySelectionToClipboard()
        {
            var selectedNodesData = _graphView.selection
                .OfType<DialogueNodeElement>()
                .Select(n => n.NodeData)
                .ToList();

            if (!selectedNodesData.Any()) return;

            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                };

                // 先用 JSON 深拷貝
                string tempJson = JsonConvert.SerializeObject(selectedNodesData, settings);
                var copiedNodes = JsonConvert.DeserializeObject<List<DialogueNodeBase>>(tempJson, settings);

                if (copiedNodes == null || copiedNodes.Count == 0)
                    return;

                // 讓每個 node 自己負責清連線/Unity 物件/狀態
                foreach (var node in copiedNodes)
                {
                    if (node == null) continue;

                    node.ClearConnectionsForClipboard();
                    node.ClearUnityReferencesForClipboard();
                    node.OnAfterClonedFromClipboard();
                }

                _clipboard = JsonConvert.SerializeObject(copiedNodes, Formatting.Indented, settings);
                Debug.Log($"[NodeClipboardHandler] Copied {copiedNodes.Count} nodes to clipboard.");
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[Copy Error] Failed to process nodes for clipboard. Error: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 從剪貼簿貼上節點，並在給定位置附近排列。
        /// </summary>
        public void PasteFromClipboard(Vector2 pastePosition)
        {
            if (string.IsNullOrEmpty(_clipboard) ||
                _graphView.Graph == null ||
                _graphView.NavigationStack == null ||
                _graphView.NavigationStack.Count == 0)
            {
                return;
            }

            List<DialogueNodeBase> pastedNodesData;
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                };
                pastedNodesData = JsonConvert.DeserializeObject<List<DialogueNodeBase>>(_clipboard, settings);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Paste Error] Failed to deserialize nodes. Error: {e.Message}\n{e.StackTrace}");
                return;
            }

            if (pastedNodesData == null || pastedNodesData.Count == 0)
                return;

            _graphView.RecordUndo("Paste Nodes");
            _graphView.Graph.BuildLookup();

            var usedIds = new HashSet<string>(_graphView.Graph.AllNodes.Select(n => n.nodeId));

            // 為貼上的節點產新的唯一 nodeId
            foreach (var nodeData in pastedNodesData)
            {
                if (nodeData == null) continue;

                string oldId = nodeData.nodeId;
                string prefix = !string.IsNullOrEmpty(oldId) && oldId.Contains('_')
                    ? oldId.Split('_')[0]
                    : "NODE";

                nodeData.nodeId = GenerateUniqueNodeIdForPaste(prefix, usedIds);

                // 若節點貼上後還需要 reset 特定狀態，也可在這裡呼叫額外 hook（目前已在 OnAfterClonedFromClipboard 做大部分）。
                // 例如：nodeData.OnAfterPastedIntoGraph(); 若你未來想再加一層。
            }

            // 找到目前的節點容器（根 Graph 或 Sequence/Parallel 等）
            var container = _graphView.NavigationStack.Peek();
            var targetList = _graphView.GetNodesFromContainer(container);
            if (targetList == null) return;

            int originalCount = targetList.Count;
            Vector2 currentPastePos = pastePosition;

            // 先把資料層級加進 Graph
            foreach (var newNodeData in pastedNodesData)
            {
                if (newNodeData == null) continue;

                _graphView.Graph.SetNodePosition(newNodeData.nodeId, currentPastePos);
                targetList.Add(newNodeData);
                currentPastePos += new Vector2(30f, 30f); // 每個 node 稍微錯開一點
            }

            EditorUtility.SetDirty(_graphView.Graph);

            // 再用 SerializedObject 寫回，並建立視覺節點
            var serializedGraph = new SerializedObject(_graphView.Graph);
            serializedGraph.Update();

            var nodesProperty = FindNodesProperty(serializedGraph, container);

            if (nodesProperty != null)
            {
                int newCount = originalCount + pastedNodesData.Count;
                if (nodesProperty.arraySize < newCount)
                {
                    nodesProperty.arraySize = newCount;
                }

                for (int i = 0; i < pastedNodesData.Count; i++)
                {
                    var nodeData = pastedNodesData[i];
                    if (nodeData == null) continue;

                    var nodeProperty = nodesProperty.GetArrayElementAtIndex(originalCount + i);
                    _graphView.CreateAndRegisterNode(nodeData, nodeProperty);
                }

                serializedGraph.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("[NodeClipboardHandler] PasteFromClipboard: nodesProperty not found for container.");
            }
        }

        /// <summary>
        /// 找到目前 container 對應的 SerializedProperty：
        /// - 若 container 是 DialogueGraph → 回傳 AllNodes。
        /// - 若 container 是某個 DialogueNodeBase → 找到它在 AllNodes 裡的位置，再取 childNodes。
        /// </summary>
        private SerializedProperty FindNodesProperty(SerializedObject serializedGraph, object container)
        {
            if (container is DialogueGraph)
            {
                return serializedGraph.FindProperty("AllNodes");
            }

            if (container is DialogueNodeBase containerNode)
            {
                string path = FindPropertyPath(_graphView.Graph.AllNodes, "AllNodes", containerNode);
                if (!string.IsNullOrEmpty(path))
                {
                    var containerProperty = serializedGraph.FindProperty(path);
                    return containerProperty?.FindPropertyRelative("childNodes");
                }
            }

            return null;
        }

        /// <summary>
        /// 在 AllNodes 的樹狀結構裡遞迴找出 targetNode 的 SerializedProperty 路徑。
        /// </summary>
        private string FindPropertyPath(
            List<DialogueNodeBase> nodes,
            string currentPath,
            DialogueNodeBase targetNode)
        {
            if (nodes == null) return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null) continue;

                if (node.nodeId == targetNode.nodeId)
                {
                    return $"{currentPath}.Array.data[{i}]";
                }

                if (node is SequenceNode seqNode)
                {
                    string foundPath = FindPropertyPath(
                        seqNode.childNodes,
                        $"{currentPath}.Array.data[{i}].childNodes",
                        targetNode);

                    if (foundPath != null) return foundPath;
                }
                else if (node is ParallelNode parNode)
                {
                    string foundPath = FindPropertyPath(
                        parNode.childNodes,
                        $"{currentPath}.Array.data[{i}].childNodes",
                        targetNode);

                    if (foundPath != null) return foundPath;
                }
            }

            return null;
        }

        /// <summary>
        /// 一般用的 nodeId 產生器（沒考慮本次 paste 批次內的重複，只看 Graph）。
        /// </summary>
        public string GenerateUniqueNodeId(string prefix)
        {
            int i = 1;
            string id;

            do
            {
                id = $"{prefix}_{i++}";
            } while (_graphView.Graph != null && _graphView.Graph.HasNode(id));

            return id;
        }

        /// <summary>
        /// 貼上專用的 nodeId 產生器：同時避免 Graph 裡和本次貼上批次裡的重複。
        /// </summary>
        private string GenerateUniqueNodeIdForPaste(string prefix, HashSet<string> usedIds)
        {
            int i = 1;
            string id;
            do
            {
                id = $"{prefix}_{i++}";
            } while ((_graphView.Graph != null && _graphView.Graph.HasNode(id)) || usedIds.Contains(id));

            usedIds.Add(id);
            return id;
        }

        public bool HasClipboardContent() => !string.IsNullOrEmpty(_clipboard);
    }
}
#endif
