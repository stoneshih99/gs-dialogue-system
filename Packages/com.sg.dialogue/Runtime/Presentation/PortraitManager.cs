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

            // 這裡的 ClearCharacterAt 會導致問題，如果 duration > 0
            // 因為它會啟動一個非同步的 WaitAndDestroy，但我們馬上就要在這個位置生成新的角色
            // 如果舊的角色還沒被銷毀，新的角色可能會被舊的擋住，或者位置重疊
            // 修正：如果是 Enter 動作且該位置已有角色，應該立即銷毀舊的，或者等待舊的消失後再生成新的
            // 但通常 Enter 動作隱含了「替換」的意思，所以應該立即清理舊的狀態
            
            // 為了避免「退場動畫還沒播完，新角色就進來」導致的視覺混亂，
            // 這裡我們強制以 0 秒清除該位置的舊角色（如果有的話），確保舞台是乾淨的。
            // 如果使用者想要「舊角色淡出 -> 新角色淡入」，應該在圖表中顯式安排一個 Exit 節點，然後才是 Enter 節點。
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
                // 1. 從 _activeCharacters 移除，這樣後續的 Enter 就不會認為這個位置還有人
                _activeCharacters.Remove(position);

                // 2. 執行淡出動畫（如果有的話）
                if (activeCharacter.Presenter != null)
                {
                    // 注意：這裡我們 await 了 Hide，這意味著如果 duration > 0，我們會在這裡等待
                    // 這對於 Exit 節點是正確的行為。
                    // 但對於 Enter 節點內部呼叫的 ClearCharacterAt(0)，這會立即完成。
                    await activeCharacter.Presenter.Hide(duration);
                }

                // 3. 銷毀物件
                // 如果 duration > 0，Hide 已經等待過了，所以這裡可以直接銷毀
                // 如果 duration == 0，Hide 立即完成，這裡也直接銷毀
                if (activeCharacter.Instance != null)
                {
                    Destroy(activeCharacter.Instance);
                }
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

        private void BuildStageLookup()
        {
            _stageLookup.Clear();
            if (leftPortraitStage != null) _stageLookup[CharacterPosition.Left] = leftPortraitStage;
            if (centerPortraitStage != null) _stageLookup[CharacterPosition.Center] = centerPortraitStage;
            if (rightPortraitStage != null) _stageLookup[CharacterPosition.Right] = rightPortraitStage;
        }
    }
}
