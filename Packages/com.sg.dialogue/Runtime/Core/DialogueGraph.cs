using System;
using System.Collections.Generic;
using System.Linq;
using SG.Dialogue.Events;
using SG.Dialogue.Nodes;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue
{
    /// <summary>
    /// DialogueGraph 是一個 ScriptableObject，用於定義對話流程的圖形結構。
    /// 它包含對話中的所有節點、起始節點、行為設定以及圖層級的事件。
    /// </summary>
    [CreateAssetMenu(menuName = "SG/Dialogue/Dialogue Graph")]
    public class DialogueGraph : ScriptableObject
    {
        [Tooltip("對話圖的唯一識別字串（用於存檔、讀檔與查找）")]
        public string graphId;
        [Tooltip("對話流程的起始節點 ID")]
        public string startNodeId;
        
        [Header("行為")]
        [Tooltip("若為 true，則允許玩家一鍵跳過此段對話")]
        public bool IsSkippable = true;

        [SerializeReference] // 允許多型序列化，這樣 List<DialogueNodeBase> 才能正確保存派生類別
        [Tooltip("對話圖中所有節點的列表")]
        public List<DialogueNodeBase> AllNodes = new List<DialogueNodeBase>();

        [Header("圖事件")]
        [Tooltip("對話開始時觸發的 UnityEvent")]
        public UnityEvent onDialogueStarted;
        [Tooltip("對話結束時觸發的 UnityEvent")]
        public UnityEvent onDialogueEnded;
        [Tooltip("任何節點進入時觸發的 StringEvent，會傳遞節點 ID")]
        public StringEvent onNodeEntered;
        [Tooltip("任何節點退出時觸發的 StringEvent，會傳遞節點 ID")]
        public StringEvent onNodeExited;

        [Header("自動前進")]
        [Tooltip("是否啟用自動前進功能")]
        public bool autoAdvanceEnabled;
        [Tooltip("自動前進的預設延遲時間（秒）")]
        public float defaultAutoAdvanceDelay = 1.2f;

        // 節點 ID 到節點實例的查找表，用於在執行時快速訪問節點，避免每次都遍歷列表
        private Dictionary<string, DialogueNodeBase> _nodeLookup;

#if UNITY_EDITOR
        /// <summary>
        /// 在編輯器中驗證資產時調用，確保所有節點 ID 的唯一性。
        /// </summary>
        private void OnValidate()
        {
            var existingIds = new HashSet<string>();
            EnsureUniqueIds(AllNodes, existingIds);
        }

        /// <summary>
        /// 遞迴地確保節點 ID 的唯一性。
        /// </summary>
        /// <param name="nodes">要檢查的節點列表。</param>
        /// <param name="existingIds">已存在的 ID 集合，用於檢查重複。</param>
        private void EnsureUniqueIds(List<DialogueNodeBase> nodes, HashSet<string> existingIds)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node == null) continue;
                // 如果節點 ID 為空或已存在，則生成一個新的 GUID 作為其唯一 ID
                if (string.IsNullOrEmpty(node.nodeId) || existingIds.Contains(node.nodeId))
                {
                    node.nodeId = Guid.NewGuid().ToString();
                }
                existingIds.Add(node.nodeId);

                // 如果是容器型節點（如序列或並行節點），則遞迴檢查其子節點
                if (node is SequenceNode sequenceNode)
                {
                    EnsureUniqueIds(sequenceNode.childNodes, existingIds);
                }
                else if (node is ParallelNode parallelNode)
                {
                    EnsureUniqueIds(parallelNode.childNodes, existingIds);
                }
            }
        }
