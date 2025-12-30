using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

        public async UniTask ProcessCharacterAction(CharacterActionNode node, float duration)
        {
            switch (node.ActionType)
            {
                case CharacterActionType.Enter:
                    await ProcessEnterAction(node, duration);
                    break;
                case CharacterActionType.Exit:
                    if (node.ClearAllOnExit) await ClearAllCharacters(duration);
                    else await ClearCharacterAt(node.TargetPosition, duration);
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

        private async UniTask ProcessEnterAction(CharacterActionNode node, float duration)
        {
            if (_activeCharacters.TryGetValue(node.TargetPosition, out var existingState))
            {
                await UpdateExistingCharacter(existingState, node);
                existingState.SpeakerName = node.speakerName;
            }
            else
            {
                await InstantiateNewCharacter(node, duration);
            }
        }

        private async UniTask InstantiateNewCharacter(CharacterActionNode node, float duration)
        {
            if (!_stageLookup.TryGetValue(node.TargetPosition, out var stage) || stage == null) return;

            await ClearCharacterAt(node.TargetPosition, 0);

            GameObject characterInstance = null;
            IDialoguePortraitPresenter presenter = null;

            switch (node.portraitRenderMode)
            {
                case PortraitRenderMode.Sprite:
                    characterInstance = new GameObject("SpritePortrait");
                    var imagePresenter = characterInstance.AddComponent<ImageDialoguePortraitPresenter>();
                    presenter = imagePresenter;
                    // 先初始化，稍後顯示
                    break;
#if SPINE_KIT_AVAILABLE
                case PortraitRenderMode.Spine:
                    characterInstance = Instantiate(node.spinePortraitConfig.modelPrefab);
                    var spinePresenter = characterInstance.GetComponent<SpineDialoguePortraitPresenter>();
                    if (spinePresenter == null) spinePresenter = characterInstance.AddComponent<SpineDialoguePortraitPresenter>();
                    presenter = spinePresenter;
                    break;
#endif
#if LIVE2D_KIT_AVAILABLE
                case PortraitRenderMode.Live2D:
                    characterInstance = Instantiate(node.live2DModelPrefab);
                    var live2DPresenter = characterInstance.GetComponent<Live2DDialoguePortraitPresenter>();
                    presenter = live2DPresenter;
                    break;
#endif
                case PortraitRenderMode.SpriteSheet:
                    characterInstance = Instantiate(node.spriteSheetPresenter);
                    var spriteSheetPresenter = characterInstance.GetComponent<SpriteSheetDialoguePortraitPresenter>();
                    presenter = spriteSheetPresenter;
                    break;
            }

            if (characterInstance != null && presenter != null)
            {
                characterInstance.transform.SetParent(stage, false);
                var newState = new CharacterState(characterInstance, node.speakerName, presenter);
                _activeCharacters[node.TargetPosition] = newState;

                // 執行顯示動畫
                switch (node.portraitRenderMode)
                {
                    case PortraitRenderMode.Sprite:
                        await presenter.ShowSprite(node.characterSprite, duration);
                        break;
#if SPINE_KIT_AVAILABLE
                    case PortraitRenderMode.Spine:
                        await presenter.ShowSpine(node.spinePortraitConfig, duration);
                        break;
#endif
#if LIVE2D_KIT_AVAILABLE
                    case PortraitRenderMode.Live2D:
                        if (presenter is Live2DDialoguePortraitPresenter l2d)
                            await l2d.ShowLive2D(node.live2DPortraitConfig, duration);
                        break;
#endif
                    case PortraitRenderMode.SpriteSheet:
                        await presenter.ShowSpriteSheet(node.spriteSheetAnimationName, duration);
                        break;
                }
            }
            else if (characterInstance != null)
            {
                Destroy(characterInstance);
            }
        }

        private async UniTask UpdateExistingCharacter(CharacterState existingState, CharacterActionNode node)
        {
            if (existingState.Presenter == null) return;

            if (node.portraitRenderMode == PortraitRenderMode.SpriteSheet)
                await existingState.Presenter.ShowSpriteSheet(node.spriteSheetAnimationName, 0f);
#if LIVE2D_KIT_AVAILABLE
            else if (node.portraitRenderMode == PortraitRenderMode.Live2D && existingState.Presenter is Live2DDialoguePortraitPresenter live2DPresenter)
                await live2DPresenter.ShowLive2D(node.live2DPortraitConfig, 0f);
#endif
#if SPINE_KIT_AVAILABLE
            else if (node.portraitRenderMode == PortraitRenderMode.Spine && existingState.Presenter is SpineDialoguePortraitPresenter spinePresenter)
                await spinePresenter.ShowSpine(node.spinePortraitConfig, 0f);
#endif
            else if (node.portraitRenderMode == PortraitRenderMode.Sprite && existingState.Presenter is ImageDialoguePortraitPresenter imagePresenter)
                await imagePresenter.ShowSprite(node.characterSprite, 0f);
        }

        private async UniTask ClearCharacterAt(CharacterPosition position, float duration)
        {
            if (_activeCharacters.TryGetValue(position, out var activeCharacter))
            {
                if (activeCharacter.Presenter != null)
                {
                    await activeCharacter.Presenter.Hide(duration);
                }
                WaitAndDestroy(duration, activeCharacter.Instance).Forget();
                _activeCharacters.Remove(position);
            }
        }

        private async UniTask ClearAllCharacters(float duration)
        {
            var positions = new List<CharacterPosition>(_activeCharacters.Keys);
            var tasks = new List<UniTask>();
            foreach (var position in positions)
            {
                tasks.Add(ClearCharacterAt(position, duration));
            }
            await UniTask.WhenAll(tasks);
        }

        private async UniTaskVoid WaitAndDestroy(float duration, GameObject target)
        {
            if (duration > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), ignoreTimeScale: true);
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
