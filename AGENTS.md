# agents.md — 對話系統（嚴格版）

## 權威聲明
本文件為**硬性契約（Hard Contract）**。  
所有 AI Agent **必須嚴格遵守**以下規則。  
任何違反行為皆視為錯誤實作。

> 適用範圍：本 Repo 的對話系統套件 `Packages/com.sg.dialogue/`（Runtime / Editor / Tests）。
> 若使用者需求與本契約衝突，以本契約為準（除非使用者明確要求修改契約本身）。

---

## 1. 專案身分（不可協商）

- 本儲存庫為 Unity 對話系統套件。
- 套件路徑：`Packages/com.sg.dialogue/`
- 此為**可重用系統套件**，非單一遊戲專用邏輯。
- Runtime 與 Editor 層級**必須嚴格分離**。

AI **不得**將本專案視為：
- 通用 Unity 專案
- UI 系統
- 自由敘事腳本沙盒

---

## 2. 核心架構鐵律（不可破壞）

### 2.1 僅限資料驅動（Data-Driven Only）

- 所有對話邏輯皆定義於 `DialogueGraph`（ScriptableObject）。
- Runtime 行為必須由圖資料推導。
- Node 僅能作為**資料模型**，不得承載行為。

**禁止：**
- 在 Node 類別中嵌入遊戲邏輯
- 在圖模型之外硬編流程邏輯

> 允許：在 Runner / Handler / 系統層提供「可被圖資料驅動」的通用行為（例如：事件派發、條件判斷、變數讀寫）。

---

### 2.2 Runtime / Editor 分離（硬邊界）

**允許：**
- Editor → Runtime 依賴

**絕對禁止：**
- Runtime → Editor 依賴

Runtime **不得**引用：
- `UnityEditor.*`
- `UnityEditor.Experimental.GraphView` / `GraphView`
- 任何 Editor-only API

違反即視為 **Build 失效等級錯誤**。

---

## 3. 序列化即契約（不可輕動）

### 3.1 序列化欄位 = 存檔格式

以下皆視為**持久化資料格式**：
- `DialogueGraph`
- 所有 `DialogueNodeBase` 子類
- 其內所有序列化欄位

**未經說明禁止：**
- 重新命名序列化欄位
- 變更欄位型別
- 移除欄位
- 改變欄位語意

**除非同時提供：**
- 明確的 migration 策略（例如：Editor migration 工具 / OnValidate 修補 / 版本號升級流程）
- 向後相容考量
- 影響範圍說明

**優先選項：**
- 僅新增欄位

---

### 3.2 欄位改名規則

若改名無法避免：
- 必須使用 `FormerlySerializedAs`
- 必須清楚說明資料遷移行為

---

## 4. 擴充規則（新增節點，不動核心）

### 4.1 新增 Node 類型

**正確做法：**
- 新增 `DialogueNodeBase` 子類
- 僅放置資料
- Editor 行為透過 `NodeHandlerRegistry` 註冊

**嚴格禁止：**
- 修改既有核心 Node 邏輯（除非修 bug，且需說明相容性與風險）
- 使用反射掃描節點
- Runtime 動態發現 Node 類型

---

### 4.2 Node Handler 註冊

- 註冊必須明確（顯式 Register）
- 使用靜態初始化（例如 `InitializeOnLoad`）
- 禁止 Runtime 反射掃描

若提議使用反射，**AI 必須停止並拒絕執行**。

> 定義：本文中的「反射掃描」指執行期遍歷 Assembly / Type 列表來找 Node/Handler（例：`AppDomain.CurrentDomain.GetAssemblies()`、`TypeCache.GetTypesDerivedFrom` 用在 Runtime、或任何等效手法）。

---

## 5. 效能限制（嚴格）

Runtime 執行流程 **必須**：
- 避免在熱路徑使用 LINQ
- 避免在執行流程中使用反射
- 避免每步產生 GC
- 禁止在迴圈中做字串拼接

**允許：**
- 在 Graph 初始化時建立快取
- O(1) Dictionary 查找

### 5.1 允許建立快取的時機（明確限定）

快取建立只允許發生在「初始化」階段，例如：
- Runner 啟動 / 綁定 graph 時（例如 `Initialize` / `StartDialogue` 的準備階段）
- Graph 資料載入完成後的一次性建置（例如明確的 `BuildCache()`）

#### 5.1.1 初始化階段的硬定義（不可延伸解釋）

本文件中的「初始化階段」定義為：
- **對話尚未進入第一個節點的執行**之前
- 且該段流程**只會執行一次**（同一段對話流程中不得重複）

