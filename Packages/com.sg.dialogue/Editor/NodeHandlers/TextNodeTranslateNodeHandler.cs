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
    public class TextNodeTranslateNodeHandler : INodeHandler
    {
        public string MenuName => "UI/Text Node Translate";
        public string GetPrefix() => "TextTranslate";

        public DialogueNodeBase CreateNodeData() => new TextNodeTranslateNode();

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new TextNodeTranslateNodeElement(node as TextNodeTranslateNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            if (sourceView is TextNodeTranslateNodeElement element && nodeData is TextNodeTranslateNode data)
            {
                var nextNodeInputPort = getInputPort(data.nextNodeId);
                connect(element.OutputPort, nextNodeInputPort);
            }
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is TextNodeTranslateNodeElement el)
            {
                return el.OutputPort;
            }
            return null;
        }
    }
}
#endif
