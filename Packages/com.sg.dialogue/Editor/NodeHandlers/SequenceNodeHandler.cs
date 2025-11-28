#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// SequenceNodeHandler 負責處理 SequenceNode 在編輯器中的創建和連接邏輯。
    /// </summary>
    public class SequenceNodeHandler : INodeHandler
    {
        public string MenuName => "Flow/Sequence Node";
        public string GetPrefix() => "SEQ";

        /// <summary>
        /// 創建一個新的 SequenceNode 數據實例。
        /// </summary>
        public DialogueNodeBase CreateNodeData() => new SequenceNode();

        /// <summary>
        /// 創建一個 SequenceNode 的視覺元素。
        /// </summary>
        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            // 將 graphView 傳遞給元素，以便處理雙擊事件
            return new SequenceNodeElement(node as SequenceNode, graphView, onChanged);
        }

        /// <summary>
        /// 連接 SequenceNode 的輸出埠。
        /// </summary>
        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var seqNode = nodeData as SequenceNode;
            var seqElem = sourceView as SequenceNodeElement;

            if (seqNode == null || seqElem == null) return;

            // 連接 "Next" 輸出埠
            Port nextPort = getInputPort(seqNode.nextNodeId);
            if (nextPort != null)
            {
                connect(seqElem.OutputPort, nextPort);
            }
        }

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is SequenceNodeElement seqElem)
            {
                // SequenceNodeElement 只有一個輸出埠，通常命名為 "Next"
                return seqElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