任何會在下列情境反覆觸發的邏輯，**一律不屬於初始化**：
- 每步推進（step）
- 每幀（frame）
- 每次 Evaluate / Query / Resolve

**嚴格禁止：**
- 以「第一次呼叫也算初始化」為理由，在熱路徑內建立或重建快取

**嚴格禁止：**
- 在 Runner 每步推進（step / tick）或 Evaluate 之類高頻呼叫中重建快取

---

## 6. Editor 規範（Undo 必須）

所有 Editor 資料變更 **必須依序執行**：
1. `Undo.RecordObject`（或等效 API）
2. 修改資料
3. `EditorUtility.SetDirty`

少一步即為 Bug。

### 6.1 Undo 等效 API（允許但不得降低語意）

以下屬於允許的 Undo 等效方式（視情境使用）：
- `Undo.RecordObject`
- `Undo.RegisterCompleteObjectUndo`
- `Undo.RegisterFullObjectHierarchyUndo`（需非常謹慎，避免記錄過大）
- `Undo.SetCurrentGroupName` / `Undo.IncrementCurrentGroup`（用於合併多步操作）

**不論使用哪種 API，必須滿足相同語意：**
- 使用者可 Undo/Redo 到正確狀態
- 資料資產會被標記 Dirty 並可被保存

GraphView 僅為呈現層。  
`DialogueGraph` 為唯一真實來源（Single Source of Truth）。

### 6.2 Undo Group 規則（多步操作必須）

凡是一次使用者操作會導致多個資料變更（例如：新增節點同時新增連線、刪除節點同時清理 edges、Paste 多個元素、Auto layout、批次 rename），**必須**：
- 使用同一個 Undo group 包裝整個操作
- 並且該 group 的最後狀態必須可完整 Undo/Redo 回到一致狀態

不允許出現：
- Undo 只回到「半套狀態」
- GraphView 與 `DialogueGraph` 資料不同步

> 允許：使用 `Undo.SetCurrentGroupName` / `Undo.IncrementCurrentGroup` 或等效方式合併多步操作。

---

## 7. Runner 邊界

`DialogueRunner`：
- 解讀圖資料
- 對外發送事件或呼叫介面

UI / 動畫 / 音效：
- **不得**修改圖資料
- **不得**直接控制執行狀態

溝通方式 **僅限**：
- 事件
- 介面

---

## 8. 變更紀律（AI 行為規範）

AI **必須**：
1. 提出最小可行變更
2. 明確說明：
    - Runtime 行為影響
    - 序列化風險（是 / 否）
    - 是否為破壞性變更
3. 未經要求 **不得進行大型重構**

若需求不明確：
- 採取保守、不破壞的路徑
- 清楚標註假設前提

---

## 9. 回答前檢查清單（強制）

在產出任何解法前，**必須確認**：

- [ ] Runtime 未引用 Editor API
- [ ] 無序列化欄位被改名或移除（除非提供 migration）
- [ ] Runtime 未新增反射掃描
- [ ] 未引入 GC-heavy 邏輯（LINQ/Alloc/字串拼接等）
- [ ] Editor 變更有完整 Undo / Dirty
- [ ] 套件可獨立搬移使用

若任一項不符：
- **立即停止**
- 說明風險
- 提出更安全替代方案

---

## 9.1 最低測試要求（強制）

當提交以下類型變更時，**必須**提供至少一個對應的自動化測試（Unity Test Framework）：

- **新增 Node 類型**：至少一個 EditMode test，驗證：
  - Node 可被建立並序列化（寫入/讀出不丟資料）
  - Node 與 Graph 的連線資料可保存（至少覆蓋一條 edge/連線）
- **變更 Runner 行為**：至少一個 PlayMode test 或 EditMode test（依 Runner 可否在 EditMode 執行而定），覆蓋：
  - 基本 step 推進（至少 3 個節點流程）

若該類測試在專案結構中尚未建立：
- AI 必須先建立最小測試骨架（Tests 資料夾、asmdef 或 test assembly 設定）
- 再進行功能變更

---

## 10. 絕對禁止事項

AI **絕不可**：
- 重寫核心架構
- 混合 Runtime 與 Editor 邏輯
- 隨意變更圖資料語意
- 引入「聰明但不穩定」的抽象
- 為了可讀性犧牲穩定性

---

## 11. 需求衝突處理（強制）

當使用者請求可能導致違反本契約（例如：要求在 Runtime 使用 Editor API、要求用反射掃描、要求隨意改序列化欄位）時：
1. AI 必須明確指出「違反哪一條」
2. AI 必須提出至少一個不違規的替代方案
3. 若無法在不違規前提下完成，AI 必須拒絕該做法並說明原因

---

agents.md 結束
