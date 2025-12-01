#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class BlurEffectNodeHandler : INodeHandler
    {
        public string MenuName => "Effect/Blur";
        public DialogueNodeBase CreateNodeData() => new BlurEffectNode();
        public string GetPrefix() => "BLUR";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new BlurEffectNodeElement(node as BlurEffectNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var blurNode = nodeData as BlurEffectNode;
            var blurElem = sourceView as BlurEffectNodeElement;
            connect(blurElem.OutputPort, getInputPort(blurNode.nextNodeId));
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is BlurEffectNodeElement blurElem)
            {
                return blurElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
