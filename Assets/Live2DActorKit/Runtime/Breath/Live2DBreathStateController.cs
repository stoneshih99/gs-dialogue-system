using UnityEngine;

namespace Live2DActorKit.Breath
{
    /// <summary>
    /// 用「狀態」管理呼吸節奏（Idle / Nervous / Sleepy...）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Live2DBreathController))]
    public class Live2DBreathStateController : MonoBehaviour
    {
        [System.Serializable]
        public class BreathState
        {
            /// <summary>
            /// 狀態名稱。 
            /// </summary>
            [Tooltip("狀態名稱")]
            public string stateName = "Idle";
            /// <summary>
            /// 目標呼吸速率（每分鐘呼吸次數）。 
            /// </summary>
            [Tooltip("目標呼吸速率（每分鐘呼吸次數）")]
            public float targetBpm = 12f;
            /// <summary>
            /// 目標呼吸強度。 
            /// </summary>
            [Tooltip("目標呼吸強度")]
            public float targetStrength = 1f;
            /// <summary>
            /// 過渡到此狀態所需時間（秒）。 
            /// </summary>
            [Tooltip("過渡到此狀態所需時間（秒）")]
            public float transitionTime = 1.5f;
        }

        [Header("Breathing States")]
        public BreathState[] states =
        {
            new BreathState { stateName = "Idle",    targetBpm = 12f, targetStrength = 1f,   transitionTime = 1.5f },
            new BreathState { stateName = "Nervous", targetBpm = 18f, targetStrength = 1.5f, transitionTime = 0.8f },
            new BreathState { stateName = "Sleepy",  targetBpm = 8f,  targetStrength = 0.4f, transitionTime = 2.5f },
        };

        [Header("Debug")]
        [SerializeField] private string currentState = "Idle";

        private Live2DBreathController _breath;
        private Coroutine _transitionRoutine;

        private void Awake()
        {
            if (_breath == null)
            {
                _breath = GetComponent<Live2DBreathController>();
            }
        }

        private void Start()
        {
            SetBreathState(currentState, instant: true);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;
            
            Debug.LogFormat("[Live2DBreathStateController] OnValidate: Applying state '{0}'", currentState);
            if (_breath == null)
            {
                _breath = GetComponent<Live2DBreathController>();
            }
            var state = FindState(currentState);
            if (state != null)
            {
                _breath.SetBpm(state.targetBpm);
                _breath.SetStrength(state.targetStrength);
            }
        }
#endif
        /// <summary>
        /// 設定呼吸狀態。 
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="instant">是否立即播放</param>
        public void SetBreathState(string stateName, bool instant = false)
        {
            var state = FindState(stateName);
            if (state == null)
            {
                Debug.LogWarning($"[Live2DBreathStateController] State '{stateName}' not found.");
                return;
            }

            currentState = stateName;

            if (instant)
            {
                _breath.SetBpm(state.targetBpm);
                _breath.SetStrength(state.targetStrength);
                return;
            }

            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);

            _transitionRoutine = StartCoroutine(TransitionToState(state));
        }

        private BreathState FindState(string name)
        {
            foreach (var s in states)
                if (s.stateName == name)
                    return s;
            return null;
        }

        private System.Collections.IEnumerator TransitionToState(BreathState state)
        {
            float startBpm = _breath.CurrentBpm > 0 ? _breath.CurrentBpm : state.targetBpm;
            float startStrength = _breath.CurrentStrength > 0 ? _breath.CurrentStrength : state.targetStrength;

            float t = 0f;
            float duration = Mathf.Max(0.01f, state.transitionTime);

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float bpm = Mathf.Lerp(startBpm, state.targetBpm, t);
                float strength = Mathf.Lerp(startStrength, state.targetStrength, t);

                _breath.SetBpm(bpm);
                _breath.SetStrength(strength);

                yield return null;
            }

            _breath.SetBpm(state.targetBpm);
            _breath.SetStrength(state.targetStrength);
        }
    }
}
