#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SG.Dialogue.Core.Instructions;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using SG.Dialogue.Variables;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// TransitionNode 是一個過場節點，用於在對話流程中處理場景切換、視覺和音效的變更。
    /// 它是一個多功能節點，可以同時控制角色、背景、BGM、SFX 以及淡入淡出效果，常用於場景轉換或重要的劇情轉折點。
    /// </summary>
    [Serializable]
    public class TransitionNode : DialogueNodeBase
    {
        [Header("事件通道")]
        [Tooltip("用於發出音訊請求的事件通道。")]
        public AudioEvent AudioEvent;

        [Tooltip("過場的說明或備註（僅供編輯器中識別，不會顯示在遊戲 UI 上）。")]
        [TextArea(1, 3)] public string note;

        [Header("角色視覺")]
        [Tooltip("此過場要顯示的角色立繪。如果留空，則表示不改變目前的角色。")]
        [JsonIgnore]
        public Sprite characterSprite;
        
        [JsonProperty("characterSpriteGuid")]
        private string _characterSpriteGuid;

        [Tooltip("此過場角色立繪要出現的位置：左 / 中 / 右。")]
        public CharacterPosition characterPosition = CharacterPosition.Left;
        [Tooltip("是否在此過場時清除畫面上所有角色立繪（讓所有角色退場）。")]
        public bool clearCharacters;

        [Header("背景視覺")]
        [Tooltip("此過場要切換的背景圖片。如果留空，則表示不改變目前的背景。")]
        [JsonIgnore]
        public Sprite backgroundSprite;

        [JsonProperty("backgroundSpriteGuid")]
        private string _backgroundSpriteGuid;

        [Tooltip("是否在此過場時清除背景（使其變成透明或預設狀態）。")]
        public bool clearBackground;

        [Header("流程控制")]
        [Tooltip("此過場結束後，對話流程要前往的下一個節點 ID。")]
        public string nextNodeId;

        [Header("變數與事件")]
        [Tooltip("進入此過場節點時要變更的變數列表。")]
        public List<VariableChange> variableChanges = new List<VariableChange>();
        [Tooltip("進入此節點時觸發的 UnityEvent。")]
        public UnityEvent onEnter;
        [Tooltip("退出此節點時觸發的 UnityEvent。")]
        public UnityEvent onExit;

        [Header("黑畫面 / 淡入淡出覆寫")]
        [Tooltip("是否在此過場時使用黑畫面過渡。通常用於場景切換。")]
        public bool useBlackScreen;
        [Tooltip("黑畫面停留的時間（秒）。僅在『使用黑畫面』為 true 時有效。")]
        public float blackScreenDuration = 0.0f;
        [Tooltip("是否覆寫 VisualManager 中預設的背景淡入淡出時間。")]
        public bool overrideBackgroundFade;
        [Tooltip("覆寫的背景淡入淡出時間（秒）。")]
        public float backgroundFadeOverride = 0.3f;
        [Tooltip("是否覆寫 VisualManager 中預設的角色淡入淡出時間。")]
        public bool overrideCharacterFade;
        [Tooltip("覆寫的角色淡入淡出時間（秒）。")]
        public float characterFadeOverride = 0.2f;

        [OnSerializing]
        private void OnBeforeSerialize(StreamingContext context)
        {
#if UNITY_EDITOR
            if (characterSprite != null)
            {
                string path = AssetDatabase.GetAssetPath(characterSprite);
                _characterSpriteGuid = AssetDatabase.AssetPathToGUID(path);
            }
            else
            {
                _characterSpriteGuid = null;
            }

            if (backgroundSprite != null)
            {
                string path = AssetDatabase.GetAssetPath(backgroundSprite);
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
            if (!string.IsNullOrEmpty(_characterSpriteGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(_characterSpriteGuid);
                characterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            else
            {
                characterSprite = null;
            }

            if (!string.IsNullOrEmpty(_backgroundSpriteGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(_backgroundSpriteGuid);
                backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            else
            {
                backgroundSprite = null;
            }
#endif
        }

        /// <summary>
        /// 處理過場節點的核心邏輯。
        /// 它會指揮視覺管理器執行過場效果，並等待其完成。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個協程迭代器，用於等待過場動畫完成。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            // 指揮視覺管理器處理過場的視覺效果，並等待其完成
            yield return controller.VisualManager.UpdateFromTransitionNode(this);
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此過場節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
    }
}
