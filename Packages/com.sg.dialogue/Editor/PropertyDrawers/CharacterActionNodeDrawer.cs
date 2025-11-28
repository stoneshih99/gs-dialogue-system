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

        var actionTypeProp = property.FindPropertyRelative("ActionType");
        var targetPositionProp = property.FindPropertyRelative("TargetPosition");
        var clearAllOnExitProp = property.FindPropertyRelative("ClearAllOnExit");
        var overrideDurationProp = property.FindPropertyRelative("OverrideDuration");
        var durationProp = property.FindPropertyRelative("Duration");
        var speakerNameProp = property.FindPropertyRelative("speakerName");
        var portraitRenderModeProp = property.FindPropertyRelative("portraitRenderMode");
        var characterSpriteProp = property.FindPropertyRelative("characterSprite");
        var spinePortraitConfigProp = property.FindPropertyRelative("spinePortraitConfig");
        var live2DModelPrefabProp = property.FindPropertyRelative("live2DModelPrefab");

        var actionTypeField = new PropertyField(actionTypeProp);
        var targetPositionField = new PropertyField(targetPositionProp);
        
        var exitContainer = new VisualElement();
        exitContainer.Add(new PropertyField(clearAllOnExitProp));
        
        var enterContainer = new VisualElement();
        enterContainer.Add(new PropertyField(speakerNameProp, "Speaker Name"));
        var portraitRenderModeField = new PropertyField(portraitRenderModeProp, "Portrait Mode");
        enterContainer.Add(portraitRenderModeField);
        var spriteField = new PropertyField(characterSpriteProp);
        var spineField = new PropertyField(spinePortraitConfigProp);
        var live2dField = new PropertyField(live2DModelPrefabProp);
        enterContainer.Add(spriteField);
        enterContainer.Add(spineField);
        enterContainer.Add(live2dField);

        var overrideToggle = new PropertyField(overrideDurationProp);
        var durationField = new PropertyField(durationProp);

        container.Add(actionTypeField);
        container.Add(targetPositionField);
        container.Add(exitContainer);
        container.Add(enterContainer);
        container.Add(overrideToggle);
        container.Add(durationField);

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
                live2dField.style.display = renderMode == PortraitRenderMode.Live2D ? DisplayStyle.Flex : DisplayStyle.None;
            }

            durationField.style.display = overrideDurationProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        actionTypeField.RegisterValueChangeCallback(evt => RefreshUI());
        portraitRenderModeField.RegisterValueChangeCallback(evt => RefreshUI());
        overrideToggle.RegisterValueChangeCallback(evt => RefreshUI());

        RefreshUI();

        return container;
    }
}
#endif
