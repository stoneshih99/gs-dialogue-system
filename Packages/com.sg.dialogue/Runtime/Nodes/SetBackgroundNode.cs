#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SG.Dialogue.Core.Instructions;
using UnityEngine;
using Newtonsoft.Json;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// SetBackgroundNode 是一個背景設定節點，專門用於在對話流程中控制背景圖片的切換。
    /// 它支援一次設定多張背景圖片，每張圖片可以有獨立的淡入淡出設定，並可指定目標圖層，實現多層背景的疊加效果。
    /// </summary>
    [Serializable]
    public class SetBackgroundNode : DialogueNodeBase
    {
        /// <summary>
        /// 定義單個背景圖片的設定。
        /// </summary>
        [Serializable]
        public class BackgroundEntry
        {
            [Tooltip("此背景圖片要作用的圖層索引（從 0 開始）。")]
            public int TargetLayerIndex = 0;

            [Tooltip("要切換的背景圖片。如果留空，則表示不改變當前圖層的背景。")]
            [JsonIgnore]
            public Sprite BackgroundSprite;

            [JsonProperty("backgroundSpriteGuid")]
            private string _backgroundSpriteGuid;

            [Tooltip("是否要清除此圖層的背景（使其變成透明或預設顏色）。")]
            public bool ClearBackground;

            [Tooltip("是否覆寫預設的背景淡入/淡出時間。")]
            public bool OverrideDuration = false;
            
            [Tooltip("覆寫的背景淡入/淡出時間（秒）。僅在『覆寫持續時間』為 true 時生效。")]
            public float Duration = 0.5f;

            [OnSerializing]
            private void OnBeforeSerialize(StreamingContext context)
            {
#if UNITY_EDITOR
                if (BackgroundSprite != null)
                {
                    string path = AssetDatabase.GetAssetPath(BackgroundSprite);
                    _backgroundSpriteGuid = AssetDatabase.AssetPathToGUID(path);
                }
                else
                {
                    _backgroundSpriteGuid = null;
                }
#endif
            }

            [OnDeserialized]
            private void OnAfterDeserialize(StreamingContext context)
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(_backgroundSpriteGuid))
                {
                    string path = AssetDatabase.GUIDToAssetPath(_backgroundSpriteGuid);
                    BackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
                else
                {
                    BackgroundSprite = null;
                }
#endif
            }
        }

        [Header("背景設定")]
        [Tooltip("要依序設定的背景圖片列表。可以同時設定多個圖層。")]
        public List<BackgroundEntry> BackgroundEntries = new List<BackgroundEntry>();

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理設定背景節點的核心邏輯。
        /// 它會呼叫視覺管理器來處理背景切換，並等待其完成。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個協程迭代器，用於等待背景切換動畫完成。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            // 呼叫視覺管理器來處理這個動作，並等待其完成。
            // VisualManager 會根據此節點的設定執行背景的淡入淡出。
            yield return controller.VisualManager.UpdateFromSetBackgroundNode(this);
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此設定背景節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
    }
}
