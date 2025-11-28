#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// DialogueNodeElement 為所有對話節點視覺元件的基底類別。
    /// 它提供了所有節點共用的基本功能，例如輸入埠、節點 ID 管理和刪除按鈕。
    /// </summary>
    public abstract class DialogueNodeElement : Node
    {
        public string NodeId { get; private set; }
        public Port InputPort { get; protected set; }
        public Action OnDelete;
        protected DialogueGraphView GraphView { get; private set; } // 新增 GraphView 引用

        protected DialogueNodeElement(string nodeId)
        {
            NodeId = nodeId;
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);
        }

        /// <summary>
        /// 初始化節點上的 UI 元件，這個方法應該在子類別的建構函式完成後呼叫。
        /// </summary>
        public void Initialize(DialogueGraphView graphView) // 接收 GraphView 引用
        {
            GraphView = graphView; // 儲存引用

            // 在標題列創建一個啟用/停用的 Toggle 開關
            var enabledToggle = new Toggle
            {
                value = NodeData.IsEnabled
            };
            enabledToggle.RegisterValueChangedCallback(evt =>
            {
                NodeData.IsEnabled = evt.newValue;
                UpdateEnabledStatus();
                GraphView.RecordUndo("Toggle Node Enabled"); // 記錄 Undo
            });
            // 將 Toggle 開關加到標題按鈕容器的最前面
            titleButtonContainer.Insert(0, enabledToggle);
            
            // 初始更新一次狀態
            UpdateEnabledStatus();
            SetIsStartNode(GraphView.Graph.startNodeId == NodeId); // 初始設定起始節點視覺狀態
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("Delete", action => OnDelete?.Invoke());
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Set as Start Node", (action) =>
            {
                GraphView.SetStartNode(NodeId); // 呼叫 GraphView 的方法來設定起始節點
            }, DropdownMenuAction.Status.Normal); // 總是可選
        }

        /// <summary>
        /// 更新節點的啟用/停用視覺狀態。
        /// </summary>
        public void UpdateEnabledStatus()
        {
            style.opacity = NodeData.IsEnabled ? 1f : 0.5f;
        }

        /// <summary>
        /// 設定節點是否為起始節點的視覺狀態。
        /// </summary>
        /// <param name="isStart">是否為起始節點。</param>
        public void SetIsStartNode(bool isStart)
        {
            if (isStart)
            {
                // 設置為起始節點的樣式，例如綠色邊框
                style.borderBottomColor = style.borderLeftColor = style.borderRightColor = style.borderTopColor = Color.green;
                style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = style.borderTopWidth = 3f;
            }
            else
            {
                // 恢復預設邊框樣式
                style.borderBottomColor = style.borderLeftColor = style.borderRightColor = style.borderTopColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f)); // 預設顏色
                style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = style.borderTopWidth = 1f; // 預設寬度
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