#endif

        /// <summary>
        /// 建立節點 ID 到節點實例的查找表，以加速運行時的節點查找。
        /// </summary>
        public void BuildLookup()
        {
            _nodeLookup = new Dictionary<string, DialogueNodeBase>();
            BuildLookupRecursively(AllNodes);
        }

        /// <summary>
        /// 遞迴地建立節點查找表。
        /// </summary>
        /// <param name="nodes">要處理的節點列表。</param>
        private void BuildLookupRecursively(List<DialogueNodeBase> nodes)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.nodeId))
                {
                    _nodeLookup[node.nodeId] = node;
                    // 如果是容器型節點，則遞迴為其子節點建立查找表
                    if (node is SequenceNode sequenceNode)
                    {
                        BuildLookupRecursively(sequenceNode.childNodes);
                    }
                    else if (node is ParallelNode parallelNode)
                    {
                        BuildLookupRecursively(parallelNode.childNodes);
                    }
                }
            }
        }

        /// <summary>
        /// 根據節點 ID 獲取節點實例。
        /// </summary>
        /// <param name="id">節點的唯一 ID。</param>
        /// <returns>對應的 DialogueNodeBase 實例；如果找不到，則返回 null。</returns>
        public DialogueNodeBase GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_nodeLookup == null) BuildLookup(); // 如果查找表尚未建立，則先建立它
            _nodeLookup.TryGetValue(id, out var node);
            return node;
        }

        /// <summary>
        /// 檢查對話圖中是否存在指定 ID 的節點。
        /// </summary>
        /// <param name="id">要檢查的節點 ID。</param>
        /// <returns>如果存在則返回 true，否則返回 false。</returns>
        public bool HasNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (_nodeLookup == null) BuildLookup();
            return _nodeLookup.ContainsKey(id);
        }

        /// <summary>
        /// 根據節點 ID 獲取指定類型的節點實例。
        /// </summary>
        /// <typeparam name="T">節點的目標類型，必須繼承自 DialogueNodeBase。</typeparam>
        /// <param name="id">節點的唯一 ID。</param>
        /// <returns>對應的節點實例；如果找不到或類型不匹配，則返回 null。</returns>
        public T GetNode<T>(string id) where T : DialogueNodeBase
        {
            return GetNode(id) as T;
        }

        /// <summary>
        /// 用於序列化節點在編輯器中位置的內部類別。
        /// </summary>
        [Serializable]
        public class NodePosition { public string nodeId; public Vector2 position; }
        [SerializeField, HideInInspector] private List<NodePosition> nodePositions = new List<NodePosition>();

        /// <summary>
        /// 獲取指定節點在編輯器中儲存的位置。
        /// </summary>
        /// <param name="nodeId">節點的唯一 ID。</param>
        /// <returns>節點的二維向量位置；如果找不到，則返回 Vector2.zero。</returns>
        public Vector2 GetNodePosition(string nodeId) => nodePositions.Find(np => np.nodeId == nodeId)?.position ?? Vector2.zero;
        
        /// <summary>
        /// 設定指定節點在編輯器中的位置。
        /// </summary>
        /// <param name="nodeId">節點的唯一 ID。</param>
        /// <param name="pos">要設定的位置。</param>
        public void SetNodePosition(string nodeId, Vector2 pos)
        {
            var posData = nodePositions.Find(np => np.nodeId == nodeId);
            if (posData != null) posData.position = pos; // 如果已存在，則更新位置
            else nodePositions.Add(new NodePosition { nodeId = nodeId, position = pos }); // 否則添加新的位置資料
        }

        /// <summary>
        /// 移除指定節點的位置資料。
        /// </summary>
        /// <param name="nodeId">節點的唯一 ID。</param>
        public void RemoveNodePosition(string nodeId) => nodePositions.RemoveAll(np => np.nodeId == nodeId);

        /// <summary>
        /// 清理孤立的節點位置資料（即在位置列表中存在，但在圖中已被刪除的節點）。
        /// </summary>
        public void CleanupOrphanPositions()
        {
            if (AllNodes == null) return;
            var allNodeIds = new HashSet<string>();
            GetAllNodeIdsRecursively(AllNodes, allNodeIds); // 獲取所有現存節點的 ID
            nodePositions.RemoveAll(np => !allNodeIds.Contains(np.nodeId)); // 移除所有不存在於現存節點 ID 列表中的位置資料
        }

        /// <summary>
        /// 遞迴地獲取所有節點的 ID。
        /// </summary>
        /// <param name="nodes">要遍歷的節點列表。</param>
        /// <param name="allNodeIds">用於儲存所有節點 ID 的集合。</param>
        private void GetAllNodeIdsRecursively(List<DialogueNodeBase> nodes, HashSet<string> allNodeIds)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node == null) continue;
                allNodeIds.Add(node.nodeId);
                if (node is SequenceNode sequenceNode)
                {
                    GetAllNodeIdsRecursively(sequenceNode.childNodes, allNodeIds);
                }
                else if (node is ParallelNode parallelNode)
                {
                    GetAllNodeIdsRecursively(parallelNode.childNodes, allNodeIds);
                }
            }
        }
    }
}
