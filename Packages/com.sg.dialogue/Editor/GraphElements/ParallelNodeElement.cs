#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

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

        // 修改建構子以接收 SerializedProperty
        public ParallelNodeElement(ParallelNode data, DialogueGraphView graphView, SerializedProperty nodeSerializedProperty, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            _onChanged = onChanged;
            _graphView = graphView;

            // 呼叫基底類別的 Initialize 方法，傳遞 SerializedProperty
            Initialize(graphView, nodeSerializedProperty);

            title = _data.parallelName; // 重新設定 title 為 parallelName
            
            // 設置節點樣式，使其看起來像一個容器
            style.backgroundColor = new StyleColor(new UnityEngine.Color(0.3f, 0.3f, 0.4f));

            // 添加描述欄位
            // 現在使用基底類別儲存的 NodeSerializedProperty 來尋找 "description"
            var descriptionProperty = NodeSerializedProperty.FindPropertyRelative("description");
            if (descriptionProperty != null)
            {
                var descriptionField = new PropertyField(descriptionProperty);
                // 綁定到 NodeSerializedProperty 的 SerializedObject
                descriptionField.Bind(NodeSerializedProperty.serializedObject);
                extensionContainer.Add(descriptionField);
            }
            else
            {
                Debug.LogWarning($"[Dialogue Editor] 無法在 ParallelNode 中找到 'description' 屬性。請確保它是 [SerializeField] private string description;");
            }
            
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
