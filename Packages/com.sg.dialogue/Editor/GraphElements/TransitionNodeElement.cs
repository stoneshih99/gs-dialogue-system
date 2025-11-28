#if UNITY_EDITOR
using System;
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// TransitionNodeElement 是 TransitionNode 的視覺化表示，用於在 GraphView 中顯示和編輯過場節點。
    /// 它允許用戶設定角色視覺、背景視覺、音訊、黑屏/淡入淡出覆寫等過場效果。
    /// </summary>
    public class TransitionNodeElement : DialogueNodeElement
    {
        /// <summary>
        /// 獲取過場節點的輸出埠。
        /// </summary>
        public Port OutputPort { get; private set; }
        /// <summary>
        /// 獲取過場節點的數據模型。
        /// </summary>
        public override DialogueNodeBase NodeData => _data; // 實現抽象屬性
        private readonly TransitionNode _data; // 過場節點的數據

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="data">過場節點的數據。</param>
        /// <param name="onChanged">當節點數據改變時觸發的回調。</param>
        public TransitionNodeElement(TransitionNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Transition"; // 節點標題

            // 備註文本輸入框
            var noteField = new TextField("Note") { multiline = true, value = _data.note };
            noteField.RegisterValueChangedCallback(e => { _data.note = e.newValue; onChanged?.Invoke(); });
            mainContainer.Add(noteField);

            // 角色視覺設定折疊面板
            var charFold = new Foldout { text = "Character Visual", value = false };
            var charField = new ObjectField("Character Sprite") { objectType = typeof(Sprite), allowSceneObjects = false, value = _data.characterSprite };
            charField.RegisterValueChangedCallback(e => { _data.characterSprite = e.newValue as Sprite; onChanged?.Invoke(); });
            charFold.Add(charField);

            var charPosField = new EnumField("Position", _data.characterPosition);
            charPosField.RegisterValueChangedCallback(e => { _data.characterPosition = (CharacterPosition)e.newValue; onChanged?.Invoke(); });
            charFold.Add(charPosField);

            var clearCharToggle = new Toggle("Clear Characters") { value = _data.clearCharacters };
            clearCharToggle.RegisterValueChangedCallback(e => { _data.clearCharacters = e.newValue; onChanged?.Invoke(); });
            charFold.Add(clearCharToggle);
            mainContainer.Add(charFold);

            // 背景視覺設定折疊面板
            var bgFold = new Foldout { text = "Background Visual", value = false };
            var bgField = new ObjectField("Background Sprite") { objectType = typeof(Sprite), allowSceneObjects = false, value = _data.backgroundSprite };
            bgField.RegisterValueChangedCallback(e => { _data.backgroundSprite = e.newValue as Sprite; onChanged?.Invoke(); });
            bgFold.Add(bgField);

            var clearBgToggle = new Toggle("Clear Background") { value = _data.clearBackground };
            clearBgToggle.RegisterValueChangedCallback(e => { _data.clearBackground = e.newValue; onChanged?.Invoke(); });
            bgFold.Add(clearBgToggle);
            mainContainer.Add(bgFold);

            // 黑屏 / 淡入淡出覆寫設定折疊面板
            var fadeFold = new Foldout { text = "Black Screen / Fade Override", value = false };
            var blackScreenToggle = new Toggle("Use Black Screen") { value = _data.useBlackScreen };
            blackScreenToggle.RegisterValueChangedCallback(e => { _data.useBlackScreen = e.newValue; onChanged?.Invoke(); });
            fadeFold.Add(blackScreenToggle);

            var blackDurationField = new FloatField("Black Duration") { value = _data.blackScreenDuration };
            blackDurationField.RegisterValueChangedCallback(e => { _data.blackScreenDuration = Mathf.Max(0f, e.newValue); onChanged?.Invoke(); });
            fadeFold.Add(blackDurationField);

            var overrideBgToggle = new Toggle("Override BG Fade") { value = _data.overrideBackgroundFade };
            overrideBgToggle.RegisterValueChangedCallback(e => { _data.overrideBackgroundFade = e.newValue; onChanged?.Invoke(); });
            fadeFold.Add(overrideBgToggle);

            var bgFadeField = new FloatField("BG Fade Time") { value = _data.backgroundFadeOverride };
            bgFadeField.RegisterValueChangedCallback(e => { _data.backgroundFadeOverride = Mathf.Max(0f, e.newValue); onChanged?.Invoke(); });
            fadeFold.Add(bgFadeField);

            var overrideCharToggle = new Toggle("Override Char Fade") { value = _data.overrideCharacterFade };
            overrideCharToggle.RegisterValueChangedCallback(e => { _data.overrideCharacterFade = e.newValue; onChanged?.Invoke(); });
            fadeFold.Add(overrideCharToggle);

            var charFadeField = new FloatField("Char Fade Time") { value = _data.characterFadeOverride };
            charFadeField.RegisterValueChangedCallback(e => { _data.characterFadeOverride = Mathf.Max(0f, e.newValue); onChanged?.Invoke(); });
            fadeFold.Add(charFadeField);

            // 創建輸出埠
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
        }

        /// <summary>
        /// 覆寫連接邏輯：當輸出埠連接到另一個節點時，更新數據模型中的 nextNodeId。
        /// </summary>
        /// <param name="outputPort">連接的輸出埠。</param>
        /// <param name="targetNodeId">目標節點的 ID。</param>
        public override void OnOutputPortConnected(Port outputPort, string targetNodeId)
        {
            if (outputPort == OutputPort)
            {
                _data.nextNodeId = targetNodeId;
            }
        }

        /// <summary>
        /// 覆寫斷開連接邏輯：當輸出埠斷開連接時，將數據模型中的 nextNodeId 設為 null。
        /// </summary>
        /// <param name="outputPort">斷開連接的輸出埠。</param>
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
