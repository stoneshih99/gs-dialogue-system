#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class CharacterActionNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly CharacterActionNode _data;

        public CharacterActionNodeElement(CharacterActionNode data, SerializedProperty nodeProperty, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Character Action";

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            if (nodeProperty != null)
            {
                var propertyField = new PropertyField(nodeProperty);
                propertyField.Bind(nodeProperty.serializedObject);
                mainContainer.Add(propertyField);
                
                // 註冊一個通用的回調，以確保任何在 PropertyDrawer 中發生的變更都能被保存
                propertyField.RegisterValueChangeCallback(evt => 
                {
                    onChanged?.Invoke();
                });
            }
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