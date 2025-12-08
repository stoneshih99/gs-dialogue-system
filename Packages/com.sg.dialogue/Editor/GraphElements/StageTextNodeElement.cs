#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class StageTextNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly StageTextNode _data;

        public StageTextNodeElement(StageTextNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Stage Text";
            
            var messageField = new TextField("Message")
            {
                value = _data.message
            };
            messageField.RegisterValueChangedCallback(evt =>
            {
                _data.message = evt.newValue;
                onChanged?.Invoke();
            });
            mainContainer.Add(messageField);
            
            var speedField = new FloatField("Typing Speed")
            {
                value = _data.typingSpeed
            };
            speedField.RegisterValueChangedCallback(evt =>
            {
                _data.typingSpeed = evt.newValue;
                onChanged?.Invoke();
            });
            mainContainer.Add(speedField);

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
        }

        public override void OnOutputPortConnected(Port outputPort, string targetNodeId)
        {
            if (outputPort == OutputPort)
            {
                _data.nextNodeId = targetNodeId;
            }
        }

        public override void OnOutputPortDisconnected(Port outputPort)
        {
            if (outputPort == OutputPort)
            {
                _data.nextNodeId = null;
            }
        }
    }
}
#endif
