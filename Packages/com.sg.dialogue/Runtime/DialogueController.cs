using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SG.Dialogue.Core.Instructions;
using SG.Dialogue.Nodes;
using SG.Dialogue.Presentation;
using SG.Dialogue.UI;
using SG.Dialogue.Variables;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue
{
    /// <summary>
    /// 對話系統的核心控制器。
    /// 負責管理對話的執行流程、狀態（變數）、以及與 DialogueUIManager 和 DialogueVisualManager 的協調。
    /// </summary>
    [RequireComponent(typeof(DialogueUIManager), typeof(DialogueVisualManager))]
    public class DialogueController : MonoBehaviour
    {
        [Header("圖表與狀態")]
        [Tooltip("要執行的對話圖資源。")]
        [SerializeField] private DialogueGraph graph;
        [Tooltip("用於存儲跨對話場景的全局變數。")]
        [SerializeField] private DialogueStateAsset globalState;

        [Header("除錯功能")]
        [Tooltip("啟用後，每個執行中的對話節點都會在主控台打印日誌，方便除錯。")]
        [SerializeField] private bool debugLoggingEnabled = false;

        [Header("管理器")]
        [SerializeField] private DialogueUIManager uiManager;
        /// <summary>
        /// 對話 UI 管理器的引用。
        /// </summary>
        public DialogueUIManager UiManager => uiManager;

        [SerializeField] private DialogueVisualManager visualManager;
        /// <summary>
        /// 對話視覺效果管理器的引用。
        /// </summary>
        public DialogueVisualManager VisualManager => visualManager;
        
        [SerializeField] private DialogueCameraController cameraController;
        /// <summary>
        /// 對話相機控制器的引用。
        /// </summary>
        public DialogueCameraController CameraController => cameraController;

        [Header("事件")]
        [Tooltip("對話開始時觸發的事件。")]
        public UnityEvent onDialogueStarted;
        [Tooltip("對話結束時觸發的事件。")]
        public UnityEvent onDialogueEnded;

        /// <summary>
        /// 標記對話是否正在執行中。
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// 當前正在處理的節點 ID。
        /// </summary>
        public string CurrentNodeId => _currentNodeId;
        /// <summary>
        /// 當前正在執行的對話圖。
        /// </summary>
        public DialogueGraph CurrentGraph => graph;

        private readonly DialogueState _localState = new DialogueState();
        /// <summary>
        /// 本地對話狀態，用於存儲僅在當前對話中有效的變數。
        /// </summary>
        public DialogueState LocalState => _localState;
        /// <summary>
        /// 全局對話狀態資源的引用。
        /// </summary>
        public DialogueStateAsset GlobalState => globalState;

        private string _currentNodeId;
        private DialogueNodeBase _lastNode;
        private Coroutine _activeNodeCoroutine;
        
        // 執行堆疊，用於處理子對話或跳轉後的返回邏輯。
        private readonly Stack<string> _executionStack = new Stack<string>();

        /// <summary>
        /// 提供一個 MonoBehaviour 實例，用於啟動協程。
        /// </summary>
        public MonoBehaviour CoroutineRunner => this;

        /// <summary>
        /// 獲取當前對話圖的自動前進延遲時間。
        /// </summary>
        public float AutoAdvanceDelay => graph != null ? graph.defaultAutoAdvanceDelay : 0f;

        private void Awake()
        {
            if (uiManager == null) uiManager = GetComponent<DialogueUIManager>();
            if (visualManager == null) visualManager = GetComponent<DialogueVisualManager>();
            
            // 在正式版本中自動禁用除錯日誌
            #if !UNITY_EDITOR
            debugLoggingEnabled = false;
            #endif
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
        /// 使用指定的對話圖開始一段新的對話。
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
                throw new ArgumentNullException(nameof(newGraph), "newGraph.startNodeId 必須要指定起始節點 ID");
            }
            
            graph = newGraph;
            graph.BuildLookup(); // 建立節點查找表以提高性能

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
        /// 使用 Inspector 中設定的對話圖開始對話。
        /// </summary>
        public void StartDialogue()
        {
            if (graph == null) { Debug.LogError("對話控制器：對話圖為空。"); return; }
            StartDialogue(graph);
        }

        /// <summary>
        /// 核心流程控制方法，前進到下一個指定的節點。
        /// </summary>
        /// <param name="nextNodeId">下一個節點的 ID。</param>
        private void Advance(string nextNodeId)
        {
            if (!IsRunning) return;

            // 停止上一個節點的協程
            if (_activeNodeCoroutine != null)
            {
                StopCoroutine(_activeNodeCoroutine);
                _activeNodeCoroutine = null;
            }

            // 觸發上一個節點的退出事件
            if (_lastNode != null)
            {
                TriggerOnExit(_lastNode);
            }

            // 尋找下一個可執行的節點，自動跳過被停用的節點
            string nodeIdToProcess = FindNextProcessableNodeId(nextNodeId);

            if (string.IsNullOrEmpty(nodeIdToProcess))
            {
                EndDialogue(); // 如果找不到下一個節點，則結束對話
                return;
            }

            _currentNodeId = nodeIdToProcess;
            var node = graph.GetNode(_currentNodeId);
            _lastNode = node;

            if (node != null)
            {
                if (debugLoggingEnabled)
                {
                    Debug.Log($"[對話除錯] 執行節點: {node.GetType().Name} (ID: {node.nodeId})");
                }

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
        /// 尋找下一個可執行的節點 ID，會自動跳過被停用的節點。
        /// </summary>
        /// <param name="startNodeId">開始搜索的節點 ID。</param>
        /// <returns>第一個可執行的節點 ID，如果找不到則返回 null。</returns>
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
                    return currentNodeId; // 找到可執行的節點
                }

                Debug.Log($"[對話] 跳過已停用的節點：{currentNodeId}");
                currentNodeId = node.GetNextNodeId(); // 繼續尋找下一個
            }

            if (safetyBreak <= 0)
            {
                Debug.LogError("在尋找可處理節點時檢測到無限迴圈。中止對話。");
                return null;
            }
            
            // 如果主路徑找不到，嘗試從執行堆疊中彈出返回地址
            if (_executionStack.Count > 0)
            {
                return FindNextProcessableNodeId(_executionStack.Pop());
            }

            return null;
        }

        /// <summary>
        /// 處理單一節點的協程。根據節點類型執行相應的操作。
        /// </summary>
        /// <param name="node">要處理的節點。</param>
        private IEnumerator ProcessNodeCoroutine(DialogueNodeBase node)
        {
            // 特殊節點的直接處理
            if (node is AnimationNode animNode)
            {
                yield return StartCoroutine(visualManager.PlayAnimations(animNode));
            }
            else if (node is CharacterActionNode charActionNode)
            {
                yield return StartCoroutine(visualManager.UpdateFromCharacterActionNode(charActionNode));
            }
            else if (node is SetBackgroundNode bgNode)
            {
                yield return StartCoroutine(visualManager.UpdateFromSetBackgroundNode(bgNode));
            }
            else if (node is FlickerEffectNode flickerNode)
            {
                yield return StartCoroutine(visualManager.ExecuteFlickerEffect(flickerNode));
            }
            else
            {
                // 通用節點處理邏輯
                var instructionEnumerator = node.Process(this);
                while (instructionEnumerator.MoveNext())
                {
                    var instruction = instructionEnumerator.Current;

                    if (instruction is AdvanceToNode advance)
                    {
                        Advance(advance.NextNodeId);
                        yield break; // 流程已轉移，結束當前協程
                    }
                    else if (instruction is WaitForUserInput)
                    {
                        yield break; // 等待用戶輸入，暫停執行
                    }
                    else if (instruction is EndDialogue)
                    {
                        EndDialogue();
                        yield break; // 結束對話
                    }
                    else if (instruction != null)
                    {
                        yield return instruction; // 執行其他協程指令 (如 WaitForSeconds)
                    }
                }
            }

            // 節點處理完畢後，自動前進到預設的下一個節點
            string defaultNextId = node.GetNextNodeId();
            Advance(defaultNextId);
        }

        /// <summary>
        /// 獲取一個對話分支的協程迭代器，用於並行執行分支邏輯。
        /// </summary>
        /// <param name="startNodeId">分支的起始節點 ID。</param>
        /// <returns>用於執行分支的 IEnumerator。</returns>
        public IEnumerator GetBranchEnumerator(string startNodeId)
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

                if (debugLoggingEnabled)
                {
                    Debug.Log($"[對話除錯] 執行分支節點: {node.GetType().Name} (ID: {node.nodeId})");
                }

                var instructionEnumerator = node.Process(this);
                while (instructionEnumerator.MoveNext())
                {
                    var instruction = instructionEnumerator.Current;
                    // 在分支中，AdvanceToNode 或 EndDialogue 指令會被忽略，以防止干擾主流程
                    if (instruction is AdvanceToNode || instruction is EndDialogue)
                    {
                        Debug.LogWarning($"[對話除錯] 分支節點 {node.GetType().Name} (ID: {node.nodeId}) 嘗試發出全局流程控制指令 ({instruction.GetType().Name})，但在並行分支中被消化。");
                        yield break; // 結束當前分支的執行
                    }
                    else if (instruction != null)
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
            // ... 其他節點類型
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
            // ... 其他節點類型
            graph?.onNodeEntered?.Invoke(node.nodeId);
        }

        /// <summary>
        /// 處理來自 UI 的「請求前進」事件（例如點擊對話框）。
        /// </summary>
        private void OnAdvanceRequested()
        {
            if (!IsRunning) return;

            // 如果文字還在打字效果中，則立即完成打字
            if (uiManager.IsTyping)
            {
                uiManager.CompleteTyping();
                return;
            }

            // 如果當前是文本節點，則前進到下一個節點
            if (_lastNode is TextNode textNode)
            {
                Advance(textNode.nextNodeId);
            }
        }

        /// <summary>
        /// 處理來自 UI 的「選擇選項」事件。
        /// </summary>
        private void OnChoiceSelected(DialogueChoice choice)
        {
            ApplyVariableChanges(choice.variableChanges);
            choice.onSelected?.Invoke();
            Advance(choice.nextNodeId);
        }

        /// <summary>
        /// 結束當前的對話。
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
        /// （主要供節點內部使用）設定當前節點 ID。
        /// </summary>
        public void SetCurrentNodeId(string nodeId)
        {
            _currentNodeId = nodeId;
        }

        /// <summary>
        /// （主要供節點內部使用）將一個節點 ID 推入執行堆疊，用於後續返回。
        /// </summary>
        public void PushToExecutionStack(string nodeId)
        {
            if (!string.IsNullOrEmpty(nodeId))
            {
                _executionStack.Push(nodeId);
            }
        }

        /// <summary>
        /// 應用節點或選項中定義的變數變更。
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
                        // 優先修改全局變數
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
        /// 格式化文本，將文本中的 {變數名} 替換為其實際值。
        /// </summary>
        /// <param name="text">要格式化的原始文本。</param>
        /// <returns>格式化後的文本。</returns>
        public string FormatString(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // 使用正則表達式查找 {variable} 格式的佔位符
            return Regex.Replace(text, @"\{(\w+)\}", match =>
            {
                string varName = match.Groups[1].Value;
                
                // 依序從本地和全局狀態中查找變數值
                if (_localState.HasString(varName)) return _localState.GetString(varName);
                if (globalState != null && globalState.HasString(varName)) return globalState.GetString(varName);
                
                if (_localState.HasInt(varName)) return _localState.GetInt(varName).ToString();
                if (globalState != null && globalState.HasInt(varName)) return globalState.GetInt(varName).ToString();

                if (_localState.HasBool(varName)) return _localState.GetBool(varName).ToString();
                if (globalState != null && globalState.HasBool(varName)) return globalState.GetBool(varName).ToString();

                // 如果找不到變數，則返回原始匹配項
                return match.Value;
            });
        }
    }
}
