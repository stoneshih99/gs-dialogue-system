#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class ScreenEffectNodeHandler : INodeHandler
    {
        public string MenuName => "Effects/Add Screen Effect Node";
        public DialogueNodeBase CreateNodeData() => new ScreenEffectNode();
        public string GetPrefix() => "EFFECT";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new ScreenEffectNodeElement(node as ScreenEffectNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var seNode = nodeData as ScreenEffectNode;
            var seElem = sourceView as ScreenEffectNodeElement;
            connect(seElem.OutputPort, getInputPort(seNode.nextNodeId));
        }

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is ScreenEffectNodeElement seElem)
            {
                // ScreenEffectNodeElement 只有一個輸出埠，通常命名為 "Next"
                return seElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
