#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SG.Dialogue.Animation;
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using LitMotion;
using SG.Dialogue.Editor.Editor.GraphElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// AnimationNodeElement 是 AnimationNode 的視覺化表示，用於在 GraphView 中顯示和編輯動畫節點。
    /// </summary>
    public class AnimationNodeElement : DialogueNodeElement
    {
        /// <summary>
        /// 獲取動畫節點的預設輸出埠（Next）。
        /// </summary>
        public Port OutputPort { get; private set; }
        
        /// <summary>
        /// 獲取動畫節點的數據模型。
        /// </summary>
        public override DialogueNodeBase NodeData => _data;
        private readonly AnimationNode _data;
        private readonly Action _onChanged;
        private VisualElement _motionsContainer; // 動畫 UI 容器

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="data">動畫節點的數據。</param>
        /// <param name="onChanged">當節點數據改變時觸發的回調。</param>
        public AnimationNodeElement(AnimationNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            _onChanged = onChanged;

            title = $"Animation ({data.nodeId})"; // 節點標題

            BuildAnimationSettings(mainContainer);

            // 創建預設輸出埠
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);
        }

        /// <summary>
        /// 構建動畫設定區塊。
        /// </summary>
        /// <param name="container">父容器。</param>
        private void BuildAnimationSettings(VisualElement container)
        {
            var animFold = new Foldout { text = "Animation", value = true };
            
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
