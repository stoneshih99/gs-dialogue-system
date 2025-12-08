#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class PlayAudioNodeHandler : INodeHandler
    {
        public string MenuName => "Action/Audio/Play Audio";
        public DialogueNodeBase CreateNodeData() => new PlayAudioNode();
        public string GetPrefix() => "AUDIO";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new PlayAudioNodeElement(node as PlayAudioNode, nodeProperty, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var paNode = nodeData as PlayAudioNode;
            var paElem = sourceView as PlayAudioNodeElement;
            connect(paElem.OutputPort, getInputPort(paNode.nextNodeId));
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is PlayAudioNodeElement paElem)
            {
                return paElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
