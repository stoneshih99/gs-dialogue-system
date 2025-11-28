using UnityEngine;
using Live2DActorKit.Breath;
using Live2DActorKit.Audio;

namespace Live2DActorKit.Actors
{
    /// <summary>
    /// 統一管理角色狀態：Motion + Expression + Breathing。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Live2DActor))]
    [RequireComponent(typeof(Live2DBreathStateController))]
    public class Live2DActorStateController : MonoBehaviour
    {
        [System.Serializable]
        public class ActorState
        {
            public string stateName = "Idle";

            [Header("Motion")]
            public string motionId = "Idle";
            public bool loopMotion = true;
            public float fadeIn = 0.2f;
            public float fadeOut = 0.2f;

            [Header("Expression")]
            public string expressionId = "";

            [Header("Breathing")]
            public string breathState = "Idle";
        }

        [Header("States")]
        public ActorState[] states =
        {
            new ActorState { stateName = "Idle",  motionId = "Idle",  expressionId = "Neutral",  breathState = "Idle"   },
            new ActorState { stateName = "Happy", motionId = "Smile", expressionId = "Happy",    breathState = "Idle"   },
            new ActorState { stateName = "Angry", motionId = "Angry", expressionId = "Angry",    breathState = "Nervous"},
            new ActorState { stateName = "Sleep", motionId = "Sleep", expressionId = "Sleepy",   breathState = "Sleepy" },
        };

        [Header("Debug")]
        [SerializeField] private string currentState;

        /// <summary>
        /// Live2D 角色元件。
        /// </summary>
        private Live2DActor _actor;
        /// <summary>
        /// Live2D 呼吸狀態控制器。
        /// </summary>
        private Live2DBreathStateController _breath;
        /// <summary>
        /// Live2D 口型同步控制器。
        /// </summary>
        private Live2DLipSyncController _lipSync;

        private void Awake()
        {
            _actor = GetComponent<Live2DActor>();
            _breath = GetComponent<Live2DBreathStateController>();
            _lipSync = GetComponent<Live2DLipSyncController>();
        }

        private void OnEnable()
        {
            if (_lipSync != null)
                _lipSync.OnVoiceFinished += HandleVoiceFinished;
        }

        private void OnDisable()
        {
            if (_lipSync != null)
                _lipSync.OnVoiceFinished -= HandleVoiceFinished;
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(currentState))
                currentState = "Idle";

            PlayState(currentState, instant: true);
        }

        /// <summary>
        /// 播放指定狀態。 
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="instant"></param>
        public void PlayState(string stateName, bool instant = false)
        {
            var s = FindState(stateName);
            if (s == null)
            {
                Debug.LogWarning($"[Live2DActorStateController] State '{stateName}' not found.");
                return;
            }

            currentState = s.stateName;

            if (!string.IsNullOrEmpty(s.motionId))
                _actor.PlayMotion(s.motionId, s.loopMotion, s.fadeIn, s.fadeOut);

            if (!string.IsNullOrEmpty(s.expressionId))
                _actor.SetExpression(s.expressionId);

            if (_breath != null && !string.IsNullOrEmpty(s.breathState))
                _breath.SetBreathState(s.breathState, instant);
        }

        /// <summary>
        /// 重設回 Idle 狀態。
        /// </summary>
        public void ResetToIdle()
        {
            PlayState("Idle");
        }

        public void ReplayCurrentState()
        {
            PlayState(currentState, instant: true);
        }

        /// <summary>
        /// 尋找狀態資料。 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private ActorState FindState(string name)
        {
            foreach (var s in states)
                if (s.stateName.Equals(name))
                    return s;
            return null;
        }

        private void HandleVoiceFinished()
        {
            // 語音結束時，自動回 Idle（如不需要可移除此行）
            ResetToIdle();
        }
    }
}
