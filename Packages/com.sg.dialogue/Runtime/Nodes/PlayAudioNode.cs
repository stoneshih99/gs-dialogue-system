using System;
using System.Collections;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// PlayAudioNode 是一個音訊播放節點，專門用於在對話流程中觸發背景音樂 (BGM) 和音效 (SFX) 的播放。
    /// 它通過 ScriptableObject 事件 (AudioEvent) 與音訊管理器解耦。
    /// </summary>
    [Serializable]
    public class PlayAudioNode : DialogueNodeBase
    {
        [Header("事件通道")]
        [Tooltip("所有音訊請求共用的事件通道 (例如 AudioChannel)。")]
        public AudioEvent AudioEvent;

        [Header("音訊設定")]
        [Tooltip("音訊動作類型")]
        public AudioActionType ActionType;
        
        [Tooltip("音訊名稱 (Key)")]
        public string SoundName;
        
        [Tooltip("是否循環 (僅 BGM 有效)")]
        public bool Loop;
        
        [Tooltip("淡入/淡出時間")]
        public float FadeDuration = 1f;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理音訊播放節點的核心邏輯。
        /// 它會發出一個音訊請求事件。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個協程迭代器。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            if (AudioEvent != null)
            {
                // 建立請求並發送
                var request = new AudioRequest(ActionType, SoundName, Loop, FadeDuration);
                AudioEvent.Raise(request);
            }
            else
            {
                Debug.LogWarning($"PlayAudioNode '{nodeId}' 缺少 AudioEvent 引用。");
            }

            yield return null;
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此音訊播放節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }

        public override void ClearConnectionsForClipboard()
        {
            nextNodeId = null;
        }

        public override void ClearUnityReferencesForClipboard()
        {
            // 清除 AudioEvent 的引用
            AudioEvent = null;
        }
    }
}
