using System.Collections;
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// DialogueCameraController 是一個 MonoBehaviour，負責執行對話系統中的攝影機動畫。
    /// 它需要掛載在一個 Camera 物件上，並處理攝影機的震動、縮放、平移和聚焦操作。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DialogueCameraController : MonoBehaviour
    {
        private Camera _camera; // 攝影機組件
        private Vector3 _originalPosition; // 攝影機的原始位置
        private float _originalOrthoSize; // 攝影機的原始正交大小 (Orthographic Size)

        private void Awake()
        {
            _camera = GetComponent<Camera>(); // 獲取攝影機組件
            _originalPosition = transform.position; // 儲存原始位置
            if (_camera.orthographic) // 如果是正交攝影機
            {
                _originalOrthoSize = _camera.orthographicSize; // 儲存原始正交大小
            }
        }

        /// <summary>
        /// 執行攝影機控制節點指定的動作。
        /// </summary>
        /// <param name="node">CameraControlNode 實例。</param>
        /// <returns>協程。</returns>
        public IEnumerator Execute(CameraControlNode node)
        {
            switch (node.ActionType)
            {
                case CameraActionType.Shake:
                    yield return Shake(node.Duration, node.ShakeIntensity); // 執行震動
                    break;
                case CameraActionType.Zoom:
                    yield return Zoom(node.TargetZoom, node.Duration); // 執行縮放
                    break;
                case CameraActionType.Pan:
                    yield return Pan(node.PanTargetPosition, node.Duration); // 執行平移
                    break;
                case CameraActionType.FocusOnTarget:
                    if (node.FocusTarget != null) // 如果有指定聚焦目標
                    {
                        // 計算目標位置（保持 Z 軸不變）
                        Vector3 targetPos = new Vector3(node.FocusTarget.position.x, node.FocusTarget.position.y, transform.position.z);
                        yield return Pan(targetPos, node.Duration); // 平移到目標位置
                    }
                    break;
            }
        }

        /// <summary>
        /// 執行攝影機震動效果。
        /// </summary>
        /// <param name="duration">震動持續時間。</param>
        /// <param name="intensity">震動強度。</param>
        /// <returns>協程。</returns>
        private IEnumerator Shake(float duration, float intensity)
        {
            float elapsed = 0f;
            Vector3 startPosition = transform.position; // 儲存震動前的起始位置

            while (elapsed < duration)
            {
                // 使用 Perlin 噪音生成隨機偏移量
                float x = startPosition.x + (Mathf.PerlinNoise(Time.time * 20f, 0) * 2 - 1) * intensity;
                float y = startPosition.y + (Mathf.PerlinNoise(0, Time.time * 20f) * 2 - 1) * intensity;
                transform.position = new Vector3(x, y, startPosition.z); // 更新攝影機位置
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = startPosition; // 震動結束後恢復到起始位置
        }

        /// <summary>
        /// 執行攝影機縮放效果（僅適用於正交攝影機）。
        /// </summary>
        /// <param name="targetOrthoSize">目標正交大小。</param>
        /// <param name="duration">縮放持續時間。</param>
        /// <returns>協程。</returns>
        private IEnumerator Zoom(float targetOrthoSize, float duration)
        {
            if (!_camera.orthographic) // 如果不是正交攝影機，則發出警告
            {
                Debug.LogWarning("Camera is not orthographic. Zoom action has no effect.");
                yield break;
            }

            float startSize = _camera.orthographicSize; // 縮放前的起始大小
            float elapsed = 0f;

            while (elapsed < duration)
            {
                _camera.orthographicSize = Mathf.Lerp(startSize, targetOrthoSize, elapsed / duration); // 漸變正交大小
                elapsed += Time.deltaTime;
                yield return null;
            }

            _camera.orthographicSize = targetOrthoSize; // 確保最終大小正確
        }

        /// <summary>
        /// 執行攝影機平移效果。
        /// </summary>
        /// <param name="targetPosition">目標世界坐標位置。</param>
        /// <param name="duration">平移持續時間。</param>
        /// <returns>協程。</returns>
        private IEnumerator Pan(Vector2 targetPosition, float duration)
        {
            Vector3 startPosition = transform.position; // 平移前的起始位置
            Vector3 endPosition = new Vector3(targetPosition.x, targetPosition.y, startPosition.z); // 目標位置（保持 Z 軸不變）
            float elapsed = 0f;

            if (duration <= 0) // 如果持續時間為 0，則立即設定位置
            {
                transform.position = endPosition;
                yield break;
            }

            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPosition, endPosition, elapsed / duration); // 漸變位置
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = endPosition; // 確保最終位置正確
        }
        
        // 新增 Context Tag 來執行 Shake Effect
        /// <summary>
        /// 執行攝影機震動效果，透過 Context Tag 觸發。
        /// </summary>
        [ContextMenu("DoShake")]
        public void DoShake()
        {
            StartCoroutine(Shake(1f, 0.3f));
        }

    }
}
