#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SG.Dialogue.Conditions;
using SG.Dialogue.Nodes;
using SG.Dialogue.Variables;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 對話模擬器引擎，用於在編輯器中模擬對話流程。
    /// 它處理節點的執行、變數的變更和條件的檢查，並觸發 UI 事件。
    /// </summary>
    public class DialogueSimulatorEngine
    {
        /// <summary>
        /// 當需要顯示文字節點時觸發。
        /// </summary>
        public event Action<TextNode> OnShowText;
        /// <summary>
        /// 當需要顯示選項節點時觸發。
        /// </summary>
        public event Action<ChoiceNode> OnShowChoices;
        /// <summary>
        /// 當模擬結束時觸發。
        /// </summary>
        public event Action OnEnd;

        private readonly DialogueGraph _graph; // 要模擬的對話圖
        private readonly DialogueStateAsset _globalState; // 全域對話狀態資產
        private readonly DialogueState _localState = new DialogueState(); // 模擬器內部的局部狀態
        private string _currentNodeId; // 當前正在處理的節點 ID

        /// <summary>
        /// 模擬器是否正在運行。
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="graph">要模擬的對話圖。</param>
        /// <param name="globalState">全域對話狀態資產。</param>
        public DialogueSimulatorEngine(DialogueGraph graph, DialogueStateAsset globalState)
        {
            _graph = graph;
            _globalState = globalState;
        }

        /// <summary>
        /// 開始對話模擬。
        /// </summary>
        public void Start()
        {
            if (_graph == null || string.IsNullOrEmpty(_graph.startNodeId))
            {
                OnEnd?.Invoke();
                return;
            }

            _localState.Clear(); // 清空局部狀態
            _graph.BuildLookup(); // 建立節點查找表
            _currentNodeId = _graph.startNodeId; // 從起始節點開始
            IsRunning = true;
            ProcessCurrentNode(); // 處理當前節點
        }

        /// <summary>
        /// 停止對話模擬。
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            OnEnd?.Invoke();
        }

        /// <summary>
        /// 前進到下一個節點。
        /// </summary>
        /// <param name="nextNodeId">下一個節點的 ID。</param>
        public void Advance(string nextNodeId)
        {
            _currentNodeId = nextNodeId;
            ProcessCurrentNode();
        }

        /// <summary>
        /// 選擇一個對話選項。
        /// </summary>
        /// <param name="choice">選擇的對話選項。</param>
        public void SelectChoice(DialogueChoice choice)
        {
            ApplyVariableChanges(choice.variableChanges); // 應用變數變更
            _currentNodeId = choice.nextNodeId; // 設定下一個節點
            ProcessCurrentNode();
        }

        /// <summary>
        /// 處理當前節點的邏輯。
        /// </summary>
        private void ProcessCurrentNode()
        {
            while (IsRunning)
            {
                var node = _graph.GetNode(_currentNodeId);
                if (node == null)
                {
                    Stop();
                    return;
                }

                // 根據節點類型，決定是暫停等待用戶輸入，還是自動前進
                switch (node)
                {
                    case TextNode textNode:
                        ApplyVariableChanges(textNode.variableChanges);
                        OnShowText?.Invoke(textNode);
                        // 暫停，等待 UI 的 Advance() 呼叫
                        return; 

                    case ChoiceNode choiceNode:
                        OnShowChoices?.Invoke(choiceNode);
                        // 暫停，等待 UI 的 SelectChoice() 呼叫
                        return;

                    case ConditionNode conditionNode:
                        bool result = CheckCondition(conditionNode.Condition);
                        _currentNodeId = result ? conditionNode.TrueNextNodeId : conditionNode.FalseNextNodeId;
                        // 繼續循環以處理下一個節點
                        break;

                    // 對於所有非互動式節點，我們只應用變數變更（如果有的話）並自動前進
                    default:
                        if (node is TextNode t) ApplyVariableChanges(t.variableChanges);
                        else if (node is TransitionNode tr) ApplyVariableChanges(tr.variableChanges);
                        
                        _currentNodeId = node.GetNextNodeId();
                        // 繼續循環以處理下一個節點
                        break;
                }
            }
        }

        /// <summary>
        /// 檢查給定條件是否滿足。
        /// </summary>
        /// <param name="condition">要檢查的條件。</param>
        /// <returns>如果條件滿足則為 true，否則為 false。</returns>
        private bool CheckCondition(Conditions.Condition condition)
        {
            // 這是模仿 DialogueController 狀態訪問的簡化檢查
            // 完整的實現可能需要一個模擬控制器或共享介面
            foreach (var ic in condition.intConditions)
            {
                int current = _localState.HasInt(ic.variableName) ? _localState.GetInt(ic.variableName) : (_globalState?.GetInt(ic.variableName) ?? 0);
                switch (ic.comparison)
                {
                    case Comparison.Equal: if (current != ic.value) return false; break;
                    case Comparison.NotEqual: if (current == ic.value) return false; break;
                    case Comparison.Greater: if (current <= ic.value) return false; break;
                    case Comparison.Less: if (current >= ic.value) return false; break;
                    case Comparison.GreaterOrEqual: if (current < ic.value) return false; break;
                    case Comparison.LessOrEqual:    if (current > ic.value)  return false; break;
                }
            }
            foreach (var bc in condition.boolConditions)
            {
                bool current = _localState.HasBool(bc.variableName) ? _localState.GetBool(bc.variableName) : (_globalState?.GetBool(bc.variableName) ?? false);
                if (current != bc.requiredValue) return false;
            }
            return true;
        }

        /// <summary>
        /// 應用變數變更。
        /// </summary>
        /// <param name="changes">要應用的變數變更列表。</param>
        private void ApplyVariableChanges(List<VariableChange> changes)
        {
            if (changes == null) return;
            foreach (var change in changes)
            {
                if (string.IsNullOrEmpty(change.variableName)) continue;
                switch (change.type)
                {
                    case VariableChange.VarType.Int:
                        if (_globalState != null && _globalState.HasInt(change.variableName))
                            _globalState.AddInt(change.variableName, change.intDelta);
                        else
                            _localState.AddInt(change.variableName, change.intDelta);
                        break;
                    case VariableChange.VarType.Bool:
                        if (_globalState != null && _globalState.HasBool(change.variableName))
                        {
                            if (change.setBool) _globalState.SetBool(change.variableName, change.boolValue);
                            else _globalState.ToggleBool(change.variableName);
                        }
                        else
                        {
                            if (change.setBool) _localState.SetBool(change.variableName, change.boolValue);
                            else _localState.ToggleBool(change.variableName);
                        }
                        break;
                }
            }
        }
    }
}
#endif
