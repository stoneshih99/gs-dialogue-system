#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class SetBackgroundNodeElement : DialogueNodeElement
    {
        public Port OutputPort { get; private set; }
        public override DialogueNodeBase NodeData => _data;
        private readonly SetBackgroundNode _data;

        public SetBackgroundNodeElement(SetBackgroundNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Set Background";

            var bgField = new ObjectField("Background") { objectType = typeof(Sprite), allowSceneObjects = false, value = _data.backgroundSprite };
            bgField.RegisterValueChangedCallback(e => { _data.backgroundSprite = e.newValue as Sprite; onChanged?.Invoke(); });
            mainContainer.Add(bgField);

            var clearBgToggle = new Toggle("Clear Before") { value = _data.clearBackground };
            clearBgToggle.RegisterValueChangedCallback(e => { _data.clearBackground = e.newValue; onChanged?.Invoke(); });
            mainContainer.Add(clearBgToggle);

            var fadeFold = new Foldout { text = "Effects & Overrides", value = false };
            
            var blackScreenToggle = new Toggle("Use Black Screen") { value = _data.useBlackScreen };
            var blackDurationField = new FloatField("Black Duration") { value = _data.blackScreenDuration };
            
            var overrideBgToggle = new Toggle("Override BG Fade") { value = _data.overrideBackgroundFade };
            var bgFadeField = new FloatField("BG Fade Time") { value = _data.backgroundFadeOverride };

            void UpdateVisibility()
            {
                blackDurationField.style.display = _data.useBlackScreen ? DisplayStyle.Flex : DisplayStyle.None;
                bgFadeField.style.display = _data.overrideBackgroundFade ? DisplayStyle.Flex : DisplayStyle.None;
            }

            blackScreenToggle.RegisterValueChangedCallback(e => { _data.useBlackScreen = e.newValue; UpdateVisibility(); onChanged?.Invoke(); });
            blackDurationField.RegisterValueChangedCallback(e => { _data.blackScreenDuration = Mathf.Max(0f, e.newValue); onChanged?.Invoke(); });
            
            overrideBgToggle.RegisterValueChangedCallback(e => { _data.overrideBackgroundFade = e.newValue; UpdateVisibility(); onChanged?.Invoke(); });
            bgFadeField.RegisterValueChangedCallback(e => { _data.backgroundFadeOverride = Mathf.Max(0f, e.newValue); onChanged?.Invoke(); });

            fadeFold.Add(blackScreenToggle);
            fadeFold.Add(blackDurationField);
            fadeFold.Add(overrideBgToggle);
            fadeFold.Add(bgFadeField);
            mainContainer.Add(fadeFold);

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
