using System.Collections;
using SG.Dialogue.Animation;
using SG.Dialogue.Enums;
using SG.Dialogue.Presentation;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// CharacterActionNode 用於控制角色立繪的進場與退場。
    /// </summary>
    public class CharacterActionNode : DialogueNodeBase
    {
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        [Tooltip("要執行的動作類型（進場或退場）。")]
        public CharacterActionType ActionType;

        [Tooltip("目標角色的位置。")]
        public CharacterPosition TargetPosition;

        [Header("Render Mode")]
        [Tooltip("立繪的渲染模式。")]
        public PortraitRenderMode portraitRenderMode;
        
        [Tooltip("說話者的名稱，用於高亮顯示。")]
        public string speakerName;

        [Header("Sprite Settings")]
        [Tooltip("要顯示的角色 Sprite。僅在 Sprite 模式下有效。")]
        public Sprite characterSprite;

        [Header("Spine Settings")]
        [Tooltip("Spine 立繪的設定。僅在 Spine 模式下有效。")]
        public SpinePortraitConfig spinePortraitConfig;

        [Header("Live2D Settings")]
        [Tooltip("Live2D 模型的 Prefab。僅在 Live2D 模式下有效。")]
        public GameObject live2DModelPrefab;
        [Tooltip("Live2D 立繪的設定。僅在 Live2D 模式下有效。")]
        public Live2DPortraitConfig live2DPortraitConfig;

        [Tooltip("Sprite Sheet 立繪的設定。僅在 Sprite Sheet 模式下有效。")]
        public GameObject spriteSheetPresenter;
        
        [Tooltip("要播放的 Sprite Sheet 動畫名稱。僅在 Sprite Sheet 模式下有效。")]
        public string spriteSheetAnimationName;

        [Header("Action Settings")]
        [Tooltip("是否在退場時清除所有角色，而不僅僅是目標位置的角色。僅在退場時有效。")]
        public bool ClearAllOnExit;

        [Tooltip("自訂的淡入淡出持續時間（秒）。")]
        public float Duration = 0.3f;

        public override string GetNextNodeId()
        {
            return nextNodeId;
        }

        public override IEnumerator Process(DialogueController controller)
        {
            // 呼叫 DialogueVisualManager 的方法，並等待其完成
            yield return controller.VisualManager.UpdateFromCharacterActionNode(this);
        }

        public override void ClearConnectionsForClipboard()
        {
            nextNodeId = null;
        }

        public override void ClearUnityReferencesForClipboard()
        {
            // 清除所有 Unity 物件引用
            ClearAllUnityObjectFields();
        }
    }
}
