using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Resource;
using TMPro;
using UnityEngine;

namespace SG.Dialogue
{
    /// <summary>
    /// UI_FavorDisplay 是一個簡單的 UI 組件，用於在畫面上顯示對話狀態中的好感度數值（"favor" 變數）。
    /// 它會在每一幀更新顯示，從 DialogueController 的當前狀態中讀取 "favor" 整數變數。
    /// 同時也負責註冊 DialogueResourceBridge 的資源提供者（Demo 用途）。
    /// </summary>
    public class UI_FavorDisplay : MonoBehaviour
    {
        [Tooltip("顯示好感度數值的 TextMeshProUGUI 文本組件。")]
        [SerializeField] private TextMeshProUGUI favorLabel;           // 顯示好感度的 Label
        [Tooltip("對話控制器 DialogueController 的引用，用於獲取對話狀態。")]
        [SerializeField] private DialogueController dialogueController; // 對話控制器參考（取得狀態）

        [Header("資源提供者（Demo）")]
        [Tooltip("自訂的資源提供者。若不指定，對話系統將使用節點上的直接引用（原有行為）。")]
        [SerializeField] private DialogueResourceProviderComponent resourceProvider;

        private async void Start()
        {
            // 註冊資源提供者到 Bridge（如果有指定）
            if (resourceProvider != null)
            {
                DialogueResourceBridge.SetProvider(resourceProvider);
                Debug.Log("[UI_FavorDisplay] 已註冊 DialogueResourceProvider。");
            }

            // 在遊戲開始時啟動對話（如果 DialogueController 已設定）
            Debug.LogFormat("UI_FavorDisplay.Start()");

            // TextNode 中的變數要使用 {} 包起來，例如 {Nickname}
            var playerDataVariables = new Dictionary<string, string>
            {
                ["Nickname"] = "亞瑟"
            };

            // 使用 async 版本，對話開始前會預載入所有資源，避免對話中載入造成掉幀
            await dialogueController.StartDialogueAsync(playerDataVariables);
        }

        private void OnDestroy()
        {
            // 清除資源提供者
            if (resourceProvider != null)
            {
                DialogueResourceBridge.ClearProvider();
            }
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
