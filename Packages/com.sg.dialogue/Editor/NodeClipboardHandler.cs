#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 專門忽略 UnityEngine.Object 的 JsonConverter：
    /// 任何 Unity 物件在序列化時一律輸出為 null。
    /// 這樣 Json.NET 就不會去動 Sprite.bounds / AudioClip.length 等等。
    /// </summary>
    internal sealed class UnityObjectNullConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteNull();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            // 從剪貼簿讀回來時，一律用 null
            return null;
        }
    }

    public class NodeClipboardHandler
    {
        private static readonly JsonSerializerSettings ClipboardJsonSettings = CreateClipboardJsonSettings();

        private static JsonSerializerSettings CreateClipboardJsonSettings()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // 核心：忽略所有 UnityEngine.Object
            settings.Converters.Add(new UnityObjectNullConverter());

            return settings;
        }

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
            if (!evt.actionKey) return;
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

        public void CopySelectionToClipboard()
        {
            var selectedNodesData = _graphView.selection
                .OfType<DialogueNodeElement>()
                .Select(n => n.NodeData)
                .ToList();

            if (!selectedNodesData.Any()) return;

            try
            {
                // 使用共用設定：忽略 UnityEngine.Object
                string tempJson = JsonConvert.SerializeObject(selectedNodesData, ClipboardJsonSettings);
                var copiedNodes = JsonConvert.DeserializeObject<List<DialogueNodeBase>>(tempJson, ClipboardJsonSettings);

                if (copiedNodes == null || copiedNodes.Count == 0)
                    return;

                foreach (var node in copiedNodes)
                {
                    ClearConnectionIds(node);
                    ClearUnityObjectReferences(node); // 雖然 Json 已經忽略，這裡當作雙重保險
                }

                _clipboard = JsonConvert.SerializeObject(copiedNodes, Formatting.Indented, ClipboardJsonSettings);
                Debug.Log($"[NodeClipboardHandler] Copied {copiedNodes.Count} nodes to clipboard.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Copy Error] Failed to process nodes for clipboard. Error: {e.Message}\n{e.StackTrace}");
            }
        }

        private void ClearConnectionIds(DialogueNodeBase node)
        {
            if (node == null) return;

            var fields = node.GetType().GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string) &&
                    field.Name.EndsWith("Id", StringComparison.Ordinal) &&
                    !field.Name.Equals("nodeId", StringComparison.Ordinal))
                {
                    field.SetValue(node, null);
                }
                else if (field.FieldType == typeof(List<string>) &&
                         field.Name.EndsWith("Ids", StringComparison.Ordinal))
                {
                    field.SetValue(node, new List<string>());
                }
            }

            if (node is ChoiceNode choiceNode && choiceNode.choices != null)
            {
                foreach (var choice in choiceNode.choices)
                {
                    choice.nextNodeId = null;
                }
            }
        }

        /// <summary>
        /// 這裡是額外保險：如果你某些欄位是 UnityEngine.Object，但
        /// 剛好 Json 設定哪天被改掉，這裡還是會清一次。
        /// </summary>
        private void ClearUnityObjectReferences(DialogueNodeBase node)
        {
            if (node == null) return;

            var fields = node.GetType().GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy);

            foreach (var field in fields)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    field.SetValue(node, null);
                }
            }
        }

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
                pastedNodesData = JsonConvert.DeserializeObject<List<DialogueNodeBase>>(
                    _clipboard,
                    ClipboardJsonSettings);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Paste Error] Failed to deserialize nodes. Error: {e.Message}\n{e.StackTrace}");
                return;
            }

            if (pastedNodesData == null || !pastedNodesData.Any()) return;

            _graphView.RecordUndo("Paste Nodes");
            _graphView.Graph.BuildLookup();

            var usedIds = new HashSet<string>(_graphView.Graph.AllNodes.Select(n => n.nodeId));

            foreach (var nodeData in pastedNodesData)
            {
                if (nodeData == null) continue;

                string oldId = nodeData.nodeId;
                string prefix = oldId != null && oldId.Contains('_')
                    ? oldId.Split('_')[0]
                    : "NODE";

                nodeData.nodeId = GenerateUniqueNodeIdForPaste(prefix, usedIds);

                if (nodeData is TextNode textNode)
                {
                    textNode.textKey = null;
                }
            }

            var container = _graphView.NavigationStack.Peek();
            var targetList = _graphView.GetNodesFromContainer(container);
            if (targetList == null) return;

            int originalCount = targetList.Count;
            Vector2 currentPastePos = pastePosition;

            foreach (var newNodeData in pastedNodesData)
            {
                if (newNodeData == null) continue;

                _graphView.Graph.SetNodePosition(newNodeData.nodeId, currentPastePos);
                targetList.Add(newNodeData);
                currentPastePos += new Vector2(30f, 30f);
            }

            EditorUtility.SetDirty(_graphView.Graph);

            var serializedGraph = new SerializedObject(_graphView.Graph);
            serializedGraph.Update();

            var nodesProperty = FindNodesProperty(serializedGraph, container);

            if (nodesProperty != null)
            {
                // 先擴大 arraySize，避免 GetArrayElementAtIndex 越界
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
        }

        private SerializedProperty FindNodesProperty(SerializedObject serializedGraph, object container)
        {
            if (container is DialogueGraph)
                return serializedGraph.FindProperty("AllNodes");

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
                    return $"{currentPath}.Array.data[{i}]";

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

        public string GenerateUniqueNodeId(string prefix)
        {
            int i = 1;
            string id;
            do { id = $"{prefix}_{i++}"; }
            while (_graphView.Graph != null && _graphView.Graph.HasNode(id));
            return id;
        }

        private string GenerateUniqueNodeIdForPaste(string prefix, HashSet<string> usedIds)
        {
            int i = 1;
            string id;
            do { id = $"{prefix}_{i++}"; }
            while ((_graphView.Graph != null && _graphView.Graph.HasNode(id)) || usedIds.Contains(id));
            usedIds.Add(id);
            return id;
        }

        public bool HasClipboardContent() => !string.IsNullOrEmpty(_clipboard);
    }
}
#endif
