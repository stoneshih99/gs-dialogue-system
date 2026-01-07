# Project Roadmap & To-Do List

此文件追蹤 **SG Dialogue System** 的開發進度、待辦事項與未來規劃。
請隨時更新此文件以保持 AI (與團隊) 的目標一致。

## 🚀 Current Sprint (本期衝刺)
> **Focus:** Debugging Tools & Developer Experience (除錯工具與開發者體驗)

- [x] **Variable Watcher (變數監視器)**: 
  - [x] 實作專用 Editor Window (`DialogueVariableDebugger`).
  - [x] 支援 Runtime 即時檢視 Global / Local 變數值.
  - [x] 允許 Runtime 手動修改變數以測試邏輯分支.
- [x] **Execution History (執行歷程視覺化)**:
  - [x] 記錄最近 50 個執行過的節點 (`DialogueController.ExecutionHistory`).
  - [x] 在 Graph Editor 中即時高亮顯示走過的節點 (Cyan Border).
  - [x] 確保 Stop Play Mode 時自動清除高亮狀態.

## 📅 Backlog (待辦清單)

### ✨ Features (新功能)
- [ ] **Visual Breakpoints (圖形化斷點)**: 允許在 Graph Node 上按右鍵設置斷點，Runner 執行到此時自動暫停。
- [ ] **In-Game Debug Overlay (實機除錯面板)**: 輕量級 Runtime UI，用於手機/Build 版本顯示當前節點與變數狀態。
- [ ] **進階邏輯節點**: 實作更多流程控制 (e.g., Random Selector, Loop, Sub-Graph).
- [ ] **編輯器 UX**: 改善 Graph Editor 操作體驗 (e.g., Minimap, 註解區塊, 搜尋節點).
- [ ] **多語言系統**: 強化 Localization 整合流程.

### 🐛 Bugs & Fixes (錯誤修復)
- [ ] 檢查 GraphView 在大量節點下的拖曳效能.
- [ ] 驗證 Undo/Redo 在複雜連線操作下的穩定性 (依據 AGENTS.md 規範).

### 🔧 Refactoring (重構與優化)
- [ ] **測試覆蓋率**: 補齊 `DialogueRunner` 與核心 Node 的自動化測試.
- [ ] **效能檢測**: 針對 Runtime 熱路徑 (Hot Path) 進行 Profiling，消除不必要的 GC.

## 📝 Documentation (文件)
- [x] 建立 `ARCHITECTURE.md` 說明系統資料流與架構設計.
- [x] 補充 `ADR.md` 記錄關鍵架構決策.

## 🛑 Known Issues (已知問題)
- GraphView 中連線 (Edge) 的變色功能因 API 限制暫時移除，目前僅支援節點變色顯示路徑。

---
*Last Updated: 2026-01-07*
