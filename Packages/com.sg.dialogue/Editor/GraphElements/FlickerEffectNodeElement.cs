#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// FlickerEffectNodeElement 是 FlickerEffectNode 的視覺化表示。
    /// </summary>
    public class FlickerEffectNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly FlickerEffectNode _data;

        public FlickerEffectNodeElement(FlickerEffectNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Flicker Effect";
            
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            var targetField = new EnumField("Target", _data.Target);
            mainContainer.Add(targetField);

            var bgLayerField = new IntegerField("Background Layer Index") { value = _data.BackgroundLayerIndex };
            bgLayerField.RegisterValueChangedCallback(evt => { _data.BackgroundLayerIndex = Mathf.Max(0, evt.newValue); onChanged?.Invoke(); });
            mainContainer.Add(bgLayerField);

            var charPosField = new EnumField("Character Position", _data.CharacterPosition);
            charPosField.RegisterValueChangedCallback(evt => { _data.CharacterPosition = (Enums.CharacterPosition)evt.newValue; onChanged?.Invoke(); });
            mainContainer.Add(charPosField);

            var durationField = new FloatField("Duration (s)") { value = _data.Duration };
            durationField.RegisterValueChangedCallback(evt => { _data.Duration = Mathf.Max(0, evt.newValue); onChanged?.Invoke(); });
            mainContainer.Add(durationField);

            var frequencyField = new FloatField("Frequency (Hz)") { value = _data.Frequency };
            frequencyField.RegisterValueChangedCallback(evt => { _data.Frequency = Mathf.Max(0, evt.newValue); onChanged?.Invoke(); });
            mainContainer.Add(frequencyField);

            var minAlphaField = new Slider("Min Alpha", 0, 1) { value = _data.MinAlpha };
            minAlphaField.RegisterValueChangedCallback(evt => { _data.MinAlpha = evt.newValue; onChanged?.Invoke(); });
            mainContainer.Add(minAlphaField);

            void RefreshUI(FlickerEffectNode.TargetType targetType)
            {
                bgLayerField.style.display = targetType == FlickerEffectNode.TargetType.Background ? DisplayStyle.Flex : DisplayStyle.None;
                charPosField.style.display = targetType == FlickerEffectNode.TargetType.Character ? DisplayStyle.Flex : DisplayStyle.None;
            }

            targetField.RegisterValueChangedCallback(evt =>
            {
                var newTarget = (FlickerEffectNode.TargetType)evt.newValue;
                _data.Target = newTarget;
                RefreshUI(newTarget);
                onChanged?.Invoke();
            });
            
            RefreshUI(_data.Target);
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
