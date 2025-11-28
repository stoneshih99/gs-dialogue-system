#if UNITY_EDITOR
using SG.Dialogue.Events;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/// <summary>
/// 為 AudioRequest 結構提供一個自訂的 UIElements 編輯器介面。
/// 這將取代預設的、無法正確顯示 struct 內容的介面。
/// </summary>
[CustomPropertyDrawer(typeof(AudioRequest))]
public class AudioRequestDrawer : PropertyDrawer
{
    /// <summary>
    /// 創建屬性的視覺化元素。
    /// </summary>
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // 創建一個根容器
        var container = new VisualElement();

        // 為每個子屬性創建帶有標籤的 PropertyField
        var actionTypeField = new PropertyField(property.FindPropertyRelative("ActionType"), "Action Type");
        var clipField = new PropertyField(property.FindPropertyRelative("Clip"), "Audio Clip");
        var loopField = new PropertyField(property.FindPropertyRelative("Loop"), "Loop");
        var fadeDurationField = new PropertyField(property.FindPropertyRelative("FadeDuration"), "Fade Duration");

        // 將子屬性欄位加入到容器中
        container.Add(actionTypeField);
        container.Add(clipField);
        container.Add(loopField);
        container.Add(fadeDurationField);

        return container;
    }
}
#endif
