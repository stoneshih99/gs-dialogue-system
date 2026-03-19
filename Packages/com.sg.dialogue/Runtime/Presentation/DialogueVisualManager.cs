using System;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Animation;
using SG.Dialogue.Nodes;
using SG.Dialogue.Resource;
using SG.Dialogue.Utils;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 管理對話中的視覺元素，作為 PortraitManager 和 BackgroundManager 的外觀 (Facade)。
    /// </summary>
    public class DialogueVisualManager : MonoBehaviour
    {
        [Header("子管理器")]
        [SerializeField] private PortraitManager portraitManager;
        [SerializeField] private BackgroundManager backgroundManager;
        
        [Header("中央舞台文字")]
        [Tooltip("用於在舞台中央顯示文字的呈現器。")]
        [SerializeField] private StageTextPresenter stageTextPresenter;

        private void Awake()
        {
            if (portraitManager == null) portraitManager = FindAnyObjectByType<PortraitManager>();
            if (backgroundManager == null) backgroundManager = FindAnyObjectByType<BackgroundManager>();
            if (stageTextPresenter == null) stageTextPresenter = FindAnyObjectByType<StageTextPresenter>();
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

        public void CompleteStageTextTyping()
        {
            if (stageTextPresenter != null)
            {
                stageTextPresenter.SkipTypewriter();
            }
        }

        public void UpdateFromTextNode(TextNode node)
        {
            if (portraitManager != null)
            {
                portraitManager.SetCharacterHighlights(node.speakerName);
            }
        }

        public async UniTask PlayAnimations(AnimationNode node)
        {
            if (portraitManager != null)
            {
                await portraitManager.PlayAnimations(node);
            }
        }

        public async UniTask UpdateFromCharacterActionNode(CharacterActionNode node)
        {
            var duration = node.Duration;
            
            if (portraitManager != null)
            {
                portraitManager.ProcessCharacterAction(node, duration);
            }
            
            if (duration > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), ignoreTimeScale: true, cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        }

        public async UniTask UpdateFromSetBackgroundNode(SetBackgroundNode node)
        {
            if (backgroundManager != null)
            {
                await backgroundManager.ProcessSetBackground(node);
            }
        }

        public async UniTask ExecuteFlickerEffect(FlickerEffectNode node)
        {
            if (node.target == FlickerEffectNode.TargetType.Background)
            {
                if (backgroundManager != null)
                {
                    var image = backgroundManager.GetBackgroundImage(node.backgroundLayerIndex);
                    if (image != null)
                    {
                        var cg = image.GetComponent<CanvasGroup>() ?? image.gameObject.AddComponent<CanvasGroup>();
                        await UIAnimationUtils.Flicker(cg, node.duration, node.frequency, node.minAlpha);
                    }
                }
            }
            else // Character
            {
                // 這部分邏輯需要 PortraitManager 支援獲取角色 Presenter
                // 目前 PortraitManager 封裝了 _activeCharacters，我們需要暴露一個方法來執行 Flicker
                // 為了簡單起見，我們暫時略過這裡，或者在 PortraitManager 中添加 FlickerCharacter 方法
                await UniTask.CompletedTask;
            }
        }

        /// <summary>
        /// 釋放所有透過 DialogueResourceBridge 載入的資源。
        /// 應在對話結束時由 DialogueController 呼叫。
        /// </summary>
        public void ReleaseAllResources()
        {
            if (portraitManager != null) portraitManager.ReleaseAllResources();
            if (backgroundManager != null) backgroundManager.ReleaseAllResources();
            DialogueResourceBridge.ReleaseAll();
        }
    }
}
