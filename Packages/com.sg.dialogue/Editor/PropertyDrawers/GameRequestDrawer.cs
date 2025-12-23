#if UNITY_EDITOR
using SG.Dialogue.Events;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(GameRequest))]
public class GameRequestDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();

        var eventNameProp = property.FindPropertyRelative("EventName");
        var parametersProp = property.FindPropertyRelative("Parameters");

        if (eventNameProp != null)
        {
            var eventNameField = new PropertyField(eventNameProp, "Event Name");
            container.Add(eventNameField);
        }

        if (parametersProp != null)
        {
            var parametersField = new PropertyField(parametersProp, "Parameters");
            container.Add(parametersField);
        }
        
        return container;
    }
}
#endif
