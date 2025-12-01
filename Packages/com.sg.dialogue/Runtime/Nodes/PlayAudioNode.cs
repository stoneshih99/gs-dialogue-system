using System;
using System.Collections;
using SG.Dialogue.Core.Instructions;
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
        [Tooltip("用於發出音訊請求的事件通道。場景中的 DialogueAudioManager 或 DialogueAudioBridge 會監聽此事件。")]
        public AudioEvent AudioEvent;

        [Header("音訊請求")]
        [Tooltip("要發送的音訊請求的詳細設定，包括音訊片段、操作類型（播放/停止）、淡入淡出時間等。")]
        public AudioRequest request;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理音訊播放節點的核心邏輯。
        /// 它會發出一個音訊請求事件，並根據情況決定是否需要等待。
        /// </summary>
        /// <param name="controller">對話總控制器（在此節點中未使用）。</param>
        /// <returns>一個協程迭代器。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            if (AudioEvent != null)
            {
                // 觸發事件，將音訊請求發送出去
                AudioEvent.Raise(request);
            }
            else
            {
                Debug.LogWarning($"音訊播放節點 '{nodeId}' 缺少 AudioEvent 的引用。");
            }

            // 如果是 BGM/BGS 且設定了淡入淡出效果，則等待淡入淡出完成，以獲得更好的流程體驗
            if (request.ActionType != AudioActionType.PlaySFX && request.FadeDuration > 0)
            {
                yield return new WaitForSeconds(request.FadeDuration);
            }
            else
            {
                // 對於音效 (SFX) 或不需要等待的操作，僅等待一幀即可繼續，以避免阻塞流程
                yield return null;
            }
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
            // 清除 AudioEvent 和 AudioClip 的引用
            AudioEvent = null;
            request.Clip = null;
        }
    }
}
