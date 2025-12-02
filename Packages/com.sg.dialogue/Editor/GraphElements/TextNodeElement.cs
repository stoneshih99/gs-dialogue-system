#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UEv = UnityEngine.Events;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class TextNodeElement : DialogueNodeElement
    {
        private const int MaxWidth = 350;
        
        public Port OutputPort { get; private set; }
        public Port InterruptPort { get; private set; }

        public override DialogueNodeBase NodeData => _data;
        private readonly TextNode _data;
        private readonly Action _onChanged;

        // 建構子保持不變，以維持與 Handler 的一致性
        public TextNodeElement(TextNode data, DialogueGraphView graphView, SerializedProperty nodeSerializedProperty, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            _onChanged = onChanged;

            // 呼叫基底類別的 Initialize 方法
            Initialize(graphView, nodeSerializedProperty);

            title = $"Text ({data.nodeId})";

            // 還原為手動建立 UI 元素
            var nameField = new TextField("Speaker") { value = data.speakerName };
            nameField.RegisterValueChangedCallback(e => { _data.speakerName = e.newValue; _onChanged?.Invoke(); });
            mainContainer.Add(nameField);

            var textField = new TextField("Text") { value = data.text, multiline = true };
            textField.style.maxWidth = MaxWidth;
            textField.RegisterValueChangedCallback(e => { _data.text = e.newValue; _onChanged?.Invoke(); });
            mainContainer.Add(textField);

            var mainFoldout = new Foldout { text = "Advanced Settings", value = false };
            mainContainer.Add(mainFoldout);

            BuildInterruptSettings(mainFoldout);
            BuildAudioSettings(mainFoldout);
            BuildCuesSettings(mainFoldout);
            BuildAutoAdvanceSettings(mainFoldout); // 此方法也將使用手動建立
            BuildEventSummary(mainFoldout);

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
        }

        private void BuildInterruptSettings(VisualElement container)
        {
            var interruptFold = new Foldout { text = "Interrupt Settings", value = false };
            
            var interruptibleToggle = new Toggle("Is Interruptible") { value = _data.IsInterruptible };
            interruptFold.Add(interruptibleToggle);

            var interruptFieldsContainer = new VisualElement();
            interruptFieldsContainer.style.display = _data.IsInterruptible ? DisplayStyle.Flex : DisplayStyle.None;
            interruptFold.Add(interruptFieldsContainer);

            interruptibleToggle.RegisterValueChangedCallback(evt =>
            {
                _data.IsInterruptible = evt.newValue;
                interruptFieldsContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                if (InterruptPort == null && evt.newValue)
                {
                    InterruptPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    InterruptPort.portName = "On Interrupt";
                    outputContainer.Add(InterruptPort);
                }
                else if (InterruptPort != null && !evt.newValue)
                {
                    if (InterruptPort.connected)
                    {
                        var edge = InterruptPort.connections.First();
                        edge.parent.Remove(edge);
                    }
                    outputContainer.Remove(InterruptPort);
                    InterruptPort = null;
                    _data.InterruptNextNodeId = null;
                }
                _onChanged?.Invoke();
            });

            var eventField = new ObjectField("Interrupt Event") { objectType = typeof(GameEvent), allowSceneObjects = false, value = _data.InterruptEvent };
            eventField.RegisterValueChangedCallback(evt => { _data.InterruptEvent = evt.newValue as GameEvent; _onChanged?.Invoke(); });
            interruptFieldsContainer.Add(eventField);

            if (_data.IsInterruptible)
            {
                InterruptPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                InterruptPort.portName = "On Interrupt";
                outputContainer.Add(InterruptPort);
            }

            container.Add(interruptFold);
        }

        private void BuildAudioSettings(VisualElement container)
        {
            // 還原為手動建立 ObjectField
            var audioField = new ObjectField("Audio Event") { objectType = typeof(AudioEvent), allowSceneObjects = false, value = _data.AudioEvent };
            audioField.RegisterValueChangedCallback(evt => { _data.AudioEvent = evt.newValue as AudioEvent; _onChanged?.Invoke(); });
            container.Add(audioField);
        }

        private void BuildCuesSettings(VisualElement container)
        {
            var cuesFold = new Foldout { text = "Text Cues", value = false };
            if (_data.textCues == null) _data.textCues = new List<TextCue>();
            var cuesToolbar = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var cuesContainer = new VisualElement();
            cuesToolbar.Add(new Button(() => { _data.textCues.Add(new TextCue()); RebuildCuesUI(cuesContainer); }) { text = "+ Cue" });
            cuesFold.Add(cuesToolbar);
            cuesFold.Add(cuesContainer);
            RebuildCuesUI(cuesContainer);
            container.Add(cuesFold);
        }

        private void RebuildCuesUI(VisualElement _cuesContainer)
        {
            _cuesContainer.Clear();
            for (int i = 0; i < _data.textCues.Count; i++)
            {
                int index = i;
                var cue = _data.textCues[i];
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var idxField = new IntegerField("Index") { value = cue.charIndex, style = { minWidth = 140 } };
                idxField.RegisterValueChangedCallback(e => { cue.charIndex = Mathf.Max(0, e.newValue); _onChanged?.Invoke(); });
                row.Add(idxField);
                row.Add(new Button(() => { _data.textCues.RemoveAt(index); RebuildCuesUI(_cuesContainer); }) { text = "-" });
                _cuesContainer.Add(row);
            }
        }

        private void BuildAutoAdvanceSettings(VisualElement container)
        {
            var autoFold = new Foldout { text = "Auto Advance & Delay", value = false };
            
            // 手動建立 Toggle
            var overrideAutoToggle = new Toggle("Override Auto") { value = _data.overrideAutoAdvance };
            overrideAutoToggle.RegisterValueChangedCallback(e => { _data.overrideAutoAdvance = e.newValue; _onChanged?.Invoke(); });
            autoFold.Add(overrideAutoToggle);

            // 手動建立 FloatField
            var autoDelayField = new FloatField("Delay (s)") { value = _data.autoAdvanceDelay };
            autoDelayField.RegisterValueChangedCallback(e => { _data.autoAdvanceDelay = Mathf.Max(0f, e.newValue); _onChanged?.Invoke(); });
            autoFold.Add(autoDelayField);

            // 手動為 postTypingDelay 建立 FloatField
            var postTypingDelayField = new FloatField("Post-Typing Delay (s)") { value = _data.postTypingDelay };
            postTypingDelayField.RegisterValueChangedCallback(e => { _data.postTypingDelay = Mathf.Max(0f, e.newValue); _onChanged?.Invoke(); });
            autoFold.Add(postTypingDelayField);

            container.Add(autoFold);
        }

        private void BuildEventSummary(VisualElement container)
        {
            var evtLabel = new Label(GetEventSummary()) { style = { fontSize = 10, color = Color.gray, unityTextAlign = TextAnchor.MiddleLeft } };
            container.Add(evtLabel);
            container.Add(new Label("Variables & UnityEvents: use Inspector to edit details") { style = { fontSize = 10, color = Color.gray } });
        }

        private string GetEventSummary()
        {
            int e = Count(_data.onEnter), x = Count(_data.onExit);
            return $"Events: Enter({e}), Exit({x})";
        }

        private int Count(UEv.UnityEvent u)
        {
            try { return u?.GetPersistentEventCount() ?? 0; }
            catch { return 0; }
        }

        public override void OnOutputPortConnected(Port outputPort, string targetNodeId)
        {
            if (outputPort == OutputPort)
            {
                _data.nextNodeId = targetNodeId;
            }
            else if (outputPort == InterruptPort)
            {
                _data.InterruptNextNodeId = targetNodeId;
            }
        }

        public override void OnOutputPortDisconnected(Port outputPort)
        {
            if (outputPort == OutputPort)
            {
                _data.nextNodeId = null;
            }
            else if (outputPort == InterruptPort)
            {
                _data.InterruptNextNodeId = null;
            }
        }
    }
}
#endif
