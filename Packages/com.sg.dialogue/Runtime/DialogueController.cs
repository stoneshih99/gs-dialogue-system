using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

    [RequireComponent(typeof(DialogueUIManager), typeof(DialogueVisualManager))]
    public class DialogueController : MonoBehaviour
    {
        [Header("圖表與狀態")]
        [SerializeField] private DialogueGraph graph;
        [SerializeField] private DialogueStateAsset globalState;

        [Header("流程控制覆寫")]
        [Tooltip("覆寫對話圖的自動前進設定")]
        public AutoAdvanceMode autoAdvanceOverride = AutoAdvanceMode.Default;
        [Tooltip("當強制啟用自動前進時，使用的延遲時間")]
        public float forcedAutoAdvanceDelay = 1.5f;

        [Header("除錯功能")]
        [SerializeField] private bool debugLoggingEnabled = false;

        [Header("管理器")]
        [SerializeField] private DialogueUIManager uiManager;
        public DialogueUIManager UiManager => uiManager;

        [SerializeField] private DialogueVisualManager visualManager;
        public DialogueVisualManager VisualManager => visualManager;
        
        [SerializeField] private DialogueCameraController cameraController;
        public DialogueCameraController CameraController => cameraController;
        
        [SerializeField] private DialoguePlayerDataResolver playerDataResolver;

        public event UnityAction onDialogueStarted;
        public event UnityAction onDialogueEnded;
        public event UnityAction onSkipRequested;

        /// <summary>
        /// 當 FormatString 無法從內部狀態解析變數時觸發。
        /// 允許外部系統提供變數值。
        /// Func<string, string>: 輸入參數是變數名稱，返回參數是解析後的值。如果無法解析，應返回 null。
        /// </summary>
        public event Func<string, string> OnResolveVariable;

        public bool IsRunning { get; private set; }
        public string CurrentNodeId => _currentNodeId;
        public DialogueGraph CurrentGraph => graph;

        private readonly DialogueState _localState = new DialogueState();
        public DialogueState LocalState => _localState;
        public DialogueStateAsset GlobalState => globalState;

        private string _currentNodeId;
        private DialogueNodeBase _lastNode;
        private WaitForAll _activeWaitForAll;

        private readonly Stack<string> _executionStack = new Stack<string>();
        
        // 用於等待輸入的 TaskCompletionSource
        private UniTaskCompletionSource _inputCompletionSource;

        public MonoBehaviour CoroutineRunner => this;
        public float AutoAdvanceDelay => graph != null ? graph.defaultAutoAdvanceDelay : 0f;

        private void Awake()
        {
            if (uiManager == null) uiManager = GetComponent<DialogueUIManager>();
            if (visualManager == null) visualManager = GetComponent<DialogueVisualManager>();
            
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
            
            // 清理等待中的 Task
            _inputCompletionSource?.TrySetCanceled();
            _inputCompletionSource = null;
        }

        /// <summary>
        /// 開始一段新的對話。 
        /// </summary>
        /// <param name="newGraph"></param>
        /// <param name="playerDataVariables">在 runtime 中取代字</param>
        /// <exception cref="ArgumentNullException"></exception>
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

            if (playerDataVariables != null)
            {
                foreach (var entry in playerDataVariables.ToList())
                {
                    playerDataResolver.AddDataMapping(entry.Key, entry.Value);
                }
            }
            
            graph = newGraph;
            graph.BuildLookup();

            _localState.Clear();
            _executionStack.Clear();
            
            uiManager.SetPanelVisibility(true);
            uiManager.SetSkipButtonVisibility(graph.IsSkippable);
            
            IsRunning = true;
            onDialogueStarted?.Invoke();
            graph?.onDialogueStarted?.Invoke();

            Advance(graph.startNodeId);
        }

        public void StartDialogue()
        {
            if (graph == null) { Debug.LogError("DialogueController: DialogueGraph is null."); return; }
            StartDialogue(graph);
        }
        
        /// <summary>
        /// 開始一段新的對話，並提供玩家資料變數。
        /// </summary>
        /// <param name="playerDataVariables"></param>
        public void StartDialogue(Dictionary<string, string> playerDataVariables)
        {
            if (graph == null) { Debug.LogError("DialogueController: DialogueGraph is null."); return; }
            StartDialogue(graph, playerDataVariables);
        }

        private void Advance(string nextNodeId)
        {
            if (!IsRunning) return;

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
                if (debugLoggingEnabled)
                {
                    Debug.Log($"[Dialogue Debug] Executing node: {node.GetType().Name} (ID: {node.nodeId})");
                }

                TriggerOnEnterAndVariableChanges(node);
                ProcessNode(node).Forget();
            }
            else
            {
                Debug.LogWarning($"DialogueController: Node with ID '{_currentNodeId}' not found. Ending dialogue.");
                EndDialogue();
            }
        }

        private string FindNextProcessableNodeId(string startNodeId)
        {
            string currentNodeId = startNodeId;
            int safetyBreak = 100;

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
                    return currentNodeId;
                }

                Debug.Log($"[Dialogue] Skipping disabled node: {currentNodeId}");
                currentNodeId = node.GetNextNodeId();
            }

            if (safetyBreak <= 0)
            {
                Debug.LogError("Infinite loop detected while finding a processable node. Aborting dialogue.");
                return null;
            }
            
            if (_executionStack.Count > 0)
            {
                return FindNextProcessableNodeId(_executionStack.Pop());
            }

            return null;
        }

        private async UniTaskVoid ProcessNode(DialogueNodeBase node)
        {
            if (node is AnimationNode animNode)
            {
                await visualManager.PlayAnimations(animNode);
            }
            else if (node is CharacterActionNode charActionNode)
            {
                await visualManager.UpdateFromCharacterActionNode(charActionNode);
            }
            else if (node is SetBackgroundNode bgNode)
            {
                await visualManager.UpdateFromSetBackgroundNode(bgNode);
            }
            else if (node is FlickerEffectNode flickerNode)
            {
                await visualManager.ExecuteFlickerEffect(flickerNode);
            }
            else
            {
                await node.Process(this);
            }

            string defaultNextId = node.GetNextNodeId();
            Advance(defaultNextId);
        }

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
                    Debug.Log($"[Dialogue] Skipping disabled node in branch: {currentBranchNodeId}");
                    currentBranchNodeId = node.GetNextNodeId();
                    continue;
                }

                if (debugLoggingEnabled)
                {
                    Debug.Log($"[Dialogue Debug] Executing branch node: {node.GetType().Name} (ID: {node.nodeId})");
                }

                await node.Process(this);

                currentBranchNodeId = node.GetNextNodeId();
            }
        }
        
        private void TriggerOnExit(DialogueNodeBase node)
        {
            node.OnExit(this);
            graph?.onNodeExited?.Invoke(node.nodeId);
        }

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

        private void OnAdvanceRequested()
        {
            if (!IsRunning) return;

            if (uiManager.IsTyping)
            {
                uiManager.CompleteTyping();
                return;
            }

            if (_activeWaitForAll != null)
            {
                _activeWaitForAll.ForceComplete();
                return;
            }
            
            // 如果有正在等待輸入的 Task，則完成它
            if (_inputCompletionSource != null)
            {
                _inputCompletionSource.TrySetResult();
                _inputCompletionSource = null;
                return; // 這裡 return，因為 ProcessNode 會繼續執行並呼叫 Advance
            }

            // 只有在沒有等待輸入的情況下，才主動 Advance (例如點擊過快或異常狀態)
            // 但通常 ProcessNode 正在執行中，我們不應該這裡亂 Advance
            // 除非是某些非同步節點卡住了，或者舊邏輯需要保留
            // 為了相容舊邏輯（如果沒有使用 WaitForInputAsync 的節點），保留此檢查
            // 但要注意這可能會導致雙重 Advance
            
            // 暫時保留舊邏輯，但加上檢查
            if (_lastNode != null && _inputCompletionSource == null) 
            {
                // 注意：如果節點正在 Process 中且沒有等待輸入，這裡呼叫 Advance 可能會打斷它或造成並行
                // 但目前的架構是 ProcessNode 結束後會自動 Advance
                // 所以這裡的 Advance 主要是給那些 "沒有 await WaitForInput" 的舊節點用的？
                // 或者是在節點執行完畢後，等待玩家點擊才前進的情況？
                
                // 在新的架構下，節點應該自己負責等待。
                // 如果節點已經執行完畢 (ProcessNode 結束)，它會自動呼叫 Advance。
                // 所以這裡可能不需要做什麼，除非是為了處理 "點擊跳過" 的邏輯。
                
                // 為了安全起見，如果我們正在等待輸入，我們已經在上面 return 了。
                // 如果沒有等待輸入，表示可能是在打字中（已處理）或是在做其他事。
            }
        }

        /// <summary>
        /// 等待使用者輸入（點擊下一步）。
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

        private void OnChoiceSelected(DialogueChoice choice)
        {
            ApplyVariableChanges(choice.variableChanges);
            choice.onSelected?.Invoke();
            Advance(choice.nextNodeId);
        }

        private void OnTypingCompleted()
        {
        }
        
        private void OnSkipRequested()
        {
            EndDialogue();
        }

        public void EndDialogue()
        {
            if (!IsRunning) return;
            IsRunning = false;

            if (_activeWaitForAll != null)
            {
                _activeWaitForAll.ForceComplete();
                _activeWaitForAll = null;
            }
            
            if (_inputCompletionSource != null)
            {
                _inputCompletionSource.TrySetCanceled();
                _inputCompletionSource = null;
            }
            
            if (_lastNode != null)
            {
                TriggerOnExit(_lastNode);
                _lastNode = null;
            }

            uiManager.SetPanelVisibility(false);

            onDialogueEnded?.Invoke();
            graph?.onDialogueEnded?.Invoke();
            Debug.Log("DialogueController: Dialogue has ended.");
        }
        
        public void SetCurrentNodeId(string nodeId)
        {
            _currentNodeId = nodeId;
        }

        public void PushToExecutionStack(string nodeId)
        {
            if (!string.IsNullOrEmpty(nodeId))
            {
                _executionStack.Push(nodeId);
            }
        }

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
        
        public string FormatString(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return Regex.Replace(text, @"\{(\w+)\}", match =>
            {
                string varName = match.Groups[1].Value;
                
                // 1. 優先從內部狀態查找
                if (_localState.HasString(varName)) return _localState.GetString(varName);
                if (globalState != null && globalState.HasString(varName)) return globalState.GetString(varName);
                
                if (_localState.HasInt(varName)) return _localState.GetInt(varName).ToString();
                if (globalState != null && globalState.HasInt(varName)) return globalState.GetInt(varName).ToString();

                if (_localState.HasBool(varName)) return _localState.GetBool(varName).ToString();
                if (globalState != null && globalState.HasBool(varName)) return globalState.GetBool(varName).ToString();

                // 2. 如果內部找不到，觸發外部事件
                if (OnResolveVariable != null)
                {
                    // 遍歷所有監聽者
                    foreach (Func<string, string> resolver in OnResolveVariable.GetInvocationList())
                    {
                        string result = resolver(varName);
                        if (result != null)
                        {
                            return result; // 一旦有監聽者成功解析，就返回結果
                        }
                    }
                }

                // 3. 如果都找不到，返回原始匹配的字串 (例如 "{PlayerName}")
                return match.Value;
            });
        }
    }
}
