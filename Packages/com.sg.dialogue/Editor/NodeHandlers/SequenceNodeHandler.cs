#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class SequenceNodeHandler : INodeHandler
    {
        public string MenuName => "Flow Control/Sequence";
        public string GetPrefix() => "SEQ";

        public DialogueNodeBase CreateNodeData() => new SequenceNode();

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new SequenceNodeElement(node as SequenceNode, graphView, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var seqNode = nodeData as SequenceNode;
            var seqElem = sourceView as SequenceNodeElement;

            if (seqNode == null || seqElem == null) return;

            Port nextPort = getInputPort(seqNode.nextNodeId);
            if (nextPort != null)
            {
                connect(seqElem.OutputPort, nextPort);
            }
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is SequenceNodeElement seqElem)
            {
                return seqElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
