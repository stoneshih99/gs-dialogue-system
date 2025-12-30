using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace SG.Dialogue.Core.Instructions
{
    /// <summary>
    /// 一個基於 UniTask 的指令，用於等待多個非同步任務同時完成。
    /// 支援透過 ForceComplete 提早結束等待。
    /// </summary>
    public class WaitForAll : DialogueInstruction
    {
        private readonly List<UniTask> _tasks;
        private readonly UniTaskCompletionSource _forceCompleteSource;
        private bool _isCompleted;

        public WaitForAll(IEnumerable<UniTask> tasks)
        {
            _tasks = new List<UniTask>(tasks);
            _forceCompleteSource = new UniTaskCompletionSource();
        }

        /// <summary>
        /// 強制結束等待。這不會取消正在執行的任務，但會讓 WaitForAll 的 await 立即返回。
        /// </summary>
        public void ForceComplete()
        {
            if (_isCompleted) return;
            _forceCompleteSource.TrySetResult();
        }
        
        public UniTask.Awaiter GetAwaiter()
        {
            return Wait().GetAwaiter();
        }

        private async UniTask Wait()
        {
            if (_tasks == null || _tasks.Count == 0)
            {
                _isCompleted = true;
                return;
            }

            // 等待所有任務完成，或者強制完成訊號觸發
            var allTasks = UniTask.WhenAll(_tasks);
            var forceTask = _forceCompleteSource.Task;

            await UniTask.WhenAny(allTasks, forceTask);
            
            _isCompleted = true;
        }
    }
}
