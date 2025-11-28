using UnityEngine;
using System;
using System.Collections.Generic;

namespace Live2DActorKit.Audio
{
    public enum VoiceConflictPolicy
    {
        StopOthers,
    }

    /// <summary>
    /// 全域 Live2D 語音 + 嘴型 管理器。
    /// </summary>
    public class Live2DVoiceManager : MonoBehaviour
    {
        public static Live2DVoiceManager Instance { get; private set; }

        [Header("Conflict Behavior")]
        public VoiceConflictPolicy conflictPolicy = VoiceConflictPolicy.StopOthers;

        private readonly Dictionary<string, Live2DLipSyncController> _speakers =
            new Dictionary<string, Live2DLipSyncController>();

        private readonly Dictionary<string, Action> _speakerHandlers =
            new Dictionary<string, Action>();

        /// <summary>
        /// 目前正在講話的 Speaker Id。
        /// </summary>
        private string _currentSpeakerId;
        /// <summary>
        /// 目前語音行結束時的回呼。 
        /// </summary>
        private Action _currentLineOnFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region Speaker Registration

        /// <summary>
        /// 註冊一個說話者。 
        /// </summary>
        /// <param name="speakerId"></param>
        /// <param name="lipSync"></param>
        public void RegisterSpeaker(string speakerId, Live2DLipSyncController lipSync)
        {
            if (string.IsNullOrEmpty(speakerId) || lipSync == null)
                return;

            if (_speakers.TryGetValue(speakerId, out var existing))
            {
                if (_speakerHandlers.TryGetValue(speakerId, out var handler))
                    existing.OnVoiceFinished -= handler;
            }

            _speakers[speakerId] = lipSync;

            Action h = () => OnSpeakerVoiceFinished(speakerId);
            _speakerHandlers[speakerId] = h;
            lipSync.OnVoiceFinished += h;
        }

        /// <summary>
        /// 取消註冊一個說話者。 
        /// </summary>
        /// <param name="speakerId"></param>
        public void UnregisterSpeaker(string speakerId)
        {
            if (string.IsNullOrEmpty(speakerId))
                return;

            if (_speakers.TryGetValue(speakerId, out var lip))
            {
                if (_speakerHandlers.TryGetValue(speakerId, out var h))
                    lip.OnVoiceFinished -= h;
            }

            _speakers.Remove(speakerId);
            _speakerHandlers.Remove(speakerId);
        }

        #endregion

        #region Play API

        /// <summary>
        /// 播放一行語音。 
        /// </summary>
        /// <param name="speakerId"></param>
        /// <param name="clip"></param>
        /// <param name="volume"></param>
        /// <param name="onFinished"></param>
        public void PlayLine(string speakerId, AudioClip clip, float volume = 1f, Action onFinished = null)
        {
            if (!_speakers.TryGetValue(speakerId, out var lipSync) || clip == null)
            {
                Debug.LogWarning($"[Live2DVoiceManager] Speaker '{speakerId}' not registered or clip is null.");
                onFinished?.Invoke();
                return;
            }

            HandleConflictBeforePlay(speakerId);

            _currentSpeakerId = speakerId;
            _currentLineOnFinished = onFinished;

            lipSync.PlayVoice(clip, volume);
        }

        /// <summary>
        /// 停止一個說話者的語音播放。 
        /// </summary>
        /// <param name="speakerId"></param>
        /// <param name="fadeOutMouth"></param>
        public void StopSpeaker(string speakerId, bool fadeOutMouth = true)
        {
            if (!_speakers.TryGetValue(speakerId, out var lipSync))
                return;

            lipSync.StopVoice(fadeOutMouth);

            if (_currentSpeakerId == speakerId)
            {
                _currentSpeakerId = null;
                _currentLineOnFinished?.Invoke();
                _currentLineOnFinished = null;
            }
        }

        /// <summary>
        /// 停止所有說話者的語音播放。 
        /// </summary>
        /// <param name="fadeOutMouth"></param>
        public void StopAll(bool fadeOutMouth = true)
        {
            foreach (var kv in _speakers)
                kv.Value.StopVoice(fadeOutMouth);

            _currentSpeakerId = null;
            _currentLineOnFinished?.Invoke();
            _currentLineOnFinished = null;
        }

        #endregion

        #region Internal

        /// <summary>
        /// 在播放新語音前處理衝突。 
        /// </summary>
        /// <param name="newSpeakerId"></param>
        private void HandleConflictBeforePlay(string newSpeakerId)
        {
            switch (conflictPolicy)
            {
                case VoiceConflictPolicy.StopOthers:
                    foreach (var kv in _speakers)
                    {
                        if (kv.Key == newSpeakerId) continue;
                        kv.Value.StopVoice(fadeOutMouth: false);
                    }
                    break;
            }
        }

        /// <summary>
        /// 當說話者語音播放結束時呼叫。 
        /// </summary>
        /// <param name="speakerId"></param>
        private void OnSpeakerVoiceFinished(string speakerId)
        {
            if (_currentSpeakerId == speakerId)
            {
                var cb = _currentLineOnFinished;
                _currentSpeakerId = null;
                _currentLineOnFinished = null;
                cb?.Invoke();
            }
        }

        #endregion
    }
}
