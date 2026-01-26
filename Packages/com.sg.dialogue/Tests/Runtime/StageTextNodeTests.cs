using System.Collections;
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SG.Dialogue;
using SG.Dialogue.Nodes;
using SG.Dialogue.Presentation;
using Cysharp.Threading.Tasks;

namespace SG.Dialogue.Tests
{
    public class StageTextNodeTests
    {
        private GameObject _testGo;
        private DialogueController _controller;
        private DialogueVisualManager _visualManager;
        private StageTextPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _testGo = new GameObject("TestObject");

            // 依序加入組件，模擬依賴關係
            _presenter = _testGo.AddComponent<StageTextPresenter>();

            // 加入 UI Manager (DialogueController 需要)
            _testGo.AddComponent<SG.Dialogue.UI.DialogueUIManager>();

            _visualManager = _testGo.AddComponent<DialogueVisualManager>();
            _controller = _testGo.AddComponent<DialogueController>();

            // 使用反射設置 visualManager，確保 Controller 能正確引用
            var visualManagerField = typeof(DialogueController).GetField("visualManager", BindingFlags.NonPublic | BindingFlags.Instance);
            visualManagerField?.SetValue(_controller, _visualManager);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGo);
            }
        }

        [UnityTest]
        public IEnumerator StageTextNode_AutoAdvance_ForceEnable_ShouldAdvanceAutomatically() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var node = new StageTextNode();
            node.message = "Test Message";
            node.typingSpeed = 0.01f; // 快速打字
            node.postTypingDelay = 0f;

            // 設定強制自動前進
            _controller.autoAdvanceOverride = AutoAdvanceMode.ForceEnable;
            _controller.forcedAutoAdvanceDelay = 0.1f; // 短延遲以便快速測試

            // Act
            bool isCompleted = false;

            // 執行節點邏輯
            // 注意：如果程式碼有 bug，這裡會卡在 DoShowText 的等待輸入
            UniTask processTask = node.Process(_controller);

            // 當 task 完成時設定旗標
            processTask.ContinueWith(() => isCompleted = true).Forget();

            // Assert
            // 預期時間計算：
            // 打字: "Test Message".Length (12) * 0.01 = 0.12s
            // 自動前進延遲: 0.1s
            // 緩衝: 0.3s
            // 總共約 0.5s 應該要完成

            float timeout = 2.0f; // 給予充裕的超時時間
            float startTime = Time.time;

            while (!isCompleted && Time.time - startTime < timeout)
            {
                await UniTask.Yield();
            }

            Assert.IsTrue(isCompleted, "StageTextNode process should complete automatically when AutoAdvance is ForceEnable.");
        });

        [UnityTest]
        public IEnumerator StageTextNode_NoAutoAdvance_ShouldWait() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var node = new StageTextNode();
            node.message = "Wait Test";
            node.typingSpeed = 0.01f;

            _controller.autoAdvanceOverride = AutoAdvanceMode.ForceDisable;

            // Act
            bool isCompleted = false;
            UniTask processTask = node.Process(_controller);
            processTask.ContinueWith(() => isCompleted = true).Forget();

            // 等待打字完成 (0.09s)
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            // Assert
            // 因為沒有自動前進，也沒有輸入，應該還在等待
            Assert.IsFalse(isCompleted, "StageTextNode should NOT complete without input when AutoAdvance is Disabled.");
        });
    }
}
