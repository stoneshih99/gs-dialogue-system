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
    /// 定義 DialogueController 應如何處理自動前進功能。
    /// </summary>
    public enum AutoAdvanceMode
    {
        /// <summary>
        /// 使用在 DialogueGraph 資產中定義的設定。
        /// </summary>
        Default,
        /// <summary>
        /// 強制啟用自動前進，覆寫圖表設定。
        /// </summary>
        ForceEnable,
        /// <summary>
        /// 強制停用自動前進，覆寫圖表設定。
        /// </summary>
        ForceDisable
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

        [Header("事件")]
        public UnityEvent onDialogueStarted;
        public UnityEvent onDialogueEnded;

        public bool IsRunning { get; private set; }
        public string CurrentNodeId => _currentNodeId;
        public DialogueGraph CurrentGraph => graph;

        private readonly DialogueState _localState = new DialogueState();
        public DialogueState LocalState => _localState;
        public DialogueStateAsset GlobalState => globalState;

        private string _currentNodeId;
        private DialogueNodeBase _lastNode;
        private Coroutine _activeNodeCoroutine;
        private WaitForAll _activeWaitForAll;

        private readonly Stack<string> _executionStack = new Stack<string>();

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
        }

        private void OnDisable()
        {
            uiManager.OnAdvanceRequested -= OnAdvanceRequested;
            uiManager.OnChoiceSelected -= OnChoiceSelected;
            uiManager.OnTypingCompleted -= OnTypingCompleted;
        }

        public void StartDialogue(DialogueGraph newGraph)
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
                if (debugLoggingEnabled)
                {
                    Debug.Log($"[Dialogue Debug] Executing node: {node.GetType().Name} (ID: {node.nodeId})");
                }

                TriggerOnEnterAndVariableChanges(node);
                _activeNodeCoroutine = StartCoroutine(ProcessNodeCoroutine(node));
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

        private IEnumerator ProcessNodeCoroutine(DialogueNodeBase node)
        {
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
                var instructionEnumerator = node.Process(this);
                while (instructionEnumerator.MoveNext())
                {
                    var instruction = instructionEnumerator.Current;

                    if (instruction is AdvanceToNode advance)
                    {
                        Advance(advance.NextNodeId);
                        yield break;
                    }
                    else if (instruction is WaitForUserInput)
                    {
                        yield break;
                    }
                    else if (instruction is EndDialogue)
                    {
                        EndDialogue();
                        yield break;
                    }
                    else if (instruction is WaitForAll waitForAll)
                    {
                        _activeWaitForAll = waitForAll;
                        waitForAll.OnComplete += () => _activeWaitForAll = null;
                        yield return waitForAll;
                    }
                    else if (instruction != null)
                    {
                        yield return instruction;
                    }
                }
            }

            string defaultNextId = node.GetNextNodeId();
            Advance(defaultNextId);
        }

        public IEnumerator GetBranchEnumerator(string startNodeId, Action onInputSwallowed)
        {
            string currentBranchNodeId = startNodeId;
            while (!string.IsNullOrEmpty(currentBranchNodeId))
            {
                var node = graph.GetNode(currentBranchNodeId);
                if (node == null)
                {
                    Debug.LogWarning($"Branch execution: Node '{currentBranchNodeId}' not found. Branch terminated.");
                    yield break;
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

                var instructionEnumerator = node.Process(this);
                while (instructionEnumerator.MoveNext())
                {
                    var instruction = instructionEnumerator.Current;
                    if (instruction is AdvanceToNode || instruction is EndDialogue)
                    {
                        Debug.LogWarning($"[Dialogue Debug] Node {node.GetType().Name} (ID: {node.nodeId}) tried to issue a global flow control instruction ({instruction.GetType().Name}), which is consumed in a parallel branch.");
                        yield break;
                    }
                    else if (instruction is WaitForUserInput)
                    {
                        onInputSwallowed?.Invoke();
                        continue;
                    }
                    else if (instruction != null)
                    {
                        yield return instruction;
                    }
                }

                currentBranchNodeId = node.GetNextNodeId();
            }
        }
        
        private void TriggerOnExit(DialogueNodeBase node)
        {
            if (node is TextNode t) t.onExit?.Invoke();
            else if (node is ChoiceNode c) c.onExit?.Invoke();
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

            if (_lastNode != null)
            {
                Advance(_lastNode.GetNextNodeId());
            }
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            ApplyVariableChanges(choice.variableChanges);
            choice.onSelected?.Invoke();
            Advance(choice.nextNodeId);
        }

        private void OnTypingCompleted()
        {
            if (_activeWaitForAll != null)
            {
                // 此處的邏輯可能在 ForceComplete 被正確處理後變得多餘。
                // 需要考慮一個分支是應該自動前進還是等待。
            }
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
                
                if (_localState.HasString(varName)) return _localState.GetString(varName);
                if (globalState != null && globalState.HasString(varName)) return globalState.GetString(varName);
                
                if (_localState.HasInt(varName)) return _localState.GetInt(varName).ToString();
                if (globalState != null && globalState.HasInt(varName)) return globalState.GetInt(varName).ToString();

                if (_localState.HasBool(varName)) return _localState.GetBool(varName).ToString();
                if (globalState != null && globalState.HasBool(varName)) return globalState.GetBool(varName).ToString();

                return match.Value;
            });
        }
    }
}
