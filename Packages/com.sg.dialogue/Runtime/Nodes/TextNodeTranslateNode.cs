using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    [Serializable]
    public class TextNodeTranslateNode : DialogueNodeBase
    {
        public enum TranslateMode { Enter, Exit }
        
        [Header("設定")]
        [Tooltip("進場或退場")]
        public TranslateMode mode = TranslateMode.Exit;
        
        [Tooltip("動畫時間")]
        public float duration = 0.5f;
        
        [Tooltip("位移偏移量 (相對於原始位置)。例如 (0, -100) 表示從下方進場或退場到下方。")]
        public Vector2 slideOffset = new Vector2(0, -100);
        
        [Tooltip("緩動函數")]
        public Ease ease = Ease.OutQuad;

        [Header("流程")]
        [Tooltip("下一個節點 ID")]
        public string nextNodeId;

        public override async UniTask Process(DialogueController controller)
        {
            bool show = mode == TranslateMode.Enter;
            await controller.UiManager.AnimatePanel(show, duration, slideOffset, ease);
        }

        public override string GetNextNodeId() => nextNodeId;
        
        public override void ClearConnectionsForClipboard() { nextNodeId = null; }
    }
}
