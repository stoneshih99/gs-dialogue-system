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
        // 創建一個根容器
        var container = new VisualElement();

        // 為每個子屬性創建帶有標籤的 PropertyField
        var eventField = new PropertyField(property.FindPropertyRelative("EventName"), "Event Name");
        
        // 將子屬性欄位加入到容器中
        container.Add(eventField);
        
        return container;
        
    }
}
#endif
