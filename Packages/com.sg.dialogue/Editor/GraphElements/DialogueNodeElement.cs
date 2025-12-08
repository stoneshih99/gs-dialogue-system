#if UNITY_EDITOR
using System;
using System.Linq;
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
        protected SerializedProperty NodeSerializedProperty { get; private set; }

        private readonly Color _defaultBackgroundColor;

        protected DialogueNodeElement(string nodeId)
        {
            NodeId = nodeId;
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            // 儲存節點的預設背景顏色，以便之後恢復
            _defaultBackgroundColor = style.backgroundColor.value;
        }

        public void Initialize(DialogueGraphView graphView, SerializedProperty nodeSerializedProperty)
        {
            GraphView = graphView;
            NodeSerializedProperty = nodeSerializedProperty;

            if (titleButtonContainer.Children().OfType<Toggle>().All(t => t.name != "IsEnabledToggle"))
            {
                var enabledToggle = new Toggle { value = NodeData.IsEnabled, name = "IsEnabledToggle" };
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

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
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

        /// <summary>
        /// 設定節點的執行狀態，並根據狀態改變其視覺外觀。
        /// </summary>
        /// <param name="isExecuting">節點是否正在執行。</param>
        public void SetExecutionState(bool isExecuting)
        {
            if (isExecuting)
            {
                style.backgroundColor = new StyleColor(new Color(0.9f, 0.9f, 0.6f)); // 設定為淡黃色
            }
            else
            {
                style.backgroundColor = _defaultBackgroundColor; // 恢復為預設顏色
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
