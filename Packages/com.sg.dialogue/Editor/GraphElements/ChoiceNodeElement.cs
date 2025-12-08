#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UEv = UnityEngine.Events;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// ChoiceNodeElement 是 ChoiceNode 的視覺化表示，用於在 GraphView 中顯示和編輯選項節點。
    /// 它允許用戶編輯選項文本，並添加/刪除選項。
    /// </summary>
    public class ChoiceNodeElement : DialogueNodeElement
    {
        /// <summary>
        /// 獲取選項節點的數據模型。
        /// </summary>
        public override DialogueNodeBase NodeData => _nodeData; // 實現抽象屬性
        private readonly ChoiceNode _nodeData; // 選項節點的數據
        private readonly Action _onChanged; // 當節點數據改變時觸發的回調
        private VisualElement _choicesContainer; // 選項容器
        private readonly List<Port> _choicePorts = new(); // 選項埠列表
        private const string ChoicePortPrefix = "Choice "; // 選項埠名稱前綴

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="data">選項節點的數據。</param>
        /// <param name="onChanged">當節點數據改變時觸發的回調。</param>
        public ChoiceNodeElement(ChoiceNode data, Action onChanged) : base(data.nodeId)
        {
            _nodeData = data;
            _onChanged = onChanged;

            title = $"Choice ({data.nodeId})"; // 節點標題

            // 事件摘要標籤
            var evtLabel = new Label(BuildEventSummary(data.onEnter, data.onExit))
            {
                style = { fontSize = 10, color = UnityEngine.Color.gray, unityTextAlign = UnityEngine.TextAnchor.MiddleLeft }
            };
            mainContainer.Add(evtLabel);

            _choicesContainer = new VisualElement { style = { marginLeft = 6 } };
            extensionContainer.Add(_choicesContainer);

            RebuildChoicesUI(); // 重新構建選項 UI

            // 添加選項按鈕
            var btnAdd = new Button(() =>
            {
                _nodeData.choices.Add(new DialogueChoice { text = "新選項" });
                _onChanged?.Invoke();
                RebuildChoicesUI();
            }) { text = "+ Choice" };
            titleButtonContainer.Add(btnAdd);
        }

        /// <summary>
        /// 構建事件摘要字符串。
        /// </summary>
        /// <param name="onEnter">進入事件。</param>
        /// <param name="onExit">退出事件。</param>
        /// <returns>事件摘要。</returns>
        private static string BuildEventSummary(UEv.UnityEvent onEnter, UEv.UnityEvent onExit)
        {
            int e = Count(onEnter), x = Count(onExit);
            return $"Events: Enter({e}), Exit({x})";
        }

        /// <summary>
        /// 計算 UnityEvent 的持久事件數量。
        /// </summary>
        /// <param name="u">UnityEvent 實例。</param>
        /// <returns>持久事件數量。</returns>
        private static int Count(UEv.UnityEvent u)
        {
            try { return u?.GetPersistentEventCount() ?? 0; }
            catch { return 0; }
        }

        /// <summary>
        /// 獲取指定索引的選項埠。
        /// </summary>
        /// <param name="index">埠索引。</param>
        /// <returns>選項埠，如果索引無效則為 null。</returns>
        public Port GetChoicePort(int index)
        {
            return (index >= 0 && index < _choicePorts.Count) ? _choicePorts[index] : null;
        }

        /// <summary>
        /// 獲取給定埠的索引。
        /// </summary>
        /// <param name="p">埠。</param>
        /// <returns>埠索引，如果不是選項埠則為 -1。</returns>
        public int GetPortIndex(Port p)
        {
            if (p == null || !p.portName.StartsWith(ChoicePortPrefix))
            {
                return -1;
            }
            string indexStr = p.portName.Substring(ChoicePortPrefix.Length);
            if (int.TryParse(indexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            {
                return index;
            }
            return -1;
        }

        /// <summary>
        /// 重新構建選項 UI 列表。
        /// </summary>
        private void RebuildChoicesUI()
        {
            _choicesContainer.Clear(); // 清空現有選項
            _choicePorts.Clear(); // 清空埠列表
            for (int i = 0; i < _nodeData.choices.Count; i++)
            {
                int idx = i;
                var ch = _nodeData.choices[i];

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

                // 選項文本輸入框
                var tf = new TextField($"Choice {i + 1}") { value = ch.text, style = { flexGrow = 1f } };
                tf.RegisterValueChangedCallback(e => { ch.text = e.newValue; _onChanged?.Invoke(); });
                row.Add(tf);

                // 創建選項輸出埠
                var outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                outPort.portName = $"{ChoicePortPrefix}{i}";
                row.Add(outPort);
                _choicePorts.Add(outPort);

                // 刪除選項按鈕
                var btnDel = new Button(() =>
                {
                    _nodeData.choices.RemoveAt(idx);
                    _onChanged?.Invoke();
                    RebuildChoicesUI();
                }) { text = "-" };
                row.Add(btnDel);

                _choicesContainer.Add(row);
            }

            RefreshExpandedState(); // 刷新展開狀態
            RefreshPorts(); // 刷新埠
        }

        /// <summary>
        /// 覆寫連接邏輯：當輸出埠連接到另一個節點時，更新數據模型中對應選項的 nextNodeId。
        /// </summary>
        /// <param name="outputPort">連接的輸出埠。</param>
        /// <param name="targetNodeId">目標節點的 ID。</param>
        public override void OnOutputPortConnected(Port outputPort, string targetNodeId)
        {
            int idx = GetPortIndex(outputPort);
            if (idx >= 0 && idx < _nodeData.choices.Count)
            {
                _nodeData.choices[idx].nextNodeId = targetNodeId;
            }
        }

        /// <summary>
        /// 覆寫斷開連接邏輯：當輸出埠斷開連接時，將數據模型中對應選項的 nextNodeId 設為 null。
        /// </summary>
        /// <param name="outputPort">斷開連接的輸出埠。</param>
        public override void OnOutputPortDisconnected(Port outputPort)
        {
            int idx = GetPortIndex(outputPort);
            if (idx >= 0 && idx < _nodeData.choices.Count)
            {
                _nodeData.choices[idx].nextNodeId = null;
            }
        }
    }
}
#endif
