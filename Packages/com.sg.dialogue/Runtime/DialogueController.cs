using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Core.Instructions;
using SG.Dialogue.Nodes;
using SG.Dialogue.Presentation;
using SG.Dialogue.UI;
using SG.Dialogue.VariableResolver;
using SG.Dialogue.Variables;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue
{
    /// <summary>
    /// 定義 DialogueController 應如何處理自動前進功能。
    /// </summary>
    public enum AutoAdvanceMode
    {
        Default,            // 使用對話圖的預設設定
        ForceEnable,        // 強制啟用自動前進
        ForceDisable        // 強制停用自動前進
    }

    /// <summary>
    /// 對話系統的核心控制器，負責驅動對話流程、管理狀態和與各個子系統（UI、視覺）互動。
    /// </summary>
    [RequireComponent(typeof(DialogueUIManager), typeof(DialogueVisualManager))]
    public class DialogueController : MonoBehaviour
    {
        // --- 靜態編譯的 Regex，避免每次 FormatString 都重新編譯 ---
        private static readonly Regex VariablePattern =
            new Regex(@"\{(\w+)\}", RegexOptions.Compiled);
        [Header("圖表與狀態")]
        [Tooltip("要執行的對話圖 ScriptableObject。")]
        [SerializeField] private DialogueGraph graph;
        [Tooltip("全域對話狀態，可在不同對話間共享。")]
        [SerializeField] private DialogueStateAsset globalState;

        [Header("流程控制覆寫")]
        [Tooltip("覆寫對話圖的自動前進設定。")]
        public AutoAdvanceMode autoAdvanceOverride = AutoAdvanceMode.Default;
        [Tooltip("當強制啟用自動前進時，使用的延遲時間。")]
        public float forcedAutoAdvanceDelay = 1.5f;

        [Header("除錯功能")]
        [Tooltip("是否在 Console 中啟用詳細的除錯日誌。")]
        [SerializeField] private bool debugLoggingEnabled = false;

        [Header("管理器")]
        [Tooltip("UI 管理器，負責處理所有 UI 相關的顯示與互動。")]
        [SerializeField] private DialogueUIManager uiManager;
        /// <summary>UI 管理器，負責處理所有 UI 相關的顯示與互動。</summary>
        public DialogueUIManager UiManager => uiManager;

        [Tooltip("視覺管理器，負責處理角色立繪、背景等視覺元素。")]
        [SerializeField] private DialogueVisualManager visualManager;
        /// <summary>視覺管理器，負責處理角色立繪、背景等視覺元素。</summary>
        public DialogueVisualManager VisualManager => visualManager;
        
        [Tooltip("攝影機控制器，用於處理對話中的鏡頭切換或動畫。")]
        [SerializeField] private DialogueCameraController cameraController;
        /// <summary>攝影機控制器，用於處理對話中的鏡頭切換或動畫。</summary>
        public DialogueCameraController CameraController => cameraController;
        
        [Tooltip("玩家資料解析器，用於將遊戲中的變數（如玩家名稱）注入到對話文本中。")]
        [SerializeField] private DialoguePlayerDataResolver playerDataResolver;

        #region 對外公開事件
        /// <summary>當對話開始時觸發。</summary>
        public event UnityAction onDialogueStarted;
        /// <summary>當對話結束時觸發。</summary>
        public event UnityAction onDialogueEnded;
        /// <summary>當使用者請求跳過對話時觸發。</summary>
        public event UnityAction onSkipRequested;
        #endregion
        
        /// <summary>
        /// 當 FormatString 無法從內部狀態解析變數時觸發。
        /// 允許外部系統提供變數值。
        /// Func<string, string>: 輸入參數是變數名稱，返回參數是解析後的值。如果無法解析，應返回 null。
        /// </summary>
        public event Func<string, string> OnResolveVariable;
        
        /// <summary>對話是否正在進行中。</summary>
        public bool IsRunning { get; private set; }
        /// <summary>目前正在執行的節點 ID。</summary>
        public string CurrentNodeId => _currentNodeId;
        /// <summary>目前使用的對話圖。</summary>
        public DialogueGraph CurrentGraph => graph;
        
        /// <summary>本地對話狀態，只在當前對話期間有效。</summary>
        public DialogueState LocalState => _localState;
        /// <summary>全域對話狀態，可在不同對話間共享。</summary>
        public DialogueStateAsset GlobalState => globalState;
        
        /// <summary>提供給節點使用的協程運行器。</summary>
        public MonoBehaviour CoroutineRunner => this;
        /// <summary>獲取當前對話圖的預設自動前進延遲時間。</summary>
        public float AutoAdvanceDelay => graph != null ? graph.defaultAutoAdvanceDelay : 0f;

        /// <summary>獲取最近執行的節點 ID 歷史記錄 (最多保留 50 筆)。</summary>
        public IReadOnlyList<string> ExecutionHistory => _executionHistory;

        private readonly DialogueState _localState = new DialogueState();
        private readonly List<string> _executionHistory = new List<string>();
        private string _currentNodeId;
        private DialogueNodeBase _lastNode;
        private WaitForAll _activeWaitForAll;
        private readonly Stack<string> _executionStack = new Stack<string>();
        private UniTaskCompletionSource _inputCompletionSource;
        private CancellationTokenSource _dialogueCts;

        private void Awake()
        {
            if (uiManager == null) uiManager = GetComponent<DialogueUIManager>();
            if (visualManager == null) visualManager = GetComponent<DialogueVisualManager>();
            if (playerDataResolver == null) playerDataResolver = GetComponent<DialoguePlayerDataResolver>();
            if (cameraController == null) cameraController = GetComponent<DialogueCameraController>();
            
            #if !UNITY_EDITOR
            debugLoggingEnabled = false;
            #endif
        }

        private void OnEnable()
        {
            uiManager.OnAdvanceRequested += OnAdvanceRequested;
            uiManager.OnChoiceSelected += OnChoiceSelected;
            uiManager.OnTypingCompleted += OnTypingCompleted;
            uiManager.OnSkipRequested += OnSkipRequested;
        }

        private void OnDisable()
        {
            uiManager.OnAdvanceRequested -= OnAdvanceRequested;
            uiManager.OnChoiceSelected -= OnChoiceSelected;
            uiManager.OnTypingCompleted -= OnTypingCompleted;
            uiManager.OnSkipRequested -= OnSkipRequested;
            
            // 清理等待中的 Task，避免在物件禁用後還持有引用
            _inputCompletionSource?.TrySetCanceled();
            _inputCompletionSource = null;
        }

        /// <summary>
        /// 開始一段新的對話。如果已有對話正在運行，會先將其結束。
        /// </summary>
        /// <param name="newGraph">要開始的對話圖。</param>
        /// <param name="playerDataVariables">用於在運行時替換文本變數的字典。</param>
        /// <exception cref="ArgumentNullException">如果 newGraph 的 startNodeId 為空或未指定。</exception>
        public void StartDialogue(DialogueGraph newGraph, Dictionary<string, string> playerDataVariables=null)
        {
            if (IsRunning)
            {
                EndDialogue();
            }

            if (string.IsNullOrEmpty(newGraph.startNodeId))
            {
                throw new ArgumentNullException(nameof(newGraph), "newGraph.startNodeId must be specified.");
            }

            graph = newGraph;
            graph.BuildLookup(); // 建立節點查找表以提高效能

            // 建立新的 CancellationTokenSource 用於取消對話中的非同步操作
            _dialogueCts?.Cancel();
            _dialogueCts?.Dispose();
            _dialogueCts = new CancellationTokenSource();

            _localState.Clear();
            _executionStack.Clear();
            _executionHistory.Clear();

            if (playerDataVariables != null)
            {
                // 方法1: 將變數加入到 LocalState 作為字串變數（直接解析，不需要額外組件）
                foreach (var entry in playerDataVariables.ToList())
                {
                    _localState.SetString(entry.Key, entry.Value);
                }
                
                // 方法2: 如果有 DialoguePlayerDataResolver，也將變數加入其中（供外部事件解析使用）
                if (playerDataResolver != null)
                {
                    foreach (var entry in playerDataVariables.ToList())
                    {
                        playerDataResolver.AddDataMapping(entry.Key, entry.Value);
                    }
                }
                else
                {
                    if (debugLoggingEnabled)
                    {
                        Debug.Log("DialogueController: playerDataResolver is null. Variables will be resolved through LocalState instead.");
                    }
                }
            }
            
            uiManager.SetPanelVisibility(true);
            uiManager.SetSkipButtonVisibility(graph.IsSkippable);
            
            IsRunning = true;
            onDialogueStarted?.Invoke();
            graph?.onDialogueStarted?.Invoke();

            Advance(graph.startNodeId);
        }

        /// <summary>
        /// 使用目前已設定的對話圖開始對話。
        /// </summary>
        public void StartDialogue()
        {
            if (graph == null) { Debug.LogError("DialogueController: DialogueGraph is null."); return; }
            StartDialogue(graph);
        }
        
        /// <summary>
        /// 使用目前已設定的對話圖開始對話，並提供玩家資料變數。
        /// </summary>
        /// <param name="playerDataVariables">用於在運行時替換文本變數的字典。</param>
        public void StartDialogue(Dictionary<string, string> playerDataVariables)
        {
            if (graph == null) { Debug.LogError("DialogueController: DialogueGraph is null."); return; }
            StartDialogue(graph, playerDataVariables);
        }

        /// <summary>
        /// 推進對話到下一個節點。
        /// </summary>
        /// <param name="nextNodeId">下一個節點的 ID。</param>
        private void Advance(string nextNodeId)
        {
            if (!IsRunning) return;

            // 觸發上一個節點的 OnExit 事件
            if (_lastNode != null)
            {
                TriggerOnExit(_lastNode);
            }

            // 尋找下一個可執行的節點（跳過被禁用的節點）
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
                if (debugLoggingEnabled)
                {
                    Debug.Log($"[Dialogue Debug] Executing node: {node.GetType().Name} (ID: {node.nodeId})");
                }

                // 記錄執行歷程 (只保留最近 50 筆)
                _executionHistory.Add(node.nodeId);
                if (_executionHistory.Count > 50)
                {
                    _executionHistory.RemoveAt(0);
                }

                TriggerOnEnterAndVariableChanges(node);
                ProcessNode(node).Forget(); // 非同步處理節點邏輯
            }
            else
            {
                Debug.LogWarning($"DialogueController: Node with ID '{_currentNodeId}' not found. Ending dialogue.");
                EndDialogue();
            }
        }

        /// <summary>
        /// 從指定 ID 開始，尋找下一個 IsEnabled 為 true 的節點。
        /// </summary>
        /// <param name="startNodeId">開始尋找的節點 ID。</param>
        /// <returns>下一個可執行節點的 ID；如果找不到，則返回 null。</returns>
        private string FindNextProcessableNodeId(string startNodeId)
        {
            string currentNodeId = startNodeId;
            int safetyBreak = 100; // 防止無限迴圈

            while (!string.IsNullOrEmpty(currentNodeId) && safetyBreak-- > 0)
            {
                var node = graph.GetNode(currentNodeId);
                if (node == null)
                {
                    Debug.LogWarning($"Node with ID '{currentNodeId}' not found in the graph.");
                    return null;
                }

                if (node.IsEnabled)
                {
                    return currentNodeId; // 找到可執行的節點
                }

                if(debugLoggingEnabled) Debug.Log($"[Dialogue] Skipping disabled node: {currentNodeId}");
                currentNodeId = node.GetNextNodeId(); // 繼續尋找下一個
            }

            if (safetyBreak <= 0)
            {
                Debug.LogError("Infinite loop detected while finding a processable node. Aborting dialogue.");
                return null;
            }
            
            // 如果線性流程結束，嘗試從堆疊中彈出節點（用於從子圖返回）
            if (_executionStack.Count > 0)
            {
                return FindNextProcessableNodeId(_executionStack.Pop());
            }

            return null;
        }

        /// <summary>
        /// 處理單一節點的邏輯。統一呼叫節點的 Process 方法。
        /// </summary>
        private async UniTaskVoid ProcessNode(DialogueNodeBase node)
        {
            // 所有節點統一透過 Process 方法執行邏輯，傳遞 CancellationToken
            await node.Process(this, _dialogueCts?.Token ?? CancellationToken.None);

            // 節點處理完畢後，自動前進到下一個節點
            string defaultNextId = node.GetNextNodeId();
            Advance(defaultNextId);
        }

        /// <summary>
        /// 執行一個並行分支的邏輯。
        /// </summary>
        public async UniTask GetBranchEnumerator(string startNodeId, Action onInputSwallowed)
        {
            string currentBranchNodeId = startNodeId;
            while (!string.IsNullOrEmpty(currentBranchNodeId))
            {
                var node = graph.GetNode(currentBranchNodeId);
                if (node == null)
                {
                    Debug.LogWarning($"Branch execution: Node '{currentBranchNodeId}' not found. Branch terminated.");
                    return;
                }

                if (!node.IsEnabled)
                {
                    if(debugLoggingEnabled) Debug.Log($"[Dialogue] Skipping disabled node in branch: {currentBranchNodeId}");
                    currentBranchNodeId = node.GetNextNodeId();
                    continue;
                }

                if (debugLoggingEnabled)
                {
                    Debug.Log($"[Dialogue Debug] Executing branch node: {node.GetType().Name} (ID: {node.nodeId})");
                }

                await node.Process(this, _dialogueCts?.Token ?? CancellationToken.None);

                currentBranchNodeId = node.GetNextNodeId();
            }
        }
        
        /// <summary>
        /// 觸發節點的 OnExit 事件和圖的全域 onNodeExited 事件。
        /// </summary>
        private void TriggerOnExit(DialogueNodeBase node)
        {
            node.OnExit(this);
            graph?.onNodeExited?.Invoke(node.nodeId);
        }

        /// <summary>
        /// 觸發節點的 OnEnter 事件、變數變更和圖的全域 onNodeEntered 事件。
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
            graph?.onNodeEntered?.Invoke(node.nodeId);
        }

        /// <summary>
        /// 處理來自 UI 的前進請求。
        /// </summary>
        private void OnAdvanceRequested()
        {
            if (!IsRunning) return;

            // 優先處理對話框文字的打字機效果
            if (uiManager.IsTyping)
            {
                uiManager.CompleteTyping();
                return;
            }
            
            // 其次處理中央舞台文字的打字機效果
            if (visualManager.IsStageTextTyping())
            {
                if (_lastNode is StageTextNode stageNode && !stageNode.allowFastForward)
                {
                    return;
                }

                visualManager.CompleteStageTextTyping();
                return;
            }

            // 如果正在等待多個並行任務，則強制完成
            if (_activeWaitForAll != null)
            {
                _activeWaitForAll.ForceComplete();
                return;
            }
            
            // 如果有節點正在等待使用者輸入，則完成該等待
            if (_inputCompletionSource != null)
            {
                _inputCompletionSource.TrySetResult();
                _inputCompletionSource = null;
            }
        }

        /// <summary>
        /// 等待使用者輸入（例如點擊）。這會暫停節點的 Process 流程。
        /// </summary>
        public async UniTask WaitForInputAsync()
        {
            if (_inputCompletionSource != null)
            {
                _inputCompletionSource.TrySetCanceled();
            }

            _inputCompletionSource = new UniTaskCompletionSource();
            await _inputCompletionSource.Task;
        }

        /// <summary>
        /// 處理選項被選擇的事件。
        /// </summary>
        private void OnChoiceSelected(DialogueChoice choice)
        {
            ApplyVariableChanges(choice.variableChanges);
            choice.onSelected?.Invoke();
            Advance(choice.nextNodeId);
        }

        private void OnTypingCompleted()
        {
            // 目前未使用，但保留以備將來擴充
        }
        
        private void OnSkipRequested()
        {
            onSkipRequested?.Invoke();
        }

        /// <summary>
        /// 結束當前的對話。
        /// </summary>
        public void EndDialogue()
        {
            if (!IsRunning) return;
            IsRunning = false;

            // 取消所有進行中的非同步操作
            _dialogueCts?.Cancel();
            _dialogueCts?.Dispose();
            _dialogueCts = null;

            // 清理所有可能正在等待的任務
            _activeWaitForAll?.ForceComplete();
            _activeWaitForAll = null;

            _inputCompletionSource?.TrySetCanceled();
            _inputCompletionSource = null;

            if (_lastNode != null)
            {
                TriggerOnExit(_lastNode);
                _lastNode = null;
            }

            uiManager.SetPanelVisibility(false);

            onDialogueEnded?.Invoke();
            graph?.onDialogueEnded?.Invoke();
            if(debugLoggingEnabled) Debug.Log("DialogueController: Dialogue has ended.");
        }
        
        /// <summary>
        /// （主要由子圖使用）設定當前的節點 ID。
        /// </summary>
        public void SetCurrentNodeId(string nodeId)
        {
            _currentNodeId = nodeId;
        }

        /// <summary>
        /// 將一個節點 ID 推入執行堆疊，用於從子圖返回時恢復流程。
        /// </summary>
        public void PushToExecutionStack(string nodeId)
        {
            if (!string.IsNullOrEmpty(nodeId))
            {
                _executionStack.Push(nodeId);
            }
        }

        /// <summary>
        /// 應用一批變數變更。
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
        /// 格式化包含變數的字串，將 {variableName} 替換為實際值。
        /// </summary>
        /// <param name="text">要格式化的原始文本。</param>
        /// <returns>格式化後的文本。</returns>
        public string FormatString(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // 使用預編譯的靜態 Regex 匹配 {variable} 格式
            return VariablePattern.Replace(text, match =>
            {
                string varName = match.Groups[1].Value;
                
                // 查找順序: Local State -> Global State -> 外部解析器
                if (_localState.HasString(varName)) return _localState.GetString(varName);
                if (globalState != null && globalState.HasString(varName)) return globalState.GetString(varName);
                
                if (_localState.HasInt(varName)) return _localState.GetInt(varName).ToString();
                if (globalState != null && globalState.HasInt(varName)) return globalState.GetInt(varName).ToString();

                if (_localState.HasBool(varName)) return _localState.GetBool(varName).ToString();
                if (globalState != null && globalState.HasBool(varName)) return globalState.GetBool(varName).ToString();

                // 如果內部找不到，觸發外部事件讓其他系統解析
                if (OnResolveVariable != null)
                {
                    foreach (Func<string, string> resolver in OnResolveVariable.GetInvocationList())
                    {
                        string result = resolver(varName);
                        if (result != null)
                        {
                            return result; // 一旦有監聽者成功解析，就返回結果
                        }
                    }
                }

                // 如果都找不到，返回原始匹配的字串 (例如 "{PlayerName}")
                return match.Value;
            });
        }
    }
}
