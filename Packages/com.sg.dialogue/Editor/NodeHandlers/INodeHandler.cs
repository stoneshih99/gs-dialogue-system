#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Dialogue.Editor;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using SG.Dialogue.Editor.Editor.GraphElements;

namespace SG.Dialogue.Editor.Editor.NodeHandlers
{
    public interface INodeHandler
    {
        string MenuName { get; }
        DialogueNodeBase CreateNodeData();
        DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged);
        string GetPrefix();
        void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect);
        Port GetOutputPort(DialogueNodeElement element, string portName);
    }
}
#endif
