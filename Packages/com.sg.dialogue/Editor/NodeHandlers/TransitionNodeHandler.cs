#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class TransitionNodeHandler : INodeHandler
    {
        public string MenuName => "Effects/Transition Node";
        public DialogueNodeBase CreateNodeData() => new TransitionNode();
        public string GetPrefix() => "TRANS";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new TransitionNodeElement(node as TransitionNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var trNode = nodeData as TransitionNode;
            var trElem = sourceView as TransitionNodeElement;
            connect(trElem.OutputPort, getInputPort(trNode.nextNodeId));
        }

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is TransitionNodeElement trElem)
            {
                // TransitionNodeElement 只有一個輸出埠，通常命名為 "Next"
                return trElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
