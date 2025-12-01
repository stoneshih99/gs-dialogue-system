#if UNITY_EDITOR
using SG.Dialogue.Nodes;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Editor.Tabs
{
    /// <summary>
    /// SimulatorTab 是一個 VisualElement，用於在編輯器中提供對話模擬器界面。
    /// 它顯示對話文本、說話者、選項，並允許用戶控制模擬的進程。
    /// </summary>
    public class SimulatorTab : VisualElement
    {
        private DialogueGraph _graph; // 當前模擬的對話圖
        private DialogueStateAsset _state; // 全域對話狀態資產
        private DialogueSimulatorEngine _engine; // 對話模擬器引擎實例

        // UI 元素引用
        private readonly Label _speakerLabel; // 顯示說話者名稱的標籤
        private readonly Label _dialogueLabel; // 顯示對話文本的標籤
        private readonly VisualElement _choicesContainer; // 容納選項按鈕的容器
        private readonly Button _startButton; // 開始/停止模擬按鈕
        private readonly Button _nextButton; // 前進到下一句對話按鈕
        private TextNode _currentTextNode; // 當前顯示的文字節點

        /// <summary>
        /// 構造函數，初始化模擬器界面的 UI 佈局。
        /// </summary>
        public SimulatorTab()
        {
            style.flexGrow = 1; // 讓分頁佔滿可用空間
            style.paddingLeft = 10;
            style.paddingRight = 10;
            style.paddingTop = 6;

            // 頂部標頭，包含開始/停止按鈕
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 10 } };
            _startButton = new Button(ToggleSimulation) { text = "開始模擬" };
            header.Add(_startButton);
            Add(header);

            // 對話面板，顯示說話者和對話文本
            var dialoguePanel = new Box
            {
                style =
                {
                    paddingLeft = 10, paddingRight = 10, paddingTop = 10, paddingBottom = 10,
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f) // 半透明背景
                }
            };
            _speakerLabel = new Label("說話者") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16, marginBottom = 5 } };
            _dialogueLabel = new Label("對話文本會顯示在這裡...") { style = { whiteSpace = WhiteSpace.Normal, fontSize = 14 } };
            dialoguePanel.Add(_speakerLabel);
            dialoguePanel.Add(_dialogueLabel);
            Add(dialoguePanel);

            // 選項容器
            _choicesContainer = new VisualElement { style = { marginTop = 10 } };
            Add(_choicesContainer);

            // 底部，包含「下一步」按鈕
            var footer = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 10 } };
            _nextButton = new Button(OnNextClicked) { text = "下一步 >" };
            footer.Add(_nextButton);
            Add(footer);
            
            ResetUI(); // 初始化 UI 狀態
        }

        /// <summary>
        /// 設定要模擬的對話圖。
        /// </summary>
        /// <param name="graph">對話圖資產。</param>
        public void SetGraph(DialogueGraph graph) => _graph = graph;
        /// <summary>
        /// 設定全域對話狀態資產。
        /// </summary>
        /// <param name="state">對話狀態資產。</param>
        public void SetState(DialogueStateAsset state) => _state = state;

        /// <summary>
        /// 切換模擬的開始/停止狀態。
        /// </summary>
        private void ToggleSimulation()
        {
            if (_engine != null && _engine.IsRunning)
            {
                _engine.Stop(); // 如果正在運行，則停止模擬
            }
            else
            {
                if (_graph == null) return; // 如果沒有對話圖，則無法開始模擬
                _engine = new DialogueSimulatorEngine(_graph, _state); // 創建新的模擬器引擎
                // 註冊引擎的事件回調
                _engine.OnShowText += HandleShowText;
                _engine.OnShowChoices += HandleShowChoices;
                _engine.OnEnd += HandleEnd;
                _engine.Start(); // 開始模擬
                _startButton.text = "停止模擬"; // 更新按鈕文本
            }
        }

        /// <summary>
        /// 處理「下一步」按鈕點擊事件。
        /// </summary>
        private void OnNextClicked()
        {
            if (_engine == null || !_engine.IsRunning) return;
            if (_currentTextNode != null)
            {
                _engine.Advance(_currentTextNode.nextNodeId); // 讓引擎前進到文字節點的下一個節點
            }
        }

        /// <summary>
        /// 處理模擬器引擎發出的顯示文字節點事件。
        /// </summary>
        /// <param name="node">要顯示的文字節點。</param>
        private void HandleShowText(TextNode node)
        {
            _currentTextNode = node;
            _speakerLabel.text = node.speakerName; // 顯示說話者名稱
            _dialogueLabel.text = node.text; // 顯示對話文本
            _choicesContainer.style.display = DisplayStyle.None; // 隱藏選項
            // 根據節點是否有下一個連接來決定是否顯示「下一步」按鈕
            _nextButton.style.display = string.IsNullOrEmpty(node.nextNodeId) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// 處理模擬器引擎發出的顯示選項節點事件。
        /// </summary>
        /// <param name="node">要顯示的選項節點。</param>
        private void HandleShowChoices(ChoiceNode node)
        {
            _currentTextNode = null; // 清空當前文字節點
            _speakerLabel.text = "玩家"; // 說話者設為玩家
            _dialogueLabel.text = "請選擇一個選項:"; // 提示選擇選項
            _choicesContainer.Clear(); // 清空現有選項
            _choicesContainer.style.display = DisplayStyle.Flex; // 顯示選項容器
            _nextButton.style.display = DisplayStyle.None; // 隱藏「下一步」按鈕

            // 為每個選項創建一個按鈕
            foreach (var choice in node.choices)
            {
                var button = new Button(() => _engine.SelectChoice(choice))
                {
                    text = choice.text
                };
                _choicesContainer.Add(button);
            }
        }

        /// <summary>
        /// 處理模擬器引擎發出的模擬結束事件。
        /// </summary>
        private void HandleEnd()
        {
            _engine = null; // 清空引擎實例
            ResetUI(); // 重置 UI 狀態
        }

        /// <summary>
        /// 重置模擬器界面的 UI 元素到初始狀態。
        /// </summary>
        private void ResetUI()
        {
            _startButton.text = "開始模擬"; // 按鈕文本設為「開始模擬」
            _speakerLabel.text = "說話者";
            _dialogueLabel.text = "模擬尚未執行。"; // 顯示模擬未運行提示
            _choicesContainer.Clear();
            _choicesContainer.style.display = DisplayStyle.None; // 隱藏選項
            _nextButton.style.display = DisplayStyle.None; // 隱藏「下一步」按鈕
        }
    }
}
#endif
