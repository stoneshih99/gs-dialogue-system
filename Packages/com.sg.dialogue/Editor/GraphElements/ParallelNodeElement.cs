#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// ParallelNodeElement 是 ParallelNode 在編輯器中的視覺化表示。
    /// 它支援雙擊進入其內部的子圖進行編輯。
    /// </summary>
    public class ParallelNodeElement : DialogueNodeElement
    {
        public Port NextPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly ParallelNode _data;
        private readonly Action _onChanged;
        private readonly DialogueGraphView _graphView;

        public ParallelNodeElement(ParallelNode data, DialogueGraphView graphView, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            _onChanged = onChanged;
            _graphView = graphView;

            title = "Parallel";
            
            // 設置節點樣式，使其看起來像一個容器
            style.backgroundColor = new StyleColor(new UnityEngine.Color(0.3f, 0.3f, 0.4f));

            // 輸出埠，用於連接並行節點完成後要執行的下一個節點
            NextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            NextPort.portName = "Next";
            outputContainer.Add(NextPort);

            // 註冊滑鼠點擊事件，用於偵測雙擊
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            // 刷新埠和擴展狀態
            RefreshExpandedState();
            RefreshPorts();
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
        /// 當此節點的輸出埠建立連接時調用。
        /// </summary>
        public override void OnOutputPortConnected(Port outputPort, string targetNodeId)
        {
            if (outputPort == NextPort)
            {
                _data.nextNodeId = targetNodeId;
            }
        }

        /// <summary>
        /// 當此節點的輸出埠斷開連接時調用。
        /// </summary>
        public override void OnOutputPortDisconnected(Port outputPort)
        {
            if (outputPort == NextPort)
            {
                _data.nextNodeId = null;
            }
        }
    }
}
#endif
