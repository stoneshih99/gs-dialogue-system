using UnityEngine;
using Live2DActorKit.Core;
using Live2DActorKit.Breath;
using Live2DActorKit.Audio;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.LookAt;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.MotionFade;
using System.Collections.Generic;

namespace Live2DActorKit.Actors
{
    /// <summary>
    /// 封裝 Cubism：Motion / Expression / LookAt / Breath / Voice。
    /// 掛在 Live2D 模型 Root。
    /// Actor 擁有多個 Controller 來分別處理各種功能。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CubismMotionController))]
    [RequireComponent(typeof(CubismFadeController))]
    public class Live2DActor : MonoBehaviour, ILive2DActor
    {
        private static readonly int Color1 = Shader.PropertyToID("_Color");

        [Header("Cubism Components")]
        [SerializeField] private CubismMotionController motionController;
        [SerializeField] private CubismFadeController fadeController;
        [SerializeField] private CubismExpressionController expressionController;
        [SerializeField] private CubismLookController lookController;

        [Header("Motion Clips")]
        [Tooltip("可選：在這裡登記可被 PlayMotion(string motionId) 使用的 AnimationClip，預設以 clip.name 對應 motionId。")]
        [SerializeField] private AnimationClip[] motionClips;

        [Header("Parameters")]
        [Tooltip("控制嘴巴開合的參數")]
        [SerializeField] private CubismParameter mouthOpenParam;
        [Tooltip("不指定時會嘗試用材質透明度來控制")]
        [SerializeField] private CubismParameter opacityParam;

        [Header("Helpers")]
        [Tooltip("用於 ScreenToWorldPoint 的相機（2D/3D 都會用到）")]
        [SerializeField] private Camera uiCamera;
        [Tooltip("若角色在 Canvas 下，可填 RectTransform 以得到更準確的 LookAt")]
        [SerializeField] private RectTransform rectTransform;
        [Tooltip("角色所屬的 Canvas（若無則視為 3D 世界物件）")]
        [SerializeField] private Canvas parentCanvas;

        [Tooltip("呼吸控制器")]
        [SerializeField] private Live2DBreathController breathController;
        [Tooltip("呼吸狀態控制器")]
        [SerializeField] private Live2DBreathStateController breathStateController;
        [Tooltip("語音唇同步控制器")]
        [SerializeField] private Live2DLipSyncController lipSync;

        // --- 重構後新增的快取欄位 ---
        /// <summary>
        /// 用於快速查詢動作的字典，以提升效能。
        /// </summary>
        private readonly Dictionary<string, AnimationClip> _motionClipDict = new();
        /// <summary>
        /// 用於快速查詢表情索引的字典，以提升效能。
        /// </summary>
        private readonly Dictionary<string, int> _expressionIndexDict = new();
        /// <summary>
        /// 快取所有 Renderer，避免在 SetOpacity/SetColor 中重複搜尋。
        /// </summary>
        private Renderer[] _renderers;
        /// <summary>
        /// 快取 Cubism Core Model，用於取得 Renderer。
        /// </summary>
        private CubismModel _model;

        /// <summary>
        /// 目前正在播放的 Motion Id。
        /// </summary>
        private string _currentMotionId;
        /// <summary>
        /// LookAt 目標快取（必須是實作 ICubismLookTarget 的 Component）。
        /// </summary>
        private Component _lookTargetComponent;

        private void Awake()
        {
            // 在 Awake 中快取所有需要的元件和資料
            _model = GetComponent<CubismModel>();
            if (_model != null)
            {
                _renderers = _model.GetComponents<Renderer>(); // 從 CubismModel 獲取 Renderer 更可靠
            }

            // 將 motionClips 陣列轉換為字典以加速查詢
            if (motionClips != null)
            {
                foreach (var clip in motionClips)
                {
                    if (clip != null && !_motionClipDict.ContainsKey(clip.name))
                    {
                        _motionClipDict[clip.name] = clip;
                    }
                }
            }

            // 將表情列表轉換為字典
            if (expressionController != null && expressionController.ExpressionsList != null)
            {
                var exprs = expressionController.ExpressionsList.CubismExpressionObjects;
                for (int i = 0; i < exprs.Length; i++)
                {
                    if (exprs[i] != null && !_expressionIndexDict.ContainsKey(exprs[i].name))
                    {
                        _expressionIndexDict[exprs[i].name] = i;
                    }
                }
            }
        }

