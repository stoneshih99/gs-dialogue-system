using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace SG.Dialogue.Animation
{
    /// <summary>
    /// LitMotionPlayer 是一個 MonoBehaviour，它接收 MotionData 並使用 LitMotion 補間動畫庫播放對應的動畫。
    /// </summary>
    public class LitMotionPlayer : MonoBehaviour
    {
        private MotionHandle _currentHandle; // 當前正在播放的動畫句柄

        /// <summary>
        /// 根據提供的 MotionData 播放動畫。
        /// </summary>
        /// <param name="data">包含動畫參數的 MotionData 實例。</param>
        public void Play(MotionData data)
        {
            // 停止目前正在播放的動畫，以避免衝突
            if (_currentHandle.IsActive())
            {
                _currentHandle.Cancel();
            }

            var target = transform; // 動畫的目標 Transform
            var duration = data.Duration; // 動畫持續時間
            var ease = data.Ease; // 緩和曲線

            switch (data.TargetProperty)
            {
                case MotionTargetProperty.Position:
                    var startPos = target.localPosition;
                    // 如果是相對運動，則目標位置是起始位置加上 EndValue；否則直接使用 EndValue
                    var endPos = data.IsRelative ? startPos + data.EndValue : data.EndValue;
                    _currentHandle = LMotion.Create(startPos, endPos, duration)
                        .WithEase(ease)
                        .WithDelay(data.Delay)
                        .WithLoops(data.Loops, GetLoopType(data.LoopType))
                        .BindToLocalPosition(target); // 綁定到本地位置
                    break;

                case MotionTargetProperty.Rotation:
                    var startRot = target.localEulerAngles;
                    // 如果是相對運動，則目標旋轉是起始旋轉加上 EndValue；否則直接使用 EndValue
                    var endRot = data.IsRelative ? startRot + data.EndValue : data.EndValue;
                    _currentHandle = LMotion.Create(startRot, endRot, duration)
                        .WithEase(ease)
                        .WithDelay(data.Delay)
                        .WithLoops(data.Loops, GetLoopType(data.LoopType))
                        .BindToLocalEulerAngles(target); // 綁定到本地歐拉角
                    break;

                case MotionTargetProperty.Scale:
                    var startScale = target.localScale;
                    // 如果是相對運動，則目標縮放是起始縮放加上 EndValue；否則直接使用 EndValue
                    var endScale = data.IsRelative ? startScale + data.EndValue : data.EndValue;
                    _currentHandle = LMotion.Create(startScale, endScale, duration)
                        .WithEase(ease)
                        .WithDelay(data.Delay)
                        .WithLoops(data.Loops, GetLoopType(data.LoopType))
                        .BindToLocalScale(target); // 綁定到本地縮放
                    break;
                
                case MotionTargetProperty.Alpha:
                    var canvasGroup = GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        var startAlpha = canvasGroup.alpha;
                        var endAlpha = data.EndValue.x; // 對於 Alpha，使用 EndValue 的 X 分量
                        _currentHandle = LMotion.Create(startAlpha, endAlpha, duration)
                            .WithEase(ease)
                            .WithDelay(data.Delay)
                            .WithLoops(data.Loops, GetLoopType(data.LoopType))
                            .BindToAlpha(canvasGroup); // 綁定到 CanvasGroup 的 Alpha
                    }
                    else
                    {
                        Debug.LogWarning("LitMotionPlayer: Alpha target requires a CanvasGroup component.", this);
                    }
                    break;
            }
        }

        /// <summary>
        /// 將自定義的 MotionLoopType 轉換為 LitMotion 庫的 LoopType。
        /// </summary>
        /// <param name="loopType">自定義的 MotionLoopType。</param>
        /// <returns>LitMotion 庫的 LoopType。</returns>
        private LoopType GetLoopType(MotionLoopType loopType)
        {
            switch (loopType)
            {
                case MotionLoopType.Restart:
                    return LoopType.Restart;
                case MotionLoopType.Yoyo:
                    return LoopType.Yoyo;
                default:
                    return LoopType.Restart; // LitMotion 對於循環次數大於 1 的預設值
            }
        }

        private void OnDestroy()
        {
            // 當物件被摧毀時，取消動畫以避免記憶體洩漏
            if (_currentHandle.IsActive())
            {
                _currentHandle.Cancel();
            }
        }
    }
}
