#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class WaitNodeHandler : INodeHandler
    {
        public string MenuName => "Flow/Wait Node";
        public DialogueNodeBase CreateNodeData() => new WaitNode();
        public string GetPrefix() => "WAIT";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new WaitNodeElement(node as WaitNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var wNode = nodeData as WaitNode;
            var wElem = sourceView as WaitNodeElement;
            connect(wElem.OutputPort, getInputPort(wNode.nextNodeId));
        }

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is WaitNodeElement wElem)
            {
                // WaitNodeElement 只有一個輸出埠，通常命名為 "Next"
                return wElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
