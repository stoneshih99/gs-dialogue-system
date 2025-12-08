#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
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

            var targetField = new EnumField("Target", _data.target);
            mainContainer.Add(targetField);

            var bgLayerField = new IntegerField("Background Layer Index") { value = _data.backgroundLayerIndex };
            bgLayerField.RegisterValueChangedCallback(evt => { _data.backgroundLayerIndex = Mathf.Max(0, evt.newValue); onChanged?.Invoke(); });
            mainContainer.Add(bgLayerField);

            var charPosField = new EnumField("Character Position", _data.characterPosition);
            charPosField.RegisterValueChangedCallback(evt => { _data.characterPosition = (Enums.CharacterPosition)evt.newValue; onChanged?.Invoke(); });
            mainContainer.Add(charPosField);

            var durationField = new FloatField("Duration (s)") { value = _data.duration };
            durationField.RegisterValueChangedCallback(evt => { _data.duration = Mathf.Max(0, evt.newValue); onChanged?.Invoke(); });
            mainContainer.Add(durationField);

            var frequencyField = new FloatField("Frequency (Hz)") { value = _data.frequency };
            frequencyField.RegisterValueChangedCallback(evt => { _data.frequency = Mathf.Max(0, evt.newValue); onChanged?.Invoke(); });
            mainContainer.Add(frequencyField);

            var minAlphaField = new Slider("Min Alpha", 0, 1) { value = _data.minAlpha };
            minAlphaField.RegisterValueChangedCallback(evt => { _data.minAlpha = evt.newValue; onChanged?.Invoke(); });
            mainContainer.Add(minAlphaField);

            void RefreshUI(FlickerEffectNode.TargetType targetType)
            {
                bgLayerField.style.display = targetType == FlickerEffectNode.TargetType.Background ? DisplayStyle.Flex : DisplayStyle.None;
                charPosField.style.display = targetType == FlickerEffectNode.TargetType.Character ? DisplayStyle.Flex : DisplayStyle.None;
            }

            targetField.RegisterValueChangedCallback(evt =>
            {
                var newTarget = (FlickerEffectNode.TargetType)evt.newValue;
                _data.target = newTarget;
                RefreshUI(newTarget);
                onChanged?.Invoke();
            });
            
            RefreshUI(_data.target);
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
