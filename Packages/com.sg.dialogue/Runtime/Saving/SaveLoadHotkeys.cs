using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using SG.Dialogue;
using SG.Dialogue.Variables;

/// <summary>
/// SaveLoadHotkeys 是一個 MonoBehaviour，用於提供對話系統的快速存檔和讀檔熱鍵功能。
/// 它監聽 F5 (存檔) 和 F9 (讀檔) 按鍵，並與 DialogueSaveSystem 互動。
/// </summary>
public class SaveLoadHotkeys : MonoBehaviour
{
    [Tooltip("對話控制器 DialogueController 的引用。")]
    public DialogueController controller;
    [Tooltip("存檔槽位的 Key。")]
    public string slotKey = "slot1";

    private void Update()
    {
        if (controller == null) return;

        // 根據是否啟用新的輸入系統來判斷按鍵輸入
        #if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.f5Key.wasPressedThisFrame) DoSave(); // F5 存檔
            if (kb.f9Key.wasPressedThisFrame) DoLoad(); // F9 讀檔
        }
        #else
        if (Input.GetKeyDown(KeyCode.F5)) DoSave(); // F5 存檔
        if (Input.GetKeyDown(KeyCode.F9)) DoLoad(); // F9 讀檔
        #endif
    }

    /// <summary>
    /// 執行存檔操作。
    /// </summary>
    private void DoSave()
    {
        if (!controller.IsRunning)
        {
            Debug.LogWarning("[SaveLoadHotkeys] Cannot save when dialogue is not running.");
            return;
        }

        string graphId = controller.CurrentGraph.graphId; // 獲取當前對話圖的 ID
        string nodeId = controller.CurrentNodeId; // 獲取當前節點的 ID
        
        // 呼叫 DialogueSaveSystem 進行存檔
        DialogueSaveSystem.Save(slotKey, graphId, nodeId, controller.LocalState);
        Debug.Log($"[SaveLoadHotkeys] Saved to {slotKey}: {graphId}@{nodeId}");
    }

    /// <summary>
    /// 執行讀檔操作。
    /// </summary>
    private void DoLoad()
    {
        // 呼叫 DialogueSaveSystem 進行讀檔
        if (DialogueSaveSystem.Load(slotKey, out var graphId, out var nodeId, out DialogueState state))
        {
            var g = FindGraphById(graphId); // 根據 ID 查找對話圖
            if (g != null)
            {
                controller.StartDialogue(g); // 啟動對話
                controller.SetCurrentNodeId(nodeId); // 設定當前節點 ID
                // controller.RestoreLocalState(state); // 恢復局部狀態
                // controller.ShowCurrentNode(); // 手動顯示載入的節點

                Debug.Log($"[SaveLoadHotkeys] Loaded {graphId}@{nodeId}");
            }
            else
            {
                Debug.LogWarning($"[SaveLoadHotkeys] Graph not found: {graphId}");
            }
        }
        else
        {
            Debug.Log($"[SaveLoadHotkeys] No save data found for slot: {slotKey}");
        }
    }

    /// <summary>
    /// 根據對話圖 ID 查找對話圖資產。
    /// </summary>
    /// <param name="id">要查找的對話圖 ID。</param>
    /// <returns>對應的 DialogueGraph 資產，如果找不到則為 null。</returns>
    private DialogueGraph FindGraphById(string id)
    {
        // 查找所有 DialogueGraph 類型的資產
        var all = Resources.FindObjectsOfTypeAll<DialogueGraph>();
        foreach (var g in all)
        {
            if (g.graphId == id) return g; // 找到匹配的 ID 則返回
        }
        return null;
    }
}
