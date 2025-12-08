using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Dialogue.Core.Instructions
{
    /// <summary>
    /// 一個自定義的 YieldInstruction，用於等待多個協程同時完成。
    /// 現在包含一個可以被強制完成的機制。
    /// </summary>
    public class WaitForAll : CustomYieldInstruction
    {
        private readonly List<IEnumerator> _enumerators;
        private readonly MonoBehaviour _runner;
        private readonly List<Coroutine> _coroutines = new List<Coroutine>();
        private int _completedCount = 0;
        private bool _forceCompleted = false;
        
        /// <summary>
        /// 當所有協程完成或被強制完成時觸發的事件。
        /// </summary>
        public event Action OnComplete;

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

            if (_enumerators.Count == 0)
            {
                _completedCount = 0;
            }
            else
            {
                foreach (var enumerator in _enumerators)
                {
                    if (enumerator != null)
                    {
                        _coroutines.Add(_runner.StartCoroutine(WrapCoroutine(enumerator)));
                    }
                    else
                    {
                        _completedCount++;
                    }
                }
            }
        }

        private IEnumerator WrapCoroutine(IEnumerator enumerator)
        {
            yield return enumerator;
            HandleCompletion();
        }

        private void HandleCompletion()
        {
            if (_forceCompleted) return;
            _completedCount++;
            if (_completedCount >= _enumerators.Count)
            {
                OnComplete?.Invoke();
            }
        }

        /// <summary>
        /// 強制停止所有正在執行的協程，並將此指令標記為完成。
        /// </summary>
        public void ForceComplete()
        {
            if (_forceCompleted) return;
            _forceCompleted = true;

            foreach (var coroutine in _coroutines)
            {
                if (coroutine != null)
                {
                    _runner.StopCoroutine(coroutine);
                }
            }
            _completedCount = _enumerators.Count;
            OnComplete?.Invoke();
        }

        public override bool keepWaiting => !_forceCompleted && _completedCount < _enumerators.Count;
    }
}
