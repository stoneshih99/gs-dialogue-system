using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SG.Dialogue.Animation;
using SG.Dialogue.Enums;
using SG.Dialogue.Nodes;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// DialogueVisualManager 負責管理對話系統中的視覺呈現，包括角色立繪、背景圖片和相關的淡入淡出效果。
    /// 現在支援多個背景圖層，並處理說話角色高亮/灰階效果。
    /// </summary>
    public class DialogueVisualManager : MonoBehaviour
    {
        /// <summary>
        /// 內部類別，用於儲存活躍角色的狀態，包括其實例、淡入淡出協程、來源 Prefab、說話者名稱和立繪呈現器。
        /// </summary>
        private class CharacterState
        {
            public GameObject Instance { get; } // 角色實例
            public Coroutine FadeRoutine { get; set; } // 淡入淡出協程
            public Object SourcePrefab { get; } // 來源 Prefab (用於判斷是否為同一個角色)
            public string SpeakerName { get; set; } // 說話者名稱
            public IDialoguePortraitPresenter Presenter { get; } // 立繪呈現器

            public CharacterState(GameObject instance, Object sourcePrefab, string speakerName, IDialoguePortraitPresenter presenter)
            {
                Instance = instance;
                SourcePrefab = sourcePrefab;
                SpeakerName = speakerName;
                Presenter = presenter;
            }
        }

        [Header("Character Stages")]
        [Tooltip("左側角色立繪的舞台 Transform。")]
        [SerializeField] private Transform leftPortraitStage;
        [Tooltip("中央角色立繪的舞台 Transform。")]
        [SerializeField] private Transform centerPortraitStage;
        [Tooltip("右側角色立繪的舞台 Transform。")]
        [SerializeField] private Transform rightPortraitStage;

        [Header("Character Settings")]
        [Tooltip("角色立繪預設的淡入淡出持續時間。")]
        [SerializeField] private float portraitFadeDuration = 0.2f;

        [Header("Background")]
        [Tooltip("顯示背景圖片的 Image 組件列表。可以有多個圖層。")]
        [SerializeField] private List<Image> backgroundImages;
        [Tooltip("背景圖片預設的淡入淡出持續時間。")]
        [SerializeField] private float backgroundFadeDuration = 0.3f;

        private readonly Dictionary<CharacterPosition, Transform> _stageLookup = new(); // 角色位置到舞台 Transform 的查找表
        private readonly Dictionary<CharacterPosition, CharacterState> _activeCharacters = new(); // 活躍角色的狀態
        // 為每個背景圖層獨立管理淡入淡出協程
        private readonly List<Coroutine> _backgroundFadeRoutines = new List<Coroutine>();

        private void Awake()
        {
            BuildStageLookup(); // 建立舞台查找表
            // 初始化背景淡入淡出協程列表
            for (int i = 0; i < backgroundImages.Count; i++)
            {
                _backgroundFadeRoutines.Add(null);
            }
        }

        /// <summary>
        /// 根據 TextNode 的設定更新視覺效果。
        /// 主要處理角色動畫和高亮/灰階效果。
        /// </summary>
        /// <param name="node">TextNode 實例。</param>
        public void UpdateFromTextNode(TextNode node)
        {
            // 處理角色動畫
            if (node.motions != null && node.motions.Count > 0)
            {
                if (_activeCharacters.TryGetValue(node.targetAnimationPosition, out var activeCharacter))
                {
                    var motionPlayer = activeCharacter.Instance.GetComponent<LitMotionPlayer>();
                    if (motionPlayer != null)
                    {
                        foreach (var motionData in node.motions) motionPlayer.Play(motionData);
                    }
                    else
                    {
                        Debug.LogWarning($"DialogueVisualManager: Character prefab '{activeCharacter.Instance.name}' is missing a LitMotionPlayer component.", activeCharacter.Instance);
                    }
                }
            }

            // 處理角色高亮/灰階
            SetCharacterHighlights(node.speakerName);
        }

        /// <summary>
        /// 根據 TransitionNode 的設定更新視覺效果。
        /// 處理背景切換、角色清除和黑屏效果。
        /// </summary>
        /// <param name="node">TransitionNode 實例。</param>
        /// <returns>協程。</returns>
        public IEnumerator UpdateFromTransitionNode(TransitionNode node)
        {
            // 判斷是否覆寫背景和角色淡入淡出時間
            float bgFadeTime = node.overrideBackgroundFade ? node.backgroundFadeOverride : backgroundFadeDuration;
            float charFadeTime = node.overrideCharacterFade ? node.characterFadeOverride : portraitFadeDuration;

            // 預設更新第 0 層背景
            int defaultLayer = 0;

            if (node.useBlackScreen && backgroundImages.Count > defaultLayer && backgroundImages[defaultLayer] != null) // 如果啟用黑屏效果
            {
                if (_backgroundFadeRoutines[defaultLayer] != null) StopCoroutine(_backgroundFadeRoutines[defaultLayer]);
                // 淡出背景到透明，然後等待黑屏持續時間
                _backgroundFadeRoutines[defaultLayer] = StartCoroutine(FadeImageRoutine(backgroundImages[defaultLayer], backgroundImages[defaultLayer].sprite, false, bgFadeTime));
                yield return _backgroundFadeRoutines[defaultLayer];
                if (node.blackScreenDuration > 0) yield return new WaitForSeconds(node.blackScreenDuration);
            }

            // 更新背景 (預設更新第 0 層)
            yield return UpdateBackground(defaultLayer, node.backgroundSprite, node.clearBackground, bgFadeTime); 
            
            if (node.clearCharacters) ClearAllCharacters(charFadeTime); // 如果設定清除所有角色

            // 等待角色淡出完成
            if (node.clearCharacters && charFadeTime > 0f) yield return new WaitForSeconds(charFadeTime);
        }

        /// <summary>
        /// 根據 CharacterActionNode 的設定更新視覺效果。
        /// 處理角色進場或退場。
        /// </summary>
        /// <param name="node">CharacterActionNode 實例。</param>
        /// <returns>協程。</returns>
        public IEnumerator UpdateFromCharacterActionNode(CharacterActionNode node)
        {
            // 判斷是否覆寫角色動畫持續時間
            float duration = node.OverrideDuration ? node.Duration : portraitFadeDuration;
            
            switch (node.ActionType)
            {
                case CharacterActionType.Enter:
                    ProcessEnterAction(node.TargetPosition, node.portraitRenderMode, GetSourcePrefab(node), node.spinePortraitConfig, node.speakerName, duration);
                    break;
                case CharacterActionType.Exit:
                    if (node.ClearAllOnExit) ClearAllCharacters(duration); // 清除所有角色
                    else ClearCharacterAt(node.TargetPosition, duration); // 清除指定位置的角色
                    break;
            }
            
            if (duration > 0) yield return new WaitForSeconds(duration); // 等待動畫完成
        }

        /// <summary>
        /// 根據 SetBackgroundNode 的設定更新視覺效果。
        /// 處理背景圖片的設定。
        /// </summary>
        /// <param name="node">SetBackgroundNode 實例。</param>
        /// <returns>協程。</returns>
        public IEnumerator UpdateFromSetBackgroundNode(SetBackgroundNode node)
        {
            if (node.BackgroundEntries == null || node.BackgroundEntries.Count == 0)
            {
                yield break;
            }

            foreach (var entry in node.BackgroundEntries)
            {
                // 判斷是否覆寫背景動畫持續時間
                float duration = entry.OverrideDuration ? entry.Duration : backgroundFadeDuration;
                
                // 更新背景
                yield return UpdateBackground(entry.TargetLayerIndex, entry.BackgroundSprite, entry.ClearBackground, duration);
            }
        }

        /// <summary>
        /// 執行閃爍效果。
        /// </summary>
        /// <param name="node">FlickerEffectNode 實例。</param>
        /// <returns>協程。</returns>
        public IEnumerator ExecuteFlickerEffect(FlickerEffectNode node)
        {
            switch (node.Target)
            {
                case FlickerEffectNode.TargetType.Background:
                    if (node.BackgroundLayerIndex >= 0 && node.BackgroundLayerIndex < backgroundImages.Count)
                    {
                        yield return FlickerImage(backgroundImages[node.BackgroundLayerIndex], node.Duration, node.Frequency, node.MinAlpha);
                    }
                    else
                    {
                        Debug.LogWarning($"FlickerEffectNode: Invalid background layer index {node.BackgroundLayerIndex}.");
                    }
                    break;
                case FlickerEffectNode.TargetType.Character:
                    if (_activeCharacters.TryGetValue(node.CharacterPosition, out var characterState) && characterState.Presenter != null)
                    {
                        yield return characterState.Presenter.Flicker(node.Duration, node.Frequency, node.MinAlpha);
                    }
                    else
                    {
                        Debug.LogWarning($"FlickerEffectNode: No active character at position {node.CharacterPosition}.");
                    }
                    break;
            }
        }

        /// <summary>
        /// 處理角色進場動作。
        /// </summary>
        /// <param name="position">角色目標位置。</param>
        /// <param name="renderMode">立繪渲染模式。</param>
        /// <param name="sourcePrefab">來源 Prefab。</param>
        /// <param name="spineConfig">Spine 設定。</param>
        /// <param name="speakerName">說話者名稱。</param>
        /// <param name="duration">淡入持續時間。</param>
        private void ProcessEnterAction(CharacterPosition position, PortraitRenderMode renderMode, Object sourcePrefab, SpinePortraitConfig spineConfig, string speakerName, float duration)
        {
            if (sourcePrefab == null) return;

            // 如果目標位置已有角色且來源 Prefab 相同，則更新現有角色
            if (_activeCharacters.TryGetValue(position, out var existingState) && existingState.SourcePrefab == sourcePrefab)
            {
                UpdateExistingCharacter(existingState.Instance, renderMode, spineConfig);
                existingState.SpeakerName = speakerName; // 更新說話者名稱
            }
            else // 否則實例化新角色
            {
                InstantiateNewCharacter(position, renderMode, sourcePrefab, spineConfig, speakerName, duration);
            }
        }

        /// <summary>
        /// 實例化一個新角色並放置到指定舞台。
        /// </summary>
        /// <param name="position">角色目標位置。</param>
        /// <param name="renderMode">立繪渲染模式。</param>
        /// <param name="sourcePrefab">來源 Prefab。</param>
        /// <param name="spineConfig">Spine 設定。</param>
        /// <param name="speakerName">說話者名稱。</param>
        /// <param name="duration">淡入持續時間。</param>
        private void InstantiateNewCharacter(CharacterPosition position, PortraitRenderMode renderMode, Object sourcePrefab, SpinePortraitConfig spineConfig, string speakerName, float duration)
        {
            if (!_stageLookup.TryGetValue(position, out var stage) || stage == null)
            {
                Debug.LogWarning($"DialogueVisualManager: No stage transform assigned for position {position}.");
                return;
            }

            ClearCharacterAt(position, duration); // 清除目標位置的舊角色

            GameObject characterInstance = null;
            IDialoguePortraitPresenter presenter = null;

            switch (renderMode)
            {
                case PortraitRenderMode.Sprite:
                    characterInstance = new GameObject("SpritePortrait");
                    var imagePresenter = characterInstance.AddComponent<ImageDialoguePortraitPresenter>();
                    imagePresenter.ShowSprite((Sprite)sourcePrefab, 0f); // 立即設定 Sprite
                    presenter = imagePresenter;
                    break;
                case PortraitRenderMode.Spine:
                    characterInstance = Instantiate((GameObject)sourcePrefab);
                    var spinePresenter = characterInstance.GetComponent<SpineDialoguePortraitPresenter>();
                    if (spinePresenter == null) spinePresenter = characterInstance.AddComponent<SpineDialoguePortraitPresenter>();
                    spinePresenter.ShowSpine(spineConfig, 0f); // 立即設定 Spine
                    presenter = spinePresenter;
                    break;
                case PortraitRenderMode.Live2D:
                    characterInstance = Instantiate((GameObject)sourcePrefab);
                    // Live2D 呈現器需要額外處理
                    presenter = characterInstance.GetComponent<IDialoguePortraitPresenter>(); // 假設 Live2D Prefab 上有 IDialoguePortraitPresenter
                    if (presenter == null) Debug.LogWarning($"Live2D Prefab '{sourcePrefab.name}' is missing an IDialoguePortraitPresenter component.");
                    break;
            }

            if (characterInstance != null && presenter != null)
            {
                characterInstance.transform.SetParent(stage, false); // 設定父物件
                
                var newState = new CharacterState(characterInstance, sourcePrefab, speakerName, presenter);
                newState.FadeRoutine = FadeCharacter(newState, true, duration, false); // 啟動淡入協程
                _activeCharacters[position] = newState; // 儲存新角色狀態
            }
            else if (characterInstance != null)
            {
                Destroy(characterInstance); // 如果沒有有效的呈現器，則銷毀實例
            }
        }

        /// <summary>
        /// 更新現有角色的視覺效果，例如 Spine 動畫設定。
        /// </summary>
        /// <param name="instance">角色實例。</param>
        /// <param name="renderMode">立繪渲染模式。</param>
        /// <param name="spineConfig">Spine 設定。</param>
        private void UpdateExistingCharacter(GameObject instance, PortraitRenderMode renderMode, SpinePortraitConfig spineConfig)
        {
            if (renderMode == PortraitRenderMode.Spine && spineConfig != null)
            {
                var skeletonAnimation = instance.GetComponent<SkeletonAnimation>();
                if (skeletonAnimation != null)
                {
                    // 更新 Spine 模型的縮放和 Skin
                    if (skeletonAnimation.Skeleton.ScaleX != spineConfig.scaleX) skeletonAnimation.Skeleton.ScaleX = spineConfig.scaleX;
                    if (!string.IsNullOrEmpty(spineConfig.skin) && skeletonAnimation.Skeleton.Skin?.Name != spineConfig.skin) skeletonAnimation.Skeleton.SetSkin(spineConfig.skin);
                    
                    // 播放進入動畫
                    var currentAnimation = skeletonAnimation.AnimationState.GetCurrent(0)?.Animation?.Name;
                    if (!string.IsNullOrEmpty(spineConfig.enterAnimation) && currentAnimation != spineConfig.enterAnimation)
                    {
                        skeletonAnimation.AnimationState.SetAnimation(0, spineConfig.enterAnimation, spineConfig.loop);
                    }

                    // 添加佇列動畫
                    if (!string.IsNullOrEmpty(spineConfig.queuedAnimation))
                    {
                        // 修正錯誤：使用 spineConfig.loop 和 spineConfig.queuedAnimationDelay
                        skeletonAnimation.AnimationState.AddAnimation(0, spineConfig.queuedAnimation, spineConfig.loop, spineConfig.queuedAnimationDelay);
                    }
                }
            }
            else if (renderMode == PortraitRenderMode.Live2D)
            {
                var animator = instance.GetComponent<Animator>();
                if (animator != null) { /* e.g., animator.SetTrigger("Enter"); */ } // Live2D 的動畫處理
            }
        }

        /// <summary>
        /// 設定所有活躍角色的高亮狀態。
        /// 當有說話者名稱時，匹配的角色高亮，其他角色灰階；
        /// 當沒有說話者名稱時，所有角色都高亮。
        /// </summary>
        /// <param name="currentSpeakerName">當前說話者的名稱。</param>
        private void SetCharacterHighlights(string currentSpeakerName)
        {
            bool hasSpeaker = !string.IsNullOrEmpty(currentSpeakerName);

            foreach (var characterState in _activeCharacters.Values)
            {
                if (characterState.Presenter == null) continue;

                if (hasSpeaker)
                {
                    // 如果有說話者，則匹配的角色高亮，其他角色灰階
                    characterState.Presenter.SetHighlight(characterState.SpeakerName == currentSpeakerName);
                }
                else
                {
                    // 如果沒有說話者（旁白），則所有角色都高亮
                    characterState.Presenter.SetHighlight(true);
                }
            }
        }

        /// <summary>
        /// 更新背景圖片。此方法現在是一個協程，會等待淡入淡出完成。
        /// </summary>
        /// <param name="layerIndex">要更新的背景圖層索引。</param>
        /// <param name="sprite">要設定的背景 Sprite。</param>
        /// <param name="clear">是否清除背景。</param>
        /// <param name="duration">淡入淡出持續時間。</param>
        /// <returns>協程。</returns>
        private IEnumerator UpdateBackground(int layerIndex, Sprite sprite, bool clear, float duration)
        {
            if (layerIndex < 0 || layerIndex >= backgroundImages.Count || backgroundImages[layerIndex] == null)
            {
                Debug.LogWarning($"DialogueVisualManager: Invalid background layer index {layerIndex} or Image component is null.");
                yield break;
            }

            Image targetImage = backgroundImages[layerIndex];
            Coroutine currentRoutine = _backgroundFadeRoutines[layerIndex];
            
            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
            }

            if (clear)
            {
                currentRoutine = StartCoroutine(FadeImageRoutine(targetImage, null, false, duration));
                _backgroundFadeRoutines[layerIndex] = currentRoutine;
                yield return currentRoutine;
            }
            
            if (sprite != null)
            {
                currentRoutine = StartCoroutine(FadeImageRoutine(targetImage, sprite, true, duration));
                _backgroundFadeRoutines[layerIndex] = currentRoutine;
                yield return currentRoutine;
            }
        }
        
        /// <summary>
        /// 根據 CharacterActionNode 的渲染模式獲取對應的來源 Prefab。
        /// </summary>
        /// <param name="node">CharacterActionNode 實例。</param>
        /// <returns>來源 Prefab。</returns>
        private Object GetSourcePrefab(CharacterActionNode node) => node.portraitRenderMode switch {
            PortraitRenderMode.Sprite => node.characterSprite,
            PortraitRenderMode.Spine => node.spinePortraitConfig?.modelPrefab,
            PortraitRenderMode.Live2D => node.live2DModelPrefab,
            _ => null
        };

        /// <summary>
        /// 清除指定位置的角色。
        /// </summary>
        /// <param name="position">角色位置。</param>
        /// <param name="duration">淡出持續時間。</param>
        private void ClearCharacterAt(CharacterPosition position, float duration)
        {
            if (_activeCharacters.TryGetValue(position, out var activeCharacter))
            {
                FadeCharacter(activeCharacter, false, duration, true); // 淡出並銷毀角色
                _activeCharacters.Remove(position);
            }
        }

        /// <summary>
        /// 清除所有活躍角色。
        /// </summary>
        /// <param name="duration">淡出持續時間。</param>
        private void ClearAllCharacters(float duration)
        {
            if (_activeCharacters.Count == 0) return;
            var positions = new List<CharacterPosition>(_activeCharacters.Keys);
            foreach (var position in positions) ClearCharacterAt(position, duration); // 遍歷所有位置並清除角色
        }

        /// <summary>
        /// 淡入淡出角色。
        /// </summary>
        /// <param name="character">角色狀態。</param>
        /// <param name="fadeIn">是否淡入。</param>
        /// <param name="duration">持續時間。</param>
        /// <param name="destroyOnComplete">淡出完成後是否銷毀物件。</param>
        /// <returns>協程。</returns>
        private Coroutine FadeCharacter(CharacterState character, bool fadeIn, float duration, bool destroyOnComplete)
        {
            if (character.FadeRoutine != null) StopCoroutine(character.FadeRoutine);
            var cg = character.Instance.GetComponent<CanvasGroup>();
            if (cg == null) cg = character.Instance.AddComponent<CanvasGroup>(); // 確保有 CanvasGroup
            if(fadeIn) cg.alpha = 0f; // 淡入時從透明開始
            var newRoutine = StartCoroutine(FadeRoutine(cg, fadeIn ? 1f : 0f, duration, destroyOnComplete ? character.Instance : null));
            character.FadeRoutine = newRoutine;
            return newRoutine;
        }

        /// <summary>
        /// CanvasGroup 的淡入淡出協程。
        /// </summary>
        /// <param name="cg">目標 CanvasGroup。</param>
        /// <param name="targetAlpha">目標 Alpha 值。</param>
        /// <param name="duration">持續時間。</param>
        /// <param name="destroyTarget">淡出完成後要銷毀的 GameObject。</param>
        /// <returns>協程。</returns>
        private IEnumerator FadeRoutine(CanvasGroup cg, float targetAlpha, float duration, GameObject destroyTarget)
        {
            if (cg == null) { if(destroyTarget != null) Destroy(destroyTarget); yield break; }
            float startAlpha = cg.alpha;
            if (duration <= 0f) { cg.alpha = targetAlpha; } // 立即設定 Alpha
            else
            {
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                    yield return null;
                }
                cg.alpha = targetAlpha; // 確保最終 Alpha 值正確
            }
            if (destroyTarget != null && Mathf.Approximately(targetAlpha, 0f)) Destroy(destroyTarget); // 如果是淡出且目標 Alpha 為 0，則銷毀物件
        }

        /// <summary>
        /// Image 組件的淡入淡出協程。
        /// </summary>
        /// <param name="image">目標 Image 組件。</param>
        /// <param name="targetSprite">目標 Sprite。</param>
        /// <param name="enable">是否啟用 Image。</param>
        /// <param name="duration">持續時間。</param>
        /// <returns>協程。</returns>
        private IEnumerator FadeImageRoutine(Image image, Sprite targetSprite, bool enable, float duration)
        {
            if (image == null) yield break;
            Color c = image.color;
            float startAlpha = c.a;
            float endAlpha = enable && targetSprite != null ? 1f : 0f;
            if (duration <= 0f) // 如果持續時間為 0，則立即設定
            {
                image.sprite = targetSprite;
                c.a = endAlpha;
                image.color = c;
                image.enabled = enable && targetSprite != null;
                yield break;
            }
            if (enable && targetSprite != null) // 如果是淡入新 Sprite
            {
                image.sprite = targetSprite;
                startAlpha = 0f;
                c.a = 0f;
                image.color = c;
                image.enabled = true;
            }
            float t = 0f;
            while (t < duration) // 漸變 Alpha
            {
                c.a = Mathf.Lerp(startAlpha, endAlpha, t / duration);
                image.color = c;
                t += Time.deltaTime;
                yield return null;
            }
            c.a = endAlpha;
            image.color = c;
            if (!enable || targetSprite == null) image.enabled = false; // 如果是淡出或沒有 Sprite，則禁用 Image
        }

        /// <summary>
        /// Image 組件的閃爍協程。
        /// </summary>
        private IEnumerator FlickerImage(Image image, float duration, float frequency, float minAlpha)
        {
            if (image == null) yield break;

            float time = 0;
            float originalAlpha = image.color.a;
            Color color = image.color;

            while (time < duration)
            {
                float alpha = Mathf.Lerp(minAlpha, originalAlpha, Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI)));
                color.a = alpha;
                image.color = color;
                time += Time.deltaTime;
                yield return null;
            }

            color.a = originalAlpha; // 恢復原始透明度
            image.color = color;
        }

        /// <summary>
        /// 建立角色位置到舞台 Transform 的查找表。
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
