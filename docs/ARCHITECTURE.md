# Architecture & Design (架構設計)

本文件說明 **SG Dialogue System** 的核心架構、資料流與設計模式。

## 1. 核心元件關係
系統主要由三個層級組成：

### A. Data Layer (資料層)
- **DialogueGraph (ScriptableObject)**: 儲存節點與連線關係的資產。
- **DialogueNodeBase (Serializable)**: 所有節點的基底類別。
- **DialogueStateAsset**: 儲存全域變數與遊戲狀態。

### B. Execution Layer (執行層)
- **DialogueRunner / Controller**: 負責解讀圖資料，並驅動流程前進。
- **Node Handler / Resolver**: 負責具體執行每個節點類型的 logic。

### C. Presentation & Integration Layer (呈現與整合層)
- **Event Channels (ScriptableObject)**: 用於發送音訊、粒子、遊戲事件的通訊管道。
- **UI View / Managers**: 訂閱 Event Channels 並執行具體的視覺演出。

---

## 2. 資料流 (Data Flow)
1. **Editor 階段**: 使用者在 Graph Window 編輯節點 -> 資料序列化至 `DialogueGraph`。
2. **啟動階段**: `DialogueController` 載入 `DialogueGraph` 並初始化 `DialogueRunner`。
3. **執行階段**: 
   - Runner 找到 Start Node。
   - 逐一執行 Node -> 觸發相關的 **Event Channels**。
   - 若節點需要等待（如 Text Node），Runner 進入暫停狀態，直到收到「繼續」訊號。
4. **外部反應**: 場景中的 Managers (如 Audio Manager) 監聽到 Channel 事件 -> 執行對應動作。

---

## 3. 關鍵技術細節

### 3.1 解耦設計：Event Channels
為了確保對話系統不直接依賴於場景中的特定物件，我們使用 ScriptableObject 作為「廣播站」。
- **優點**: 即使場景中沒有音訊管理器，對話系統也能正常執行（只是沒聲音），不會報錯。

### 3.2 變數解析與數據映射
- **Local Variables**: 僅存於單次對話流程的 Dictionary。
- **Global Variables**: 存於 `DialogueStateAsset`。
- **External Data**: 透過 `IVariableDataProvider` 介面動態讀取遊戲內資料（如 PlayerHP）。

### 3.3 Editor Undo 系統
所有資料變更必須透過 Unity 的 `Undo` 系統記錄，確保編輯器體驗穩定。
- 變更流程：`RecordObject` -> `Modify Data` -> `SetDirty`。

---

## 4. 擴充建議 (Extensibility)
- **新增節點**: 繼承 `DialogueNodeBase` 並建立對應的 `NodeView` 與執行邏輯。
- **自定義效果**: 透過 `GameEventNode` 與自定義的 Event Channel 擴充，不需修改核心代碼。

---
*Last Updated: 2026-01-07*
