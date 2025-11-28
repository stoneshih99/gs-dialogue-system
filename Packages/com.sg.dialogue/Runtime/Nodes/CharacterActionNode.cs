#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Runtime.Serialization;
using SG.Dialogue.Core.Instructions;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using SG.Dialogue.Presentation;
using UnityEngine;
using Newtonsoft.Json;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// CharacterActionNode 是一個角色動作節點，專門用於控制對話流程中角色的非對白行為，
    /// 例如進場、退場、改變造型（立繪）等。
    /// </summary>
    [Serializable]
    public class CharacterActionNode : DialogueNodeBase
    {
        [Header("事件通道")]
        [Tooltip("用於發出音訊請求的事件通道。")]
        public AudioEvent AudioEvent;

        [Header("動作設定")]
        [Tooltip("此節點要執行的角色動作類型。")]
        public CharacterActionType ActionType = CharacterActionType.Enter;

        [Tooltip("要執行動作的角色位置（例如：左、中、右）。")]
        public CharacterPosition TargetPosition = CharacterPosition.Center;

        [Tooltip("當動作類型為『退場』(Exit) 時，是否清除所有角色，而非僅清除指定位置的角色。")]
        public bool ClearAllOnExit = false;
        
        [Tooltip("是否覆寫預設的角色動畫（如淡入淡出）的持續時間。")]
        public bool OverrideDuration = false;
        
        [Tooltip("覆寫的角色動畫持續時間（秒）。僅在『覆寫持續時間』為 true 時生效。")]
        public float Duration = 0.3f;

        [Header("進場動作的視覺設定")]
        [Tooltip("當動作類型為『進場』(Enter) 時，要顯示的角色立繪 Sprite。")]
        [JsonIgnore] // 忽略此欄位，避免直接序列化 Sprite 物件
        public Sprite characterSprite;
        
        [JsonProperty("characterSpriteGuid")] // 實際序列化的是這個 GUID 字串
        private string _characterSpriteGuid;

        [Tooltip("當動作類型為『進場』(Enter) 且立繪呈現模式為 Spine 時，Spine 模型的額外設定。")]
        public SpinePortraitConfig spinePortraitConfig;

        [Tooltip("當動作類型為『進場』(Enter) 且立繪呈現模式為 Live2D 時，要實例化的 Live2D 模型 Prefab。")]
        [JsonIgnore] // 忽略此欄位，避免直接序列化 GameObject
        public GameObject live2DModelPrefab;

        [JsonProperty("live2DModelPrefabGuid")] // 實際序列化的是這個 GUID 字串
        private string _live2DModelPrefabGuid;

        [Tooltip("此節點立繪的呈現模式（Sprite, Spine, Live2D）。")]
        public PortraitRenderMode portraitRenderMode = PortraitRenderMode.Sprite;
        [Tooltip("當動作類型為『進場』(Enter) 時，設定此角色的說話者名稱。這會影響後續文本節點對角色的高亮/灰階處理。")]
        public string speakerName;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。如果留空，對話可能會在此處結束。")]
        public string nextNodeId;

        [OnSerializing]
        private void OnBeforeSerialize(StreamingContext context)
        {
#if UNITY_EDITOR
            // 處理 Sprite
            if (characterSprite != null)
            {
                string path = AssetDatabase.GetAssetPath(characterSprite);
                _characterSpriteGuid = AssetDatabase.AssetPathToGUID(path);
            }
            else
            {
                _characterSpriteGuid = null;
            }

            // 處理 GameObject (Prefab)
            if (live2DModelPrefab != null)
            {
                string path = AssetDatabase.GetAssetPath(live2DModelPrefab);
                _live2DModelPrefabGuid = AssetDatabase.AssetPathToGUID(path);
            }
            else
            {
                _live2DModelPrefabGuid = null;
            }
#endif
        }

        [OnDeserialized]
        private void OnAfterDeserialize(StreamingContext context)
        {
#if UNITY_EDITOR
            // 處理 Sprite
            if (!string.IsNullOrEmpty(_characterSpriteGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(_characterSpriteGuid);
                characterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            else
            {
                characterSprite = null;
            }

            // 處理 GameObject (Prefab)
            if (!string.IsNullOrEmpty(_live2DModelPrefabGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(_live2DModelPrefabGuid);
                live2DModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            else
            {
                live2DModelPrefab = null;
            }
#endif
        }

        /// <summary>
        /// 處理角色動作節點的核心邏輯。
        /// 它會呼叫視覺管理器來處理視覺效果，並等待其完成。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個協程迭代器，用於等待視覺效果完成。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            // 呼叫視覺管理器處理視覺效果，並等待其完成。
            // VisualManager 會根據此節點的設定執行進場、退場等動畫。
            yield return controller.VisualManager.UpdateFromCharacterActionNode(this);
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此角色動作節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
    }
}
