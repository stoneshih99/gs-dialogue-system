#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class CameraControlNodeHandler : INodeHandler
    {
        public string MenuName => "Action/Visual/Camera Control";
        public DialogueNodeBase CreateNodeData() => new CameraControlNode();
        public string GetPrefix() => "CAM";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new CameraControlNodeElement(node as CameraControlNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var ccNode = nodeData as CameraControlNode;
            var ccElem = sourceView as CameraControlNodeElement;
            connect(ccElem.OutputPort, getInputPort(ccNode.nextNodeId));
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is CameraControlNodeElement ccElem)
            {
                return ccElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
