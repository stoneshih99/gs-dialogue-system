# 對話系統功能與更新文件 (2023-10-27)

## 總覽

本次更新為對話系統增加了多項重要的演出特效功能，並對編輯器後端架構進行了重大重構，以提高系統的擴展性和可維護性。主要更新包括：新增並行節點、多種視覺特效節點、打字機增強功能、角色高亮系統，以及對 `DialogueGraphView` 的完全重構。

---

## 1. 新增節點類型

### 1.1. 並行節點 (Parallel Node)

**功能**：
此節點允許同時執行多個獨立的對話分支，並在所有分支都執行完畢後，再匯合到一個點繼續主流程。適用於同時觸發多個角色的獨立行為、或同時進行多個無關的事件。

**如何使用**：
1.  在 GraphView 中右鍵，選擇 "Add Parallel Node"。
2.  節點上會有一個 `+ Branch` 按鈕，點擊可新增一個並行分支的輸出埠。
3.  從每個 "Branch" 輸出埠拉出連線，各自代表一個並行任務的起點。
4.  從固定的 "Next (After Converge)" 輸出埠拉出連線，定義所有分支都結束後要執行的下一個節點。

**技術實現**：
-   `ParallelNode.cs`: 數據模型，包含一個 `outputNodeIds` 列表和一個 `nextNodeId`。
-   `DialogueController.cs`: 新增了 `ExecuteBranch(string startNodeId)` 協程，它能像一個迷你控制器一樣，獨立執行一個分支直到結束。`ParallelNode` 的 `Process` 方法會為每個分支啟動這個協程，並使用 `yield return` 等待它們全部完成。

---

### 1.2. 等待節點 (Wait Node)

**功能**：
在繼續執行下一個節點之前，暫停指定的秒數。用於控制對話節奏，例如在角色做出動作後、或在顯示重要資訊前製造停頓。

**如何使用**：
1.  在 GraphView 中右鍵，選擇 "Add Wait Node"。
2.  在節點的 "Wait Time (s)" 欄位中輸入要等待的秒數。

**技術實現**：
-   `WaitNode.cs`: 數據模型，其 `Process` 方法會 `yield return new WaitForSeconds(WaitTime);`。

---

### 1.3. 螢幕特效節點 (Screen Effect Nodes)

我們新增了一套基於 UGUI Shader 的螢幕特效系統，通過控制一個覆蓋全螢幕的 `Image` 來實現，**效能優於 Post-Processing**。

**重要：必要的前置設定**
1.  **建立 Shader 和材質**：
    -   建立一個 `UI/Grayscale` Shader 和對應的 `UIGrayscale_Mat` 材質。
    -   建立一個 `UI/Blur` Shader 和對應的 `UIBlur_Mat` 材質。
2.  **設定場景 UI**：
    -   在對話 Canvas 中，建立三個全螢幕的 `Image` 物件，並將它們的 Alpha 設為 0。
    -   `GrayscaleMask`: 用於灰階效果，使用 `UIGrayscale_Mat` 材質。
    -   `FlashMask`: 用於閃爍效果，使用預設 UI 材質即可。
    -   `BackgroundBlurImage`: 用於背景模糊，**它必須是您主要背景 Image 的副本或兄弟物件，並使用 `UIBlur_Mat` 材質**。
3.  **設定控制器**：
    -   將 `ScreenEffectController.cs` 掛載到場景中的管理器物件上。
    -   將上述三個 `Image` 物件拖曳到 `ScreenEffectController` 對應的欄位中。

#### 1.3.1. 灰階節點 (Screen Effect Node)

**功能**：
啟用或禁用全螢幕灰階效果，常用於回憶、夢境或特殊情境。

**如何使用**：
-   **Action**: `Enable` (啟用) 或 `Disable` (禁用)。
-   **Duration**: 效果淡入/淡出的持續時間。

#### 1.3.2. 畫面閃爍節點 (Flash Effect Node)

**功能**：
讓整個畫面快速閃爍一次指定的顏色，用於強調重擊、驚嚇或爆炸。

**如何使用**：
-   **Flash Color**: 閃爍的顏色。
-   **Duration**: 整個閃爍（淡入+淡出）的持續時間。
-   **Intensity**: 閃爍的最高亮度 (Alpha 值)。

#### 1.3.3. 背景模糊節點 (Blur Effect Node)

**功能**：
啟用或禁用背景的模糊效果，用於內心獨白或強調前景。

**如何使用**：
-   **Action**: `Enable` (啟用) 或 `Disable` (禁用)。
-   **Duration**: 效果淡入/淡出的持續時間。
-   **Blur Amount**: 模糊的程度，僅在 `Enable` 時有效。

#### 1.3.4. 閃爍特效節點 (Flicker Effect Node)

**功能**：
讓指定的背景圖層或角色產生閃爍效果。

**如何使用**：
-   **Target**: 選擇目標是 `Background` 還是 `Character`。
-   **Background Layer Index**: 如果目標是背景，指定要閃爍的圖層索引。
-   **Character Position**: 如果目標是角色，指定要閃爍的角色位置。
-   **Duration**: 閃爍的總持續時間。
-   **Frequency**: 閃爍的頻率（每秒次數）。
-   **Min Alpha**: 閃爍時的最低透明度。