        private void Reset()
        {
            motionController ??= GetComponent<CubismMotionController>();
            fadeController ??= GetComponent<CubismFadeController>();
            expressionController ??= GetComponent<CubismExpressionController>();
            lookController ??= GetComponent<CubismLookController>();
            breathController ??= GetComponent<Live2DBreathController>();
            breathStateController ??= GetComponent<Live2DBreathStateController>();
            lipSync ??= GetComponent<Live2DLipSyncController>();

            rectTransform ??= GetComponent<RectTransform>();
            parentCanvas ??= GetComponentInParent<Canvas>();
        }

        #region Motion

        /// <summary>
        /// 依照 motionId 播放對應的 AnimationClip。
        /// </summary>
        public void PlayMotion(string motionId, bool loop = false, float fadeIn = 0.2f, float fadeOut = 0.2f)
        {
            if (motionController == null || string.IsNullOrEmpty(motionId))
                return;

            // 使用字典進行 O(1) 快速查詢，而不是遍歷陣列
            if (_motionClipDict.TryGetValue(motionId, out var clip))
            {
                // Cubism 官方建議：由 CubismMotionController.PlayAnimation() 播放，CubismFadeController 會自動處理淡入淡出
                motionController.PlayAnimation(clip, isLoop: loop);
                _currentMotionId = motionId;
            }
            else
            {
                Debug.LogWarning($"[Live2DActor] Motion clip '{motionId}' not found on {name}. 請確認已將對應的 AnimationClip 登記到 Live2DActor.motionClips。");
            }
        }

        /// <summary>
        /// 停止目前的動作播放。
        /// </summary>
        /// <param name="layer"></param>
        public void StopMotion(string layer = "Base")
        {
            // CubismMotionController 沒有直接 Stop API，
            // 常見做法是播放一個 Idle / 空白動作覆蓋，或交由外部控制 Animator。
            // 這裡只清除 currentMotionId，實際停止行為交給上層決定。
            _currentMotionId = null;
        }

        /// <summary>
        /// 目前是否正在播放指定的 motionId。
        /// </summary>
        public bool IsPlaying(string motionId) => _currentMotionId == motionId;

        #endregion

        #region Expression

        /// <summary>
        /// 設定表情（Expression）。
        /// </summary>
        public void SetExpression(string expressionId)
        {
            if (expressionController == null || string.IsNullOrEmpty(expressionId))
                return;

            // 使用字典快速查詢表情索引
            if (_expressionIndexDict.TryGetValue(expressionId, out int index))
            {
                expressionController.CurrentExpressionIndex = index;
            }
            else
            {
                Debug.LogWarning($"[Live2DActor] Expression '{expressionId}' not found in ExpressionList on {name}.");
            }
        }

        /// <summary>
        /// 清除目前表情，回到預設狀態。
        /// </summary>
        public void ClearExpression()
        {
            if (expressionController == null)
                return;
            // 設成 -1 通常代表「無表情」、回預設狀態
            expressionController.CurrentExpressionIndex = -1;
        }

        #endregion

        #region LookAt

        /// <summary>
        /// 快取 LookAt 目標元件。
        /// </summary>
        private void CacheLookTarget()
        {
            if (lookController == null) return;
            if (_lookTargetComponent == null && lookController.Target != null)
            {
                _lookTargetComponent = lookController.Target as Component;
                if (_lookTargetComponent == null)
                {
                    Debug.LogWarning($"[Live2DActor] LookController.Target on {name} 不是 Component，請指定實作 ICubismLookTarget 的元件（例如 CubismLookTarget）。");
                }
            }
        }

        /// <summary>
        /// 以螢幕座標（ScreenPosition）讓角色看向某點。
        /// 若角色在 Canvas 下，會自動使用 Canvas 友善的轉換；否則使用 3D 世界轉換。
        /// </summary>
        public void LookAt(Vector2 screenPosition)
        {
            if (lookController == null) return;
            CacheLookTarget();
            if (_lookTargetComponent == null) return;

            if (rectTransform != null && parentCanvas != null)
            {
                LookAtOnCanvas(screenPosition);
            }
            else
            {
                LookAtInWorld(screenPosition);
            }
        }

