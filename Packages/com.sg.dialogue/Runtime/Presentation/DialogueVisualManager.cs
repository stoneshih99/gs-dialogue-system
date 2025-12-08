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
            public Coroutine FadeRoutine { get; set; }
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
        // [SerializeField] private float backgroundFadeDuration = 0.3f;

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
        
        /// <summary>
        /// 顯示中央舞台文字。
        /// </summary>
        /// <param name="message">要顯示的最終文字訊息。</param>
        /// <param name="speed"></param>
        public void ShowStageText(string message, float speed)
        {
            if (stageTextPresenter != null)
            {
                stageTextPresenter.ShowMessage(message, speed);
            }
        }

        /// <summary>
        /// 隱藏中央舞台文字。
        /// </summary>
        public void HideStageText()
        {
            if (stageTextPresenter != null)
            {
                stageTextPresenter.Hide();
            }
        }

        /// <summary>
        /// 查詢中央舞台文字的打字機效果是否正在進行中。
        /// </summary>
        /// <returns>如果正在打字，則為 true；否則為 false。</returns>
        public bool IsStageTextTyping()
        {
            return stageTextPresenter != null && stageTextPresenter.IsTyping;
        }

        /// <summary>
        /// 根據文本節點更新視覺效果，主要是設定角色高光。
        /// </summary>
        /// <param name="node">當前的文本節點。</param>
        public void UpdateFromTextNode(TextNode node)
        {
            SetCharacterHighlights(node.speakerName);
        }

        /// <summary>
        /// 播放動畫節點中定義的動畫。
        /// </summary>
        /// <param name="node">包含動畫數據的節點。</param>
        public IEnumerator PlayAnimations(AnimationNode node)
        {
            if (node.motions == null || node.motions.Count == 0)
            {
                yield break;
            }

            if (_activeCharacters.TryGetValue(node.targetAnimationPosition, out var activeCharacter))
            {
                var motionPlayer = activeCharacter.Instance.GetComponent<LitMotionPlayer>();
                if (motionPlayer != null)
                {
                    foreach (var motionData in node.motions)
                    {
                        motionPlayer.Play(motionData);
                    }
                    float maxDuration = 0;
                    foreach (var motionData in node.motions)
                    {
                        maxDuration = Mathf.Max(maxDuration, motionData.Duration + motionData.Delay);
                    }
                    if (maxDuration > 0)
                    {
                        yield return new WaitForSeconds(maxDuration);
                    }
                }
            }
        }

        /// <summary>
        /// 根據角色動作節點更新場景，處理角色的進入和退出。
        /// </summary>
        /// <param name="node">包含角色動作指令的節點。</param>
        public IEnumerator UpdateFromCharacterActionNode(CharacterActionNode node)
        {
            // float duration = node.OverrideDuration ? node.Duration : portraitFadeDuration;
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

        /// <summary>
        /// 根據設定背景節點更新背景圖片。
        /// </summary>
        /// <param name="node">包含背景設定資訊的節點。</param>
        public IEnumerator UpdateFromSetBackgroundNode(SetBackgroundNode node)
        {
            float bgFadeTime = node.backgroundFadeOverride;
            // int layerIndex = 0;
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

        /// <summary>
        /// 執行閃爍效果。
        /// </summary>
        /// <param name="node">包含閃爍效果參數的節點。</param>
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

        /// <summary>
        /// 處理角色的進入動作。
        /// </summary>
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

        /// <summary>
        /// 實例化一個新的角色肖像。
        /// </summary>
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
                case PortraitRenderMode.Spine:
                    characterInstance = Instantiate(node.spinePortraitConfig.modelPrefab);
                    var spinePresenter = characterInstance.GetComponent<SpineDialoguePortraitPresenter>();
                    if (spinePresenter == null) spinePresenter = characterInstance.AddComponent<SpineDialoguePortraitPresenter>();
                    spinePresenter.ShowSpine(node.spinePortraitConfig, duration);
                    presenter = spinePresenter;
                    break;
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
                newState.FadeRoutine = FadeCharacter(newState, true, duration, false);
                _activeCharacters[node.TargetPosition] = newState;
            }
            else if (characterInstance != null)
            {
                Destroy(characterInstance);
            }
        }

        /// <summary>
        /// 更新已存在的角色肖像。
        /// </summary>
        private void UpdateExistingCharacter(CharacterState existingState, CharacterActionNode node)
        {
            if (node.portraitRenderMode == PortraitRenderMode.Spine)
            {
                var spinePresenter = existingState.Instance.GetComponent<SpineDialoguePortraitPresenter>();
                if (spinePresenter != null) spinePresenter.ShowSpine(node.spinePortraitConfig, 0f);
            }
#if LIVE2D_KIT_AVAILABLE
            else if (node.portraitRenderMode == PortraitRenderMode.Live2D)
            {
                var live2DPresenter = existingState.Instance.GetComponent<Live2DDialoguePortraitPresenter>();
                if (live2DPresenter != null) live2DPresenter.ShowLive2D(node.live2DPortraitConfig, 0f);
            }
#endif
            else if (node.portraitRenderMode == PortraitRenderMode.SpriteSheet)
            {
                var spriteSheetPresenter = existingState.Instance.GetComponent<SpriteSheetDialoguePortraitPresenter>();
                if (spriteSheetPresenter != null)
                    spriteSheetPresenter.ShowSpriteSheet(node.spriteSheetAnimationName, 0f);
            }
            else if (node.portraitRenderMode == PortraitRenderMode.Sprite)
            {
                var imagePresenter = existingState.Instance.GetComponent<ImageDialoguePortraitPresenter>();
                if (imagePresenter != null) imagePresenter.ShowSprite(node.characterSprite, 0f);
            }
        }
        
        /// <summary>
        /// 根據當前說話者設定角色的高光狀態。
        /// </summary>
        /// <param name="currentSpeakerName">當前說話者的名字。</param>
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

        /// <summary>
        /// 清除指定位置的角色。
        /// </summary>
        /// <param name="position">要清除的角色位置。</param>
        /// <param name="duration">淡出持續時間。</param>
        private void ClearCharacterAt(CharacterPosition position, float duration)
        {
            if (_activeCharacters.TryGetValue(position, out var activeCharacter))
            {
                FadeCharacter(activeCharacter, false, duration, true);
                _activeCharacters.Remove(position);
            }
        }

        /// <summary>
        /// 清除所有角色。
        /// </summary>
        /// <param name="duration">淡出持續時間。</param>
        private void ClearAllCharacters(float duration)
        {
            var positions = new List<CharacterPosition>(_activeCharacters.Keys);
            foreach (var position in positions) ClearCharacterAt(position, duration);
        }

        /// <summary>
        /// 對角色進行淡入或淡出。
        /// </summary>
        private Coroutine FadeCharacter(CharacterState character, bool fadeIn, float duration, bool destroyOnComplete)
        {
            if (character.FadeRoutine != null) StopCoroutine(character.FadeRoutine);
            var cg = character.Instance.GetComponent<CanvasGroup>();
            if (cg == null) cg = character.Instance.AddComponent<CanvasGroup>();
            if(fadeIn) cg.alpha = 0f;
            var newRoutine = StartCoroutine(FadeRoutine(cg, fadeIn ? 1f : 0f, duration, destroyOnComplete ? character.Instance : null));
            character.FadeRoutine = newRoutine;
            return newRoutine;
        }

        /// <summary>
        /// CanvasGroup 的淡入淡出協程。
        /// </summary>
        private IEnumerator FadeRoutine(CanvasGroup cg, float targetAlpha, float duration, GameObject destroyTarget)
        {
            if (cg == null) { if(destroyTarget != null) Destroy(destroyTarget); yield break; }
            float startAlpha = cg.alpha;
            if (duration <= 0f) { cg.alpha = targetAlpha; }
            else
            {
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                    yield return null;
                }
                cg.alpha = targetAlpha;
            }
            if (destroyTarget != null && Mathf.Approximately(targetAlpha, 0f)) Destroy(destroyTarget);
        }
        
        /// <summary>
        /// 更新背景圖片。
        /// </summary>
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

        /// <summary>
        /// Image 的淡入淡出協程。
        /// </summary>
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
            float t = 0f;
            while (t < duration)
            {
                c.a = Mathf.Lerp(startAlpha, endAlpha, t / duration);
                image.color = c;
                t += Time.deltaTime;
                yield return null;
            }
            c.a = endAlpha;
            image.color = c;
            if (!enable || targetSprite == null) image.enabled = false;
        }
        
        /// <summary>
        /// 對 Image 執行閃爍效果。
        /// </summary>
        private IEnumerator FlickerImage(Image image, float duration, float frequency, float minAlpha)
        {
            if (image == null) yield break;
            var cg = image.GetComponent<CanvasGroup>();
            if (cg == null) cg = image.gameObject.AddComponent<CanvasGroup>();
            yield return FlickerCanvasGroup(cg, duration, frequency, minAlpha);
        }

        /// <summary>
        /// 對 CanvasGroup 執行閃爍效果。
        /// </summary>
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

        /// <summary>
        /// 建立角色舞台位置的查找表。
        /// </summary>
        private void BuildStageLookup()
        {
            _stageLookup.Clear();
            if (leftPortraitStage != null) _stageLookup[CharacterPosition.Left] = leftPortraitStage;
            if (centerPortraitStage != null) _stageLookup[CharacterPosition.Center] = centerPortraitStage;
            if (rightPortraitStage != null) _stageLookup[CharacterPosition.Right] = rightPortraitStage;
        }
    }
}
