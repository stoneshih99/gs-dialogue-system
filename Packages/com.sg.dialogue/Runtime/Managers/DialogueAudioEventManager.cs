using System;
using System.Collections.Generic;
using SG.Dialogue.Events;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Dialogue.Managers
{
    
    /// <summary>
    /// 用於在 Inspector 中設定事件名稱與回應的配對。
    /// </summary>
    [Serializable]
    public struct AudioEventMapping
    {
        public string EventName;
        public UnityEvent<AudioRequest> Response;
    }

    
    /// <summary>
    /// DialogueAudioEventManager 負責管理對話系統中的音訊播放，包括背景音樂 (BGM) 和音效 (SFX)。
    /// 它透過監聽 AudioEvent 來回應音訊請求，實現了與對話節點的解耦。
    /// 這個管理器提供了比 DialogueAudioBridge 更完整的功能，例如 BGM 的淡入淡出。
    /// </summary>
    public class DialogueAudioEventManager : MonoBehaviour
    {
        [Header("事件通道")]
        [Tooltip("要監聽的 AudioEvent 事件通道。")]
        [SerializeField] private AudioEvent audioEventChannel;

        [Header("事件映射")]
        [Tooltip("設定事件名稱與對應的回應。")]
        [SerializeField] private List<AudioEventMapping> eventMappings;

        // 用於快速查找的字典
        private Dictionary<string, UnityEvent<AudioRequest>> _eventDictionary;

        private void Awake()
        {
            // 將 Inspector 中的列表轉換為字典，以提高執行時的查找效率。
            _eventDictionary = new Dictionary<string, UnityEvent<AudioRequest>>();
            foreach (var mapping in eventMappings)
            {
                if (!string.IsNullOrEmpty(mapping.EventName) && !_eventDictionary.ContainsKey(mapping.EventName))
                {
                    _eventDictionary.Add(mapping.EventName, mapping.Response);
                }
            }
        }

        private void OnEnable()
        {
            if (audioEventChannel != null)
            {
                audioEventChannel.RegisterListener(OnAudioRequest);
            }
        }

        private void OnDisable()
        {
            if (audioEventChannel != null)
            {
                audioEventChannel.UnregisterListener(OnAudioRequest);
            }
        }

        /// <summary>
        /// 接收到事件請求時的回呼。
        /// </summary>
        private void OnAudioRequest(AudioRequest request)
        {
            Debug.Log($"[DialogueAudioManager] 收到事件請求: {request.EventName}");

            // 嘗試從字典中找到對應的 UnityEvent 並觸發它。
            if (_eventDictionary.TryGetValue(request.EventName, out UnityEvent<AudioRequest> response))
            {
                response?.Invoke(request);
            }
            else
            {
                Debug.LogWarning($"[DialogueAudioManager] 找不到針對事件 '{request.EventName}' 的回應設定。");
            }
        }

        // /// <summary>
        // /// 處理接收到的音訊請求。
        // /// </summary>
        // /// <param name="request">音訊請求的詳細資料。</param>
        // private void OnAudioRequest(AudioRequest request)
        // {
        //     Debug.LogFormat("[DialogueAudioManager] 接收到音訊請求: {0}", request);
        //     // 如果請求中指定了淡入淡出時間，則使用它；否則使用預設值。
        //     float fadeDuration = request.FadeDuration >= 0 ? request.FadeDuration : defaultBgmFadeDuration;
        //
        //     switch (request.ActionType)
        //     {
        //         case AudioActionType.PlayBGM:
        //             if (request.Clip != null)
        //             {
        //                 FadeBgm(request.Clip, true, fadeDuration, request.Loop);
        //             }
        //             break;
        //         case AudioActionType.StopBGM:
        //             FadeBgm(null, false, fadeDuration);
        //             break;
        //         case AudioActionType.PlaySFX:
        //             PlaySfx(request.Clip);
        //             break;
        //     }
        // }
        //
        // /// <summary>
        // /// 播放一個單次的音效。
        // /// </summary>
        // /// <param name="clip">要播放的音訊片段。</param>
        // public void PlaySfx(AudioClip clip)
        // {
        //     if (sfxSource != null && clip != null)
        //     {
        //         sfxSource.PlayOneShot(clip);
        //     }
        // }
        //
        // /// <summary>
        // /// 執行背景音樂的淡入或淡出。
        // /// </summary>
        // /// <param name="targetClip">目標音訊片段。若為 null 且為淡出，則表示停止目前的 BGM。</param>
        // /// <param name="fadeIn">是否為淡入。</param>
        // /// <param name="duration">淡入/淡出的持續時間。</param>
        // /// <param name="loop">是否循環播放。</param>
        // private void FadeBgm(AudioClip targetClip, bool fadeIn, float duration, bool loop = true)
        // {
        //     if (bgmSource == null) return;
        //     if (_bgmFadeRoutine != null)
        //     {
        //         StopCoroutine(_bgmFadeRoutine);
        //     }
        //     _bgmFadeRoutine = StartCoroutine(FadeBgmRoutine(targetClip, fadeIn, duration, loop));
        // }
        //
        // /// <summary>
        // /// 處理背景音樂淡入淡出的協程。
        // /// </summary>
        // private IEnumerator FadeBgmRoutine(AudioClip targetClip, bool fadeIn, float duration, bool loop)
        // {
        //     float startVolume = bgmSource.volume;
        //     float endVolume = fadeIn ? 1f : 0f;
        //
        //     // 如果是淡入且是新的 BGM，則設定新的 Clip 並從頭播放
        //     if (fadeIn && bgmSource.clip != targetClip)
        //     {
        //         bgmSource.clip = targetClip;
        //         bgmSource.loop = loop;
        //         bgmSource.volume = 0f;
        //         startVolume = 0f;
        //         if (targetClip != null)
        //         {
        //             bgmSource.Play();
        //         }
        //         else
        //         {
        //             // 如果目標 Clip 為空，則直接停止
        //             bgmSource.Stop();
        //             yield break;
        //         }
        //     }
        //
        //     // 如果持續時間為 0 或更少，立即設定最終音量並結束
        //     if (duration <= 0)
        //     {
        //         bgmSource.volume = endVolume;
        //         if (endVolume == 0) bgmSource.Stop();
        //         yield break;
        //     }
        //
        //     // 在指定時間內平滑地改變音量
        //     float timer = 0f;
        //     while (timer < duration)
        //     {
        //         bgmSource.volume = Mathf.Lerp(startVolume, endVolume, timer / duration);
        //         timer += Time.deltaTime;
        //         yield return null;
        //     }
        //
        //     bgmSource.volume = endVolume;
        //     // 如果是淡出，則在結束後停止播放並清除 Clip
        //     if (endVolume == 0)
        //     {
        //         bgmSource.Stop();
        //         bgmSource.clip = null;
        //     }
        // }
    }
}
