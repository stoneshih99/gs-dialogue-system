#if UNITY_EDITOR
using System;
using System.Linq; // 引入 System.Linq 以使用 Any()
using SG.Dialogue.Editor.Dialogue.Editor;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Editor.GraphElements
{
    public abstract class DialogueNodeElement : Node
    {
        protected const int MaxWidth = 250;
        public string NodeId { get; private set; }
        public Port InputPort { get; protected set; }
        public Action OnDelete;
        protected DialogueGraphView GraphView { get; private set; }
        protected SerializedProperty NodeSerializedProperty { get; private set; } // 新增：儲存節點的 SerializedProperty

        protected DialogueNodeElement(string nodeId)
        {
            NodeId = nodeId;
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);
        }

        // 修改 Initialize 方法，使其接收 SerializedProperty
        public void Initialize(DialogueGraphView graphView, SerializedProperty nodeSerializedProperty)
        {
            GraphView = graphView;
            NodeSerializedProperty = nodeSerializedProperty; // 儲存傳入的 SerializedProperty

            // 檢查是否已經添加過 IsEnabled 的 Toggle，避免重複添加
            if (titleButtonContainer.Children().OfType<Toggle>().All(t => t.name != "IsEnabledToggle"))
            {
                var enabledToggle = new Toggle { value = NodeData.IsEnabled, name = "IsEnabledToggle" }; // 給 Toggle 一個名稱以便識別
                enabledToggle.RegisterValueChangedCallback(evt =>
                {
                    NodeData.IsEnabled = evt.newValue;
                    UpdateEnabledStatus();
                    GraphView.RecordUndo("Toggle Node Enabled");
                });
                titleButtonContainer.Insert(0, enabledToggle);
            }
            
            UpdateEnabledStatus();
            SetIsStartNode(GraphView.Graph.startNodeId == NodeId);
        }

        /// <summary>
        /// 建立節點的上下文菜單。 
        /// </summary>
        /// <param name="evt"></param>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            // 修正：不再直接呼叫 OnDelete，而是讓 GraphView 處理刪除操作
            evt.menu.AppendAction("Delete", action => GraphView.DeleteSelection());
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Set as Start Node", (action) => { GraphView.SetStartNode(NodeId); }, DropdownMenuAction.Status.Normal);
        }

        public void UpdateEnabledStatus()
        {
            style.opacity = NodeData.IsEnabled ? 1f : 0.5f;
        }

        public void SetIsStartNode(bool isStart)
        {
            if (isStart)
            {
                style.borderBottomColor = style.borderLeftColor = style.borderRightColor = style.borderTopColor = Color.green;
                style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = style.borderTopWidth = 3f;
            }
            else
            {
                style.borderBottomColor = style.borderLeftColor = style.borderRightColor = style.borderTopColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));
                style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = style.borderTopWidth = 1f;
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
            Selection.activeObject = null;
        }

        public virtual void OnOutputPortConnected(Port outputPort, string targetNodeId)
        {
            var nextNodeIdField = NodeData.GetType().GetField("nextNodeId");
            if (nextNodeIdField != null)
            {
                nextNodeIdField.SetValue(NodeData, targetNodeId);
            }
        }

        public virtual void OnOutputPortDisconnected(Port outputPort)
        {
            var nextNodeIdField = NodeData.GetType().GetField("nextNodeId");
            if (nextNodeIdField != null)
            {
                nextNodeIdField.SetValue(NodeData, null);
            }
        }
        
        public abstract DialogueNodeBase NodeData { get; }
    }
}
#endif
