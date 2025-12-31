# Change Log


## [1.0.7] - 2025-12-31
### Added
- CharacterActionNodeElement，新增 Sprite、Spine 和 SpriteSheet 預覽功能，並優化顯示邏輯。

### Fixed
- 修正 CharacterActionNode 進出場動畫漸入漸出上個節點與下節點的銜接問題。

### Changed
-DialogueController，將事件類型從 UnityEvent 更改為 UnityAction，並新增跳過請求事件


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

