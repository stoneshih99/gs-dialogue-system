#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using SG.Dialogue.Nodes;
using UnityEditor;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 提供靜態方法來驗證 DialogueGraph 是否存在常見錯誤。
    /// </summary>
    public static class DialogueGraphValidator
    {
        /// <summary>
        /// 驗證給定的對話圖。
        /// </summary>
        /// <param name="graph">要驗證的對話圖。</param>
        /// <returns>問題列表，如果沒有問題則為空列表。</returns>
        public static List<string> Validate(DialogueGraph graph)
        {
            var issues = new List<string>();
            if (graph == null)
            {
                issues.Add("Graph is null.");
                return issues;
            }

            graph.BuildLookup(); // 確保節點查找表已建立

            // 規則 1: 檢查是否存在有效的起始節點
            if (string.IsNullOrEmpty(graph.startNodeId))
            {
                issues.Add("Start Node is not set.");
            }
            else if (!graph.HasNode(graph.startNodeId))
            {
                issues.Add($"Start Node ID '{graph.startNodeId}' does not exist in the graph.");
            }

            // 規則 2: 檢查懸空連接 (指向不存在的節點)
            CheckDanglingConnections(graph, issues);

            // 規則 3: 檢查孤島節點 (從起始節點無法到達的節點)
            CheckIslandNodes(graph, issues);

            return issues;
        }

        /// <summary>
        /// 檢查對話圖中是否存在懸空連接。
        /// </summary>
        /// <param name="graph">要檢查的對話圖。</param>
        /// <param name="issues">問題列表。</param>
        private static void CheckDanglingConnections(DialogueGraph graph, List<string> issues)
        {
            foreach (var node in graph.AllNodes)
            {
                if (node == null) continue;
                
                var childrenIds = GetNodeChildren(node); // 獲取當前節點的所有子節點 ID
                int portIndex = 0;
                foreach (var childId in childrenIds)
                {
                    string portName = GetPortName(node, portIndex); // 獲取埠名稱
                    CheckLink(graph, issues, node.nodeId, childId, portName); // 檢查連接是否有效
                    portIndex++;
                }
            }
        }

        /// <summary>
        /// 根據節點類型和埠索引獲取埠的名稱。
        /// </summary>
        /// <param name="node">節點。</param>
        /// <param name="index">埠索引。</param>
        /// <returns>埠名稱。</returns>
        private static string GetPortName(DialogueNodeBase node, int index)
        {
            if (node is ChoiceNode c) return $"Choice {index + 1}"; // 選項節點的埠名稱
            if (node is ConditionNode) return index == 0 ? "True" : "False"; // 條件節點的埠名稱
            if (node is TextNode && index == 1) return "On Interrupt"; // 文字節點的打斷埠名稱
            return "Next"; // 預設埠名稱
        }

        /// <summary>
        /// 檢查一個連接是否指向一個不存在的節點。
        /// </summary>
        /// <param name="graph">對話圖。</param>
        /// <param name="issues">問題列表。</param>
        /// <param name="fromId">源節點 ID。</param>
        /// <param name="toId">目標節點 ID。</param>
        /// <param name="portName">埠名稱。</param>
        private static void CheckLink(DialogueGraph graph, List<string> issues, string fromId, string toId, string portName)
        {
            if (!string.IsNullOrEmpty(toId) && !graph.HasNode(toId))
            {
                issues.Add($"Node '{fromId}' ({portName}) points to a non-existent node ID '{toId}'.");
            }
        }

        /// <summary>
        /// 檢查對話圖中是否存在孤島節點（從起始節點無法到達的節點）。
        /// </summary>
        /// <param name="graph">要檢查的對話圖。</param>
        /// <param name="issues">問題列表。</param>
        private static void CheckIslandNodes(DialogueGraph graph, List<string> issues)
        {
            if (string.IsNullOrEmpty(graph.startNodeId) || !graph.HasNode(graph.startNodeId)) return;

            var allNodeIds = new HashSet<string>(graph.AllNodes.Select(n => n.nodeId)); // 所有節點的 ID
            var reachableNodeIds = new HashSet<string>(); // 可達節點的 ID
            var queue = new Queue<string>(); // 用於 BFS 遍歷

            reachableNodeIds.Add(graph.startNodeId);
            queue.Enqueue(graph.startNodeId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var currentNode = graph.GetNode(currentId);

                var children = GetNodeChildren(currentNode); // 獲取當前節點的所有子節點 ID
                foreach (var childId in children)
                {
                    if (!string.IsNullOrEmpty(childId) && graph.HasNode(childId) && !reachableNodeIds.Contains(childId))
                    {
                        reachableNodeIds.Add(childId);
                        queue.Enqueue(childId);
                    }
                }
            }

            allNodeIds.ExceptWith(reachableNodeIds); // 找出所有節點中不可達的節點

            foreach (var islandNodeId in allNodeIds)
            {
                issues.Add($"Node '{islandNodeId}' is an island (unreachable from the start node).");
            }
        }

        /// <summary>
        /// 獲取給定節點的所有子節點 ID。
        /// </summary>
        /// <param name="node">父節點。</param>
        /// <returns>子節點 ID 的集合。</returns>
        private static IEnumerable<string> GetNodeChildren(DialogueNodeBase node)
        {
            if (node is TextNode t)
            {
                yield return t.nextNodeId;
                yield return t.InterruptNextNodeId;
            }
            else if (node is ChoiceNode c)
            {
                foreach (var choice in c.choices) yield return choice.nextNodeId;
            }
            else if (node is ConditionNode cond)
            {
                yield return cond.TrueNextNodeId;
                yield return cond.FalseNextNodeId;
            }
            else
            {
                // 對於所有其他單一輸出節點，使用反射獲取 nextNodeId
                var field = node.GetType().GetField("nextNodeId");
                if (field != null)
                {
                    yield return field.GetValue(node) as string;
                }
            }
        }
    }
}
#endif
