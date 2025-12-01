#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class TextNodeHandler : INodeHandler
    {
        public string MenuName => "Content/Text";
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

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is TextNodeElement textElem)
            {
                if (portName == "On Interrupt") return textElem.InterruptPort;
                return textElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
