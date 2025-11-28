#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 代表 ParallelNode 子圖分支起點的視覺元素。
    /// 這是一個不可刪除、不可移動的特殊節點，用於定義多個並行分支的開始。
    /// </summary>
    public class ParallelBranchStartNodeElement : Node
    {
        public List<Port> BranchPorts { get; } = new List<Port>();
        
        public Action OnBranchesChanged;

        public ParallelBranchStartNodeElement()
        {
            title = "Branch Start";
            capabilities &= ~(Capabilities.Movable | Capabilities.Deletable | Capabilities.Selectable);
            SetPosition(new UnityEngine.Rect(100, 200, 150, 200));

            var addButton = new Button(() =>
            {
                AddBranchPort();
                OnBranchesChanged?.Invoke();
            })
            {
                text = "Add Branch"
            };
            titleButtonContainer.Add(addButton);
        }

        public void BuildPorts(List<string> targetNodeIds)
        {
            // 清理舊的 UI 元素和埠列表
            outputContainer.Clear();
            BranchPorts.Clear();

            if (targetNodeIds == null) return;

            for (int i = 0; i < targetNodeIds.Count; i++)
            {
                AddBranchPort(i);
            }
            RefreshExpandedState();
            RefreshPorts();
        }

        private void AddBranchPort(int index = -1)
        {
            int portIndex = (index == -1) ? BranchPorts.Count : index;

            // 創建一個容器來放置埠和按鈕
            var portRow = new VisualElement()
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
            };

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = $"Branch {portIndex}";
            
            var deleteButton = new Button(() =>
            {
                RemoveBranchPort(port);
                OnBranchesChanged?.Invoke();
            })
            {
                text = "-"
            };

            portRow.Add(port);
            portRow.Add(deleteButton);
            
            outputContainer.Add(portRow);
            BranchPorts.Add(port);
            
            RefreshExpandedState();
            RefreshPorts();
        }

        private void RemoveBranchPort(Port portToRemove)
        {
            var containerToRemove = outputContainer.Children().FirstOrDefault(c => c.Contains(portToRemove));
            if (containerToRemove != null)
            {
                outputContainer.Remove(containerToRemove);
                BranchPorts.Remove(portToRemove);

                // 更新剩餘埠的名稱
                for (int i = 0; i < BranchPorts.Count; i++)
                {
                    BranchPorts[i].portName = $"Branch {i}";
                }
                RefreshExpandedState();
                RefreshPorts();
            }
        }
    }
}
#endif