        /// <summary>
        /// 在 Canvas 下的 LookAt。
        /// </summary>
        private void LookAtOnCanvas(Vector2 screenPosition)
        {
            if (rectTransform == null || parentCanvas == null) return;
            Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (parentCanvas.worldCamera ?? uiCamera);
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, screenPosition, cam, out var world))
            {
                _lookTargetComponent.transform.position = world;
            }
        }

        /// <summary>
        /// 在 3D 世界中的 LookAt。
        /// </summary>
        private void LookAtInWorld(Vector2 screenPosition)
        {
            if (uiCamera == null) return;
            float z = Mathf.Abs(uiCamera.transform.position.z - transform.position.z);
            var world = uiCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, z));
            _lookTargetComponent.transform.position = world;
        }

        /// <summary>
        /// 重設 LookAt 目標位置到角色正前方。
        /// </summary>
        public void ResetLookAt()
        {
            if (lookController == null) return;
            CacheLookTarget();
            if (_lookTargetComponent == null) return;
            // 回到角色正前方一小段距離
            var forwardTarget = transform.position + transform.forward * 1f;
            _lookTargetComponent.transform.position = forwardTarget;
        }

        #endregion

        #region Mouth

        /// <summary>
        /// 設定嘴巴開合程度，value01 範圍 0~1。
        /// </summary>
        public void SetMouthOpen(float value01)
        {
            if (mouthOpenParam == null) return;
            float v = Mathf.Clamp01(value01);
            mouthOpenParam.Value = Mathf.Lerp(mouthOpenParam.MinimumValue, mouthOpenParam.MaximumValue, v);
        }

        #endregion

        #region Appearance

        /// <summary>
        /// 設定角色透明度，value01 範圍 0~1。
        /// </summary>
        public void SetOpacity(float value01)
        {
            float v = Mathf.Clamp01(value01);
            if (opacityParam != null)
            {
                opacityParam.Value = Mathf.Lerp(opacityParam.MinimumValue, opacityParam.MaximumValue, v);
                return;
            }

            // 直接使用快取的 _renderers 陣列，效能更高
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r.material.HasProperty(Color1))
                {
                    var c = r.material.color;
                    c.a = v;
                    r.material.color = c;
                }
            }
        }

        /// <summary>
        /// 取得目前透明度。
        /// </summary>
        /// <returns>透明度，範圍 0~1。</returns>
        public float GetOpacity()
        {
            if (opacityParam != null)
            {
                return Mathf.InverseLerp(opacityParam.MinimumValue, opacityParam.MaximumValue, opacityParam.Value);
            }

            // 使用快取的 Renderer
            if (_renderers != null && _renderers.Length > 0 && _renderers[0].material.HasProperty(Color1))
            {
                return _renderers[0].material.color.a;
            }

            return 1f;
        }

        /// <summary>
        /// 設定角色顏色。
        /// </summary>
        /// <param name="color">要設定的顏色。</param>
        public void SetColor(Color color)
        {
            // 直接使用快取的 _renderers 陣列
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r.material.HasProperty(Color1))
                {
                    r.material.color = color;
                }
            }
        }

        /// <summary>
        /// 顯示或隱藏角色。
        /// </summary>
        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }

        #endregion

        #region Breathing

        /// <summary>
        /// 開始呼吸。
        /// </summary>
        public void StartBreathing(float speed = 1.0f, float strength = 1.0f)
        {
            if (breathController == null) return;
            float baseBpm = 12f * Mathf.Max(0.1f, speed);
            breathController.StartBreathing(baseBpm, strength);
        }

        /// <summary>
        /// 停止呼吸。
        /// </summary>
        public void StopBreathing()
        {
            if (breathController == null) return;
            breathController.StopBreathing();
        }

        #endregion

        #region Voice

        /// <summary>
        /// 播放語音並進行唇同步。
        /// </summary>
        public void PlayVoice(AudioClip clip, float volume = 1f)
        {
            if (lipSync == null) return;
            lipSync.PlayVoice(clip, volume);
        }

        #endregion
    }
}
