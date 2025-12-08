#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class AnimationNodeHandler : INodeHandler
    {
        public string MenuName => "Action/Visual/Animation";
        public string GetPrefix() => "Anim";

        public DialogueNodeBase CreateNodeData() => new AnimationNode();

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onNodeChanged)
        {
            return new AnimationNodeElement(node as AnimationNode, onNodeChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> tryGetInputPort, Action<Port, Port> connectPorts)
        {
            var animNode = nodeData as AnimationNode;
            var animNodeView = sourceView as AnimationNodeElement;
            if (animNode == null || animNodeView == null) return;

            var targetPort = tryGetInputPort(animNode.nextNodeId);
            if (targetPort != null)
            {
                connectPorts(animNodeView.OutputPort, targetPort);
            }
        }
        
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is AnimationNodeElement animElement)
            {
                return animElement.OutputPort;
            }
            return null;
        }
    }
}
#endif
