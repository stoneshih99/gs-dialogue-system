#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// SetBackgroundNodeElement 是 SetBackgroundNode 的視覺化表示，用於在 GraphView 中顯示和編輯設定背景節點。
    /// 它允許用戶設定多個背景圖片，每個圖片可以有獨立的淡入淡出設定，並可指定目標圖層。
    /// </summary>
    public class SetBackgroundNodeElement : DialogueNodeElement
    {
        /// <summary>
        /// 獲取設定背景節點的輸出埠。
        /// </summary>
        public Port OutputPort { get; private set; }
        /// <summary>
        /// 獲取設定背景節點的數據模型。
        /// </summary>
        public override DialogueNodeBase NodeData => _data; // 實現抽象屬性
        private readonly SetBackgroundNode _data; // 設定背景節點的數據
        private readonly Action _onChanged; // 當節點數據改變時觸發的回調
        private VisualElement _entriesContainer; // 背景條目列表的容器

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="data">設定背景節點的數據。</param>
        /// <param name="onChanged">當節點數據改變時觸發的回調。</param>
        public SetBackgroundNodeElement(SetBackgroundNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            _onChanged = onChanged;
            title = "Set Background"; // 節點標題
            
            // 創建輸出埠
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            // 添加背景條目按鈕
            var addEntryButton = new Button(() => { _data.BackgroundEntries.Add(new SetBackgroundNode.BackgroundEntry()); RebuildEntriesUI(); _onChanged?.Invoke(); })
            {
                text = "Add Background"
            };
            mainContainer.Add(addEntryButton);

            _entriesContainer = new VisualElement();
            mainContainer.Add(_entriesContainer);
            
            RebuildEntriesUI(); // 初始構建 UI
        }

        /// <summary>
        /// 重新構建背景條目列表的 UI。
        /// </summary>
        private void RebuildEntriesUI()
        {
            _entriesContainer.Clear();
            if (_data.BackgroundEntries == null) _data.BackgroundEntries = new System.Collections.Generic.List<SetBackgroundNode.BackgroundEntry>();

            for (int i = 0; i < _data.BackgroundEntries.Count; i++)
            {
                int index = i;
                var entry = _data.BackgroundEntries[i];

                var entryFoldout = new Foldout { text = $"Background {index + 1}", value = true };
                entryFoldout.style.marginTop = 5;
                entryFoldout.style.marginBottom = 5;
                entryFoldout.style.borderBottomColor = new StyleColor(Color.gray);
                entryFoldout.style.borderBottomWidth = 1;

                // 移除按鈕
                var removeButton = new Button(() => { _data.BackgroundEntries.RemoveAt(index); RebuildEntriesUI(); _onChanged?.Invoke(); })
                {
                    text = "Remove",
                    style = { alignSelf = Align.FlexEnd }
                };
                entryFoldout.Add(removeButton);

                // 目標圖層索引輸入框
                var layerIndexField = new IntegerField("Target Layer Index") { value = entry.TargetLayerIndex, tooltip = "" +
                    "Target layer index, starting from 0. Be careful not to exceed the valid index."};
                layerIndexField.RegisterValueChangedCallback(evt => { entry.TargetLayerIndex = Mathf.Max(0, evt.newValue); _onChanged?.Invoke(); });
                entryFoldout.Add(layerIndexField);

                // 背景圖片 ObjectField
                var spriteField = new ObjectField("Background Sprite") { objectType = typeof(Sprite), allowSceneObjects = false, value = entry.BackgroundSprite };
                spriteField.RegisterValueChangedCallback(evt => { entry.BackgroundSprite = evt.newValue as Sprite; _onChanged?.Invoke(); });
                entryFoldout.Add(spriteField);

                // 清除背景開關
                var clearToggle = new Toggle("Clear Background") { value = entry.ClearBackground, tooltip = "Check to clear background, uncheck to keep it."};
                clearToggle.RegisterValueChangedCallback(evt => { entry.ClearBackground = evt.newValue; _onChanged?.Invoke(); });
                entryFoldout.Add(clearToggle);
                
                // 覆寫持續時間開關
                var overrideToggle = new Toggle("Override Duration") { value = entry.OverrideDuration };
                entryFoldout.Add(overrideToggle);

                // 持續時間輸入框
                var durationField = new FloatField("Duration (Seconds)") { value = entry.Duration };
                durationField.SetEnabled(overrideToggle.value); // 根據開關狀態啟用/禁用
                durationField.RegisterValueChangedCallback(evt => { entry.Duration = evt.newValue; _onChanged?.Invoke(); });
                entryFoldout.Add(durationField);
                
                overrideToggle.RegisterValueChangedCallback(evt => { entry.OverrideDuration = evt.newValue; durationField.SetEnabled(evt.newValue); _onChanged?.Invoke(); });

                _entriesContainer.Add(entryFoldout);
            }
        }

        /// <summary>
        /// 覆寫連接邏輯：當輸出埠連接到另一個節點時，更新數據模型中的 nextNodeId。
        /// </summary>
        /// <param name="outputPort">連接的輸出埠。</param>
        /// <param name="targetNodeId">目標節點的 ID。</param>
        public override void OnOutputPortConnected(Port outputPort, string targetNodeId)
        {
            if (outputPort == OutputPort)
            {
                _data.nextNodeId = targetNodeId;
            }
        }

        /// <summary>
        /// 覆寫斷開連接邏輯：當輸出埠斷開連接時，將數據模型中的 nextNodeId 設為 null。
        /// </summary>
        /// <param name="outputPort">斷開連接的輸出埠。</param>
        public override void OnOutputPortDisconnected(Port outputPort)
        {
            if (outputPort == OutputPort)
            {
                _data.nextNodeId = null;
            }
        }
    }
}
#endif