---

## 2. 核心功能與視覺增強

### 2.1. 角色高亮系統

**功能**：
當一個角色說話時，該角色的立繪會保持正常顏色（高亮），而場景中的其他角色立繪會自動變為灰階。當進入旁白（說話者名稱為空）時，所有角色都會恢復正常顏色。

**如何使用**：
-   此功能為**自動觸發**。
-   **必要條件**：在使用 `Character Action Node` 讓角色進場時，必須在 "Speaker Name" 欄位中填寫與該角色後續對話時 `TextNode` 中 "Speaker" 欄位**完全一致**的名稱。系統通過這個名稱來匹配角色。

**技術實現**：
-   `IDialoguePortraitPresenter` 新增了 `SetHighlight(bool)` 和 `Flicker(...)` 方法。
-   所有 Presenter (Image, Spine, SpineUI) 都實現了這些方法，通過修改 `Color` 或 `Skeleton.A` 來達成效果。
-   `DialogueVisualManager` 現在會儲存每個活躍角色的 `SpeakerName` 和 `Presenter` 實例，並在 `UpdateFromTextNode` 中根據當前說話者是誰，來更新所有角色的高亮狀態。

### 2.2. 打字機效果增強

#### 2.2.1. 支援 Rich Text
-   **功能**：現在可以在對話文本中自由使用 TextMeshPro 的 Rich Text 標籤，例如 `<b>`, `<i>`, `<color=red>` 等，打字機效果會正確顯示它們而不會將其破壞。

#### 2.2.2. 速度變化
-   **功能**：可以在一段文本中動態改變打字機的速度。
-   **如何使用**：使用 `<speed=X>` 和 `</speed>` 標籤來包裹需要變速的文本。`X` 是一個速度倍率，例如 `<speed=2>` 表示兩倍速，`<speed=0.5>` 表示半速。
-   **範例**：`這是一段正常速度的文本...<speed=2>這部分會變快！</speed>...然後恢復正常速度。`

#### 2.2.3. 間隔音效
-   **功能**：可以設定每顯示 N 個有效字元（非空白、非標點）後播放一次打字音效。
-   **如何使用**：在 `DialogueUIManager` 的 Inspector 中設定 `Typewriter Sound` 區塊的參數：
    -   `Enable Typewriter Sound`: 啟用此功能。
    -   `Sound Interval`: 設定間隔字元數 N。
    -   `Typewriter Audio Event`: 指定觸發音效的 `AudioEvent`。
    -   `Typewriter Sfx`: 指定要播放的 `AudioClip`。

---

## 3. 架構重構與編輯器優化

### 3.1. DialogueGraphView 重構 (Handler/Registry 模式)

**背景**：
隨著節點類型不斷增加，`DialogueGraphView.cs` 變得越來越臃腫，違反了「開閉原則」。每次新增節點都需要修改 `DialogueGraphView` 的四個地方。

**重構方案**：
1.  **`INodeHandler` 介面**：定義了所有節點處理器（Handler）的標準，包括如何創建節點數據、創建視覺元素、定義菜單名稱等。
2.  **`NodeHandlerRegistry` 註冊表**：一個靜態類別，在編輯器啟動時通過反射自動查找並註冊所有 `INodeHandler` 的實現。
3.  **`DialogueGraphView` 簡化**：`DialogueGraphView` 不再包含任何關於具體節點類型的 `if/else if` 判斷。它現在的所有操作（如創建節點、連接埠）都委託給從 `NodeHandlerRegistry` 中查詢到的對應 Handler 來執行。

**成果**：
現在要新增一個節點，只需要專注於實現該節點自身的**數據模型 (.cs)**、**視覺元素 (.cs)** 和**處理器 (.cs)**，**完全不需要再修改 `DialogueGraphView.cs`**，極大地提升了系統的擴展性和可維護性。

### 3.2. 編輯器 UI/UX 優化

-   **移除 `isTerminal`**：從 `TextNode` 中徹底移除了多餘的 `isTerminal` 欄位。現在，對話是否結束完全由 `nextNodeId` 是否為空來決定，邏輯更統一，也簡化了編輯器界面。
-   **節點寬度限制**：`TextNode` 在編輯器中的文本輸入框被設定了最大寬度，避免因文字過長導致節點無限變寬，使圖表更整潔。
-   **動態欄位顯示**：`FlickerEffectNode` 現在會根據選擇的 `Target`（背景或角色）動態顯示或隱藏相關的設定欄位，使界面更簡潔。
-   **上下文菜單優化**：在 `ParallelNode` 內部編輯時，右鍵菜單會智能地提供 `BranchStartNode` 選項，而在其他地方則會隱藏它。

### 3.3. 錯誤修復

-   修復了在 `SequenceNode` 和 `ParallelNode` 內部新增的節點，在重新進入後會消失的序列化問題。
-   修復了多個因在協程中使用 `ref` 參數導致的編譯錯誤。
-   修復了 `SpineUiDialoguePortraitPresenter` 中因大小寫錯誤 (`skeleton` vs `Skeleton`) 導致的 Bug。
-   修復了 `DialogueSimulatorEngine` 中對 `isTerminal` 的殘留依賴。
-   修復了 `DialogueUIManager` 中因缺少 `using` 引用導致的編譯錯誤。