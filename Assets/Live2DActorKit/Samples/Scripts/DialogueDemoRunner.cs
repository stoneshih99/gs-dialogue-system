using UnityEngine;
using Live2DActorKit.Actors;
using Live2DActorKit.Audio;

namespace Live2DActorKit.Samples
{
    [System.Serializable]
    public class DialogueEntry
    {
        public string SpeakerId;
        public string ActorState;
        public string Text;
        public AudioClip VoiceClip;
    }

    /// <summary>
    /// 簡易對話播放示範。
    /// </summary>
    public class DialogueDemoRunner : MonoBehaviour
    {
        public DialogueEntry[] entries;
        public UnityEngine.UI.Text textUI;
        public Live2DActorStateController[] actorStateControllers;

        private int _index;

        private void Start()
        {
            PlayNext();
        }

        public void PlayNext()
        {
            if (_index >= entries.Length)
            {
                if (textUI != null)
                    textUI.text = "(End)";
                return;
            }

            var line = entries[_index++];
            var actor = FindActor(line.SpeakerId);

            if (actor != null && !string.IsNullOrEmpty(line.ActorState))
                actor.PlayState(line.ActorState);

            if (textUI != null)
                textUI.text = $"{line.SpeakerId}: {line.Text}";

            Live2DVoiceManager.Instance.PlayLine(
                line.SpeakerId,
                line.VoiceClip,
                1f,
                onFinished: PlayNext
            );
        }

        private Live2DActorStateController FindActor(string speakerId)
        {
            // Demo：你可以在這裡實作自己專案的 SpeakerId -> Actor 映射
            foreach (var a in actorStateControllers)
            {
                if (a == null) continue;
                if (a.gameObject.name == speakerId)
                    return a;
            }
            return null;
        }
    }
}
