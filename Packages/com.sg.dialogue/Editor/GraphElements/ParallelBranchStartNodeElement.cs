#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 代表 ParallelNode 子圖分支起點的視覺元素。
    /// 這是一個不可刪除、不可移動的特殊節點，用於定義多個並行分支的開始。
    /// </summary>
    public class ParallelBranchStartNodeElement : Node
    {
        /// <summary>
        /// 所有分支輸出埠集合。索引即為分支順序。
        /// </summary>
        public List<Port> BranchPorts { get; } = new List<Port>();

        /// <summary>
        /// 當分支結構變更時觸發，例如新增或刪除分支。
        /// 由外部註冊，用於同步資料層或重新連線等。
        /// </summary>
        public Action OnBranchesChanged;

        public ParallelBranchStartNodeElement()
        {
            title = "Branch Start";

            // 關閉可移動、可刪除、可選取能力，讓這個節點固定存在。
            capabilities &= ~(Capabilities.Movable | Capabilities.Deletable | Capabilities.Selectable);

            // 預設位置，如果之後有資料層位置，會由外部覆蓋。
            SetPosition(new Rect(100, 200, 150, 200));

            // 在標題列加入「Add Branch」按鈕
            var addButton = new Button(() =>
            {
                AddBranchPort();              // 新增一個新的分支埠
                OnBranchesChanged?.Invoke();  // 通知外部
            })
            {
                text = "Add Branch"
            };

            titleButtonContainer.Add(addButton);
        }

        /// <summary>
        /// 根據目標節點 ID 清空並重建所有分支輸出埠。
        /// targetNodeIds 的 Count 決定分支數量，其內容可透過 port.userData 取回。
        /// </summary>
        public void BuildPorts(List<string> targetNodeIds)
        {
            // 清理舊的 UI 元素和埠列表
            outputContainer.Clear();
            BranchPorts.Clear();

            if (targetNodeIds == null)
            {
                RefreshExpandedState();
                RefreshPorts();
                MarkDirtyRepaint();
                return;
            }

            for (int i = 0; i < targetNodeIds.Count; i++)
            {
                // 根據 index 與對應的 target node id 建立輸出埠
                AddBranchPort(i, targetNodeIds[i]);
            }

            RefreshExpandedState();
            RefreshPorts();
            MarkDirtyRepaint();
        }

        /// <summary>
        /// 新增一個分支埠。
        /// index 為顯示用 index，若為 -1 則使用目前 BranchPorts.Count。
        /// targetNodeId 可為 null，會存入 port.userData 以便資料層對應。
        /// </summary>
        private void AddBranchPort(int index = -1, string targetNodeId = null)
        {
            int portIndex = (index == -1) ? BranchPorts.Count : index;

            // 建立一列容器放埠與刪除按鈕
            var portRow = new VisualElement();
            portRow.style.flexDirection = FlexDirection.Row;
            portRow.style.alignItems = Align.Center;

            // 建立輸出埠。型別先用 bool，依需求可調整。
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = $"Branch {portIndex}";

            // 把對應的 target node id 存進 userData 方便之後查回
            if (!string.IsNullOrEmpty(targetNodeId))
            {
                port.userData = targetNodeId;
            }

            // 刪除按鈕，點擊會移除對應的分支埠
            var deleteButton = new Button(() =>
            {
                RemoveBranchPort(port, portRow);
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
            MarkDirtyRepaint();
        }

        /// <summary>
        /// 刪除指定的分支埠與其 UI 容器，並重新命名剩餘的埠。
        /// 同時會斷開該埠上所有 Edge 連線。
        /// </summary>
        private void RemoveBranchPort(Port portToRemove, VisualElement containerToRemove)
        {
            if (portToRemove == null)
            {
                return;
            }

            // 先斷開與此埠相關的所有 Edge
            // 注意：port.connections 是 IEnumerable<Edge>
            var connectedEdges = portToRemove.connections?.ToList();
            if (connectedEdges != null)
            {
                foreach (var edge in connectedEdges)
                {
                    // 從兩端埠斷開
                    // edge.Disconnect();
                    // 從視覺層移除 Edge
                    edge.RemoveFromHierarchy();
                }
            }

            // 再移除 UI 與 BranchPorts 列表內的紀錄
            if (outputContainer.Contains(containerToRemove))
            {
                outputContainer.Remove(containerToRemove);
            }

            BranchPorts.Remove(portToRemove);

            // 更新剩餘埠的顯示名稱，保持 Branch 0, 1, 2 的連續性
            for (int i = 0; i < BranchPorts.Count; i++)
            {
                BranchPorts[i].portName = $"Branch {i}";
            }

            RefreshExpandedState();
            RefreshPorts();
            MarkDirtyRepaint();
        }
    }
}
#endif
