#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class ConditionNodeHandler : INodeHandler
    {
        public string MenuName => "Flow Control/Condition";
        public DialogueNodeBase CreateNodeData() => new ConditionNode();
        public string GetPrefix() => "IF";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new ConditionNodeElement(node as ConditionNode, onChanged, graphView.GlobalState);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var condNode = nodeData as ConditionNode;
            var condElem = sourceView as ConditionNodeElement;
            connect(condElem.TrueOutputPort, getInputPort(condNode.TrueNextNodeId));
            connect(condElem.FalseOutputPort, getInputPort(condNode.FalseNextNodeId));
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is ConditionNodeElement condElem)
            {
                if (portName == "True") return condElem.TrueOutputPort;
                if (portName == "False") return condElem.FalseOutputPort;
            }
            return null;
        }
    }
}
#endif
