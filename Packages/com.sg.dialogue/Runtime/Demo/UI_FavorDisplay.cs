using TMPro;
using UnityEngine;

namespace SG.Dialogue
{
    /// <summary>
    /// UI_FavorDisplay 是一個簡單的 UI 組件，用於在畫面上顯示對話狀態中的好感度數值（"favor" 變數）。
    /// 它會在每一幀更新顯示，從 DialogueController 的當前狀態中讀取 "favor" 整數變數。
    /// </summary>
    public class UI_FavorDisplay : MonoBehaviour
    {
        [Tooltip("顯示好感度數值的 TextMeshProUGUI 文本組件。")]
        [SerializeField] private TextMeshProUGUI favorLabel;           // 顯示好感度的 Label
        [Tooltip("對話控制器 DialogueController 的引用，用於獲取對話狀態。")]
        [SerializeField] private DialogueController dialogueController; // 對話控制器參考（取得狀態）

        private void Start()
        {
            // 在遊戲開始時啟動對話（如果 DialogueController 已設定）
            Debug.LogFormat("UI_FavorDisplay.Start()");
            dialogueController.StartDialogue();
        }

        private void Update()
        {
            if (dialogueController == null || favorLabel == null) return; // 如果引用為空，則不執行任何操作
            
            // 每幀更新顯示目前狀態中的 "favor" 值
            // 注意：原始程式碼中此行被註釋掉，如果需要顯示，請取消註釋並確保 DialogueController.CurrentState 可訪問
            // favorLabel.text = $"好感度：{dialogueController.CurrentState.GetInt("favor")}";
        }
    }
}
