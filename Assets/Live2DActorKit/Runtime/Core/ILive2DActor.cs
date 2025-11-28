using UnityEngine;

namespace Live2DActorKit.Core
{
    /// <summary>
    /// 高階 Live2D 角色控制介面。
    /// </summary>
    public interface ILive2DActor
    {
        // Motion
        void PlayMotion(string motionId, bool loop = false, float fadeIn = 0.2f, float fadeOut = 0.2f);
        void StopMotion(string layer = "Base");
        bool IsPlaying(string motionId);

        // Expression
        void SetExpression(string expressionId);
        void ClearExpression();

        // LookAt
        void LookAt(Vector2 screenPosition);
        void ResetLookAt();

        // Mouth (手動控制用；若使用語音同步則由 LipSync 控制)
        void SetMouthOpen(float value01);

        // Appearance
        void SetOpacity(float value01);
        float GetOpacity(); // 取得目前透明度
        void SetColor(Color color); // 設定顏色
        void Show(bool show);

        // Breathing
        void StartBreathing(float speed = 1.0f, float strength = 1.0f);
        void StopBreathing();

        // Voice
        void PlayVoice(AudioClip clip, float volume = 1f);
    }
}
