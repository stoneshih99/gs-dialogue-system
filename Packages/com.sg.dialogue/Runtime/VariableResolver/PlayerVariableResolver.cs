using System.Collections.Generic;
using UnityEngine;

namespace SG.Dialogue.VariableResolver
{
    /// <summary>
    /// 一個簡單的玩家資料存儲類別（範例）。
    /// 在您的實際專案中，這可能是更複雜的玩家管理系統。
    /// </summary>
    public static class PlayerProfile
    {
        public static string PlayerName { get; set; } = "亞瑟";
    }

    /// <summary>
    /// 一個可擴充的外部變數解析器。
    /// 它本身不包含任何遊戲邏輯，而是作為一個中央樞紐，管理所有註冊給它的「資料提供者」(IVariableDataProvider)。
    /// 當對話系統請求一個變數時，它會依次詢問每一個提供者，直到找到能回應請求的提供者為止。
    /// </summary>
    [RequireComponent(typeof(DialogueController))]
    public class PlayerVariableResolver : MonoBehaviour
    {
        private DialogueController _dialogueController;
        
        // 維護一個所有已註冊資料提供者的列表
        private readonly List<IVariableDataProvider> _providers = new List<IVariableDataProvider>();

        private void Awake()
        {
            _dialogueController = GetComponent<DialogueController>();
        }

        private void OnEnable()
        {
            if (_dialogueController != null)
            {
                _dialogueController.OnResolveVariable += ResolveCustomVariables;
            }
        }

        private void OnDisable()
        {
            if (_dialogueController != null)
            {
                _dialogueController.OnResolveVariable -= ResolveCustomVariables;
            }
        }

        /// <summary>
        /// 註冊一個新的資料提供者。
        /// </summary>
        public void RegisterProvider(IVariableDataProvider provider)
        {
            if (provider != null && !_providers.Contains(provider))
            {
                _providers.Add(provider);
            }
        }

        /// <summary>
        /// 取消註冊一個資料提供者。
        /// </summary>
        public void UnregisterProvider(IVariableDataProvider provider)
        {
            if (provider != null)
            {
                _providers.Remove(provider);
            }
        }

        /// <summary>
        /// 這是實際處理變數解析的方法。它會遍歷所有提供者來尋找值。
        /// </summary>
        private string ResolveCustomVariables(string variableName)
        {
            // 遍歷所有已註冊的提供者
            foreach (var provider in _providers)
            {
                // 嘗試從當前提供者獲取值
                if (provider.TryGetValue(variableName, out string value))
                {
                    // 如果成功獲取，立即返回該值
                    return value;
                }
            }

            // 如果所有提供者都無法解析這個變數，返回 null
            return null;
        }
    }
}
