# Change Log

## [1.1.0] - 2026-01-07
### Added
- **除錯工具 (Debugging Tools)**:
  - 實作 `DialogueVariableDebugger` 編輯器視窗，支援 Runtime 即時檢視與修改全域/局部變數。
  - 實作 `Execution History` 追蹤，自動紀錄最近 50 個執行過的節點。
  - 在 Graph Editor 中即時高亮顯示走過的節點路徑 (Cyan Border)。
- **UI 與視覺呈現 (UI & Presentation)**:
  - 實作 `DialogueStyleProfile` 系統，支援自定義對話框背景、顏色、字體與文字大小。
  - `TextNode` 新增 `styleProfile` 欄位，支援根據不同對話自動切換 UI 樣式。
  - `DialogueUIManager` 支援捕捉與還原預設 UI 樣式。
- **編輯器生產力 (Editor Productivity)**:
  - 在 Graph Editor 中加入 `Groups` (群組) 與 `Sticky Notes` (便利貼) 功能，支援持久化存檔。
  - 優化群組刪除邏輯，支援「刪除群組但保留節點 (Ungroup)」功能。
- **文件與架構 (Documentation)**:
  - 建立 `docs/ARCHITECTURE.md` 詳細說明系統架構與資料流。
  - 建立 `docs/ADR.md` 紀錄關鍵技術決策。
  - 建立 `TODO.md` 追蹤開發進度與產品路線圖。

### Changed
- 優化 `DialogueGraphView` 的生命週期管理，確保在切換資產或停止 Play Mode 時正確清理視覺高亮。
- 調整 `TextNodeElement` 的 UI 配置，整合 Style Profile 選擇功能。

## [1.0.9] - 2026-01-06
### Added
- `StageTextNode` 新增 `Allow Fast Forward` 選項，允許開發者控制是否讓玩家透過點擊來快轉打字機效果。
- 完善 `StageTextNode` 相關單元測試 (Runtime Tests)。

### Fixed
- 修復 `StageTextNode` 在 `AutoAdvanceMode.ForceEnable` 模式下會被強制暫停等待輸入，導致無法自動推進的問題。

### Changed
- 優化 `DialogueController` 與 `VisualManager` 的打字機狀態同步邏輯，改為等待打字機狀態完成而非強制等待輸入信號。

## [1.0.8] - 2026-01-02
### Added
- TextNodeTranslateNode`，支援對話框的進場與退場動畫 (位移與淡入淡出)，可與 `StageTextNode` 配合使用。
- `CharacterActionNode` 新增 `ForceReplace` 選項，允許在同一位置強制銷毀舊角色並生成新角色。
- `DialogueUIManager` 新增 `dialoguePanel` 欄位，支援獨立控制對話框的動畫效果，避免影響全域 UI。

### Fixed
AnimationNode 和 PortraitManager，新增等待動畫完成的選項，優化動畫播放邏輯

### Changed
- `DialogueController` 將跳過請求事件的邏輯從結束對話更改為觸發事件，提供更靈活的控制。
- `PortraitManager` 優化角色進場邏輯，支援 `ForceReplace` 行為。
- 更新 README.md，擴充使用手冊內容，新增環境需求、安裝步驟及疑難排解章節。

## [1.0.7] - 2025-12-31
### Added
- CharacterActionNodeElement，新增 Sprite、Spine 和 SpriteSheet 預覽功能，並優化顯示邏輯。

### Fixed
- 修正 CharacterActionNode 進出場動畫漸入漸出上個節點與下節點的銜接問題。

### Changed
- DialogueController，將事件類型從 UnityEvent 更改為 UnityAction，並新增跳過請求事件


## [1.0.6] - 2025-12-31
### Fixed
- SequenceNode 修復了在非同步架構下無法正確進入子序列的問題。
- SequenceNode 修復了序列結束後無法正確返回上層節點的問題。

## [1.0.5] - 2025-12-30
### Changed
- 更新版本號至 1.0.5，以反映最新的功能與修復。

### Added
- 全面導入 UniTask 至對話系統核心邏輯，提升非同步控制與效能。
- 提升效能並優化等待邏輯

## [0.1.0] - 2023-10-27
### Added
- Initial release of SG Dialogue System.
- Core node-based dialogue graph editor.
- Basic nodes: Text, Choice, Character Action, Camera Control, etc.
- Support for Spine and Live2D integrations.
