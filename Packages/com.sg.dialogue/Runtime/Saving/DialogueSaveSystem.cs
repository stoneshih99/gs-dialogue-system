using System;
using System.Collections.Generic;
using SG.Dialogue.Variables;
using UnityEngine;
using static SG.Dialogue.DialogueStateAsset;

namespace SG.Dialogue
{
    /// <summary>
    /// DialogueSaveSystem 提供了一個簡易的對話存檔/讀檔機制，使用 Unity 的 PlayerPrefs 進行數據儲存。
    /// 它負責序列化對話圖 ID、當前節點 ID 和局部的 DialogueState 變數為 JSON 格式。
    /// </summary>
    public static class DialogueSaveSystem
    {
        /// <summary>
        /// 內部類別，定義了存檔數據的結構。
        /// </summary>
        [Serializable]
        private class SaveData
        {
            public string graphId; // 對話圖的 ID
            public string nodeId; // 當前節點的 ID
            public List<IntPair> ints; // 儲存的整數變數列表
            public List<BoolPair> bools; // 儲存的布林變數列表
        }

        /// <summary>
        /// 將當前對話狀態儲存到指定的存檔槽位。
        /// </summary>
        /// <param name="slotKey">存檔槽位的唯一 Key。</param>
        /// <param name="graphId">當前對話圖的 ID。</param>
        /// <param name="nodeId">當前節點的 ID。</param>
        /// <param name="state">要儲存的局部 DialogueState。</param>
        public static void Save(string slotKey, string graphId, string nodeId, DialogueState state)
        {
            var data = new SaveData // 創建存檔數據實例
            {
                graphId = graphId,
                nodeId = nodeId,
                ints = state.ExportInts(), // 匯出整數變數
                bools = state.ExportBools() // 匯出布林變數
            };
            var json = JsonUtility.ToJson(data); // 將數據序列化為 JSON 字符串
            PlayerPrefs.SetString($"SG.Dialogue.{slotKey}", json); // 儲存到 PlayerPrefs
            PlayerPrefs.Save(); // 立即保存 PlayerPrefs 更改
        }

        /// <summary>
        /// 從指定的存檔槽位載入對話狀態。
        /// </summary>
        /// <param name="slotKey">存檔槽位的唯一 Key。</param>
        /// <param name="graphId">載入的對話圖 ID。</param>
        /// <param name="nodeId">載入的節點 ID。</param>
        /// <param name="state">載入的局部 DialogueState。</param>
        /// <returns>如果成功載入數據則為 true，否則為 false。</returns>
        public static bool Load(string slotKey, out string graphId, out string nodeId, out DialogueState state)
        {
            graphId = null;
            nodeId = null;
            state = new DialogueState(); // 實例化一個新的 DialogueState

            var json = PlayerPrefs.GetString($"SG.Dialogue.{slotKey}", null); // 從 PlayerPrefs 獲取 JSON 字符串
            if (string.IsNullOrEmpty(json)) return false; // 如果沒有數據，則載入失敗

            var data = JsonUtility.FromJson<SaveData>(json); // 將 JSON 字符串反序列化為 SaveData
            graphId = data.graphId;
            nodeId = data.nodeId;
            state.ImportInts(data.ints); // 匯入整數變數
            state.ImportBools(data.bools); // 匯入布林變數
            return true; // 載入成功
        }

        /// <summary>
        /// 清除指定存檔槽位的數據。
        /// </summary>
        /// <param name="slotKey">要清除的存檔槽位 Key。</param>
        public static void Clear(string slotKey) => PlayerPrefs.DeleteKey($"SG.Dialogue.{slotKey}");
    }
}
