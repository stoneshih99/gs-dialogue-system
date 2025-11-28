using UnityEngine;
using Live2D.Cubism.Core;

namespace Live2DActorKit.Breath
{
    /// <summary>
    /// Live2D 呼吸控制器：透過參數模擬自然呼吸。
    /// 掛在 Live2D 模型 Root 上。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public class Live2DBreathController : MonoBehaviour
    {
        [Header("Parameter Auto Detect")]
        [SerializeField] private bool autoDetectById = true;
        [SerializeField] private string paramBreathId = "ParamBreath";
        [SerializeField] private string paramBodyYId = "ParamBodyY";
        [SerializeField] private string paramBustYId = "ParamBustY";
        [SerializeField] private string paramAngleZId = "ParamAngleZ";

        [Header("Breath Parameters (Optional Manual Bind)")]
        [SerializeField] private CubismParameter paramBreath;
        [SerializeField] private CubismParameter paramBodyY;
        [SerializeField] private CubismParameter paramBustY;
        [SerializeField] private CubismParameter paramAngleZ;

        [Header("Breathing Settings")]
        [Tooltip("啟用時自動開始呼吸")]
        [SerializeField] private bool playOnAwake = true;
        [Tooltip("每分鐘呼吸次數（成人一般 10~18）")]
        [SerializeField] private float breathsPerMinute = 12f;
        [Range(0f, 3f)]
        [SerializeField] private float strength = 1.0f;
        [Tooltip("呼吸波形曲線")]
        [SerializeField] private AnimationCurve wave = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Randomization")]
        [Tooltip("多久隨機微調一次 Bpm / Strength（秒）")]
        [SerializeField] private bool isEnableRandomization = false;
        [SerializeField] private float randomizeInterval = 10f;
        [SerializeField] private Vector2 bpmRandomOffset = new Vector2(-2f, 2f);
        [SerializeField] private Vector2 strengthRandomOffset = new Vector2(-0.2f, 0.2f);

        private bool _isPlaying;
        private float _time;
        private float _timeSinceRandomize;
        private float _currentBpm;
        private float _currentStrength;

        private float _baseBodyY;
        private float _baseBustY;
        private float _baseAngleZ;

        private CubismParameter[] _allParams;

        public float CurrentBpm => _currentBpm;
        public float CurrentStrength => _currentStrength;

        private void Awake()
        {
            CacheAllParameters();
            AutoDetectParametersIfNeeded();
            CacheBaseValues();
        }

        private void OnEnable()
        {
            if (playOnAwake)
                StartBreathing();
        }

        private void OnDisable()
        {
            _isPlaying = false;
        }

        private void LateUpdate()
        {
            if (!_isPlaying || _currentBpm <= 0f)
                return;

            _time += Time.deltaTime;
            if (isEnableRandomization)
            {
                _timeSinceRandomize += Time.deltaTime;

                if (_timeSinceRandomize >= randomizeInterval)
                {
                    RandomizeBpmAndStrength();
                    _timeSinceRandomize = 0f;
                }
            }

            float secondsPerBreath = 60f / _currentBpm;
            float phase = (_time % secondsPerBreath) / secondsPerBreath;
            float t = wave != null ? wave.Evaluate(phase) : phase;

            ApplyBreathToParameters(t);
        }

        #region Public API

        public void StartBreathing(float? overrideBpm = null, float? overrideStrength = null)
        {
            _isPlaying = true;
            _time = 0f;
            _timeSinceRandomize = 0f;

            _currentBpm = overrideBpm ?? breathsPerMinute;
            _currentStrength = overrideStrength ?? strength;

            if (isEnableRandomization)
            {
                RandomizeBpmAndStrength();
            }
        }

        public void StopBreathing()
        {
            _isPlaying = false;
        }

        public void SetStrength(float value)
        {
            strength = Mathf.Max(0f, value);
            _currentStrength = strength;
        }

        public void SetBpm(float bpm)
        {
            breathsPerMinute = Mathf.Max(1f, bpm);
            _currentBpm = breathsPerMinute;
        }

        public void ResetToBase()
        {
            if (paramBodyY != null) paramBodyY.Value = _baseBodyY;
            if (paramBustY != null) paramBustY.Value = _baseBustY;
            if (paramAngleZ != null) paramAngleZ.Value = _baseAngleZ;
            if (paramBreath != null) paramBreath.Value = paramBreath.DefaultValue;
        }

        #endregion

        #region Internal

        private void CacheAllParameters()
        {
            _allParams = GetComponentsInChildren<CubismParameter>(true);
        }

        private void AutoDetectParametersIfNeeded()
        {
            if (!autoDetectById || _allParams == null)
                return;

            CubismParameter Find(string id)
            {
                if (string.IsNullOrEmpty(id)) return null;
                foreach (var p in _allParams)
                    if (p != null && p.Id == id)
                        return p;
                return null;
            }

            if (paramBreath == null) paramBreath = Find(paramBreathId);
            if (paramBodyY == null) paramBodyY = Find(paramBodyYId);
            if (paramBustY == null) paramBustY = Find(paramBustYId);
            if (paramAngleZ == null) paramAngleZ = Find(paramAngleZId);
        }

        private void CacheBaseValues()
        {
            if (paramBodyY != null) _baseBodyY = paramBodyY.Value;
            if (paramBustY != null) _baseBustY = paramBustY.Value;
            if (paramAngleZ != null) _baseAngleZ = paramAngleZ.Value;
        }

        private void ApplyBreathToParameters(float t01)
        {
            float s = _currentStrength;

            if (paramBreath != null)
            {
                float min = paramBreath.MinimumValue;
                float max = paramBreath.MaximumValue;
                paramBreath.Value = Mathf.Lerp(min, max, t01);
                // print min max and value
                // Debug.Log($"[Breath] ParamBreath Min: {min}, Max: {max}, Value: {paramBreath.Value}");
            }

            if (paramBodyY != null)
            {
                float offset = Mathf.Sin((t01 - 0.25f) * Mathf.PI * 2f) * 0.15f * s;
                paramBodyY.Value = _baseBodyY + offset;
            }

            if (paramBustY != null)
            {
                float offset = Mathf.Sin(t01 * Mathf.PI * 2f) * 0.2f * s;
                paramBustY.Value = _baseBustY + offset;
            }

            if (paramAngleZ != null)
            {
                float offset = Mathf.Sin(t01 * Mathf.PI * 4f) * 2f * s;
                paramAngleZ.Value = _baseAngleZ + offset;
            }
        }

        private void RandomizeBpmAndStrength()
        {
            float bpmOffset = Random.Range(bpmRandomOffset.x, bpmRandomOffset.y);
            float strengthOffset = Random.Range(strengthRandomOffset.x, strengthRandomOffset.y);

            _currentBpm = Mathf.Max(1f, breathsPerMinute + bpmOffset);
            _currentStrength = Mathf.Max(0f, strength + strengthOffset);
        }

        #endregion
    }
}
