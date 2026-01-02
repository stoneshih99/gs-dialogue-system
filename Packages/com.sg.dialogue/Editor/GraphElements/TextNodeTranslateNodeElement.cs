#if UNITY_EDITOR
using System;
using LitMotion;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class TextNodeTranslateNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly TextNodeTranslateNode _data;

        public TextNodeTranslateNodeElement(TextNodeTranslateNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Text Node Translate";
            
            // Mode
            var modeField = new EnumField("Mode", _data.mode);
            modeField.RegisterValueChangedCallback(evt =>
            {
                _data.mode = (TextNodeTranslateNode.TranslateMode)evt.newValue;
                onChanged?.Invoke();
            });
            mainContainer.Add(modeField);
            
            // Duration
            var durationField = new FloatField("Duration")
            {
                value = _data.duration
            };
            durationField.RegisterValueChangedCallback(evt =>
            {
                _data.duration = evt.newValue;
                onChanged?.Invoke();
            });
            mainContainer.Add(durationField);
            
            // Slide Offset
            var offsetField = new Vector2Field("Slide Offset")
            {
                value = _data.slideOffset
            };
            offsetField.RegisterValueChangedCallback(evt =>
            {
                _data.slideOffset = evt.newValue;
                onChanged?.Invoke();
            });
            mainContainer.Add(offsetField);
            
            // Ease
            var easeField = new EnumField("Ease", _data.ease);
            easeField.RegisterValueChangedCallback(evt =>
            {
                _data.ease = (Ease)evt.newValue;
                onChanged?.Invoke();
            });
            mainContainer.Add(easeField);

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
