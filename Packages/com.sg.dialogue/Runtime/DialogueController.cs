using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SG.Dialogue.Core.Instructions;
using SG.Dialogue.Events;
using SG.Dialogue.Nodes;
using SG.Dialogue.Presentation;
using SG.Dialogue.UI;
using SG.Dialogue.Variables;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue
{
    /// <summary>
    /// DialogueController 是對話系統的核心控制器，負責管理對話流程、UI 互動、視覺呈現和狀態變更。
    /// 它通過事件與外部系統（如音訊、視覺效果等）通信，以保持低耦合。
    /// </summary>
    [RequireComponent(typeof(DialogueUIManager), typeof(DialogueVisualManager))]
    public class DialogueController : MonoBehaviour
    {
        [Header("圖表與狀態")]
        [Tooltip("要執行的對話圖資產")]
        [SerializeField] private DialogueGraph graph;
        [Tooltip("全域變數資產，用於跨對話儲存狀態")]
        [SerializeField] private DialogueStateAsset globalState;

        [Header("管理器")]
        [Tooltip("對話 UI 管理器")]
        [SerializeField] private DialogueUIManager uiManager;
        /// <summary>
        /// 對話 UI 管理器，負責處理所有 UI 相關的顯示。
        /// </summary>
        public DialogueUIManager UiManager => uiManager;

        [Tooltip("對話視覺管理器")]
        [SerializeField] private DialogueVisualManager visualManager;
        /// <summary>
        /// 對話視覺管理器，負責處理角色、背景等視覺元素的呈現。
        /// </summary>
        public DialogueVisualManager VisualManager => visualManager;
        
        [Tooltip("場景中的攝影機控制器")]
        [SerializeField] private DialogueCameraController cameraController;
        /// <summary>
        /// 對話攝影機控制器，用於控制鏡頭的移動和聚焦。
        /// </summary>
        public DialogueCameraController CameraController => cameraController;

        [Header("事件")]
        [Tooltip("對話開始時觸發的 UnityEvent")]
        public UnityEvent onDialogueStarted;
        [Tooltip("對話結束時觸發的 UnityEvent")]
        public UnityEvent onDialogueEnded;

        /// <summary>
        /// 對話是否正在運行。
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// 當前正在處理的節點 ID。
        /// </summary>
        public string CurrentNodeId => _currentNodeId;
        /// <summary>
        /// 當前正在運行的對話圖。
        /// </summary>
        public DialogueGraph CurrentGraph => graph;

        private readonly DialogueState _localState = new DialogueState();
        /// <summary>
        /// 本地狀態，僅在當前對話中有效。
        /// </summary>
        public DialogueState LocalState => _localState;
        /// <summary>
        /// 全域狀態，可在不同對話之間共享。
        /// </summary>
        public DialogueStateAsset GlobalState => globalState;

        private string _currentNodeId;
        private DialogueNodeBase _lastNode;
        private Coroutine _activeNodeCoroutine;
        
        // 執行堆疊，用於處理分支和返回
        private readonly Stack<string> _executionStack = new Stack<string>();

        private void Awake()
        {
            if (uiManager == null) uiManager = GetComponent<DialogueUIManager>();
            if (visualManager == null) visualManager = GetComponent<DialogueVisualManager>();
        }

        private void OnEnable()
        {
            uiManager.OnAdvanceRequested += OnAdvanceRequested;
            uiManager.OnChoiceSelected += OnChoiceSelected;
        }

        private void OnDisable()
        {
            uiManager.OnAdvanceRequested -= OnAdvanceRequested;
            uiManager.OnChoiceSelected -= OnChoiceSelected;
        }

        /// <summary>
        /// 使用指定的對話圖開始對話。
        /// </summary>
        /// <param name="newGraph">要開始的對話圖。</param>
        public void StartDialogue(DialogueGraph newGraph)
        {
            if (IsRunning)
            {
                EndDialogue();
            }

            if (string.IsNullOrEmpty(newGraph.startNodeId))
            {
                throw new ArgumentNullException(nameof(newGraph),
                    "newGraph.startNodeId 必須要指定起始節點 ID");
            }
            
            graph = newGraph;
            graph.BuildLookup(); // 建立節點查找表以提高效能

            _localState.Clear();
            _executionStack.Clear();
            
            uiManager.SetPanelVisibility(true);
            uiManager.SetSkipButtonVisibility(graph.IsSkippable);
            
            IsRunning = true;
            onDialogueStarted?.Invoke();
            graph?.onDialogueStarted?.Invoke();

            Advance(graph.startNodeId);
        }

        /// <summary>
        /// 使用目前設定的對話圖開始對話。
        /// </summary>
        public void StartDialogue()
        {
            if (graph == null) { Debug.LogError("對話控制器：對話圖為空。"); return; }
            StartDialogue(graph);
        }

        /// <summary>
        /// 前進到下一個節點。
        /// </summary>
        /// <param name="nextNodeId">下一個節點的 ID。</param>
        private void Advance(string nextNodeId)
        {
            if (!IsRunning) return;

            if (_activeNodeCoroutine != null)
            {
                StopCoroutine(_activeNodeCoroutine);
                _activeNodeCoroutine = null;
            }

            if (_lastNode != null)
            {
                TriggerOnExit(_lastNode);
            }

            string nodeIdToProcess = FindNextProcessableNodeId(nextNodeId);

            if (string.IsNullOrEmpty(nodeIdToProcess))
            {
                EndDialogue();
                return;
            }

            _currentNodeId = nodeIdToProcess;
            var node = graph.GetNode(_currentNodeId);
            _lastNode = node;

            if (node != null)
            {
                TriggerOnEnterAndVariableChanges(node);
                _activeNodeCoroutine = StartCoroutine(ProcessNodeCoroutine(node));
            }
            else
            {
                Debug.LogWarning($"對話控制器：找不到節點 ID：{_currentNodeId}。對話結束。");
                EndDialogue();
            }
        }

        /// <summary>
        /// 尋找下一個可處理的節點 ID，會自動跳過被禁用的節點。
        /// </summary>
        /// <param name="startNodeId">開始尋找的節點 ID。</param>
        /// <returns>下一個可處理的節點 ID，如果找不到則返回 null。</returns>
        private string FindNextProcessableNodeId(string startNodeId)
        {
            string currentNodeId = startNodeId;
            int safetyBreak = 100; // 防止無限迴圈

            while (!string.IsNullOrEmpty(currentNodeId) && safetyBreak-- > 0)
            {
                var node = graph.GetNode(currentNodeId);
                if (node == null)
                {
                    Debug.LogWarning($"在圖表中找不到 ID 為 '{currentNodeId}' 的節點。");
                    return null;
                }

                if (node.IsEnabled)
                {
                    return currentNodeId; // 找到一個啟用的節點，返回其 ID
                }

                // 如果當前節點被停用，則自動跳到它的下一個節點
                Debug.Log($"[對話] 跳過已停用的節點：{currentNodeId}");
                currentNodeId = node.GetNextNodeId();
            }

            if (safetyBreak <= 0)
            {
                Debug.LogError("在尋找可處理節點時檢測到無限迴圈。中止對話。");
                return null;
            }
            
            // 如果一路跳到底，或者從一開始就沒有 ID，則嘗試從堆疊中獲取
            if (_executionStack.Count > 0)
            {
                return FindNextProcessableNodeId(_executionStack.Pop());
            }

            return null; // 找不到可執行的節點
        }

        /// <summary>
        /// 處理單個節點的協程。
        /// </summary>
        /// <param name="node">要處理的節點。</param>
        private IEnumerator ProcessNodeCoroutine(DialogueNodeBase node)
        {
            var instructionEnumerator = node.Process(this);
            while (instructionEnumerator.MoveNext())
            {
                var instruction = instructionEnumerator.Current;

                if (instruction is AdvanceToNode advance)
                {
                    Advance(advance.NextNodeId);
                    yield break; // 結束此協程，因為 Advance 會啟動新的流程
                }
                else if (instruction is WaitForUserInput)
                {
                    yield break; // 等待使用者輸入，暫停執行
                }
                else if (instruction is EndDialogue)
                {
                    EndDialogue();
                    yield break;
                }
                else if (instruction != null)
                {
                    // 執行其他指令，例如等待時間、動畫等
                    yield return instruction;
                }
            }

            // 如果節點處理完畢且沒有明確的跳轉指令，則自動前進到預設的下一個節點
            string defaultNextId = node.GetNextNodeId();
            Advance(defaultNextId);
        }

        /// <summary>
        /// 執行一個獨立的分支（例如，用於觸發事件或動畫，不影響主對話流程）。
        /// </summary>
        /// <param name="startNodeId">分支的起始節點 ID。</param>
        public Coroutine ExecuteBranch(string startNodeId)
        {
            return StartCoroutine(ExecuteBranchCoroutine(startNodeId));
        }

        private IEnumerator ExecuteBranchCoroutine(string startNodeId)
        {
            string currentBranchNodeId = startNodeId;
            while (!string.IsNullOrEmpty(currentBranchNodeId))
            {
                var node = graph.GetNode(currentBranchNodeId);
                if (node == null)
                {
                    Debug.LogWarning($"執行分支：找不到節點：{currentBranchNodeId}。分支已終止。");
                    yield break;
                }

                if (!node.IsEnabled)
                {
                    Debug.Log($"[對話] 在分支中跳過已停用的節點：{currentBranchNodeId}");
                    currentBranchNodeId = node.GetNextNodeId();
                    continue;
                }

                var instructionEnumerator = node.Process(this);
                while (instructionEnumerator.MoveNext())
                {
                    var instruction = instructionEnumerator.Current;
                    if (instruction != null)
                    {
                        yield return instruction;
                    }
                }

                currentBranchNodeId = node.GetNextNodeId();
            }
        }
        
        /// <summary>
        /// 觸發節點的退出事件。
        /// </summary>
        private void TriggerOnExit(DialogueNodeBase node)
        {
            if (node is TextNode t) t.onExit?.Invoke();
            else if (node is ChoiceNode c) c.onExit?.Invoke();
            else if (node is TransitionNode tr) tr.onExit?.Invoke();
            graph?.onNodeExited?.Invoke(node.nodeId);
        }

        /// <summary>
        /// 觸發節點的進入事件並應用變數變更。
        /// </summary>
        private void TriggerOnEnterAndVariableChanges(DialogueNodeBase node)
        {
            if (node is TextNode t)
            {
                ApplyVariableChanges(t.variableChanges);
                t.onEnter?.Invoke();
            }
            else if (node is ChoiceNode c)
            {
                c.onEnter?.Invoke();
            }
            else if (node is TransitionNode tr)
            {
                ApplyVariableChanges(tr.variableChanges);
                tr.onEnter?.Invoke();
            }
            graph?.onNodeEntered?.Invoke(node.nodeId);
        }

        /// <summary>
        /// 處理來自 UI 的前進請求。
        /// </summary>
        private void OnAdvanceRequested()
        {
            if (!IsRunning || uiManager.IsTyping) return;

            if (_lastNode is TextNode textNode)
            {
                Advance(textNode.nextNodeId);
            }
        }

        /// <summary>
        /// 處理來自 UI 的選項選擇。
        /// </summary>
        private void OnChoiceSelected(DialogueChoice choice)
        {
            ApplyVariableChanges(choice.variableChanges);
            choice.onSelected?.Invoke();
            Advance(choice.nextNodeId);
        }

        /// <summary>
        /// 結束當前對話。
        /// </summary>
        public void EndDialogue()
        {
            if (!IsRunning) return;
            IsRunning = false;

            if (_activeNodeCoroutine != null)
            {
                StopCoroutine(_activeNodeCoroutine);
                _activeNodeCoroutine = null;
            }
            
            if (_lastNode != null)
            {
                TriggerOnExit(_lastNode);
                _lastNode = null;
            }

            uiManager.SetPanelVisibility(false);

            onDialogueEnded?.Invoke();
            graph?.onDialogueEnded?.Invoke();
            Debug.Log("對話控制器：對話已結束。");
        }
        
        /// <summary>
        /// 設定當前節點 ID（主要由節點處理邏輯呼叫）。
        /// </summary>
        public void SetCurrentNodeId(string nodeId)
        {
            _currentNodeId = nodeId;
        }

        /// <summary>
        /// 將一個節點 ID 推入執行堆疊，用於稍後返回。
        /// </summary>
        /// <param name="nodeId">要推入的節點 ID。</param>
        public void PushToExecutionStack(string nodeId)
        {
            if (!string.IsNullOrEmpty(nodeId))
            {
                _executionStack.Push(nodeId);
            }
        }

        /// <summary>
        /// 應用一組變數變更。
        /// </summary>
        private void ApplyVariableChanges(List<VariableChange> changes)
        {
            if (changes == null) return;
            foreach (var change in changes)
            {
                if (string.IsNullOrEmpty(change.variableName)) continue;

                switch (change.type)
                {
                    case VariableChange.VarType.Int:
                        if (globalState != null && globalState.HasInt(change.variableName))
                            globalState.AddInt(change.variableName, change.intDelta);
                        else
                            _localState.AddInt(change.variableName, change.intDelta);
                        break;
                    case VariableChange.VarType.Bool:
                        if (globalState != null && globalState.HasBool(change.variableName))
                        {
                            if (change.setBool) globalState.SetBool(change.variableName, change.boolValue);
                            else globalState.ToggleBool(change.variableName);
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
        
        /// <summary>
        /// 格式化字串，將其中的變數佔位符（例如 {PlayerName}）替換為實際的變數值。
        /// </summary>
        /// <param name="text">要格式化的原始字串。</param>
        /// <returns>格式化後的字串。</returns>
        public string FormatString(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return Regex.Replace(text, @"\{(\w+)\}", match =>
            {
                string varName = match.Groups[1].Value;
                
                // 優先從本地狀態查找
                if (_localState.HasString(varName)) return _localState.GetString(varName);
                if (globalState != null && globalState.HasString(varName)) return globalState.GetString(varName);
                
                if (_localState.HasInt(varName)) return _localState.GetInt(varName).ToString();
                if (globalState != null && globalState.HasInt(varName)) return globalState.GetInt(varName).ToString();

                if (_localState.HasBool(varName)) return _localState.GetBool(varName).ToString();
                if (globalState != null && globalState.HasBool(varName)) return globalState.GetBool(varName).ToString();

                // 如果找不到變數，則返回原始的佔位符
                return match.Value;
            });
        }
    }
}
