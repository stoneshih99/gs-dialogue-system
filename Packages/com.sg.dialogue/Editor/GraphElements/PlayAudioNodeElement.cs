#if UNITY_EDITOR
using System;
using SG.Dialogue.Enums;
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

        // 保存 UI 元素的引用以便動態控制
        private readonly PropertyField _loopField;
        private readonly PropertyField _fadeDurationField;

        public PlayAudioNodeElement(PlayAudioNode data, SerializedProperty nodeProperty, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Play Audio";

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            if (nodeProperty != null)
            {
                // 顯示 AudioEvent 通道
                var audioEventProperty = nodeProperty.FindPropertyRelative("AudioEvent");
                if (audioEventProperty != null)
                {
                    var propertyField = new PropertyField(audioEventProperty, "Audio Channel");
                    propertyField.Bind(nodeProperty.serializedObject);
                    mainContainer.Add(propertyField);
                }

                // 顯示音訊參數
                // ActionType 需要特別處理，因為要監聽它的變更
                var actionTypeProp = nodeProperty.FindPropertyRelative("ActionType");
                if (actionTypeProp != null)
                {
                    var actionTypeField = new PropertyField(actionTypeProp, "Action Type");
                    actionTypeField.Bind(nodeProperty.serializedObject);
                    // 註冊回調：當值改變時更新 UI
                    actionTypeField.RegisterValueChangeCallback(evt => UpdateVisibility((AudioActionType)evt.changedProperty.enumValueIndex));
                    mainContainer.Add(actionTypeField);
                }

                AddPropertyField(nodeProperty, "SoundName", "Sound Name");
                
                // 保存 Loop 和 FadeDuration 的引用
                _loopField = AddPropertyField(nodeProperty, "Loop", "Loop");
                _fadeDurationField = AddPropertyField(nodeProperty, "FadeDuration", "Fade Duration");

                // 初始化可見性
                if (actionTypeProp != null)
                {
                    UpdateVisibility((AudioActionType)actionTypeProp.enumValueIndex);
                }
            }
            
            // 確保 Undo/Redo 和變更保存能夠正常運作
            this.RegisterCallback<ChangeEvent<string>>(evt => onChanged?.Invoke());
            this.RegisterCallback<ChangeEvent<float>>(evt => onChanged?.Invoke());
            this.RegisterCallback<ChangeEvent<bool>>(evt => onChanged?.Invoke());
            this.RegisterCallback<ChangeEvent<Enum>>(evt => onChanged?.Invoke());
            this.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt => onChanged?.Invoke());
        }

        private void UpdateVisibility(AudioActionType actionType)
        {
            bool isBGM = actionType == AudioActionType.PlayBGM;
            
            if (_loopField != null)
            {
                _loopField.style.display = isBGM ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_fadeDurationField != null)
            {
                _fadeDurationField.style.display = isBGM ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private PropertyField AddPropertyField(SerializedProperty root, string relativePath, string label)
        {
            var prop = root.FindPropertyRelative(relativePath);
            if (prop != null)
            {
                var field = new PropertyField(prop, label);
                field.Bind(root.serializedObject);
                mainContainer.Add(field);
                return field;
            }
            return null;
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
