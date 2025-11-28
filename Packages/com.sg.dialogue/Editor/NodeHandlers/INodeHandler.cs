#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 定義了處理特定節點類型在 GraphView 中行為的介面。
    /// </summary>
    public interface INodeHandler
    {
        /// <summary>
        /// 在右鍵菜單中顯示的名稱。
        /// </summary>
        string MenuName { get; }

        /// <summary>
        /// 創建節點的數據模型實例。
        /// </summary>
        DialogueNodeBase CreateNodeData();

        /// <summary>
        /// 創建節點的視覺元素實例。
        /// </summary>
        DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged);

        /// <summary>
        /// 獲取節點 ID 的前綴。
        /// </summary>
        string GetPrefix();

        /// <summary>
        /// 處理埠的連接邏輯。
        /// </summary>
        void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect);

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        Port GetOutputPort(DialogueNodeElement element, string portName);
    }
}
#endif
