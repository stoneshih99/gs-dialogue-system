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
    /// BlurEffectNodeElement 是 BlurEffectNode 的視覺化表示。
    /// </summary>
    public class BlurEffectNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly BlurEffectNode _data;

        public BlurEffectNodeElement(BlurEffectNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Background Blur";
            
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            var actionField = new EnumField("Action", _data.Action);
            mainContainer.Add(actionField);

            var durationField = new FloatField("Duration (s)") { value = _data.Duration };
            durationField.RegisterValueChangedCallback(evt => { _data.Duration = Mathf.Max(0, evt.newValue); onChanged?.Invoke(); });
            mainContainer.Add(durationField);

            var blurAmountField = new Slider("Blur Amount", 0, 0.01f) { value = _data.BlurAmount };
            blurAmountField.RegisterValueChangedCallback(evt => { _data.BlurAmount = evt.newValue; onChanged?.Invoke(); });
            mainContainer.Add(blurAmountField);

            void RefreshUI(BlurEffectNode.ActionType actionType)
            {
                blurAmountField.style.display = actionType == BlurEffectNode.ActionType.Enable ? DisplayStyle.Flex : DisplayStyle.None;
            }

            actionField.RegisterValueChangedCallback(evt =>
            {
                var newAction = (BlurEffectNode.ActionType)evt.newValue;
                _data.Action = newAction;
                RefreshUI(newAction);
                onChanged?.Invoke();
            });

            RefreshUI(_data.Action);
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
