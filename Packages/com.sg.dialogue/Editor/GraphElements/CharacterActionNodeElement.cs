#if UNITY_EDITOR
using System;
using SG.Dialogue.Animation;
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using SG.Dialogue.Presentation;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Editor.GraphElements
{
    public class CharacterActionNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly CharacterActionNode _data;

        public CharacterActionNodeElement(CharacterActionNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Character Action";

            var actionTypeField = new EnumField("Action", _data.ActionType);
            mainContainer.Add(actionTypeField);
            
            var positionField = new EnumField("Position", _data.TargetPosition);
            positionField.RegisterValueChangedCallback(e => { _data.TargetPosition = (CharacterPosition)e.newValue; onChanged?.Invoke(); });
            mainContainer.Add(positionField);

            var speakerNameField = new TextField("Speaker Name") { value = _data.speakerName };
            speakerNameField.RegisterValueChangedCallback(e => { _data.speakerName = e.newValue; onChanged?.Invoke(); });
            mainContainer.Add(speakerNameField);

            var renderModeField = new EnumField("Render Mode", _data.portraitRenderMode);
            mainContainer.Add(renderModeField);

            var spriteField = new ObjectField("Sprite") { objectType = typeof(Sprite), allowSceneObjects = false, value = _data.characterSprite };
            spriteField.RegisterValueChangedCallback(e => { _data.characterSprite = e.newValue as Sprite; onChanged?.Invoke(); });
            mainContainer.Add(spriteField);

            var spineConfigBox = new Foldout { text = "Spine Config", value = false };
            var spineModelField = new ObjectField("Model Prefab") { objectType = typeof(GameObject), allowSceneObjects = false, value = _data.spinePortraitConfig?.modelPrefab };
            spineModelField.RegisterValueChangedCallback(e => { if(_data.spinePortraitConfig == null) _data.spinePortraitConfig = new SpinePortraitConfig(); _data.spinePortraitConfig.modelPrefab = e.newValue as GameObject; onChanged?.Invoke(); });
            var spineAnimField = new TextField("Enter Animation") { value = _data.spinePortraitConfig?.enterAnimation };
            spineAnimField.RegisterValueChangedCallback(e => { if(_data.spinePortraitConfig == null) _data.spinePortraitConfig = new SpinePortraitConfig(); _data.spinePortraitConfig.enterAnimation = e.newValue; onChanged?.Invoke(); });
            spineConfigBox.Add(spineModelField);
            spineConfigBox.Add(spineAnimField);
            mainContainer.Add(spineConfigBox);

            var live2DConfigBox = new Foldout { text = "Live2D Config", value = false };
            var live2DModelField = new ObjectField("Model Prefab") { objectType = typeof(GameObject), allowSceneObjects = false, value = _data.live2DModelPrefab };
            live2DModelField.RegisterValueChangedCallback(e => { _data.live2DModelPrefab = e.newValue as GameObject; onChanged?.Invoke(); });
            var live2DExpressionField = new TextField("Expression") { value = _data.live2DPortraitConfig?.expression };
            live2DExpressionField.RegisterValueChangedCallback(e => { if(_data.live2DPortraitConfig == null) _data.live2DPortraitConfig = new Live2DPortraitConfig(); _data.live2DPortraitConfig.expression = e.newValue; onChanged?.Invoke(); });
            live2DConfigBox.Add(live2DModelField);
            live2DConfigBox.Add(live2DExpressionField);
            mainContainer.Add(live2DConfigBox);

            var clearAllField = new Toggle("Clear All On Exit") { value = _data.ClearAllOnExit };
            clearAllField.RegisterValueChangedCallback(e => { _data.ClearAllOnExit = e.newValue; onChanged?.Invoke(); });
            mainContainer.Add(clearAllField);
            
            // var overrideToggle = new Toggle("Override Duration") { value = _data.OverrideDuration };
            var durationField = new FloatField("Duration") { value = _data.Duration };
            // overrideToggle.RegisterValueChangedCallback(e => { _data.OverrideDuration = e.newValue; durationField.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None; onChanged?.Invoke(); });
            durationField.RegisterValueChangedCallback(e => { _data.Duration = Mathf.Max(0, e.newValue); onChanged?.Invoke(); });
            // mainContainer.Add(overrideToggle);
            mainContainer.Add(durationField);

            void UpdateVisibility()
            {
                var isEnter = _data.ActionType == CharacterActionType.Enter;
                renderModeField.style.display = isEnter ? DisplayStyle.Flex : DisplayStyle.None;
                speakerNameField.style.display = isEnter ? DisplayStyle.Flex : DisplayStyle.None;
                clearAllField.style.display = !isEnter ? DisplayStyle.Flex : DisplayStyle.None;

                spriteField.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.Sprite ? DisplayStyle.Flex : DisplayStyle.None;
                spineConfigBox.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.Spine ? DisplayStyle.Flex : DisplayStyle.None;
                live2DConfigBox.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.Live2D ? DisplayStyle.Flex : DisplayStyle.None;
                
                durationField.style.display = _data.Duration > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                // durationField.style.display = _data.OverrideDuration ? DisplayStyle.Flex : DisplayStyle.None;
            }

            actionTypeField.RegisterValueChangedCallback(e => { _data.ActionType = (CharacterActionType)e.newValue; UpdateVisibility(); onChanged?.Invoke(); });
            renderModeField.RegisterValueChangedCallback(e => { _data.portraitRenderMode = (PortraitRenderMode)e.newValue; UpdateVisibility(); onChanged?.Invoke(); });

            UpdateVisibility();

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
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
