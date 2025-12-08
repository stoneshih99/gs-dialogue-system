#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Dialogue.Editor;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor;

namespace SG.Dialogue.Editor.NodeHandlers
{
    public class StageTextNodeHandler : INodeHandler
    {
        public string MenuName => "Action/Stage Text";
        public string GetPrefix() => "StageText";

        public DialogueNodeBase CreateNodeData() => new StageTextNode();

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new StageTextNodeElement(node as StageTextNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            if (sourceView is StageTextNodeElement element && nodeData is StageTextNode data)
            {
                var nextNodeInputPort = getInputPort(data.nextNodeId);
                connect(element.OutputPort, nextNodeInputPort);
            }
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is StageTextNodeElement el)
            {
                return el.OutputPort;
            }
            return null;
        }
    }
}
#endif
