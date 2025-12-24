using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace SG.Dialogue.Animation
{
    /// <summary>
    /// DialogueTextAnimator 是一個用於為 TextMeshPro 文本添加即時頂點動畫效果的組件。
    /// <para>
    /// <b>功能說明：</b><br/>
    /// 1. 自動解析文本中的自定義 XML 標籤（目前支援 <shake>內容</shake>）。<br/>
    /// 2. 在運行時修改 TextMeshPro 的網格頂點，實現動態效果。<br/>
    /// </para>
    /// <para>
    /// <b>使用範例：</b><br/>
    /// 在對話節點的文本中輸入：<br/>
    /// <code>"這是一段普通的文字，<shake>這段文字會震動</shake>，然後恢復正常。"</code>
    /// </para>
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class DialogueTextAnimator : MonoBehaviour
    {
        
        /// <summary>
        /// 震動效果的振幅（像素單位）。數值越大，震動幅度越大。
        /// </summary>
        private const float ShakeAmplitude = 2f;
        /// <summary>
        /// 震動效果的頻率（次/秒）。數值越大，震動越快。
        /// </summary>
        private const float ShakeFrequency = 25f;
        
        /// <summary>
        /// 內部結構，用於儲存動畫效果的類型和作用範圍。
        /// </summary>
        private struct AnimatedEffect
        {
            public string Type; // 效果類型，例如 "shake"
            public int StartIndex; // 效果開始的字元索引（包含）
            public int EndIndex; // 效果結束的字元索引（不包含）
        }

        // 支持的自定義動畫標籤集合
        private static readonly HashSet<string> SupportedEffects = new HashSet<string> { "shake" };

        private TMP_Text _textMeshPro; // 綁定到的 TextMeshPro 組件
        private readonly List<AnimatedEffect> _effects = new List<AnimatedEffect>(); // 儲存解析出的動畫效果列表
        private string _processedText; // 移除了自定義標籤後的純文本（用於顯示）

        private void Awake()
        {
            _textMeshPro = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// 解析輸入文本中的動畫標籤，並將處理後的文本設置到 TextMeshPro 組件。
        /// <para>此方法應在每次文本內容改變時呼叫。</para>
        /// </summary>
        /// <param name="text">包含動畫標籤的原始文本。</param>
        public void Animate(string text)
        {
            _effects.Clear(); // 清空之前的效果
            _processedText = ParseText(text); // 解析文本，提取效果並獲取純文本
            _textMeshPro.text = _processedText; // 將純文本設置到 TextMeshPro
        }

        /// <summary>
        /// 獲取字符串中可見字符的長度（即移除所有 HTML/Rich Text 標籤後的長度）。
        /// </summary>
        private int GetVisibleCharacterLength(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            // 移除所有 HTML 標籤來計算可見字符長度
            return Regex.Replace(s, "<.*?>", string.Empty).Length;
        }

        /// <summary>
        /// 解析文本中的自定義動畫標籤（例如 <shake>內容</shake>），同時保留 Rich Text 和其他特殊標籤。
        /// </summary>
        /// <param name="text">原始文本。</param>
        /// <returns>移除了自定義動畫標籤的純文本。</returns>
        private string ParseText(string text)
        {
            _effects.Clear();

            // 正則表達式用於查找所有成對的標籤
            // Group 1: 標籤名稱 (例如 "shake")
            // Group 2: 標籤內容
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
                    // 計算該內容在最終顯示文本中的起始索引
                    // 原始索引 - 之前移除的標籤長度 - 當前標籤的開頭長度 (<shake>)
                    int startIndexInProcessedString = match.Groups[2].Index - customTagOffset - (fullTag.Length + 2);
                    int visibleLengthOfContent = GetVisibleCharacterLength(match.Groups[2].Value);

                    var effect = new AnimatedEffect
                    {
                        Type = tagName,
                        StartIndex = startIndexInProcessedString,
                        EndIndex = startIndexInProcessedString + visibleLengthOfContent
                    };
                    _effects.Add(effect);

                    // 累加移除的標籤長度到偏移量
                    // 標籤總長度 = 開頭標籤長度 + 結尾標籤長度 + 尖括號和斜線
                    // <tag>...</tag> -> tag長度*2 + 5 (<, >, <, /, >)
                    customTagOffset += fullTag.Length * 2 + 5; 
                }
            }

            // 如果沒有支持的效果，直接返回原文本
            if (SupportedEffects.Count == 0) return text;

            // 移除所有自定義標籤，只保留內容和標準 Rich Text 標籤
            var processedText = text;
            foreach (var effectName in SupportedEffects)
            {
                // 移除開頭標籤 (case-insensitive)
                processedText = Regex.Replace(processedText, $"<{effectName}>", string.Empty, RegexOptions.IgnoreCase);
                // 移除結尾標籤 (case-insensitive)
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
        /// 使用 Perlin Noise 來產生隨機但平滑的位移。
        /// </summary>
        /// <param name="textInfo">TextMeshPro 的文本資訊。</param>
        /// <param name="startIndex">效果開始的字元索引。</param>
        /// <param name="endIndex">效果結束的字元索引。</param>
        private void ApplyShakeEffect(TMP_TextInfo textInfo, int startIndex, int endIndex)
        {
            if (_textMeshPro == null) return;
            if (textInfo == null) return;

            var characterCount = textInfo.characterCount;
            if (characterCount == 0) return;

            // 確保索引在有效範圍內
            var clampedStart = Mathf.Clamp(startIndex, 0, characterCount);
            var clampedEnd = Mathf.Clamp(endIndex, clampedStart, characterCount);

            var t = Time.unscaledTime * ShakeFrequency;

            for (var i = clampedStart; i < clampedEnd; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                // 跳過不可見字元（如空格）
                if (!charInfo.isVisible) continue;

                var materialIndex = charInfo.materialReferenceIndex;
                if (materialIndex < 0 || materialIndex >= textInfo.meshInfo.Length) continue;

                var vertices = textInfo.meshInfo[materialIndex].vertices;
                if (vertices == null || vertices.Length == 0) continue;

                var vertexIndex = charInfo.vertexIndex;
                if (vertexIndex < 0 || vertexIndex + 3 >= vertices.Length) continue;

                // 使用 Perlin Noise 計算隨機偏移
                var seed = i * 0.37f;
                var x = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f;
                var y = (Mathf.PerlinNoise(seed + 19.1f, t) - 0.5f) * 2f;
                var offset = new Vector3(x, y, 0f) * ShakeAmplitude;

                // 將偏移應用到該字元的四個頂點
                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }
        }
    }
}
