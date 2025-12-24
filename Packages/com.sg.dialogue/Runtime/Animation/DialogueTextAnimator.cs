using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace SG.Dialogue.Animation
{
    /// <summary>
    /// DialogueTextAnimator 是一個用於為 TextMeshPro 文本添加動畫效果的組件。
    /// 它通過解析文本中的自定義標籤（例如 &lt;shake&gt;...&lt;/shake&gt;）來應用不同的視覺效果。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class DialogueTextAnimator : MonoBehaviour
    {
        
        /// <summary>
        /// 震動效果的振幅和頻率設定。 
        /// </summary>
        private const float ShakeAmplitude = 2f;
        /// <summary>
        /// 震動效果的頻率設定。 
        /// </summary>
        private const float ShakeFrequency = 25f;
        
        /// <summary>
        /// 內部結構，用於儲存動畫效果的類型和作用範圍。
        /// </summary>
        private struct AnimatedEffect
        {
            public string Type; // 效果類型，例如 "shake"
            public int StartIndex; // 效果開始的字元索引
            public int EndIndex; // 效果結束的字元索引
        }

        // 支持的自定義動畫標籤
        private static readonly HashSet<string> SupportedEffects = new HashSet<string> { "shake" };

        private TMP_Text _textMeshPro; // 綁定到的 TextMeshPro 組件
        private readonly List<AnimatedEffect> _effects = new List<AnimatedEffect>(); // 儲存解析出的動畫效果列表
        private string _processedText; // 移除了自定義標籤的純文本

        private void Awake()
        {
            _textMeshPro = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// 解析輸入文本中的動畫標籤，並將純文本設置到 TextMeshPro 組件。
        /// </summary>
        /// <param name="text">包含動畫標籤的原始文本。</param>
        public void Animate(string text)
        {
            _effects.Clear(); // 清空之前的效果
            _processedText = ParseText(text); // 解析文本，提取效果並獲取純文本
            _textMeshPro.text = _processedText; // 將純文本設置到 TextMeshPro
        }

        /// <summary>
        /// 獲取字符串中可見字符的長度（即移除所有標籤後）。
        /// </summary>
        private int GetVisibleCharacterLength(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            // 移除所有 HTML 標籤來計算可見字符長度
            return Regex.Replace(s, "<.*?>", string.Empty).Length;
        }

        /// <summary>
        /// 解析文本中的自定義動畫標籤（例如 &lt;shake&gt;...&lt;/shake&gt;），同時保留 Rich Text 和其他特殊標籤。
        /// </summary>
        /// <param name="text">原始文本。</param>
        /// <returns>移除了自定義動畫標籤的純文本。</returns>
        private string ParseText(string text)
        {
            _effects.Clear();

            // 正則表達式用於查找所有成對的標籤
            var regex = new Regex(@"<(\w+.*?)>(.*?)<\/\1>", RegexOptions.Singleline);
            var matches = regex.Matches(text);
            int customTagOffset = 0; // 追蹤到目前為止移除的自定義標籤的總長度

            foreach (Match match in matches)
            {
                string fullTag = match.Groups[1].Value;
                string tagName = fullTag.Split('=')[0].ToLower();

                if (SupportedEffects.Contains(tagName))
                {
                    // 這是一個自定義動畫標籤
                    int startIndexInProcessedString = match.Groups[2].Index - customTagOffset - (fullTag.Length + 2);
                    int visibleLengthOfContent = GetVisibleCharacterLength(match.Groups[2].Value);

                    var effect = new AnimatedEffect
                    {
                        Type = tagName,
                        StartIndex = startIndexInProcessedString,
                        EndIndex = startIndexInProcessedString + visibleLengthOfContent
                    };
                    _effects.Add(effect);

                    // 僅增加自定義標籤的長度到偏移量
                    customTagOffset += fullTag.Length * 2 + 5; // 例如 <shake> 和 </shake>
                }
            }

            // 如果沒有支持的效果，直接返回原文本
            if (SupportedEffects.Count == 0) return text;

            // 建立一個正則表達式，僅匹配並移除我們的自定義動畫標籤
            var processedText = text;
            foreach (var effectName in SupportedEffects)
            {
                // 移除開頭標籤
                processedText = Regex.Replace(processedText, $"<{effectName}>", string.Empty, RegexOptions.IgnoreCase);
                // 移除結尾標籤
                processedText = Regex.Replace(processedText, $"</{effectName}>", string.Empty, RegexOptions.IgnoreCase);
            }
            return processedText;
        }


        private void Update()
        {
            // 如果 TextMeshPro 組件為空或沒有動畫效果，則不執行任何操作
            if (_textMeshPro == null || _effects.Count == 0) return;

            _textMeshPro.ForceMeshUpdate(); // 強制更新網格，以獲取最新的文本信息
            var textInfo = _textMeshPro.textInfo;
            if (textInfo.characterCount == 0) return; // 如果沒有字元，則不執行任何操作

            // 獲取第一個網格的頂點數據
            // 注意：對於多個網格的文本，可能需要遍歷所有 meshInfo
            // var vertices = textInfo.meshInfo[0].vertices;

            foreach (var effect in _effects)
            {
                if (effect.Type == "shake")
                {
                    ApplyShakeEffect(textInfo, effect.StartIndex, effect.EndIndex); // 應用震動效果
                }
                // 在這裡可以添加其他效果的處理，例如 wave, rainbow 等
            }

            // 將修改後的頂點數據推回網格
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                if (textInfo.meshInfo[i].vertexCount > 0)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    _textMeshPro.UpdateGeometry(textInfo.meshInfo[i].mesh, i); // 更新網格幾何體
                }
            }
        }

        /// <summary>
        /// 對指定範圍內的字元應用震動效果。 
        /// </summary>
        /// <param name="textInfo"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        private void ApplyShakeEffect(TMP_TextInfo textInfo, int startIndex, int endIndex)
        {
            if (_textMeshPro == null) return;
            if (textInfo == null) return;

            int characterCount = textInfo.characterCount;
            if (characterCount == 0) return;

            int clampedStart = Mathf.Clamp(startIndex, 0, characterCount);
            int clampedEnd = Mathf.Clamp(endIndex, clampedStart, characterCount);

            float t = Time.unscaledTime * ShakeFrequency;

            for (int i = clampedStart; i < clampedEnd; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                if (materialIndex < 0 || materialIndex >= textInfo.meshInfo.Length) continue;

                var vertices = textInfo.meshInfo[materialIndex].vertices;
                if (vertices == null || vertices.Length == 0) continue;

                int vertexIndex = charInfo.vertexIndex;
                if (vertexIndex < 0 || vertexIndex + 3 >= vertices.Length) continue;

                float seed = i * 0.37f;
                float x = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(seed + 19.1f, t) - 0.5f) * 2f;
                Vector3 offset = new Vector3(x, y, 0f) * ShakeAmplitude;

                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }
        }
    }
}
