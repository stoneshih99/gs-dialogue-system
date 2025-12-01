#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// ScreenEffectNodeElement 是 ScreenEffectNode 的視覺化表示。
    /// </summary>
    public class ScreenEffectNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly ScreenEffectNode _data;

        public ScreenEffectNodeElement(ScreenEffectNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Screen Effect";
            
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            var actionField = new EnumField("Action", _data.Action);
            actionField.RegisterValueChangedCallback(evt => { _data.Action = (ScreenEffectNode.ActionType)evt.newValue; onChanged?.Invoke(); });
            mainContainer.Add(actionField);

            var durationField = new FloatField("Duration (s)") { value = _data.Duration };
            durationField.RegisterValueChangedCallback(evt => { _data.Duration = Mathf.Max(0, evt.newValue); onChanged?.Invoke(); });
            mainContainer.Add(durationField);
        }

        public override void OnOutputPortConnected(Port outputPort, string targetNodeId)
        {
            if (outputPort == OutputPort) _data.nextNodeId = targetNodeId;
        }

        public override void OnOutputPortDisconnected(Port outputPort)
        {
            if (outputPort == OutputPort) _data.nextNodeId = null;
        }
    }
}
#endif
