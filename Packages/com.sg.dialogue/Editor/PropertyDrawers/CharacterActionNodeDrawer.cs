#if UNITY_EDITOR
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(CharacterActionNode))]
public class CharacterActionNodeDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();

        // 獲取所有相關屬性
        var actionTypeProp = property.FindPropertyRelative("ActionType");
        var targetPositionProp = property.FindPropertyRelative("TargetPosition");
        var clearAllOnExitProp = property.FindPropertyRelative("ClearAllOnExit");
        var durationProp = property.FindPropertyRelative("Duration");
        var speakerNameProp = property.FindPropertyRelative("speakerName");
        var portraitRenderModeProp = property.FindPropertyRelative("portraitRenderMode");
        var characterSpriteProp = property.FindPropertyRelative("characterSprite");
        var spinePortraitConfigProp = property.FindPropertyRelative("spinePortraitConfig");
        var live2DModelPrefabProp = property.FindPropertyRelative("live2DModelPrefab");
        var live2DPortraitConfigProp = property.FindPropertyRelative("live2DPortraitConfig");
        var spriteSheetPortraitConfigProp = property.FindPropertyRelative("spriteSheetPortraitConfig");

        // 建立基礎 UI 元素
        var actionTypeField = new PropertyField(actionTypeProp);
        var targetPositionField = new PropertyField(targetPositionProp);
        
        // --- 退場設定容器 ---
        var exitContainer = new VisualElement();
        exitContainer.Add(new PropertyField(clearAllOnExitProp));
        
        // --- 進場設定容器 ---
        var enterContainer = new VisualElement();
        enterContainer.Add(new PropertyField(speakerNameProp, "Speaker Name"));
        var portraitRenderModeField = new PropertyField(portraitRenderModeProp, "Portrait Mode");
        enterContainer.Add(portraitRenderModeField);
        
        // 為不同渲染模式建立 PropertyField
        var spriteField = new PropertyField(characterSpriteProp);
        var spineField = new PropertyField(spinePortraitConfigProp);
        var live2dPrefabField = new PropertyField(live2DModelPrefabProp);
        var live2dConfigField = new PropertyField(live2DPortraitConfigProp);
        var spriteSheetField = new PropertyField(spriteSheetPortraitConfigProp);
        
        enterContainer.Add(spriteField);
        enterContainer.Add(spineField);
        enterContainer.Add(live2dPrefabField);
        enterContainer.Add(live2dConfigField);
        enterContainer.Add(spriteSheetField);

        // --- 其他設定 ---
        var durationField = new PropertyField(durationProp);

        // 將所有元素加入到主容器
        container.Add(actionTypeField);
        container.Add(targetPositionField);
        container.Add(exitContainer);
        container.Add(enterContainer);
        container.Add(durationField);

        // --- UI 更新邏輯 ---
        void RefreshUI()
        {
            var actionType = (CharacterActionType)actionTypeProp.enumValueIndex;
            exitContainer.style.display = actionType == CharacterActionType.Exit ? DisplayStyle.Flex : DisplayStyle.None;
            enterContainer.style.display = actionType == CharacterActionType.Enter ? DisplayStyle.Flex : DisplayStyle.None;

            if (actionType == CharacterActionType.Enter)
            {
                var renderMode = (PortraitRenderMode)portraitRenderModeProp.enumValueIndex;
                spriteField.style.display = renderMode == PortraitRenderMode.Sprite ? DisplayStyle.Flex : DisplayStyle.None;
                spineField.style.display = renderMode == PortraitRenderMode.Spine ? DisplayStyle.Flex : DisplayStyle.None;
                live2dPrefabField.style.display = renderMode == PortraitRenderMode.Live2D ? DisplayStyle.Flex : DisplayStyle.None;
                live2dConfigField.style.display = renderMode == PortraitRenderMode.Live2D ? DisplayStyle.Flex : DisplayStyle.None;
                spriteSheetField.style.display = renderMode == PortraitRenderMode.SpriteSheet ? DisplayStyle.Flex : DisplayStyle.None;
            }

        }

        // 註冊回調
        actionTypeField.RegisterValueChangeCallback(evt => RefreshUI());
        portraitRenderModeField.RegisterValueChangeCallback(evt => RefreshUI());

        // 初始刷新
        RefreshUI();

        return container;
    }
}
#endif