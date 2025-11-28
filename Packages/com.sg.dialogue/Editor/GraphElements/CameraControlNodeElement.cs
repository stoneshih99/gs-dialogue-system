#if UNITY_EDITOR
using System;
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// CameraControlNodeElement 是 CameraControlNode 的視覺化表示，用於在 GraphView 中顯示和編輯攝影機控制節點。
    /// 它允許用戶設定攝影機動作類型、持續時間、震動強度、目標縮放、平移位置和聚焦目標。
    /// </summary>
    public class CameraControlNodeElement : DialogueNodeElement
    {
        /// <summary>
        /// 獲取攝影機控制節點的輸出埠。
        /// </summary>
        public Port OutputPort { get; private set; }
        /// <summary>
        /// 獲取攝影機控制節點的數據模型。
        /// </summary>
        public override DialogueNodeBase NodeData => _data; // 實現抽象屬性
        private readonly CameraControlNode _data; // 攝影機控制節點的數據

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="data">攝影機控制節點的數據。</param>
        /// <param name="onChanged">當節點數據改變時觸發的回調。</param>
        public CameraControlNodeElement(CameraControlNode data, Action onChanged) : base(data.nodeId)
        {
            _data = data;
            title = "Camera Control"; // 節點標題
            
            // 創建輸出埠
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Next";
            outputContainer.Add(OutputPort);

            // 動作類型下拉選單
            var actionTypeField = new EnumField("Action", _data.ActionType);
            mainContainer.Add(actionTypeField);

            // 持續時間輸入框
            var durationField = new FloatField("Duration") { value = _data.Duration };
            durationField.RegisterValueChangedCallback(evt => { _data.Duration = evt.newValue; onChanged?.Invoke(); });
            mainContainer.Add(durationField);

            // 震動設定容器
            var shakeContainer = new VisualElement();
            var intensityField = new FloatField("Intensity") { value = _data.ShakeIntensity };
            intensityField.RegisterValueChangedCallback(evt => { _data.ShakeIntensity = evt.newValue; onChanged?.Invoke(); });
            shakeContainer.Add(intensityField);
            mainContainer.Add(shakeContainer);

            // 縮放設定容器
            var zoomContainer = new VisualElement();
            var zoomField = new FloatField("Target Zoom") { value = _data.TargetZoom };
            zoomField.RegisterValueChangedCallback(evt => { _data.TargetZoom = evt.newValue; onChanged?.Invoke(); });
            zoomContainer.Add(zoomField);
            mainContainer.Add(zoomContainer);

            // 平移設定容器
            var panContainer = new VisualElement();
            var panField = new Vector2Field("Target Position") { value = _data.PanTargetPosition };
            panField.RegisterValueChangedCallback(evt => { _data.PanTargetPosition = evt.newValue; onChanged?.Invoke(); });
            panContainer.Add(panField);
            mainContainer.Add(panContainer);
            
            // 聚焦設定容器
            var focusContainer = new VisualElement();
            var focusField = new ObjectField("Focus Target") { objectType = typeof(Transform), allowSceneObjects = true, value = _data.FocusTarget };
            focusField.RegisterValueChangedCallback(evt => { _data.FocusTarget = evt.newValue as Transform; onChanged?.Invoke(); });
            focusContainer.Add(focusField);
            mainContainer.Add(focusContainer);

            // 根據動作類型刷新 UI 顯示
            void RefreshUI(CameraActionType actionType)
            {
                shakeContainer.style.display = actionType == CameraActionType.Shake ? DisplayStyle.Flex : DisplayStyle.None;
                zoomContainer.style.display = actionType == CameraActionType.Zoom ? DisplayStyle.Flex : DisplayStyle.None;
                panContainer.style.display = actionType == CameraActionType.Pan ? DisplayStyle.Flex : DisplayStyle.None;
                focusContainer.style.display = actionType == CameraActionType.FocusOnTarget ? DisplayStyle.Flex : DisplayStyle.None;
            }

            actionTypeField.RegisterValueChangedCallback(evt =>
            {
                _data.ActionType = (CameraActionType)evt.newValue;
                RefreshUI(_data.ActionType);
                onChanged?.Invoke();
            });
            
            RefreshUI(_data.ActionType);
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
