#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using SG.Dialogue.Animation;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UEv = UnityEngine.Events;
using LitMotion;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// TextNodeElement 是 TextNode 的視覺化表示，用於在 GraphView 中顯示和編輯文字節點。
    /// 它包含了說話者名稱、文本內容、打斷設定、動畫、音效、文本提示、自動前進和事件等編輯功能。
    /// </summary>
    public class TextNodeElement : DialogueNodeElement
    {
        private const int MaxWidth = 350;
        
        /// <summary>
        /// 獲取文字節點的預設輸出埠（Next）。
        /// </summary>
        public Port OutputPort { get; private set; }
        /// <summary>
        /// 獲取文字節點的打斷輸出埠（On Interrupt）。
        /// </summary>
        public Port InterruptPort { get; private set; }

        /// <summary>
        /// 獲取文字節點的數據模型。
        /// </summary>
        public override DialogueNodeBase NodeData => _data; // 實現抽象屬性
        private readonly TextNode _data; // 文字節點的數據
        private readonly Action _onChanged; // 當節點數據改變時觸發的回調
        private VisualElement _cuesContainer; // 文本提示容器
        private VisualElement _motionsContainer; // 動畫容器

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="data">文字節點的數據。</param>
        /// <param name="onChanged">當節點數據改變時觸發的回調。</param>
        public TextNodeElement(TextNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            _onChanged = onChanged;

            title = $"Text ({data.nodeId})"; // 節點標題

            // 說話者名稱輸入框
            var nameField = new TextField("Speaker") { value = data.speakerName };
            nameField.RegisterValueChangedCallback(e => { _data.speakerName = e.newValue; _onChanged?.Invoke(); });
            mainContainer.Add(nameField);

            // 文本內容輸入框
            var textField = new TextField("Text") { value = data.text, multiline = true };
            textField.style.maxWidth = MaxWidth; // 設定最大寬度
            textField.RegisterValueChangedCallback(e => { _data.text = e.newValue; _onChanged?.Invoke(); });
            mainContainer.Add(textField);

            // 高級設定折疊面板
            var mainFoldout = new Foldout { text = "Advanced Settings", value = false };
            mainContainer.Add(mainFoldout);

            // 構建各個設定區塊
            BuildInterruptSettings(mainFoldout);
            BuildAnimationSettings(mainFoldout);
            BuildAudioSettings(mainFoldout);
            BuildCuesSettings(mainFoldout);
            BuildAutoAdvanceSettings(mainFoldout);
            BuildEventSummary(mainFoldout);

            // 創建預設輸出埠
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
        }

        /// <summary>
        /// 構建打斷設定區塊。
        /// </summary>
        /// <param name="container">父容器。</param>
        private void BuildInterruptSettings(VisualElement container)
        {
            var interruptFold = new Foldout { text = "Interrupt Settings", value = false };
            
            // 是否可打斷開關
            var interruptibleToggle = new Toggle("Is Interruptible") { value = _data.IsInterruptible };
            interruptFold.Add(interruptibleToggle);

            var interruptFieldsContainer = new VisualElement();
            interruptFieldsContainer.style.display = _data.IsInterruptible ? DisplayStyle.Flex : DisplayStyle.None;
            interruptFold.Add(interruptFieldsContainer);

            interruptibleToggle.RegisterValueChangedCallback(evt =>
            {
                _data.IsInterruptible = evt.newValue;
                interruptFieldsContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                // 根據是否可打斷來創建或移除打斷埠
                if (InterruptPort == null && evt.newValue)
                {
                    InterruptPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    InterruptPort.portName = "On Interrupt";
                    outputContainer.Add(InterruptPort);
                }
                else if (InterruptPort != null && !evt.newValue)
                {
                    // 如果打斷埠已連接，則移除連接
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

            // 打斷事件 ObjectField
            var eventField = new ObjectField("Interrupt Event") { objectType = typeof(GameEvent), allowSceneObjects = false, value = _data.InterruptEvent };
            eventField.RegisterValueChangedCallback(evt => { _data.InterruptEvent = evt.newValue as GameEvent; _onChanged?.Invoke(); });
            interruptFieldsContainer.Add(eventField);

            // 如果初始就是可打斷的，則創建打斷埠
            if (_data.IsInterruptible)
            {
                InterruptPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                InterruptPort.portName = "On Interrupt";
                outputContainer.Add(InterruptPort);
            }

            container.Add(interruptFold);
        }

        /// <summary>
        /// 構建動畫設定區塊。
        /// </summary>
        /// <param name="container">父容器。</param>
        private void BuildAnimationSettings(VisualElement container)
        {
            var animFold = new Foldout { text = "Animation", value = false };
            
            // 目標動畫位置下拉選單
            var targetPosField = new EnumField("Target Position", _data.targetAnimationPosition);
            targetPosField.RegisterValueChangedCallback(e => { _data.targetAnimationPosition = (CharacterPosition)e.newValue; _onChanged?.Invoke(); });
            animFold.Add(targetPosField);

            // 添加動畫按鈕
            var motionsToolbar = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            motionsToolbar.Add(new Button(() => { _data.motions.Add(new MotionData()); RebuildMotionsUI(); }) { text = "+ Motion" });
            animFold.Add(motionsToolbar);

            _motionsContainer = new VisualElement();
            animFold.Add(_motionsContainer);
            
            RebuildMotionsUI(); // 重新構建動畫 UI
            container.Add(animFold);
        }

        /// <summary>
        /// 重新構建動畫 UI 列表。
        /// </summary>
        private void RebuildMotionsUI()
        {
            _motionsContainer.Clear();
            if (_data.motions == null) _data.motions = new List<MotionData>();

            for (int i = 0; i < _data.motions.Count; i++)
            {
                int index = i;
                var motion = _data.motions[i];
                var motionBox = new Box { style = { marginTop = 2, marginBottom = 2, paddingTop = 2, paddingBottom = 4 } };

                var row1 = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var propField = new EnumField(motion.TargetProperty) { style = { flexGrow = 1 } };
                propField.RegisterValueChangedCallback(e => { motion.TargetProperty = (MotionTargetProperty)e.newValue; _onChanged?.Invoke(); });
                row1.Add(propField);
                row1.Add(new Button(() => { _data.motions.RemoveAt(index); RebuildMotionsUI(); }) { text = "-" }); // 移除動畫按鈕
                motionBox.Add(row1);

                // 動畫參數輸入欄位
                var endValueField = new Vector3Field("End Value") { value = motion.EndValue };
                endValueField.RegisterValueChangedCallback(e => { motion.EndValue = e.newValue; _onChanged?.Invoke(); });
                motionBox.Add(endValueField);

                var durationField = new FloatField("Duration") { value = motion.Duration };
                durationField.RegisterValueChangedCallback(e => { motion.Duration = e.newValue; _onChanged?.Invoke(); });
                motionBox.Add(durationField);

                var delayField = new FloatField("Delay") { value = motion.Delay };
                delayField.RegisterValueChangedCallback(e => { motion.Delay = e.newValue; _onChanged?.Invoke(); });
                motionBox.Add(delayField);

                var easeField = new EnumField("Ease", motion.Ease);
                easeField.RegisterValueChangedCallback(e => { motion.Ease = (Ease)e.newValue; _onChanged?.Invoke(); });
                motionBox.Add(easeField);

                var loopTypeField = new EnumField("Loop Type", motion.LoopType);
                loopTypeField.RegisterValueChangedCallback(e => { motion.LoopType = (MotionLoopType)e.newValue; _onChanged?.Invoke(); });
                motionBox.Add(loopTypeField);

                var loopsField = new IntegerField("Loops") { value = motion.Loops, tooltip = "-1 for infinite" };
                loopsField.RegisterValueChangedCallback(e => { motion.Loops = e.newValue; _onChanged?.Invoke(); });
                motionBox.Add(loopsField);

                var isRelativeToggle = new Toggle("Is Relative") { value = motion.IsRelative, tooltip = "是否為相對值" };
                isRelativeToggle.RegisterValueChangedCallback(e => { motion.IsRelative = e.newValue; _onChanged?.Invoke(); });
                motionBox.Add(isRelativeToggle);
                
                _motionsContainer.Add(motionBox);
            }
        }

        /// <summary>
        /// 構建音效設定區塊。
        /// </summary>
        /// <param name="container">父容器。</param>
        private void BuildAudioSettings(VisualElement container)
        {
            // 此區塊目前為空，保留以備未來擴充
        }

        /// <summary>
        /// 構建文本提示設定區塊。
        /// </summary>
        /// <param name="container">父容器。</param>
        private void BuildCuesSettings(VisualElement container)
        {
            var cuesFold = new Foldout { text = "Text Cues", value = false };
            if (_data.textCues == null) _data.textCues = new List<TextCue>();
            var cuesToolbar = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            cuesToolbar.Add(new Button(() => { _data.textCues.Add(new TextCue()); RebuildCuesUI(); }) { text = "+ Cue" }); // 添加提示按鈕
            cuesFold.Add(cuesToolbar);
            _cuesContainer = new VisualElement();
            cuesFold.Add(_cuesContainer);
            RebuildCuesUI(); // 重新構建提示 UI
            container.Add(cuesFold);
        }

        /// <summary>
        /// 重新構建文本提示 UI 列表。
        /// </summary>
        private void RebuildCuesUI()
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
                row.Add(new Button(() => { _data.textCues.RemoveAt(index); RebuildCuesUI(); }) { text = "-" }); // 移除提示按鈕
                _cuesContainer.Add(row);
            }
        }

        /// <summary>
        /// 構建自動前進設定區塊。
        /// </summary>
        /// <param name="container">父容器。</param>
        private void BuildAutoAdvanceSettings(VisualElement container)
        {
            var autoFold = new Foldout { text = "Auto Advance", value = false };
            // 覆寫自動前進開關
            var overrideAutoToggle = new Toggle("Override Auto") { value = _data.overrideAutoAdvance };
            overrideAutoToggle.RegisterValueChangedCallback(e => { _data.overrideAutoAdvance = e.newValue; _onChanged?.Invoke(); });
            autoFold.Add(overrideAutoToggle);
            // 自動前進延遲時間輸入框
            var autoDelayField = new FloatField("Delay (s)") { value = _data.autoAdvanceDelay };
            autoDelayField.RegisterValueChangedCallback(e => { _data.autoAdvanceDelay = Mathf.Max(0f, e.newValue); _onChanged?.Invoke(); });
            autoFold.Add(autoDelayField);
        }

        /// <summary>
        /// 構建事件摘要顯示區塊。
        /// </summary>
        /// <param name="container">父容器。</param>
        private void BuildEventSummary(VisualElement container)
        {
            var evtLabel = new Label(GetEventSummary()) { style = { fontSize = 10, color = Color.gray, unityTextAlign = TextAnchor.MiddleLeft } };
            container.Add(evtLabel);
            container.Add(new Label("Variables & UnityEvents: use Inspector to edit details") { style = { fontSize = 10, color = Color.gray } });
        }

        /// <summary>
        /// 獲取事件摘要字符串。
        /// </summary>
        /// <returns>事件摘要。</returns>
        private string GetEventSummary()
        {
            int e = Count(_data.onEnter), x = Count(_data.onExit);
            return $"Events: Enter({e}), Exit({x})";
        }

        /// <summary>
        /// 計算 UnityEvent 的持久事件數量。
        /// </summary>
        /// <param name="u">UnityEvent 實例。</param>
        /// <returns>持久事件數量。</returns>
        private int Count(UEv.UnityEvent u)
        {
            try { return u?.GetPersistentEventCount() ?? 0; }
            catch { return 0; }
        }

        /// <summary>
        /// 覆寫連接邏輯：當輸出埠連接到另一個節點時，更新數據模型中的 nextNodeId 或 InterruptNextNodeId。
        /// </summary>
        /// <param name="outputPort">連接的輸出埠。</param>
        /// <param name="targetNodeId">目標節點的 ID。</param>
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

        /// <summary>
        /// 覆寫斷開連接邏輯：當輸出埠斷開連接時，將數據模型中的 nextNodeId 或 InterruptNextNodeId 設為 null。
        /// </summary>
        /// <param name="outputPort">斷開連接的輸出埠。</param>
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
