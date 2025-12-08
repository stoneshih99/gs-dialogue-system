#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class FlickerEffectNodeHandler : INodeHandler
    {
        public string MenuName => "Effect/Flicker";
        public DialogueNodeBase CreateNodeData() => new FlickerEffectNode();
        public string GetPrefix() => "FLICKER";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new FlickerEffectNodeElement(node as FlickerEffectNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var feNode = nodeData as FlickerEffectNode;
            var feElem = sourceView as FlickerEffectNodeElement;
            connect(feElem.OutputPort, getInputPort(feNode.nextNodeId));
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is FlickerEffectNodeElement feElem)
            {
                return feElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
