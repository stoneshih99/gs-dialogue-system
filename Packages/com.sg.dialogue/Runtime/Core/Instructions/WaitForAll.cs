using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Dialogue.Core.Instructions
{
    /// <summary>
    /// 一個自定義的 YieldInstruction，用於等待多個協程同時完成。
    /// </summary>
    public class WaitForAll : CustomYieldInstruction
    {
        private List<IEnumerator> _enumerators;
        private MonoBehaviour _runner;
        private int _completedCount = 0;

        /// <summary>
        /// 構造函數。
        /// </summary>
        /// <param name="runner">用於啟動子協程的 MonoBehaviour 實例。</param>
        /// <param name="enumerators">要等待完成的協程迭代器列表。</param>
        public WaitForAll(MonoBehaviour runner, List<IEnumerator> enumerators)
        {
            if (runner == null)
            {
                Debug.LogError("WaitForAll: MonoBehaviour runner 不能為空。");
                return;
            }
            if (enumerators == null)
            {
                Debug.LogError("WaitForAll: 協程迭代器列表不能為空。");
                return;
            }

            _runner = runner;
            _enumerators = enumerators;
            _completedCount = 0;

            foreach (var enumerator in _enumerators)
            {
                if (enumerator != null)
                {
                    _runner.StartCoroutine(WrapCoroutine(enumerator));
                }
                else
                {
                    _completedCount++; // 如果有空的迭代器，直接計為完成
                }
            }
        }

        private IEnumerator WrapCoroutine(IEnumerator enumerator)
        {
            yield return enumerator; // 等待這個子協程完成
            _completedCount++; // 完成後計數
        }

        /// <summary>
        /// 當所有子協程都完成時，keepWaiting 為 false。
        /// </summary>
        public override bool keepWaiting => _completedCount < _enumerators.Count;
    }
}
