using System.Collections.Generic;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using SG.Dialogue.Nodes;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    public class DialogueTextPresenter : BaseTextPresenter
    {
        [Header("打字機音效")]
        [SerializeField] private bool enableTypewriterSound = true;
        [SerializeField] private int soundInterval = 3;
        [SerializeField] private AudioEvent typewriterAudioEvent;
        [SerializeField] private string typewriterSoundName = "TypewriterKey";

        private List<TextCue> _currentCues;
        private int _nextCueIndex;
        private int _soundCharacterCount;
        private AudioRequest _typewriterAudioRequest;

        protected override void Awake()
        {
            base.Awake();
            _typewriterAudioRequest = new AudioRequest(AudioActionType.PlaySFX, typewriterSoundName, false, 0f);
        }

        public void ShowText(string text, List<TextCue> cues)
        {
            _currentCues = (cues != null && cues.Count > 0) ? new List<TextCue>(cues) : null;
            _currentCues?.Sort((a, b) => a.charIndex.CompareTo(b.charIndex));
            _nextCueIndex = 0;
            _soundCharacterCount = 0;

            base.ShowText(text);
        }

        protected override void OnCharacterTyped(char character, int index)
        {
            TriggerCuesUpToIndex(index);

            if (enableTypewriterSound && !char.IsWhiteSpace(character) && !char.IsPunctuation(character))
            {
                _soundCharacterCount++;
                if (_soundCharacterCount % soundInterval == 0)
                {
                    if (typewriterAudioEvent != null)
                        typewriterAudioEvent.Raise(_typewriterAudioRequest);
                }
            }
        }

        protected override void OnTypingCompleteInternal()
        {
            TriggerRemainingCues();
            base.OnTypingCompleteInternal();
        }



        private void TriggerCuesUpToIndex(int charIndex)
        {
            if (_currentCues == null) return;
            while (_nextCueIndex < _currentCues.Count && _currentCues[_nextCueIndex].charIndex <= charIndex)
            {
                _currentCues[_nextCueIndex]?.onTrigger?.Invoke();
                _nextCueIndex++;
            }
        }

        private void TriggerRemainingCues()
        {
            if (_currentCues == null) return;
            while (_nextCueIndex < _currentCues.Count)
            {
                _currentCues[_nextCueIndex]?.onTrigger?.Invoke();
                _nextCueIndex++;
            }
        }
    }
}
