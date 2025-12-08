#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class FlashEffectNodeHandler : INodeHandler
    {
        public string MenuName => "Effect/Flash";
        public DialogueNodeBase CreateNodeData() => new FlashEffectNode();
        public string GetPrefix() => "FLASH";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new FlashEffectNodeElement(node as FlashEffectNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var flashNode = nodeData as FlashEffectNode;
            var flashElem = sourceView as FlashEffectNodeElement;
            connect(flashElem.OutputPort, getInputPort(flashNode.nextNodeId));
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is FlashEffectNodeElement flashElem)
            {
                return flashElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
