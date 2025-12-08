using System.Collections;
using SG.Dialogue.Animation;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// IDialoguePortraitPresenter 是一個對話立繪呈現器的介面。
    /// 它定義了顯示和隱藏不同類型立繪（例如 Sprite、Spine、Live2D）以及播放動畫的標準方法。
    /// 這允許 DialogueVisualManager 以統一的方式與不同類型的立繪呈現器互動。
    /// </summary>
    public interface IDialoguePortraitPresenter
    {
        /// <summary>
        /// 根據指定的淡入時間顯示一個靜態的 Sprite 立繪。
        /// </summary>
        /// <param name="sprite">要顯示的 Sprite。</param>
        /// <param name="fadeDuration">淡入持續時間（秒）。</param>
        void ShowSprite(Sprite sprite, float fadeDuration);

        /// <summary>
        /// 根據指定的淡入時間顯示一個 Spine 立繪。
        /// 如果實現此介面的類別不支援 Spine，可以忽略此方法。
        /// </summary>
        /// <param name="config">Spine 立繪的設定。</param>
        /// <param name="fadeDuration">淡入持續時間（秒）。</param>
        void ShowSpine(SpinePortraitConfig config, float fadeDuration);

        /// <summary>
        /// 根據指定的淡入時間顯示一個 Sprite Sheet 動畫立繪。
        /// 如果實現此介面的類別不支援 Sprite Sheet，可以忽略此方法。
        /// </summary>
        /// <param name="animationName">動畫名稱。</param>
        /// <param name="fadeDuration">淡入持續時間（秒）。</param>
        void ShowSpriteSheet(string animationName, float fadeDuration);

        /// <summary>
        /// 根據指定的淡出時間隱藏當前顯示的立繪。
        /// </summary>
        /// <param name="fadeDuration">淡出持續時間（秒）。</param>
        void Hide(float fadeDuration);

        /// <summary>
        /// 立即清除並隱藏立繪，沒有淡出效果。
        /// </summary>
        void HideImmediate();

        /// <summary>
        /// 播放一個指定的 LitMotion 動畫。
        /// </summary>
        /// <param name="data">包含動畫參數的 MotionData 實例。</param>
        void PlayMotion(MotionData data);

        /// <summary>
        /// 設定立繪的高亮狀態。
        /// 當 isHighlighted 為 true 時，立繪應顯示為正常狀態（例如全彩）；
        /// 當 isHighlighted 為 false 時，立繪應顯示為非高亮狀態（例如灰階或半透明）。
        /// </summary>
        /// <param name="isHighlighted">是否高亮。</param>
        void SetHighlight(bool isHighlighted);

        /// <summary>
        /// 執行閃爍效果。
        /// </summary>
        /// <param name="duration">總持續時間。</param>
        /// <param name="frequency">閃爍頻率。</param>
        /// <param name="minAlpha">閃爍時的最低透明度。</param>
        /// <returns>一個協程，用於等待閃爍效果完成。</returns>
        IEnumerator Flicker(float duration, float frequency, float minAlpha);
    }
}
