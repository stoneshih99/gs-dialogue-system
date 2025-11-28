#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class TextNodeHandler : INodeHandler
    {
        public string MenuName => "Content/Text Node";
        public DialogueNodeBase CreateNodeData() => new TextNode { speakerName = "NPC", text = "..." };
        public string GetPrefix() => "TEXT";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new TextNodeElement(node as TextNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var textNode = nodeData as TextNode;
            var textElem = sourceView as TextNodeElement;
            connect(textElem.OutputPort, getInputPort(textNode.nextNodeId));
            connect(textElem.InterruptPort, getInputPort(textNode.InterruptNextNodeId));
        }

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is TextNodeElement textElem)
            {
                if (portName == "On Interrupt") return textElem.InterruptPort;
                return textElem.OutputPort; // 預設返回 Next 埠
            }
            return null;
        }
    }
}
#endif
