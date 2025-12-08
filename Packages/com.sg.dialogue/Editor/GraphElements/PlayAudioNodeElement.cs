#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class PlayAudioNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly PlayAudioNode _data;

        public PlayAudioNodeElement(PlayAudioNode data, SerializedProperty nodeProperty, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Play Audio";

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            if (nodeProperty != null)
            {
                var audioEventProperty = nodeProperty.FindPropertyRelative("AudioEvent");
                var requestProperty = nodeProperty.FindPropertyRelative("request");

                if (audioEventProperty != null)
                {
                    var propertyField = new PropertyField(audioEventProperty);
                    propertyField.Bind(nodeProperty.serializedObject);
                    mainContainer.Add(propertyField);
                }

                if (requestProperty != null)
                {
                    var propertyField = new PropertyField(requestProperty);
                    propertyField.Bind(nodeProperty.serializedObject);
                    mainContainer.Add(propertyField);
                }
            }
            
            // 確保 Undo/Redo 和變更保存能夠正常運作
            this.RegisterCallback<ChangeEvent<string>>(evt => onChanged?.Invoke());
            this.RegisterCallback<ChangeEvent<float>>(evt => onChanged?.Invoke());
            this.RegisterCallback<ChangeEvent<bool>>(evt => onChanged?.Invoke());
            this.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt => onChanged?.Invoke());
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
