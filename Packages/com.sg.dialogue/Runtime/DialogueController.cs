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
    [RequireComponent(typeof(DialogueUIManager), typeof(DialogueVisualManager))]
    public class DialogueController : MonoBehaviour
    {
        [Header("圖表與狀態")]
        [SerializeField] private DialogueGraph graph;
        [SerializeField] private DialogueStateAsset globalState;

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
            if (graph == null) { Debug.LogError("對話控制器：對話圖為空。"); return; }
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
                TriggerOnEnterAndVariableChanges(node);
                _activeNodeCoroutine = StartCoroutine(ProcessNodeCoroutine(node));
            }
            else
            {
                Debug.LogWarning($"對話控制器：找不到節點 ID：{_currentNodeId}。對話結束。");
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
                    Debug.LogWarning($"在圖表中找不到 ID 為 '{currentNodeId}' 的節點。");
                    return null;
                }

                if (node.IsEnabled)
                {
                    return currentNodeId;
                }

                Debug.Log($"[對話] 跳過已停用的節點：{currentNodeId}");
                currentNodeId = node.GetNextNodeId();
            }

            if (safetyBreak <= 0)
            {
                Debug.LogError("在尋找可處理節點時檢測到無限迴圈。中止對話。");
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
                    else if (instruction != null)
                    {
                        yield return instruction;
                    }
                }
            }

            string defaultNextId = node.GetNextNodeId();
            Advance(defaultNextId);
        }

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
        
        private void TriggerOnExit(DialogueNodeBase node)
        {
            if (node is TextNode t) t.onExit?.Invoke();
            else if (node is ChoiceNode c) c.onExit?.Invoke();
            else if (node is AnimationNode) { }
            else if (node is CharacterActionNode) { }
            else if (node is SetBackgroundNode) { }
            else if (node is FlickerEffectNode) { }
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
            else if (node is AnimationNode) { }
            else if (node is CharacterActionNode) { }
            else if (node is SetBackgroundNode) { }
            else if (node is FlickerEffectNode) { }
            graph?.onNodeEntered?.Invoke(node.nodeId);
        }

        private void OnAdvanceRequested()
        {
            if (!IsRunning || uiManager.IsTyping) return;

            if (_lastNode is TextNode textNode)
            {
                Advance(textNode.nextNodeId);
            }
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            ApplyVariableChanges(choice.variableChanges);
            choice.onSelected?.Invoke();
            Advance(choice.nextNodeId);
        }

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
