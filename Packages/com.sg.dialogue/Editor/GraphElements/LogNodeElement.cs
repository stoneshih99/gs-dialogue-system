#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// LogNodeElement 是 LogNode 的視覺化表示，用於在 GraphView 中顯示和編輯日誌節點。
    /// </summary>
    public class LogNodeElement : DialogueNodeElement
    {
        /// <summary>
        /// 獲取日誌節點的輸出埠。
        /// </summary>
        public Port OutputPort { get; private set; }
        /// <summary>
        /// 獲取日誌節點的數據模型。
        /// </summary>
        public override DialogueNodeBase NodeData => _data; // 實現抽象屬性
        private readonly LogNode _data; // 日誌節點的數據

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="data">日誌節點的數據。</param>
        /// <param name="onChanged">當節點數據改變時觸發的回調。</param>
        public LogNodeElement(LogNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Log Message"; // 節點標題
            
            // 創建輸出埠
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            // 訊息類型下拉選單
            var typeField = new EnumField("Type", _data.messageType);
            typeField.RegisterValueChangedCallback(evt =>
            {
                _data.messageType = (LogType)evt.newValue;
                onChanged?.Invoke(); // 觸發數據改變回調
            });
            mainContainer.Add(typeField);

            // 訊息文本輸入框
            var messageField = new TextField("Message")
            {
                value = _data.message,
                multiline = true
            };
            messageField.RegisterValueChangedCallback(evt =>
            {
                _data.message = evt.newValue;
                onChanged?.Invoke(); // 觸發數據改變回調
            });
            mainContainer.Add(messageField);
        }

        /// <summary>
        /// 覆寫連接邏輯：當輸出埠連接到另一個節點時，更新數據模型中的 nextNodeId。
        /// </summary>
        /// <param name="outputPort">連接的輸出埠。</param>
        /// <param name="targetNodeId">目標節點的 ID。</param>
        public override void OnOutputPortConnected(Port outputPort, string targetNodeId)
        {
            if (outputPort == OutputPort)
            {
                _data.nextNodeId = targetNodeId;
            }
        }

        /// <summary>
        /// 覆寫斷開連接邏輯：當輸出埠斷開連接時，將數據模型中的 nextNodeId 設為 null。
        /// </summary>
        /// <param name="outputPort">斷開連接的輸出埠。</param>
        public override void OnOutputPortDisconnected(Port outputPort)
        {
            if (outputPort == OutputPort)
            {
                _data.nextNodeId = null;
            }
        }
    }
}
#endif
