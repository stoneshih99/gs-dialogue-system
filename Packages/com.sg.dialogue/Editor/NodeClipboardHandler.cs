#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// NodeClipboardHandler 負責處理 DialogueGraphView 中的節點複製、剪下和貼上操作。
    /// </summary>
    public class NodeClipboardHandler
    {
        private readonly DialogueGraphView _graphView;
        private string _clipboard;              // 用於儲存複製的節點資料
        private Vector2 _lastMousePosition;     // 用於追蹤滑鼠位置（Graph 內容座標）

        public NodeClipboardHandler(DialogueGraphView graphView)
        {
            _graphView = graphView;

            // 註冊鍵盤和滑鼠事件到 GraphView
            _graphView.RegisterCallback<KeyDownEvent>(OnKeyDown);
            _graphView.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        /// <summary>
        /// 處理滑鼠移動事件，更新最後的滑鼠位置。
        /// </summary>
        private void OnMouseMove(MouseMoveEvent evt)
        {
            // 統一使用 contentViewContainer 的座標系（跟節點位置一致）
            _lastMousePosition = _graphView.contentViewContainer.WorldToLocal(evt.mousePosition);
        }

        /// <summary>
        /// 處理鍵盤按下事件，用於觸發複製和貼上操作。
        /// </summary>
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!evt.actionKey) // Command/Ctrl key
                return;

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
        /// 將選中的節點複製到剪貼簿。
        /// </summary>
        public void CopySelectionToClipboard()
        {
            var selectedNodesData = _graphView.selection
                .OfType<DialogueNodeElement>()
                .Select(n => n.NodeData)
                .ToList();

            if (selectedNodesData.Any())
            {
                try
                {
                    var settings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore // 避免循環引用問題
                    };
                    _clipboard = JsonConvert.SerializeObject(selectedNodesData, Formatting.Indented, settings);
                    Debug.Log($"Copied {selectedNodesData.Count} nodes to clipboard.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Copy Error] Failed to serialize nodes. Error: {e.Message}\n{e.StackTrace}");
                    // 嘗試找出是哪個節點類型的問題
                    foreach (var nodeData in selectedNodesData)
                    {
                        try
                        {
                            JsonConvert.SerializeObject(nodeData, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
                        }
                        catch (Exception innerEx)
                        {
                            Debug.LogError($"[Copy Error] Problem might be with node of type: {nodeData.GetType().FullName}. Inner Exception: {innerEx.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 從剪貼簿貼上節點。
        /// </summary>
        /// <param name="pastePosition">貼上節點的起始位置（Graph 內容座標）。</param>
        public void PasteFromClipboard(Vector2 pastePosition)
        {
            if (string.IsNullOrEmpty(_clipboard)) return;
            if (_graphView.Graph == null) return;

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
                Debug.LogError($"[Paste Error] Failed to deserialize nodes from clipboard. Error: {e.Message}\n{e.StackTrace}");
                Debug.LogWarning($"Clipboard content was:\n{_clipboard}");
                return;
            }

            if (pastedNodesData == null || !pastedNodesData.Any()) return;

            _graphView.RecordUndo("Paste Nodes");

            _graphView.Graph.BuildLookup();

            var oldIdToNewId = new Dictionary<string, string>();
            
            // 第一次遍歷：為反序列化出來的節點產生新的唯一 ID
            foreach (var nodeData in pastedNodesData)
            {
                string oldId = nodeData.nodeId;
                string prefix = oldId.Contains('_') ? oldId.Split('_')[0] : "NODE";
                nodeData.nodeId = GenerateUniqueNodeId(prefix);
                oldIdToNewId[oldId] = nodeData.nodeId;
            }

            // 第二次遍歷：更新節點內部的連接，將舊的目標節點 ID 替換為新的 ID
            foreach (var newNodeData in pastedNodesData)
            {
                UpdatePastedNodeConnections(newNodeData, oldIdToNewId);
            }

            // 獲取當前容器（對話圖或子圖）
            var container = _graphView.NavigationStack.Peek();
            var targetList = _graphView.GetNodesFromContainer(container);
            if (targetList == null) return;

            int originalCount = targetList.Count;

            // 將節點添加到數據模型
            Vector2 currentPastePos = pastePosition;
            foreach (var newNodeData in pastedNodesData)
            {
                _graphView.Graph.SetNodePosition(newNodeData.nodeId, currentPastePos);
                targetList.Add(newNodeData);
                currentPastePos += new Vector2(30f, 30f);
            }

            // 更新序列化對象並創建視覺元素
            EditorUtility.SetDirty(_graphView.Graph);
            var serializedGraph = new SerializedObject(_graphView.Graph);
            var nodesProperty = FindNodesProperty(serializedGraph, container);

            if (nodesProperty != null)
            {
                for (int i = 0; i < pastedNodesData.Count; i++)
                {
                    var nodeData = pastedNodesData[i];
                    var nodeProperty = nodesProperty.GetArrayElementAtIndex(originalCount + i);
                    _graphView.CreateAndRegisterNode(nodeData, nodeProperty);
                }
            }
        }

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

        private string FindPropertyPath(List<DialogueNodeBase> nodes, string currentPath, DialogueNodeBase targetNode)
        {
            if (nodes == null) return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.nodeId == targetNode.nodeId)
                {
                    return $"{currentPath}.Array.data[{i}]";
                }

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
        /// 更新貼上節點的內部連接，將舊的節點 ID 替換為新的 ID。
        /// </summary>
        private void UpdatePastedNodeConnections(DialogueNodeBase newNode, Dictionary<string, string> oldIdToNewId)
        {
            // 抓 public + private + 父類別欄位，避免漏掉序列化欄位
            var fields = newNode.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

            foreach (var field in fields)
            {
                // 單一節點 ID 引用 (例如 nextNodeId, InterruptNextNodeId)
                if (field.FieldType == typeof(string) &&
                    field.Name.EndsWith("Id", StringComparison.Ordinal) &&
                    !field.Name.Equals("nodeId", StringComparison.Ordinal))
                {
                    string oldTargetId = field.GetValue(newNode) as string;
                    if (string.IsNullOrEmpty(oldTargetId)) continue;

                    if (oldIdToNewId.TryGetValue(oldTargetId, out string newTargetId))
                    {
                        field.SetValue(newNode, newTargetId);
                    }
                    else
                    {
                        // 不在貼上選取中的 Id → 保留原值，維持連線到圖外節點
                        field.SetValue(newNode, oldTargetId);
                    }
                }
                // 節點 ID 列表引用 (例如 ParallelNode 的 branchStartNodeIds)
                else if (field.FieldType == typeof(List<string>) &&
                         field.Name.EndsWith("Ids", StringComparison.Ordinal))
                {
                    var oldList = field.GetValue(newNode) as List<string>;
                    if (oldList == null) continue;

                    var newList = new List<string>(oldList.Count);
                    foreach (var oldId in oldList)
                    {
                        if (string.IsNullOrEmpty(oldId)) continue;

                        if (oldIdToNewId.TryGetValue(oldId, out string newId))
                        {
                            newList.Add(newId);
                        }
                        else
                        {
                            // 沒在 mapping 裡 → 保留原 Id
                            newList.Add(oldId);
                        }
                    }

                    field.SetValue(newNode, newList);
                }
            }
            
            // 特殊處理 ChoiceNode 的選項連接
            if (newNode is ChoiceNode newChoiceNode)
            {
                foreach (var choice in newChoiceNode.choices)
                {
                    if (string.IsNullOrEmpty(choice.nextNodeId)) continue;

                    if (oldIdToNewId.TryGetValue(choice.nextNodeId, out string newTargetId))
                    {
                        choice.nextNodeId = newTargetId;
                    }
                    else
                    {
                        // 不在貼上選取中的 Id → 保留原值
                        // choice.nextNodeId = choice.nextNodeId;
                    }
                }
            }
        }

        /// <summary>
        /// 生成一個唯一的節點 ID。
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
        /// 檢查剪貼簿是否有內容。
        /// </summary>
        public bool HasClipboardContent() => !string.IsNullOrEmpty(_clipboard);
    }
}
#endif
