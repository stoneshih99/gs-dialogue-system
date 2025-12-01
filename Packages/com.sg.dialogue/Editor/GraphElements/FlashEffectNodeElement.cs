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
    /// FlashEffectNodeElement 是 FlashEffectNode 的視覺化表示。
    /// </summary>
    public class FlashEffectNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly FlashEffectNode _data;

        public FlashEffectNodeElement(FlashEffectNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Screen Flash";
            
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            var colorField = new ColorField("Flash Color") { value = _data.FlashColor };
            colorField.RegisterValueChangedCallback(evt => { _data.FlashColor = evt.newValue; onChanged?.Invoke(); });
            mainContainer.Add(colorField);

            var durationField = new FloatField("Duration (s)") { value = _data.Duration };
            durationField.RegisterValueChangedCallback(evt => { _data.Duration = Mathf.Max(0, evt.newValue); onChanged?.Invoke(); });
            mainContainer.Add(durationField);

            var intensityField = new Slider("Intensity", 0, 1) { value = _data.Intensity };
            intensityField.RegisterValueChangedCallback(evt => { _data.Intensity = evt.newValue; onChanged?.Invoke(); });
            mainContainer.Add(intensityField);
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
