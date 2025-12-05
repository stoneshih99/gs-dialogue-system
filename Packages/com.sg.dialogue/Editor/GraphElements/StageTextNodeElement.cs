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
            
            var messageKeyField = new TextField("Message Key")
            {
                value = _data.messageKey,
                tooltip = "對話內容",
                style = { width = 200}
            };
            messageKeyField.RegisterValueChangedCallback(evt =>
            {
                _data.messageKey = evt.newValue;
                onChanged?.Invoke();
            });
            mainContainer.Add(messageKeyField);
            
            var messageSpeedField = new FloatField("Message Speed")
            {
                value = _data.typingSpeed,
                tooltip = "文字顯示速度（字元/秒）",
                style = { width = 200}
            };
            messageSpeedField.RegisterValueChangedCallback(evt =>
            {
                _data.typingSpeed = evt.newValue;
                onChanged?.Invoke();
            });
            mainContainer.Add(messageSpeedField);

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
