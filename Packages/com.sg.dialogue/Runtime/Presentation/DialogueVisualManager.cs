using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Animation;
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 管理對話中的視覺元素，包括角色肖像、背景和動畫。
    /// </summary>
    public class DialogueVisualManager : MonoBehaviour
    {
        /// <summary>
        /// 保存角色實例及其狀態的內部類別。
        /// </summary>
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
        
        [Header("中央舞台文字")]
        [Tooltip("用於在舞台中央顯示文字的呈現器。")]
        [SerializeField] private StageTextPresenter stageTextPresenter;

        [Header("角色設定")]
        [SerializeField] private float portraitFadeDuration = 0.2f;

        [Header("背景")]
        [SerializeField] private List<Image> backgroundImages;

        /// <summary>
        /// 角色位置到舞台 Transform 的查找表。
        /// </summary>
        private readonly Dictionary<CharacterPosition, Transform> _stageLookup = new();
        /// <summary>
        /// 當前活躍角色的狀態字典。 
        /// </summary>
        private readonly Dictionary<CharacterPosition, CharacterState> _activeCharacters = new();
        /// <summary>
        /// 背景淡入淡出協程列表。 
        /// </summary>
        private readonly List<Coroutine> _backgroundFadeRoutines = new List<Coroutine>();

        private void Awake()
        {
            BuildStageLookup();
            for (int i = 0; i < backgroundImages.Count; i++)
            {
                _backgroundFadeRoutines.Add(null);
            }
        }
        
        public void ShowStageText(string message, float speed)
        {
            if (stageTextPresenter != null) stageTextPresenter.ShowMessage(message, speed);
        }

        public void HideStageText()
        {
            if (stageTextPresenter != null) stageTextPresenter.Hide();
        }

        public bool IsStageTextTyping()
        {
            return stageTextPresenter != null && stageTextPresenter.IsTyping;
        }

        public void UpdateFromTextNode(TextNode node)
        {
            SetCharacterHighlights(node.speakerName);
        }

        public IEnumerator PlayAnimations(AnimationNode node)
        {
            if (node.motions == null || node.motions.Count == 0) yield break;

            if (_activeCharacters.TryGetValue(node.targetAnimationPosition, out var activeCharacter))
            {
                if (activeCharacter.Presenter != null)
                {
                    foreach (var motionData in node.motions)
                    {
                        activeCharacter.Presenter.PlayMotion(motionData);
                    }
                    
                    float maxDuration = 0;
                    foreach (var motionData in node.motions)
                    {
                        maxDuration = Mathf.Max(maxDuration, motionData.Duration + motionData.Delay);
                    }
                    if (maxDuration > 0) yield return new WaitForSeconds(maxDuration);
                }
            }
        }

        public IEnumerator UpdateFromCharacterActionNode(CharacterActionNode node)
        {
            var duration = node.Duration;
            
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
            
            if (duration > 0) yield return new WaitForSeconds(duration);
        }

        public IEnumerator UpdateFromSetBackgroundNode(SetBackgroundNode node)
        {
            float bgFadeTime = node.backgroundFadeOverride;
            var layerIndex = node.spriteIndex;
            if (node.useBlackScreen && backgroundImages.Count > layerIndex && backgroundImages[layerIndex] != null)
            {
                if (_backgroundFadeRoutines[layerIndex] != null) StopCoroutine(_backgroundFadeRoutines[layerIndex]);
                _backgroundFadeRoutines[layerIndex] = StartCoroutine(FadeImageRoutine(backgroundImages[layerIndex], backgroundImages[layerIndex].sprite, false, bgFadeTime));
                yield return _backgroundFadeRoutines[layerIndex];
                if (node.blackScreenDuration > 0) yield return new WaitForSeconds(node.blackScreenDuration);
            }

            yield return UpdateBackground(layerIndex, node.backgroundSprite, node.clearBackground, bgFadeTime);
        }

        public IEnumerator ExecuteFlickerEffect(FlickerEffectNode node)
        {
            if (node.target == FlickerEffectNode.TargetType.Background)
            {
                if (node.backgroundLayerIndex >= 0 && node.backgroundLayerIndex < backgroundImages.Count)
                {
                    yield return FlickerImage(backgroundImages[node.backgroundLayerIndex], node.duration, node.frequency, node.minAlpha);
                }
            }
            else // Character
            {
                if (_activeCharacters.TryGetValue(node.characterPosition, out var characterState) && characterState.Presenter != null)
                {
                    yield return characterState.Presenter.Flicker(node.duration, node.frequency, node.minAlpha);
                }
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

            // 如果該位置已有角色，先清除（這裡設為 0 秒是因為我們馬上要放新角色，或者你可以選擇淡出舊的再放新的）
            // 但為了流暢性，通常直接替換或快速淡出
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
                // 注意：這裡不再呼叫 FadeCharacter，因為 Presenter.ShowXXX 已經處理了淡入
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
        
        private void SetCharacterHighlights(string currentSpeakerName)
        {
            bool hasSpeaker = !string.IsNullOrEmpty(currentSpeakerName);

            foreach (var characterState in _activeCharacters.Values)
            {
                if (characterState.Presenter == null) continue;

                if (hasSpeaker)
                {
                    characterState.Presenter.SetHighlight(characterState.SpeakerName == currentSpeakerName);
                }
                else
                {
                    characterState.Presenter.SetHighlight(true);
                }
            }
        }

        private void ClearCharacterAt(CharacterPosition position, float duration)
        {
            if (_activeCharacters.TryGetValue(position, out var activeCharacter))
            {
                if (activeCharacter.Presenter != null)
                {
                    activeCharacter.Presenter.Hide(duration);
                }
                
                // 啟動協程等待淡出完成後銷毀物件
                StartCoroutine(WaitAndDestroy(duration, activeCharacter.Instance));
                
                _activeCharacters.Remove(position);
            }
        }

        private void ClearAllCharacters(float duration)
        {
            var positions = new List<CharacterPosition>(_activeCharacters.Keys);
            foreach (var position in positions) ClearCharacterAt(position, duration);
        }

        /// <summary>
        /// 等待指定時間後銷毀物件。
        /// </summary>
        private IEnumerator WaitAndDestroy(float duration, GameObject target)
        {
            if (duration > 0)
            {
                // 使用 unscaledTime 確保暫停時也能銷毀
                yield return new WaitForSecondsRealtime(duration);
            }
            
            if (target != null) Destroy(target);
        }

        // --- 背景相關方法保持不變 ---

        private IEnumerator UpdateBackground(int layerIndex, Sprite sprite, bool clear, float duration)
        {
            if (layerIndex < 0 || layerIndex >= backgroundImages.Count || backgroundImages[layerIndex] == null) yield break;
            Image targetImage = backgroundImages[layerIndex];
            if (!targetImage.gameObject.activeSelf)
            {
                targetImage.gameObject.SetActive(true);
            }
            if (layerIndex < _backgroundFadeRoutines.Count && _backgroundFadeRoutines[layerIndex] != null) StopCoroutine(_backgroundFadeRoutines[layerIndex]);
            if (clear)
            {
                var routine = StartCoroutine(FadeImageRoutine(targetImage, null, false, duration));
                if(layerIndex < _backgroundFadeRoutines.Count) _backgroundFadeRoutines[layerIndex] = routine;
                yield return routine;
            }
            if (sprite != null)
            {
                var routine = StartCoroutine(FadeImageRoutine(targetImage, sprite, true, duration));
                if(layerIndex < _backgroundFadeRoutines.Count) _backgroundFadeRoutines[layerIndex] = routine;
                yield return routine;
            }
        }

        private IEnumerator FadeImageRoutine(Image image, Sprite targetSprite, bool enable, float duration)
        {
            if (image == null) yield break;
            Color c = image.color;
            float startAlpha = c.a;
            float endAlpha = enable && targetSprite != null ? 1f : 0f;
            if (duration <= 0f)
            {
                image.sprite = targetSprite;
                c.a = endAlpha;
                image.color = c;
                image.enabled = enable && targetSprite != null;
                yield break;
            }
            if (enable && targetSprite != null)
            {
                image.sprite = targetSprite;
                startAlpha = 0f;
                c.a = 0f;
                image.color = c;
                image.enabled = true;
            }
            
            float startTime = Time.unscaledTime;
            while (Time.unscaledTime < startTime + duration)
            {
                float t = (Time.unscaledTime - startTime) / duration;
                c.a = Mathf.Lerp(startAlpha, endAlpha, Mathf.SmoothStep(0f, 1f, t));
                image.color = c;
                yield return null;
            }
            c.a = endAlpha;
            image.color = c;
            if (!enable || targetSprite == null) image.enabled = false;
        }
        
        private IEnumerator FlickerImage(Image image, float duration, float frequency, float minAlpha)
        {
            if (image == null) yield break;
            var cg = image.GetComponent<CanvasGroup>();
            if (cg == null) cg = image.gameObject.AddComponent<CanvasGroup>();
            yield return FlickerCanvasGroup(cg, duration, frequency, minAlpha);
        }

        private IEnumerator FlickerCanvasGroup(CanvasGroup cg, float duration, float frequency, float minAlpha)
        {
            if (cg == null) yield break;
            float time = 0;
            float originalAlpha = cg.alpha;
            while (time < duration)
            {
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)));
                cg.alpha = alpha;
                time += Time.deltaTime;
                yield return null;
            }
            cg.alpha = originalAlpha;
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
