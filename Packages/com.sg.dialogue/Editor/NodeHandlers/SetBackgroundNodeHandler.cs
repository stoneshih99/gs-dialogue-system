#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class SetBackgroundNodeHandler : INodeHandler
    {
        public string MenuName => "Action/Visual/Set Background";
        public string GetPrefix() => "SetBG";

        public DialogueNodeBase CreateNodeData() => new SetBackgroundNode();

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onNodeChanged)
        {
            return new SetBackgroundNodeElement(node as SetBackgroundNode, onNodeChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> tryGetInputPort, Action<Port, Port> connectPorts)
        {
            var bgNode = nodeData as SetBackgroundNode;
            var bgNodeView = sourceView as SetBackgroundNodeElement;
            if (bgNode == null || bgNodeView == null) return;

            var targetPort = tryGetInputPort(bgNode.nextNodeId);
            if (targetPort != null)
            {
                connectPorts(bgNodeView.OutputPort, targetPort);
            }
        }
        
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            return (element as SetBackgroundNodeElement)?.OutputPort;
        }
    }
}
#endif
