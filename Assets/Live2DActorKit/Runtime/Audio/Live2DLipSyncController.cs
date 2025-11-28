using UnityEngine;
using System;
using Live2D.Cubism.Core;

namespace Live2DActorKit.Audio
{
    /// <summary>
    /// 根據 AudioSource 音量控制 ParamMouthOpenY 嘴型開合。
    /// </summary>
    [DisallowMultipleComponent]
    public class Live2DLipSyncController : MonoBehaviour
    {
        [Header("Target Components")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private CubismParameter paramMouthOpenY;

        [Header("Detection Settings")]
        [Range(256, 8192)] public int sampleSize = 1024;
        public float sensitivity = 8f;
        [Range(0f, 1f)] public float smoothTime = 0.08f;
        [Range(0f, 1f)] public float minMouth = 0.05f;
        [Range(0f, 1f)] public float maxMouth = 1f;

        [Header("Fade Out Settings")]
        [Range(0.5f, 5f)] public float mouthFadeOutSpeed = 2.5f;

        /// <summary>
        /// 語音真正播完（含嘴型淡出）時觸發。
        /// </summary>
        public event Action OnVoiceFinished;

        private float[] _samples;
        private float _currentMouthValue;
        private float _velocity;
        private bool _wasPlaying;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (paramMouthOpenY == null)
            {
                var parameters = GetComponentsInChildren<CubismParameter>(true);
                foreach (var p in parameters)
                {
                    if (p.Id == "ParamMouthOpenY")
                    {
                        paramMouthOpenY = p;
                        break;
                    }
                }
            }

            _samples = new float[sampleSize];
        }

        private void LateUpdate()
        {
            if (audioSource == null || paramMouthOpenY == null)
                return;

            float target = 0f;

            if (audioSource.isPlaying)
            {
                _wasPlaying = true;
                audioSource.GetOutputData(_samples, 0);

                float sum = 0f;
                for (int i = 0; i < _samples.Length; i++)
                    sum += _samples[i] * _samples[i];

                float rms = Mathf.Sqrt(sum / _samples.Length);
                target = Mathf.Clamp01(rms * sensitivity);
            }
            else if (_wasPlaying)
            {
                _currentMouthValue = Mathf.Lerp(_currentMouthValue, 0f, Time.deltaTime * mouthFadeOutSpeed);
                if (_currentMouthValue < 0.01f)
                {
                    _wasPlaying = false;
                    _currentMouthValue = 0f;
                    OnVoiceFinished?.Invoke();
                }
            }

            float smoothed = Mathf.SmoothDamp(_currentMouthValue, target, ref _velocity, smoothTime);
            _currentMouthValue = smoothed;

            float mapped = Mathf.Lerp(minMouth, maxMouth, smoothed);
            paramMouthOpenY.Value = mapped * paramMouthOpenY.MaximumValue;
        }

        /// <summary>
        /// 播放語音並同步嘴型。 
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="volume"></param>
        public void PlayVoice(AudioClip clip, float volume = 1f)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.Play();
        }

        /// <summary>
        /// 停止語音播放並停止嘴型同步。 
        /// </summary>
        /// <param name="fadeOutMouth"></param>
        public void StopVoice(bool fadeOutMouth = true)
        {
            if (audioSource == null)
                return;

            audioSource.Stop();

            if (!fadeOutMouth)
            {
                _currentMouthValue = 0f;
                if (paramMouthOpenY != null)
                    paramMouthOpenY.Value = 0f;
                _wasPlaying = false;
            }
            else
            {
                _wasPlaying = true;
            }
        }

        /// <summary>
        /// 是否正在播放語音。 
        /// </summary>
        public bool IsPlaying => audioSource != null && audioSource.isPlaying;
    }
}
