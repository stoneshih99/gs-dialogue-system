using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using UnityEngine;

namespace SG.Dialogue
{
    /// <summary>
    /// DialogueAudioBridge 是一個橋接元件，負責監聽來自對話系統的 AudioEvent，
    /// 並將其轉換為對您專案中實際音訊管理器的呼叫。
    /// </summary>
    public class DialogueAudioBridge : MonoBehaviour
    {
        [Tooltip("要監聽的對話音訊事件通道。")] 
        [SerializeField] private AudioEvent[] allAudioEvents;

        private void OnEnable()
        {
            if (allAudioEvents == null) return;
            foreach (var audioEvent in allAudioEvents)
            {
                if (audioEvent != null)
                {
                    audioEvent.RegisterListener(OnDialogueAudioRequested);
                }
            }
        }

        private void OnDisable()
        {
            if (allAudioEvents == null) return;
            foreach (var audioEvent in allAudioEvents)
            {
                if (audioEvent != null)
                {
                    audioEvent.UnregisterListener(OnDialogueAudioRequested);
                }
            }
        }

        /// <summary>
        /// 當監聽的 AudioEvent 被觸發時，此方法會被呼叫。
        /// </summary>
        /// <param name="request">包含音訊播放資料的請求。</param>
        private void OnDialogueAudioRequested(AudioRequest request)
        {
            // validate
            if (string.IsNullOrEmpty(request.SoundName) && request.ActionType != AudioActionType.StopBGM)
            {
                Debug.LogErrorFormat("DialogueAudioBridge: 收到無效的音訊請求，音訊名稱為空。");
                return;
            }
            
            Debug.LogFormat("DialogueAudioBridge: 收到音訊請求，動作類型：{0}，音訊名稱：{1}",
                request.ActionType, request.SoundName);
            
            switch (request.ActionType)
            {
                case AudioActionType.PlayBGM:
                    break;
                case AudioActionType.StopBGM:
                    break;
                case AudioActionType.PlaySFX:
                    break;
            }
        }
    }
}
