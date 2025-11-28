using UnityEngine;
using SG.Dialogue.Events;
using SG.Dialogue.Enums;

namespace SG.Dialogue
{
    /// <summary>
    /// DialogueAudioBridge 是一個橋接元件，負責監聽來自對話系統的 AudioEvent，
    /// 並將其轉換為對您專案中實際音訊管理器的呼叫。
    /// 這樣可以讓對話系統與您專案的音訊系統解耦。
    /// 您可以在這個類別中，將第三方的 AudioManager 進行橋接。
    /// 注意：這個腳本應該被放在您的遊戲場景中，並與您的音訊管理器一起運作。
    /// </summary>
    public class DialogueAudioBridge : MonoBehaviour
    {
        [Tooltip("要監聽的對話音訊事件通道。")]
        [SerializeField] private AudioEvent dialogueAudioEvent;

        // 提示：您應該在這裡引用您專案中實際的音訊管理器。
        // 例如：public YourProjectAudioManager projectAudioManager;
        // 為了演示目的，我們將直接使用 AudioSource 元件。
        [Header("音訊來源 (僅供演示)")]
        [Tooltip("用於播放背景音樂 (BGM) 的 AudioSource。")]
        [SerializeField] private AudioSource bgmSource;
        [Tooltip("用於播放音效 (SFX) 的 AudioSource。")]
        [SerializeField] private AudioSource sfxSource;

        private void OnEnable()
        {
            if (dialogueAudioEvent != null)
            {
                dialogueAudioEvent.RegisterListener(OnDialogueAudioRequested);
            }
        }

        private void OnDisable()
        {
            if (dialogueAudioEvent != null)
            {
                dialogueAudioEvent.UnregisterListener(OnDialogueAudioRequested);
            }
        }

        /// <summary>
        /// 當監聽的 AudioEvent 被觸發時，此方法會被呼叫。
        /// </summary>
        /// <param name="request">包含音訊播放資料的請求。</param>
        private void OnDialogueAudioRequested(AudioRequest request)
        {
            // 根據請求的類型，呼叫您專案的音訊管理器的方法。
            // 以下是使用 AudioSource 的範例實作。
            switch (request.ActionType)
            {
                case AudioActionType.PlayBGM:
                    if (bgmSource != null)
                    {
                        bgmSource.clip = request.Clip;
                        bgmSource.loop = request.Loop;
                        bgmSource.Play();
                        // 您可以在這裡添加淡入 (Fade In) 的邏輯
                    }
                    break;
                case AudioActionType.StopBGM:
                    if (bgmSource != null)
                    {
                        bgmSource.Stop();
                        // 您可以在這裡添加淡出 (Fade Out) 的邏輯
                    }
                    break;
                case AudioActionType.PlaySFX:
                    if (sfxSource != null && request.Clip != null)
                    {
                        sfxSource.PlayOneShot(request.Clip);
                    }
                    break;
            }
        }
    }
}
