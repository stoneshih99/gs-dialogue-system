using UnityEngine;
using Live2DActorKit.Audio;

namespace Live2DActorKit.Init
{
    /// <summary>
    /// 把角色註冊到 Live2DVoiceManager 的小幫手。
    /// 一個角色一個即可。
    /// </summary>
    [DisallowMultipleComponent]
    public class Live2DSpeakerBootstrap : MonoBehaviour
    {
        [Tooltip("此角色在對話系統中的 Speaker Id，例如 Hina / Ryo")]
        [SerializeField] private string speakerId = "Hina";

        [Tooltip("負責嘴型同步的元件")]
        [SerializeField] private Live2DLipSyncController lipSync;

        private void Reset()
        {
            lipSync ??= GetComponent<Live2DLipSyncController>();
        }

        private void Start()
        {
            if (lipSync == null)
                lipSync = GetComponent<Live2DLipSyncController>();

            if (Live2DVoiceManager.Instance != null)
                Live2DVoiceManager.Instance.RegisterSpeaker(speakerId, lipSync);
        }

        private void OnDestroy()
        {
            if (Live2DVoiceManager.Instance != null)
                Live2DVoiceManager.Instance.UnregisterSpeaker(speakerId);
        }
    }
}
