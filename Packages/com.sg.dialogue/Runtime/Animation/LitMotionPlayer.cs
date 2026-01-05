using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using SG.Dialogue.Presentation;
using UnityEngine;

namespace SG.Dialogue.Animation
{
    /// <summary>
    /// LitMotionPlayer 是一個 MonoBehaviour，它接收 MotionData 並使用 LitMotion 補間動畫庫播放對應的動畫。
    /// </summary>
    public class LitMotionPlayer : MonoBehaviour
    {
        // 使用 Dictionary 來管理不同屬性的動畫 Handle，允許同時播放不同屬性的動畫
        private readonly Dictionary<MotionTargetProperty, MotionHandle> _activeHandles = new Dictionary<MotionTargetProperty, MotionHandle>();

        /// <summary>
        /// 根據提供的 MotionData 播放動畫。
        /// </summary>
        /// <param name="data">包含動畫參數的 MotionData 實例。</param>
        public async UniTask Play(MotionData data)
        {
            // 根據 TargetProperty 取消舊的動畫，避免同屬性動畫衝突
            if (_activeHandles.TryGetValue(data.TargetProperty, out var handle) && handle.IsActive())
            {
                handle.Cancel();
            }

            var target = transform; // 動畫的目標 Transform
            var rectTransform = target as RectTransform; // 嘗試轉型為 RectTransform
            var duration = data.Duration; // 動畫持續時間
            var ease = data.Ease; // 緩和曲線
            
            // 處理循環次數：0 表示播放一次（不循環），-1 表示無限循環
            int loops = data.Loops == 0 ? 1 : data.Loops;

            MotionHandle newHandle = default;

            // 根據不同屬性印出不同的 Log
            if (data.TargetProperty == MotionTargetProperty.Alpha)
            {
                Debug.Log($"[LitMotionPlayer] Play Alpha on {target.name}. Duration: {duration}, Relative: {data.IsRelative}, EndAlpha: {data.EndAlpha}, Ease: {ease}");
            }
            else
            {
                Debug.Log($"[LitMotionPlayer] Play {data.TargetProperty} on {target.name}. Duration: {duration}, Relative: {data.IsRelative}, EndValue: {data.EndValue}, Ease: {ease}");
            }

            switch (data.TargetProperty)
            {
                case MotionTargetProperty.Position:
                    if (rectTransform != null)
                    {
                        var startPos = rectTransform.anchoredPosition;
                        var endPos = data.IsRelative ? startPos + (Vector2)data.EndValue : (Vector2)data.EndValue;
                        newHandle = LMotion.Create(startPos, endPos, duration)
                            .WithEase(ease)
                            .WithDelay(data.Delay)
                            .WithLoops(loops, GetLoopType(data.LoopType))
                            .BindToAnchoredPosition(rectTransform);
                    }
                    else
                    {
                        var startPos = target.localPosition;
                        var endPos = data.IsRelative ? startPos + data.EndValue : data.EndValue;
                        newHandle = LMotion.Create(startPos, endPos, duration)
                            .WithEase(ease)
                            .WithDelay(data.Delay)
                            .WithLoops(loops, GetLoopType(data.LoopType))
                            .BindToLocalPosition(target);
                    }
                    break;

                case MotionTargetProperty.Rotation:
                    var startRot = target.localEulerAngles;
                    var endRot = data.IsRelative ? startRot + data.EndValue : data.EndValue;
                    newHandle = LMotion.Create(startRot, endRot, duration)
                        .WithEase(ease)
                        .WithDelay(data.Delay)
                        .WithLoops(loops, GetLoopType(data.LoopType))
                        .BindToLocalEulerAngles(target);
                    break;

                case MotionTargetProperty.Scale:
                    var startScale = target.localScale;
                    var endScale = data.IsRelative ? startScale + data.EndValue : data.EndValue;
                    newHandle = LMotion.Create(startScale, endScale, duration)
                        .WithEase(ease)
                        .WithDelay(data.Delay)
                        .WithLoops(loops, GetLoopType(data.LoopType))
                        .BindToLocalScale(target);
                    break;
                
                case MotionTargetProperty.Alpha:
                    // 使用 EndAlpha 欄位
                    float targetAlpha = data.EndAlpha;
                    
                    var presenter = GetComponent<IDialoguePortraitPresenter>();
                    if (presenter != null)
                    {
                        var startAlpha = presenter.Alpha;
                        var endAlpha = data.IsRelative ? startAlpha + targetAlpha : targetAlpha;
                        Debug.Log($"[LitMotionPlayer] Alpha Animation: {startAlpha} -> {endAlpha}");
                        
                        newHandle = LMotion.Create(startAlpha, endAlpha, duration)
                            .WithEase(ease)
                            .WithDelay(data.Delay)
                            .WithLoops(loops, GetLoopType(data.LoopType))
                            .Bind(val => presenter.Alpha = val);
                    }
                    else
                    {
                        var canvasGroup = GetComponent<CanvasGroup>();
                        if (canvasGroup != null)
                        {
                            var startAlpha = canvasGroup.alpha;
                            var endAlpha = data.IsRelative ? startAlpha + targetAlpha : targetAlpha;
                            Debug.Log($"[LitMotionPlayer] Alpha Animation (CanvasGroup): {startAlpha} -> {endAlpha}");

                            newHandle = LMotion.Create(startAlpha, endAlpha, duration)
                                .WithEase(ease)
                                .WithDelay(data.Delay)
                                .WithLoops(loops, GetLoopType(data.LoopType))
                                .BindToAlpha(canvasGroup);
                        }
                        else
                        {
                            Debug.LogWarning("LitMotionPlayer: Alpha target requires an IDialoguePortraitPresenter or CanvasGroup component.", this);
                            return;
                        }
                    }
                    break;
            }

            // 儲存新的 Handle
            _activeHandles[data.TargetProperty] = newHandle;

            if (newHandle.IsActive())
            {
                await newHandle.ToUniTask();
            }
        }

        /// <summary>
        /// 將自定義的 MotionLoopType 轉換為 LitMotion 庫的 LoopType。
        /// </summary>
        private LoopType GetLoopType(MotionLoopType loopType)
        {
            switch (loopType)
            {
                case MotionLoopType.Restart:
                    return LoopType.Restart;
                case MotionLoopType.Yoyo:
                    return LoopType.Yoyo;
                default:
                    return LoopType.Restart;
            }
        }

        private void OnDestroy()
        {
            foreach (var handle in _activeHandles.Values)
            {
                if (handle.IsActive()) handle.Cancel();
            }
            _activeHandles.Clear();
        }
    }
}
