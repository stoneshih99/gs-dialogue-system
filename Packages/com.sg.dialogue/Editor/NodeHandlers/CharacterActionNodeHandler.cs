#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;


namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class CharacterActionNodeHandler : INodeHandler
    {
        public string MenuName => "Action/Visual/Character Action";
        public string GetPrefix() => "CharAct";

        public DialogueNodeBase CreateNodeData() => new CharacterActionNode();

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onNodeChanged)
        {
            return new CharacterActionNodeElement(node as CharacterActionNode, onNodeChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> tryGetInputPort, Action<Port, Port> connectPorts)
        {
            var charNode = nodeData as CharacterActionNode;
            var charNodeView = sourceView as CharacterActionNodeElement;
            if (charNode == null || charNodeView == null) return;

            var targetPort = tryGetInputPort(charNode.nextNodeId);
            if (targetPort != null)
            {
                connectPorts(charNodeView.OutputPort, targetPort);
            }
        }
        
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            return (element as CharacterActionNodeElement)?.OutputPort;
        }
    }
}
#endif
