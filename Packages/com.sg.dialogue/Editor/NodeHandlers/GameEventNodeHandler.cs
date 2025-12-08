#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class GameEventNodeHandler : INodeHandler
    {
        public string MenuName => "Action/Logic/Game Event";
        public DialogueNodeBase CreateNodeData() => new GameEventNode();
        public string GetPrefix() => "EVENT";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new GameEventNodeElement(node as GameEventNode, nodeProperty, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var geNode = nodeData as GameEventNode;
            var geElem = sourceView as GameEventNodeElement;
            connect(geElem.OutputPort, getInputPort(geNode.nextNodeId));
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is GameEventNodeElement geElem)
            {
                return geElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
