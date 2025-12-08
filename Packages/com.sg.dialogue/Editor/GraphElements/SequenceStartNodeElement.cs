#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 代表 SequenceNode 子圖起點的視覺元素。
    /// 這是一個不可刪除、不可移動的特殊節點，僅用於定義流程的開始。
    /// </summary>
    public class SequenceStartNodeElement : Node
    {
        public Port OutputPort { get; private set; }

        public SequenceStartNodeElement()
        {
            title = "Start";
            // 將節點設為無法移動、刪除或選取
            capabilities &= ~(Capabilities.Movable | Capabilities.Deletable | Capabilities.Selectable); 
            
            // 給它一個固定的位置，通常在左邊
            SetPosition(new UnityEngine.Rect(100, 200, 100, 150));

            // 創建唯一的輸出埠
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "Start";
            outputContainer.Add(OutputPort);
            
            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
#endif
