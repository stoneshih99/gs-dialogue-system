#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class ScreenEffectNodeHandler : INodeHandler
    {
        public string MenuName => "Effect/Screen Effect";
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

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is ScreenEffectNodeElement seElem)
            {
                return seElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
