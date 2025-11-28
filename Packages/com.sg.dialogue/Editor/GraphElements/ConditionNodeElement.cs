#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using SG.Dialogue.Conditions;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// ConditionNodeElement 是 ConditionNode 的視覺化表示，用於在 GraphView 中顯示和編輯條件節點。
    /// 它允許用戶設定整數和布林條件，並提供 True/False 兩個輸出埠。
    /// </summary>
    public class ConditionNodeElement : DialogueNodeElement
    {
        public Port TrueOutputPort { get; private set; }
        public Port FalseOutputPort { get; private set; }

        public override DialogueNodeBase NodeData => _data;
        private readonly ConditionNode _data;
        private VisualElement _conditionContainer;
        private Action _onChanged;

        public ConditionNodeElement(ConditionNode data, Action onChanged, DialogueStateAsset globalState) : base(data.nodeId)
        {
            _data = data;
            _onChanged = onChanged;
            title = "Condition";

            TrueOutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            TrueOutputPort.portName = "True";
            outputContainer.Add(TrueOutputPort);

            FalseOutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            FalseOutputPort.portName = "False";
            outputContainer.Add(FalseOutputPort);

            if (_data.Condition == null) _data.Condition = new Condition();

            _conditionContainer = new VisualElement();
            mainContainer.Add(_conditionContainer);

            UpdateDropdowns(globalState);
        }

        public void UpdateDropdowns(DialogueStateAsset globalState)
        {
            RebuildConditionUI(_conditionContainer, _data.Condition, _onChanged, globalState);
        }

        private void RebuildConditionUI(VisualElement container, Condition condition, Action onChanged, DialogueStateAsset globalState)
        {
            container.Clear();

            var intChoices = globalState?.InitialInts.Select(p => p.key).ToList() ?? new List<string>();
            var boolChoices = globalState?.InitialBools.Select(p => p.key).ToList() ?? new List<string>();

            container.Add(new Label("Int Conditions") { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold } });
            container.Add(new Button(() => { condition.intConditions.Add(new IntCondition()); UpdateDropdowns(globalState); onChanged?.Invoke(); }) { text = "+ Int" });

            for (int i = 0; i < condition.intConditions.Count; i++)
            {
                var intCond = condition.intConditions[i];
                var box = new Box { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

                var currentVar = intCond.variableName;
                var currentChoices = new List<string>(intChoices);
                if (!string.IsNullOrEmpty(currentVar) && !currentChoices.Contains(currentVar))
                {
                    currentChoices.Insert(0, currentVar);
                }

                var popup = new PopupField<string>(currentChoices, currentVar ?? (currentChoices.Count > 0 ? currentChoices[0] : ""));
                popup.RegisterValueChangedCallback(evt => { intCond.variableName = evt.newValue; onChanged?.Invoke(); });
                box.Add(popup);

                var comparisonField = new EnumField("Comparison", intCond.comparison) { style = { minWidth = 80 } };
                comparisonField.RegisterValueChangedCallback(evt => { intCond.comparison = (Comparison)evt.newValue; onChanged?.Invoke(); });
                box.Add(comparisonField);

                var valueField = new IntegerField("Value") { value = intCond.value, style = { width = 50 } };
                valueField.RegisterValueChangedCallback(evt => { intCond.value = evt.newValue; onChanged?.Invoke(); });
                box.Add(valueField);

                var deleteButton = new Button(() => { condition.intConditions.Remove(intCond); UpdateDropdowns(globalState); onChanged?.Invoke(); }) { text = "-" };
                box.Add(deleteButton);
                
                container.Add(box);
            }

            container.Add(new Label("Bool Conditions") { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold, marginTop = 6 } });
            container.Add(new Button(() => { condition.boolConditions.Add(new BoolCondition()); UpdateDropdowns(globalState); onChanged?.Invoke(); }) { text = "+ Bool" });

            for (int i = 0; i < condition.boolConditions.Count; i++)
            {
                var boolCond = condition.boolConditions[i];
                var box = new Box { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

                var currentVar = boolCond.variableName;
                var currentChoices = new List<string>(boolChoices);
                if (!string.IsNullOrEmpty(currentVar) && !currentChoices.Contains(currentVar))
                {
                    currentChoices.Insert(0, currentVar);
                }

                var popup = new PopupField<string>(currentChoices, currentVar ?? (currentChoices.Count > 0 ? currentChoices[0] : ""));
                popup.RegisterValueChangedCallback(evt => { boolCond.variableName = evt.newValue; onChanged?.Invoke(); });
                box.Add(popup);

                var valueToggle = new Toggle("Required Value") { value = boolCond.requiredValue };
                valueToggle.RegisterValueChangedCallback(evt => { boolCond.requiredValue = evt.newValue; onChanged?.Invoke(); });
                box.Add(valueToggle);

                var deleteButton = new Button(() => { condition.boolConditions.Remove(boolCond); UpdateDropdowns(globalState); onChanged?.Invoke(); }) { text = "-" };
                box.Add(deleteButton);

                container.Add(box);
            }
        }

        public override void OnOutputPortConnected(Port outputPort, string targetNodeId)
        {
            if (outputPort == TrueOutputPort)
            {
                _data.TrueNextNodeId = targetNodeId;
            }
            else if (outputPort == FalseOutputPort)
            {
                _data.FalseNextNodeId = targetNodeId;
            }
        }

        public override void OnOutputPortDisconnected(Port outputPort)
        {
            if (outputPort == TrueOutputPort)
            {
                _data.TrueNextNodeId = null;
            }
            else if (outputPort == FalseOutputPort)
            {
                _data.FalseNextNodeId = null;
            }
        }
    }
}
#endif
