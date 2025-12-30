# Change Log


### Added
- 在 `DialogueController` 新增 `WaitForInputAsync` 方法（使用 `UniTask`），用於正確地以非同步方式等待玩家輸入。
- 在 `ParticleNode` 新增 `WaitForInput` 布林欄位，可選擇在粒子特效播放完成後等待玩家輸入。

### Changed
- 將對話系統中多處協程（Coroutine）流程重構為非同步任務（`UniTask`）。
- 更新多個核心 `Instruction` 與 `Node` 的流程控制，以配合 `UniTask` / async 設計。
- 重構 `WaitForAll`、`DialogueCameraController`、`ScreenEffectController`、`PortraitManager` 等，以移除對 Coroutine 的依賴並統一非同步呼叫方式。
- 重構 Live2DActorKit 的 `Live2DBreathStateController`：改以 `UniTask` 搭配 `CancellationToken` 控制呼吸狀態切換。

### Fixed
- 修正 `WaitForUserInput` 指令可能「立即完成」而未真正等待的問題，避免在關閉自動前進時仍可能造成對話自動跳過。
- 修正 `ParticleNode` 的等待邏輯，確保會正確遵守 `WaitForInput` 設定。

## [1.0.5] - 2025-12-30
### Changed
- 更新版本號至 1.0.5，以反映最新的功能與修復。

## [0.1.0] - 2023-10-27
### Added
- Initial release of SG Dialogue System.
- Core node-based dialogue graph editor.
- Basic nodes: Text, Choice, Character Action, Camera Control, etc.
- Support for Spine and Live2D integrations.

