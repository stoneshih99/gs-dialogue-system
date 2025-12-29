#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class ParticleNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly ParticleNode _data;

        // UI 引用
        private PropertyField _prefabField;
        private PropertyField _positionField;
        private PropertyField _scaleField;
        
        // Dummy Object 相關
        private GameObject _dummyObject;
        private Button _editButton;
        private bool _isEditing;
        private SerializedProperty _positionProp;

        public ParticleNodeElement(ParticleNode data, SerializedProperty nodeProperty, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Particle Effect";

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            if (nodeProperty != null)
            {
                // 顯示 ParticleEvent 通道
                var eventProp = nodeProperty.FindPropertyRelative("ParticleEvent");
                if (eventProp != null)
                {
                    var field = new PropertyField(eventProp, "Event Channel");
                    field.Bind(nodeProperty.serializedObject);
                    mainContainer.Add(field);
                }

                // Action Type
                var actionTypeProp = nodeProperty.FindPropertyRelative("ActionType");
                if (actionTypeProp != null)
                {
                    var field = new PropertyField(actionTypeProp, "Action");
                    field.Bind(nodeProperty.serializedObject);
                    field.RegisterValueChangeCallback(evt => UpdateVisibility((ParticleActionType)evt.changedProperty.enumValueIndex));
                    mainContainer.Add(field);
                }

                // ID
                AddPropertyField(nodeProperty, "ParticleID", "Particle ID");

                // Prefab
                _prefabField = AddPropertyField(nodeProperty, "ParticlePrefab", "Prefab");
                
                // Position & Edit Button
                _positionField = AddPropertyField(nodeProperty, "Position", "Position");
                _positionProp = nodeProperty.FindPropertyRelative("Position");
                
                _editButton = new Button(ToggleEditMode) { text = "Edit Position in Scene" };
                _editButton.style.marginTop = 2;
                _editButton.style.marginBottom = 5;
                mainContainer.Add(_editButton);

                // Scale
                _scaleField = AddPropertyField(nodeProperty, "Scale", "Scale");

                // 初始化可見性
                if (actionTypeProp != null)
                {
                    UpdateVisibility((ParticleActionType)actionTypeProp.enumValueIndex);
                }
            }

            // 註冊變更回調
            this.RegisterCallback<ChangeEvent<string>>(evt => onChanged?.Invoke());
            this.RegisterCallback<ChangeEvent<Enum>>(evt => onChanged?.Invoke());
            this.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt => onChanged?.Invoke());
            this.RegisterCallback<ChangeEvent<Vector3>>(evt => onChanged?.Invoke());
            
            // 註冊銷毀回調，確保節點被刪除時 Dummy 也被刪除
            RegisterCallback<DetachFromPanelEvent>(evt => CleanupDummy());
        }

        private void UpdateVisibility(ParticleActionType type)
        {
            bool isPlay = type == ParticleActionType.Play;
            DisplayStyle style = isPlay ? DisplayStyle.Flex : DisplayStyle.None;

            if (_prefabField != null) _prefabField.style.display = style;
            if (_positionField != null) _positionField.style.display = style;
            if (_scaleField != null) _scaleField.style.display = style;
            if (_editButton != null) _editButton.style.display = style;
        }

        private void ToggleEditMode()
        {
            if (_isEditing)
            {
                StopEditing();
            }
            else
            {
                StartEditing();
            }
        }

        private void StartEditing()
        {
            if (_isEditing) return;
            _isEditing = true;
            _editButton.text = "Stop Editing";
            _editButton.style.backgroundColor = new StyleColor(new Color(0.8f, 0.3f, 0.3f)); // 紅色提示

            // 建立 Dummy
            _dummyObject = new GameObject($"Particle_Dummy_{_data.ParticleID}");
            _dummyObject.transform.position = _data.Position;
            
            // 加上一個圖示方便看到
            var icon = EditorGUIUtility.IconContent("sv_label_1");
            if (icon != null) EditorGUIUtility.SetIconForObject(_dummyObject, (Texture2D)icon.image);

            // 選中它
            Selection.activeGameObject = _dummyObject;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            // 開始監聽更新
            EditorApplication.update += OnEditorUpdate;
        }

        private void StopEditing()
        {
            if (!_isEditing) return;
            _isEditing = false;
            _editButton.text = "Edit Position in Scene";
            _editButton.style.backgroundColor = StyleKeyword.Null;

            CleanupDummy();
            EditorApplication.update -= OnEditorUpdate;
        }

        private void CleanupDummy()
        {
            if (_dummyObject != null)
            {
                Object.DestroyImmediate(_dummyObject);
                _dummyObject = null;
            }
            // 確保移除監聽
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_dummyObject == null)
            {
                // Dummy 被使用者手動刪除了
                StopEditing(); 
                return;
            }

            // 如果 Dummy 位置變了，回寫到節點
            if (_dummyObject.transform.position != _data.Position)
            {
                _data.Position = _dummyObject.transform.position;
                
                // 更新 UI 數值
                if (_positionProp != null)
                {
                    _positionProp.vector3Value = _data.Position;
                    _positionProp.serializedObject.ApplyModifiedProperties();
                }
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
            if (outputPort == OutputPort) _data.nextNodeId = targetNodeId;
        }

        public override void OnOutputPortDisconnected(Port outputPort)
        {
            if (outputPort == OutputPort) _data.nextNodeId = null;
        }
    }
}
#endif
