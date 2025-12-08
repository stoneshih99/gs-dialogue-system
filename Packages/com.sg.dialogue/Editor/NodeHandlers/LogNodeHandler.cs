#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class LogNodeHandler : INodeHandler
    {
        public string MenuName => "Action/Logic/Log";
        public DialogueNodeBase CreateNodeData() => new LogNode();
        public string GetPrefix() => "LOG";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new LogNodeElement(node as LogNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var lNode = nodeData as LogNode;
            var lElem = sourceView as LogNodeElement;
            connect(lElem.OutputPort, getInputPort(lNode.nextNodeId));
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is LogNodeElement lElem)
            {
                return lElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
