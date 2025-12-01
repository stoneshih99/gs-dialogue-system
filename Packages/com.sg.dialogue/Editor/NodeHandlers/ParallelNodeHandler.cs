#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class ParallelNodeHandler : INodeHandler
    {
        public string MenuName => "Flow Control/Parallel";
        public string GetPrefix() => "PARALLEL";

        public DialogueNodeBase CreateNodeData() => new ParallelNode();

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new ParallelNodeElement(node as ParallelNode, graphView, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var parallelNode = nodeData as ParallelNode;
            var parallelElem = sourceView as ParallelNodeElement;

            if (parallelNode == null || parallelElem == null) return;

            Port nextPort = getInputPort(parallelNode.nextNodeId);
            if (nextPort != null)
            {
                connect(parallelElem.NextPort, nextPort);
            }
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is ParallelNodeElement parallelElem)
            {
                return parallelElem.NextPort;
            }
            return null;
        }
    }
}
#endif
