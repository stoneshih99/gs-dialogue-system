using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Nodes;
using SG.Dialogue.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Dialogue.Presentation
{
    /// <summary>
    /// 專門管理背景圖片的切換和轉場。
    /// </summary>
    public class BackgroundManager : MonoBehaviour
    {
        [Header("背景")]
        [SerializeField] private List<Image> backgroundImages;

        private readonly List<Coroutine> _backgroundFadeRoutines = new List<Coroutine>();

        private void Awake()
        {
            for (int i = 0; i < backgroundImages.Count; i++)
            {
                _backgroundFadeRoutines.Add(null);
            }
        }

        public IEnumerator ProcessSetBackground(SetBackgroundNode node)
        {
            float bgFadeTime = node.backgroundFadeOverride;
            var layerIndex = node.spriteIndex;
            if (node.useBlackScreen && backgroundImages.Count > layerIndex && backgroundImages[layerIndex] != null)
            {
                if (_backgroundFadeRoutines[layerIndex] != null) StopCoroutine(_backgroundFadeRoutines[layerIndex]);
                
                var fadeOutRoutine = StartCoroutine(UIAnimationUtils.FadeImage(backgroundImages[layerIndex], 0f, bgFadeTime));
                _backgroundFadeRoutines[layerIndex] = fadeOutRoutine;
                yield return fadeOutRoutine;
                
                if (node.blackScreenDuration > 0) yield return new WaitForSeconds(node.blackScreenDuration);
            }

            yield return UpdateBackground(layerIndex, node.backgroundSprite, node.clearBackground, bgFadeTime);
        }

        public Image GetBackgroundImage(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < backgroundImages.Count)
            {
                return backgroundImages[layerIndex];
            }
            return null;
        }

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
                var routine = StartCoroutine(UIAnimationUtils.FadeImage(targetImage, 0f, duration));
                if(layerIndex < _backgroundFadeRoutines.Count) _backgroundFadeRoutines[layerIndex] = routine;
                yield return routine;
            }
            if (sprite != null)
            {
                targetImage.sprite = sprite;
                var routine = StartCoroutine(UIAnimationUtils.FadeImage(targetImage, 1f, duration));
                if(layerIndex < _backgroundFadeRoutines.Count) _backgroundFadeRoutines[layerIndex] = routine;
                yield return routine;
            }
        }
    }
}
