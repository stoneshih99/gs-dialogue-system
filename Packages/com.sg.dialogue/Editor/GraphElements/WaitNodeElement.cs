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
    /// WaitNodeElement 是 WaitNode 的視覺化表示，用於在 GraphView 中顯示和編輯等待節點。
    /// </summary>
    public class WaitNodeElement : DialogueNodeElement
    {
        /// <summary>
        /// 獲取等待節點的輸出埠。
        /// </summary>
        public Port OutputPort { get; private set; }
        /// <summary>
        /// 獲取等待節點的數據模型。
        /// </summary>
        public override DialogueNodeBase NodeData => _data; // 實現抽象屬性
        private readonly WaitNode _data; // 等待節點的數據

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="data">等待節點的數據。</param>
        /// <param name="onChanged">當節點數據改變時觸發的回調。</param>
        public WaitNodeElement(WaitNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Wait"; // 節點標題
            
            // 創建輸出埠
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            // 等待時間輸入框
            var waitTimeField = new FloatField("Wait Time (s)") { value = _data.WaitTime };
            waitTimeField.RegisterValueChangedCallback(evt =>
            {
                _data.WaitTime = Mathf.Max(0, evt.newValue); // 確保等待時間不為負數
                onChanged?.Invoke();
            });
            mainContainer.Add(waitTimeField);
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
