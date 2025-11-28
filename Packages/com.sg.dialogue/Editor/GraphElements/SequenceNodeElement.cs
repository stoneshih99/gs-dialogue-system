#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// SequenceNodeElement 是 SequenceNode 的視覺化表示，用於在 GraphView 中顯示和編輯序列節點。
    /// 它允許用戶編輯序列名稱，並提供一個輸出埠。
    /// </summary>
    public class SequenceNodeElement : DialogueNodeElement
    {
        /// <summary>
        /// 獲取序列節點的輸出埠。
        /// </summary>
        public Port OutputPort { get; private set; }
        /// <summary>
        /// 獲取序列節點的數據模型。
        /// </summary>
        public override DialogueNodeBase NodeData => _data; // 實現抽象屬性
        private readonly SequenceNode _data; // 序列節點的數據
        private readonly DialogueGraphView _graphView; // GraphView 的引用

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="data">序列節點的數據。</param>
        /// <param name="graphView">GraphView 的實例。</param>
        /// <param name="onChanged">當節點數據改變時觸發的回調。</param>
        public SequenceNodeElement(SequenceNode data, DialogueGraphView graphView, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            _graphView = graphView;
            title = "Sequence"; // 節點標題
            
            // 設置節點樣式，使其看起來像一個容器
            style.backgroundColor = new StyleColor(new UnityEngine.Color(0.3f, 0.4f, 0.3f));
            
            // 序列名稱輸入框
            var nameField = new TextField { value = _data.sequenceName };
            nameField.RegisterValueChangedCallback(evt =>
            {
                _data.sequenceName = evt.newValue;
                onChanged?.Invoke(); // 觸發數據改變回調
            });
            titleContainer.Add(nameField);

            // 創建輸出埠
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
            
            // 註冊滑鼠點擊事件，用於偵測雙擊
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        /// <summary>
        /// 處理滑鼠點擊事件，如果為雙擊，則觸發進入子圖的導航。
        /// </summary>
        private void OnMouseDown(MouseDownEvent evt)
        {
            // 檢查是否為滑鼠左鍵雙擊
            if (evt.button == 0 && evt.clickCount == 2)
            {
                _graphView.EnterContainerNode(_data);
                evt.StopPropagation();
            }
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
