# Project Roadmap & To-Do List

此文件追蹤 **SG Dialogue System** 的開發進度、待辦事項與未來規劃。
請隨時更新此文件以保持 AI (與團隊) 的目標一致。

## 🚀 Current Sprint (本期衝刺)
> **Focus:** Narrative Features & UI Presentation (敘事功能與介面呈現)

- [x] **Variable Watcher (變數監視器)**: 
  - [x] 實作專用 Editor Window.
- [x] **Execution History (執行歷程視覺化)**:
  - [x] 在 Graph Editor 中即時高亮顯示走過的節點.
- [x] **Dynamic Dialogue Box (動態對話框)**: 
  - [x] 支援 `DialogueStyleProfile` 資產。
  - [x] 根據 `TextNode` 設定自動切換對話框背景、顏色與字體。
- [ ] **Rich Text Typewriter Tags (豐富打字機標籤)**:
  - [ ] 支援 `<wait=0.5>` (暫停打字).
  - [ ] 支援 `<speed=50>` (改變打字速度).
  - [ ] 支援 `<signal=EventName>` (打字途中觸發事件).

## 📅 Backlog (待辦清單)

### 🐛 Bugs & Fixes (錯誤修復)
- [ ] 檢查 GraphView 在大量節點下的拖曳效能.
- [ ] 驗證 Undo/Redo 在複雜連線操作下的穩定性 (依據 AGENTS.md 規範).
- [ ] **Known Issue**: GraphView 中連線 (Edge) 的變色功能因 API 限制暫時移除。

### 🔧 Refactoring (重構與優化)
- [ ] **測試覆蓋率**: 補齊核心 Node 的自動化測試.
- [ ] **效能檢測**: 針對 Runtime 熱路徑進行 Profiling.

### ✨ Features: Debugging (除錯功能)
- [ ] **Visual Breakpoints (圖形化斷點)**: 節點右鍵設置斷點，執行到此時自動暫停。
- [ ] **In-Game Debug Overlay (實機除錯面板)**: 輕量級 Runtime UI 顯示當前狀態。

### ✨ Features: Narrative & Polish (敘事與打磨)
- [ ] **Text Effects (文字特效)**: 新增 `<wave>`, `<rainbow>`, `<wiggle>` 等頂點動畫。
- [ ] **Voice Over Integration (語音整合)**: 支援節點綁定 AudioClip。

### ✨ Features: Logic & Flow (邏輯與流程)
- [ ] **Random Selector Node (隨機節點)**.
- [ ] **Visit Count Node (次數計數)**.
- [ ] **Sub-Graph (子圖)**.

### ✨ Features: UI & Presentation (介面與呈現)
- [ ] **World Space Bubbles (頭頂氣泡)**: 支援渲染在角色頭頂。

### ✨ Features: Editor Productivity (編輯器生產力)
- [x] **Groups & Sticky Notes (群組與註解)**: 支援基礎建立與存檔。
- [ ] **Search / Finder (搜尋功能)**: 快速定位包含特定文字或變數的節點。
- [ ] **CSV/Excel Import/Export**: 支援批次匯出/匯入文本。

## 📝 Documentation (文件)
- [x] 建立 `ARCHITECTURE.md` 說明系統資料流.
- [x] 補充 `ADR.md` 記錄關鍵架構決策.

---
*Last Updated: 2026-01-07*
