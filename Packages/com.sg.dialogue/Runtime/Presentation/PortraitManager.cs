using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 專門管理角色立繪的生命週期和視覺呈現。
    /// </summary>
    public class PortraitManager : MonoBehaviour
    {
        private class CharacterState
        {
            public GameObject Instance { get; }
            public string SpeakerName { get; set; }
            public IDialoguePortraitPresenter Presenter { get; }

            public CharacterState(GameObject instance, string speakerName, IDialoguePortraitPresenter presenter)
            {
                Instance = instance;
                SpeakerName = speakerName;
                Presenter = presenter;
            }
        }

        [Header("角色舞台")]
        [SerializeField] private Transform leftPortraitStage;
        [SerializeField] private Transform centerPortraitStage;
        [SerializeField] private Transform rightPortraitStage;

        private readonly Dictionary<CharacterPosition, Transform> _stageLookup = new();
        private readonly Dictionary<CharacterPosition, CharacterState> _activeCharacters = new();

        private void Awake()
        {
            BuildStageLookup();
        }

        public void ProcessCharacterAction(CharacterActionNode node, float duration)
        {
            switch (node.ActionType)
            {
                case CharacterActionType.Enter:
                    ProcessEnterAction(node, duration);
                    break;
                case CharacterActionType.Exit:
                    if (node.ClearAllOnExit) ClearAllCharacters(duration);
                    else ClearCharacterAt(node.TargetPosition, duration);
                    break;
            }
        }

        public void SetCharacterHighlights(string currentSpeakerName)
        {
            bool hasSpeaker = !string.IsNullOrEmpty(currentSpeakerName);
            foreach (var characterState in _activeCharacters.Values)
            {
                if (characterState.Presenter == null) continue;
                characterState.Presenter.SetHighlight(hasSpeaker ? characterState.SpeakerName == currentSpeakerName : true);
            }
        }

        private void ProcessEnterAction(CharacterActionNode node, float duration)
        {
            if (_activeCharacters.TryGetValue(node.TargetPosition, out var existingState))
            {
                UpdateExistingCharacter(existingState, node);
                existingState.SpeakerName = node.speakerName;
            }
            else
            {
                InstantiateNewCharacter(node, duration);
            }
        }

        private void InstantiateNewCharacter(CharacterActionNode node, float duration)
        {
            if (!_stageLookup.TryGetValue(node.TargetPosition, out var stage) || stage == null) return;

            ClearCharacterAt(node.TargetPosition, 0);

            GameObject characterInstance = null;
            IDialoguePortraitPresenter presenter = null;

            switch (node.portraitRenderMode)
            {
                case PortraitRenderMode.Sprite:
                    characterInstance = new GameObject("SpritePortrait");
                    var imagePresenter = characterInstance.AddComponent<ImageDialoguePortraitPresenter>();
                    imagePresenter.ShowSprite(node.characterSprite, duration);
                    presenter = imagePresenter;
                    break;
#if SPINE_KIT_AVAILABLE
                case PortraitRenderMode.Spine:
                    characterInstance = Instantiate(node.spinePortraitConfig.modelPrefab);
                    var spinePresenter = characterInstance.GetComponent<SpineDialoguePortraitPresenter>();
                    if (spinePresenter == null) spinePresenter = characterInstance.AddComponent<SpineDialoguePortraitPresenter>();
                    spinePresenter.ShowSpine(node.spinePortraitConfig, duration);
                    presenter = spinePresenter;
                    break;
#endif
#if LIVE2D_KIT_AVAILABLE
                case PortraitRenderMode.Live2D:
                    characterInstance = Instantiate(node.live2DModelPrefab);
                    var live2DPresenter = characterInstance.GetComponent<Live2DDialoguePortraitPresenter>();
                    if (live2DPresenter != null) live2DPresenter.ShowLive2D(node.live2DPortraitConfig, duration);
                    presenter = live2DPresenter;
                    break;
#endif
                case PortraitRenderMode.SpriteSheet:
                    characterInstance = Instantiate(node.spriteSheetPresenter);
                    var spriteSheetPresenter = characterInstance.GetComponent<SpriteSheetDialoguePortraitPresenter>();
                    if(spriteSheetPresenter != null) spriteSheetPresenter.ShowSpriteSheet(node.spriteSheetAnimationName, duration);
                    presenter = spriteSheetPresenter;
                    break;
            }

            if (characterInstance != null && presenter != null)
            {
                characterInstance.transform.SetParent(stage, false);
                var newState = new CharacterState(characterInstance, node.speakerName, presenter);
                _activeCharacters[node.TargetPosition] = newState;
            }
            else if (characterInstance != null)
            {
                Destroy(characterInstance);
            }
        }

        private void UpdateExistingCharacter(CharacterState existingState, CharacterActionNode node)
        {
            if (existingState.Presenter == null) return;

            if (node.portraitRenderMode == PortraitRenderMode.SpriteSheet)
                existingState.Presenter.ShowSpriteSheet(node.spriteSheetAnimationName, 0f);
#if LIVE2D_KIT_AVAILABLE
            else if (node.portraitRenderMode == PortraitRenderMode.Live2D && existingState.Presenter is Live2DDialoguePortraitPresenter live2DPresenter)
                live2DPresenter.ShowLive2D(node.live2DPortraitConfig, 0f);
#endif
#if SPINE_KIT_AVAILABLE
            else if (node.portraitRenderMode == PortraitRenderMode.Spine && existingState.Presenter is SpineDialoguePortraitPresenter spinePresenter)
                spinePresenter.ShowSpine(node.spinePortraitConfig, 0f);
#endif
            else if (node.portraitRenderMode == PortraitRenderMode.Sprite && existingState.Presenter is ImageDialoguePortraitPresenter imagePresenter)
                imagePresenter.ShowSprite(node.characterSprite, 0f);
        }

        private void ClearCharacterAt(CharacterPosition position, float duration)
        {
            if (_activeCharacters.TryGetValue(position, out var activeCharacter))
            {
                if (activeCharacter.Presenter != null)
                {
                    activeCharacter.Presenter.Hide(duration);
                }
                StartCoroutine(WaitAndDestroy(duration, activeCharacter.Instance));
                _activeCharacters.Remove(position);
            }
        }

        private void ClearAllCharacters(float duration)
        {
            var positions = new List<CharacterPosition>(_activeCharacters.Keys);
            foreach (var position in positions) ClearCharacterAt(position, duration);
        }

        private IEnumerator WaitAndDestroy(float duration, GameObject target)
        {
            if (duration > 0)
            {
                yield return new WaitForSecondsRealtime(duration);
            }
            if (target != null) Destroy(target);
        }

        private void BuildStageLookup()
        {
            _stageLookup.Clear();
            if (leftPortraitStage != null) _stageLookup[CharacterPosition.Left] = leftPortraitStage;
            if (centerPortraitStage != null) _stageLookup[CharacterPosition.Center] = centerPortraitStage;
            if (rightPortraitStage != null) _stageLookup[CharacterPosition.Right] = rightPortraitStage;
        }
    }
}
