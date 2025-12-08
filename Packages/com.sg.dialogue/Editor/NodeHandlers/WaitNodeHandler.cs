#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class WaitNodeHandler : INodeHandler
    {
        public string MenuName => "Action/Logic/Wait";
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

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is WaitNodeElement wElem)
            {
                return wElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
