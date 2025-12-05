#if UNITY_EDITOR
using System;
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using SG.Dialogue.Presentation;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Editor.GraphElements
{
    /// <summary>
    /// CharacterActionNodeElement 是 CharacterActionNode 的視覺化表示，用於在 GraphView 中顯示和編輯角色動作節點。
    /// </summary>
    public sealed class CharacterActionNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly CharacterActionNode _data;

        // Cache for visibility updates
        private EnumField _renderModeField;
        private TextField _speakerNameField;
        private Toggle _clearAllField;
        private ObjectField _spriteField;
        private Foldout _spineConfigBox;
        private Foldout _live2DConfigBox;
        private Foldout _spriteSheetConfigBox;
        private FloatField _durationField;

        public CharacterActionNodeElement(CharacterActionNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Character Action";

            var actionTypeField = CreateEnumField("Action", _data.ActionType, value =>
            {
                _data.ActionType = value;
                UpdateVisibility();
                onChanged?.Invoke();
            });
            mainContainer.Add(actionTypeField);

            var positionField = CreateEnumField("Position", _data.TargetPosition, value =>
            {
                _data.TargetPosition = value;
                onChanged?.Invoke();
            });
            mainContainer.Add(positionField);

            _speakerNameField = CreateTextField("Speaker Name", _data.speakerName, value =>
            {
                _data.speakerName = value;
                onChanged?.Invoke();
            });
            mainContainer.Add(_speakerNameField);

            _renderModeField = CreateEnumField("Render Mode", _data.portraitRenderMode, value =>
            {
                _data.portraitRenderMode = value;
                UpdateVisibility();
                onChanged?.Invoke();
            });
            mainContainer.Add(_renderModeField);

            _spriteField = CreateObjectField<Sprite>("Sprite", _data.characterSprite, obj =>
            {
                _data.characterSprite = obj;
                onChanged?.Invoke();
            });
            mainContainer.Add(_spriteField);

            BuildSpineConfig(onChanged);
            BuildLive2DConfig(onChanged);
            BuildSpriteSheetConfig(onChanged);

            _clearAllField = new Toggle("Clear All On Exit")
            {
                value = _data.ClearAllOnExit
            };
            _clearAllField.RegisterValueChangedCallback(e =>
            {
                _data.ClearAllOnExit = e.newValue;
                onChanged?.Invoke();
            });
            mainContainer.Add(_clearAllField);

            _durationField = new FloatField("Duration")
            {
                value = _data.Duration
            };
            _durationField.RegisterValueChangedCallback(e =>
            {
                _data.Duration = Mathf.Max(0, e.newValue);
                UpdateVisibility();
                onChanged?.Invoke();
            });
            mainContainer.Add(_durationField);

            UpdateVisibility();

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
        }

        private EnumField CreateEnumField<TEnum>(string label, TEnum initialValue, Action<TEnum> onChanged)
            where TEnum : Enum
        {
            var field = new EnumField(label, initialValue);
            field.RegisterValueChangedCallback(e => { onChanged?.Invoke((TEnum)e.newValue); });
            return field;
        }

        private TextField CreateTextField(string label, string initialValue, Action<string> onChanged)
        {
            var field = new TextField(label)
            {
                value = initialValue
            };
            field.RegisterValueChangedCallback(e => { onChanged?.Invoke(e.newValue); });
            return field;
        }

        private ObjectField CreateObjectField<TObject>(string label, TObject initialValue, Action<TObject> onChanged)
            where TObject : UnityEngine.Object
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(TObject),
                allowSceneObjects = false,
                value = initialValue
            };
            field.RegisterValueChangedCallback(e => { onChanged?.Invoke(e.newValue as TObject); });
            return field;
        }

        private void BuildSpineConfig(Action onChanged)
        {
            _spineConfigBox = new Foldout
            {
                text = "Spine Config",
                value = false
            };

            var spineModelField = CreateObjectField<GameObject>(
                "Model Prefab",
                _data.spinePortraitConfig?.modelPrefab,
                obj =>
                {
                    if (_data.spinePortraitConfig == null)
                    {
                        _data.spinePortraitConfig = new SpinePortraitConfig();
                    }

                    _data.spinePortraitConfig.modelPrefab = obj;
                    onChanged?.Invoke();
                });

            var spineAnimField = CreateTextField(
                "Enter Animation",
                _data.spinePortraitConfig?.enterAnimation,
                value =>
                {
                    if (_data.spinePortraitConfig == null)
                    {
                        _data.spinePortraitConfig = new SpinePortraitConfig();
                    }

                    _data.spinePortraitConfig.enterAnimation = value;
                    onChanged?.Invoke();
                });

            _spineConfigBox.Add(spineModelField);
            _spineConfigBox.Add(spineAnimField);
            mainContainer.Add(_spineConfigBox);
        }

        private void BuildLive2DConfig(Action onChanged)
        {
            _live2DConfigBox = new Foldout
            {
                text = "Live2D Config",
                value = false
            };

            var live2DModelField = CreateObjectField<GameObject>(
                "Model Prefab",
                _data.live2DModelPrefab,
                obj =>
                {
                    _data.live2DModelPrefab = obj;
                    onChanged?.Invoke();
                });

            var live2DExpressionField = CreateTextField(
                "Expression",
                _data.live2DPortraitConfig?.expression,
                value =>
                {
                    if (_data.live2DPortraitConfig == null)
                    {
                        _data.live2DPortraitConfig = new Live2DPortraitConfig();
                    }

                    _data.live2DPortraitConfig.expression = value;
                    onChanged?.Invoke();
                });

            _live2DConfigBox.Add(live2DModelField);
            _live2DConfigBox.Add(live2DExpressionField);
            mainContainer.Add(_live2DConfigBox);
        }

        private void BuildSpriteSheetConfig(Action onChanged)
        {
            _spriteSheetConfigBox = new Foldout
            {
                text = "SpriteSheet Config",
                value = false
            };

            var spriteSheetConfigField = CreateObjectField<GameObject>(
                "Object",
                _data.spriteSheetPresenter,
                obj =>
                {
                    _data.spriteSheetPresenter = obj;
                    onChanged?.Invoke();
                });

            var spriteSheetAnimationField = CreateTextField(
                "Animation Name",
                _data.spriteSheetAnimationName,
                value =>
                {
                    _data.spriteSheetAnimationName = value;
                    onChanged?.Invoke();
                });

            var presenter = _data.spriteSheetPresenter != null
                ? _data.spriteSheetPresenter.GetComponent<SpriteSheetDialoguePortraitPresenter>()
                : null;

            var fpsField = new IntegerField("FPS")
            {
                value = presenter != null ? presenter.fps : 60
            };
            fpsField.RegisterValueChangedCallback(e =>
            {
                if (_data.spriteSheetPresenter == null)
                {
                    return;
                }

                var targetPresenter = _data.spriteSheetPresenter.GetComponent<SpriteSheetDialoguePortraitPresenter>();
                if (targetPresenter == null)
                {
                    return;
                }

                targetPresenter.fps = e.newValue;
                onChanged?.Invoke();
            });

            var loopField = new Toggle("Loop")
            {
                value = presenter != null ? presenter.loop : true
            };
            loopField.RegisterValueChangedCallback(e =>
            {
                if (_data.spriteSheetPresenter == null)
                {
                    return;
                }

                var targetPresenter = _data.spriteSheetPresenter.GetComponent<SpriteSheetDialoguePortraitPresenter>();
                if (targetPresenter == null)
                {
                    return;
                }

                targetPresenter.loop = e.newValue;
                onChanged?.Invoke();
            });

            _spriteSheetConfigBox.Add(spriteSheetConfigField);
            _spriteSheetConfigBox.Add(spriteSheetAnimationField);
            _spriteSheetConfigBox.Add(fpsField);
            _spriteSheetConfigBox.Add(loopField);
            mainContainer.Add(_spriteSheetConfigBox);
        }

        private void UpdateVisibility()
        {
            var isEnter = _data.ActionType == CharacterActionType.Enter;

            if (_renderModeField != null)
            {
                _renderModeField.style.display = isEnter ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_speakerNameField != null)
            {
                _speakerNameField.style.display = isEnter ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_clearAllField != null)
            {
                _clearAllField.style.display = !isEnter ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_spriteField != null)
            {
                _spriteField.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.Sprite
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_spineConfigBox != null)
            {
                _spineConfigBox.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.Spine
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_live2DConfigBox != null)
            {
                _live2DConfigBox.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.Live2D
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_spriteSheetConfigBox != null)
            {
                _spriteSheetConfigBox.style.display =
                    isEnter && _data.portraitRenderMode == PortraitRenderMode.SpriteSheet
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }

            if (_durationField != null)
            {
                _durationField.style.display = _data.Duration > 0 ? DisplayStyle.Flex : DisplayStyle.None;
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
