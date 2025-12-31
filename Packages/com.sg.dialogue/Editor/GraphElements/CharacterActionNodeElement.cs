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
        private Image _spritePreview; // Sprite 預覽圖
        private Foldout _spineConfigBox;
        private Image _spinePreview; // Spine 預覽圖
#if LIVE2D_KIT_AVAILABLE
        private Foldout _live2DConfigBox;
#endif
        private Foldout _spriteSheetConfigBox;
        private Image _spriteSheetPreview; // SpriteSheet 預覽圖
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

            // Sprite 預覽容器
            _spritePreview = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = 100,
                    height = 100,
                    alignSelf = Align.Center,
                    display = DisplayStyle.None,
                    marginBottom = 5
                }
            };
            mainContainer.Add(_spritePreview);

            _spriteField = CreateObjectField<Sprite>("Sprite", _data.characterSprite, obj =>
            {
                _data.characterSprite = obj;
                UpdateSpritePreview(); // 更新預覽
                onChanged?.Invoke();
            });
            mainContainer.Add(_spriteField);

            BuildSpineConfig(onChanged);
#if LIVE2D_KIT_AVAILABLE
            BuildLive2DConfig(onChanged);
#endif
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
            UpdateSpritePreview(); // 初始化時更新預覽
            UpdateSpinePreview(); // 初始化時更新預覽
            UpdateSpriteSheetPreview(); // 初始化時更新預覽

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
        }

        private void UpdateSpritePreview()
        {
            if (_data.characterSprite != null)
            {
                // 使用 UI Toolkit 的 sprite 屬性，它能正確處理 Atlas 和 Slicing
                _spritePreview.sprite = _data.characterSprite;
                _spritePreview.image = null; // 清除 texture 以防萬一
                _spritePreview.style.display = DisplayStyle.Flex;
            }
            else
            {
                _spritePreview.sprite = null;
                _spritePreview.image = null;
                _spritePreview.style.display = DisplayStyle.None;
            }
        }

        private void UpdateSpinePreview()
        {
            if (_data.spinePortraitConfig?.modelPrefab != null)
            {
                var previewTexture = UnityEditor.AssetPreview.GetAssetPreview(_data.spinePortraitConfig.modelPrefab);
                _spinePreview.image = previewTexture;
                _spinePreview.style.display = previewTexture != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
            else
            {
                _spinePreview.image = null;
                _spinePreview.style.display = DisplayStyle.None;
            }
        }

        private void UpdateSpriteSheetPreview()
        {
            if (_data.spriteSheetPresenter != null)
            {
                var previewTexture = UnityEditor.AssetPreview.GetAssetPreview(_data.spriteSheetPresenter);
                _spriteSheetPreview.image = previewTexture;
                _spriteSheetPreview.style.display = previewTexture != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
            else
            {
                _spriteSheetPreview.image = null;
                _spriteSheetPreview.style.display = DisplayStyle.None;
            }
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

            // Spine 預覽容器
            _spinePreview = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = 100,
                    height = 100,
                    alignSelf = Align.Center,
                    display = DisplayStyle.None,
                    marginBottom = 5
                }
            };
            _spineConfigBox.Add(_spinePreview);

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
                    UpdateSpinePreview(); // 更新預覽
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

        #if LIVE2D_KIT_AVAILABLE
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
#endif

        private void BuildSpriteSheetConfig(Action onChanged)
        {
            _spriteSheetConfigBox = new Foldout
            {
                text = "SpriteSheet Config",
                value = false
            };

            // SpriteSheet 預覽容器
            _spriteSheetPreview = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = 100,
                    height = 100,
                    alignSelf = Align.Center,
                    display = DisplayStyle.None,
                    marginBottom = 5
                }
            };
            _spriteSheetConfigBox.Add(_spriteSheetPreview);

            var spriteSheetConfigField = CreateObjectField<GameObject>(
                "Object",
                _data.spriteSheetPresenter,
                obj =>
                {
                    _data.spriteSheetPresenter = obj;
                    UpdateSpriteSheetPreview(); // 更新預覽
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
            
            if (_spritePreview != null)
            {
                // 只有在 Sprite 模式且有圖片時才顯示預覽
                _spritePreview.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.Sprite && _data.characterSprite != null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
            
#if SPINE_KIT_AVAILABLE
            if (_spineConfigBox != null)
            {
                _spineConfigBox.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.Spine
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
            
            if (_spinePreview != null)
            {
                 // 只有在 Spine 模式且有 Prefab 時才顯示預覽
                 _spinePreview.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.Spine && _data.spinePortraitConfig?.modelPrefab != null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
#endif
#if LIVE2D_KIT_AVAILABLE
            if (_live2DConfigBox != null)
            {
                _live2DConfigBox.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.Live2D
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
#endif

            if (_spriteSheetConfigBox != null)
            {
                _spriteSheetConfigBox.style.display =
                    isEnter && _data.portraitRenderMode == PortraitRenderMode.SpriteSheet
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }
            
            if (_spriteSheetPreview != null)
            {
                 // 只有在 SpriteSheet 模式且有 Prefab 時才顯示預覽
                 _spriteSheetPreview.style.display = isEnter && _data.portraitRenderMode == PortraitRenderMode.SpriteSheet && _data.spriteSheetPresenter != null
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
