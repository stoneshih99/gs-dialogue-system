# 對話系統使用手冊

## 1. 總覽

### 系統目標
本系統旨在成為一個基於節點、時間軸式的、可擴充的對話編輯工具。其核心設計理念是讓敘事設計師能夠以最少的程式碼，創作出複雜、動態且具有電影感的對話演出。

### 核心概念
- **DialogueGraph (對話圖)**: 一個 `ScriptableObject` 資產，作用類似於「演出時間軸」。它包含一系列定義了對話流程與事件的節點。
- **DialogueController (對話控制器)**: 場景中的一個 `MonoBehaviour`，扮演「導演」的角色。它會讀取一個 `DialogueGraph` 並逐一執行其中的節點，同時協調各個管理器（UI、視覺、音訊）。
- **Nodes (節點)**: 每一個節點都是序列中的一個獨立、具體的動作（例如：顯示文字、讓角色登場、播放音樂）。透過連接這些節點，您可以創造出一場複雜的演出。

### 設計哲學
本系統建立在**單一職責原則 (Single Responsibility Principle)** 之上。每個節點只做一件事，並把它做好。這種模組化的方法帶來了以下好處：
- **清晰**: 對話圖變得易於閱讀和理解。任何人都能一眼看懂事件的發生順序。
- **靈活**: 複雜的序列是透過組合簡單的節點來完成的，這讓您能精準地控制事件的節奏和順序。
- **可擴充**: 新增功能（例如攝影機震動或新的動畫類型）就像建立一個新的節點類型一樣簡單，而無需修改現有的節點。

---

## 2. 快速入門：你的第一個對話

請依照以下步驟來建立並執行您的第一個對話序列。

### 步驟一：建立資產
1.  **建立對話圖**: 在 Project 視窗中，點擊右鍵並選擇 `Create > SG/Dialogue > Dialogue Graph`。將它命名為，例如 `CH1_Intro_Graph`。
2.  **建立全域狀態資產**: 點擊右鍵並選擇 `Create > SG/Dialogue > Dialogue State Asset`。將它命名為 `Global_GameState`。這個檔案將用來儲存跨對話的變數（例如：玩家聲望、任務旗標）。

### 步驟二：開啟編輯器
1.  從頂部選單，導航至 `SG/Dialogue > Graph + Localization Window`。
2.  這會開啟主要的對話編輯視窗。

### 步驟三：編排對話流
1.  **選取資產**:
    *   在「Graph」分頁中，找到「Graph」物件欄位，並將您的 `CH1_Intro_Graph.asset` 拖曳進去。
    *   找到「Global State」物件欄位，並將您的 `Global_GameState.asset` 拖曳進去。
2.  **建立節點**:
    *   在灰色的網格上點擊右鍵，開啟快捷選單。
    *   選擇「Add Character Action Node」。一個新的節點將會出現。
    *   選擇「Add Text Node」。
3.  **設定節點**:
    *   **CharacterActionNode**:
        *   將 `Action` 設為 `Enter`。
        *   將 `Target` 設為 `Center`。
        *   將 `Portrait Mode` 設為 `Sprite` 並指定一個角色圖片。
    *   **TextNode**:
        *   將 `Speaker` 設為「英雄」。
        *   將 `Text` 設為「哈囉，世界！」。
4.  **連接節點**:
    *   從 `CharacterActionNode` 的「Next」輸出口點擊並拖曳，連接到 `TextNode` 的「In」輸入口。一條線會將它們連起來，定義了流程。
5.  **設定開始節點**:
    *   在 `CH1_Intro_Graph.asset` 的 Inspector 中，找到 `Start Node Id` 欄位。
    *   輸入您第一個節點的 ID（例如，`CharacterActionNode` 的「A1」）。節點 ID 顯示在圖形編輯器中該節點的標題上。

### 步驟四：在場景中執行
1.  **設定控制器**:
    *   在您的場景中，建立一個空的 GameObject 並命名為 `DialogueSystem`。
    *   將 `DialogueController` 元件加入到這個物件上。
    *   將您的 `CH1_Intro_Graph.asset` 拖曳到 `DialogueController` 的 `Graph` 欄位。
    *   將您的 `Global_GameState.asset` 拖曳到 `Global State` 欄位。
    *   確保 `UI Manager`, `Visual Manager`, `Audio Manager` 等欄位都已連結到它們各自的元件。
2.  **觸發對話**:
    *   要開始對話，您需要在某個腳本中取得 `DialogueController` 的引用，並呼叫 `controller.StartDialogue()`。
    *   為了快速測試，您可以建立一個簡單的觸發腳本：
        ```csharp
        public class DialogueTrigger : MonoBehaviour
        {
            public DialogueController controller;
            void Start()
            {
                if (controller != null) controller.StartDialogue();
            }
        }
        ```

現在，當您執行遊戲時，您應該會看到您的角色登場並說出「哈囉，世界！」。恭喜！

---

## 3. 節點參考手冊

### Text Node (文字節點)
- **用途**: 顯示一行對話文字和說話者名稱。這是最核心的內容節點。
- **參數詳解**:
    - `Speaker`: 說話者的名稱。
    - `Text`: 對話內容。
    - `Terminal`: 若勾選，對話將在此節點後結束（除非 `nextNodeId` 已被設定）。
- **進階：文字動畫與特效 (Rich Text)**
    - **運作原理**: 本系統透過 `DialogueTextAnimator.cs` 元件來解析文字標籤，並實現即時的頂點動畫。
    - **如何設定**:
        1.  **確認**: 確保您的對話 UI 中，顯示文字的 `TextMeshProUGUI` 物件上，已經掛載了 `DialogueTextAnimator.cs` 腳本。
        2.  **提醒**: 如果沒有手動掛載，`DialogueUIManager` 會在執行時自動為您加上一個，但建議手動掛載以便進行潛在的參數調整。
    - **內建標籤範例**:
        - **抖動效果**: `<shake>這段文字會抖動。</shake>`
        - **未來擴充**: 開發者可以在 `DialogueTextAnimator.cs` 的 `Update` 方法中，輕鬆加入新的 `case` 來擴充更多自訂的文字特效（例如波浪 `<wave>`、彩虹 `<rainbow>` 等）。
- **進階：可打斷對話 (Interruptible Dialogue)**
    - **用途**: 讓此文字節點在顯示過程中，可以被特定的遊戲事件打斷，並跳轉到一個反應式分支。
    - **參數詳解**:
        - `Is Interruptible`: 若勾選，此節點將監聽打斷事件。
        - `Interrupt Event`: 指定一個 `GameEvent` 資產。當此事件被觸發時，對話將被打斷。
        - `Interrupt Next Node Id`: 當對話被打斷時，流程將跳轉到此節點 ID。
    - **運作方式**: 當對話執行到一個可打斷的 `TextNode` 時，`DialogueController` 會開始監聽指定的 `Interrupt Event`。如果事件在文字顯示完成前被觸發，對話將立即停止並跳轉到 `Interrupt Next Node Id` 所指向的分支。

### Choice Node (選項節點)
- **用途**: 向玩家呈現可互動的選項，並根據其選擇導向不同的對話分支。
- **參數詳解**:
    - `Choices`: 一個選項列表。每個選項包含：
        - `Text`: 顯示在按鈕上的文字。
        - `Condition`: (可選) 一個必須被滿足才能讓此選項出現的條件。
        - `nextNodeId`: 如果此選項被選中，要跳轉到的節點 ID。

### Character Action Node (角色動作節點)
- **用途**: 這是控制所有角色視覺行為的主要節點。
- **參數詳解**:
    - `Action`:
        - `Enter`: 讓角色登場，或改變指定位置上現有角色的外觀。
        - `Exit`: 讓角色退場。
    - `Target`: 動作發生的螢幕位置（左/中/右）。
    - `Clear All On Exit`: 若 `Action` 為 `Exit`，勾選此項將移除畫面上所有角色。
    - `Override Duration`: 使用自訂的持續時間來播放登場/退場動畫。
    - **Enter Action Visuals**: (當 `Action` 為 `Enter` 時可見)
        - `Enter SFX`: 角色登場時播放的音效。
        - `Portrait Mode`: 選擇 `Sprite`, `Spine`, 或 `Live2D` 模式。
        - 設定對應的視覺資產（Sprite 圖片、Spine Prefab 等）。
    - **Exit Action Visuals**: (當 `Action` 為 `Exit` 時可見)
        - `Exit SFX`: 角色退場時播放的音效。

### Set Background Node (背景設定節點)
- **用途**: 切換或清除對話的背景圖片。
- **參數詳解**:
    - `Background`: 要設定為新背景的 `Sprite` 圖片。
    - `Clear Background`: 若勾選，則將背景淡化為透明。
    - `Override Duration`: 使用自訂的持續時間來播放淡入淡出動畫。

### Play Audio Node (音訊播放節點)
- **用途**: 控制背景音樂 (BGM) 和一次性的音效 (SFX)。
- **參數詳解**:
    - `ActionType`:
        - `PlayBGM`: 將指定的 `AudioClip` 作為 BGM 淡入播放。
        - `StopBGM`: 將目前的 BGM 淡出停止。
        - `PlaySFX`: 將 `AudioClip` 作為一次性音效播放。
    - `AudioClip`: 要播放的聲音檔案。
    - `Loop BGM`: (僅 BGM) 音樂是否應該循環。
    - `Override Fade Duration`: (僅 BGM) 使用自訂的淡入淡出時間。

### Condition Node (條件節點)
- **用途**: 對話系統的「大腦」。根據遊戲變數的值，將流程導向「True」或「False」兩個不同的分支。
- **參數詳解**:
    - `Int Conditions`: 一個基於整數變數的條件列表。
    - `Bool Conditions`: 一個基於布林變數的條件列表。
    - **邏輯**: 所有列出的條件都必須被滿足，最終結果才為「True」。
    - **輸出端口**:
        - `True`: 如果所有條件都滿足，流程將從此端口繼續。
        - `False`: 如果有任何一個條件不滿足，流程將從此端口繼續。
    - **變數來源**: 節點會優先從**局部**對話狀態中查找變數，如果找不到，則會去**全域**狀態資產中查找。變數名稱會從您在「Graph」分頁中選擇的 `GlobalStateAsset` 自動生成一個方便的下拉選單。

### Camera Control Node (攝影機控制節點)
- **用途**: 透過控制對話攝影機，來增加電影般的演出效果。
- **參數詳解**:
    - `ActionType`: `Shake` (震動), `Zoom` (縮放), `Pan` (平移), 或 `FocusOnTarget` (聚焦目標)。
    - `Duration`: 攝影機動作的持續時間。
    - `Shake Intensity`: (用於 Shake) 震動的強度。
    - `Target Zoom`: (用於 Zoom) 目標的正交攝影機大小（數值越小，鏡頭越近）。
    - `Target Position`: (用於 Pan) 攝影機要移動到的世界空間目標位置。
    - `Focus Target`: (用於 Focus) 場景中一個讓攝影機移動過去的 `Transform` 目標。

### Game Event Node (遊戲事件節點)
- **用途**: 對話系統與遊戲世界溝通的「橋樑」，用於觸發外部的遊戲邏輯。
- **參數詳解**:
    - `Event`: 一個 `GameEvent` 型的 `ScriptableObject` 資產。
- **運作方式**: 當對話流程執行到此節點時，它會「廣播」指定的事件。場景中任何帶有 `GameEventListener` 元件並正在監聽此事件的物件，都會執行它們被設定好的回應（例如：開門、將物品加入背包、開始一場戰鬥）。

---

## 4. 進階主題與最佳實踐

### 演出時間軸概念
本系統的核心優勢在於將複雜的演出拆解為一系列簡單、獨立的步驟。請始終以「時間軸」的思維來編排您的對話。

**範例場景**: 「天黑了，緊張的音樂響起，反派從右側登場，說出他的台詞。」

**推薦的節點序列**:
1.  **`SetBackgroundNode`**: 設定 `Background Sprite` 為夜晚的場景，`Duration` 設為 1.5 秒，實現一個平滑的晝夜轉換。
    *   *連接到 ->*
2.  **`PlayAudioNode`**: 設定 `ActionType` 為 `PlayBGM`，並指定一個緊張的音樂 `AudioClip`。
    *   *連接到 ->*
3.  **`CharacterActionNode`**:
    *   `Action`: `Enter`
    *   `Target`: `Right`
    *   `Portrait Mode`: `Spine`
    *   `Spine Config`: 設定反派的 Prefab 和登場動畫。
    *   *連接到 ->*
4.  **`TextNode`**:
    *   `Speaker`: "反派"
    *   `Text`: "我等你好久了，英雄。"

這種作法讓每一個事件的發生時機都清晰可控，極易閱讀和修改。

### 全域變數 vs. 局部變數
- **全域變數 (`GlobalStateAsset`)**:
    - **用途**: 用於儲存需要**長期保留**、**跨場景**或**需要被存檔**的狀態。
    - **範例**: 玩家對某個陣營的好感度、是否完成了某個關鍵任務、玩家的名字。
    - **最佳實踐**: 在專案中建立一個或多個 `GlobalStateAsset` 來分類管理您的全域變數，例如 `PlayerStats`, `WorldFlags`。
- **局部變數 (Local Variables)**:
    - **用途**: 用於儲存**僅在當前對話中**有意義的臨時狀態。這些變數在對話結束後會被清除。
    - **範例**: 在一次盤問中，玩家連續選擇了三次「說謊」選項；在一次測驗中，玩家答對了幾道題。
    - **最佳實踐**: 當您在 `ConditionNode` 中需要一個變數，但這個變數在對話結束後就毫無用處時，請直接在節點的輸入框中手動輸入一個新的變數名稱。系統在全域變數中找不到它時，會自動將其作為局部變數處理。

### 事件驅動架構 (`GameEvent`)
`GameEventNode` 是將對話與遊戲玩法深度整合的關鍵。請盡可能地使用它，而不是在對話系統的程式碼中硬式編寫遊戲邏輯。
- **優點**: **解耦**。您的對話團隊可以專心編寫故事，而不需要知道程式設計師是如何實現「開門」或「給予道具」的。他們只需要在正確的時機，觸發正確的 `GameEvent` 即可。
- **範例**:
    - **觸發任務**: 在對話的結尾，使用 `GameEventNode` 觸發一個 `QuestStart_MainQuest_01` 事件。您的 `QuestManager` 正在監聽這個事件，並在收到後啟動對應的任務。
    - **給予獎勵**: 在玩家做出正確選擇後，觸發 `ReceiveItem_HealthPotion` 事件。您的 `InventoryManager` 監聽到後，會為玩家增加一瓶生命藥水。

### 圖表驗證
在完成一個複雜的對話圖後，特別是在刪除或修改了節點之後，請務必使用「Localization」分頁中的「**Validate Graph**」按鈕。這個「一鍵體檢」功能可以為您找出絕大多數潛在的執行期錯誤，例如懸空的連線或無法到達的「孤島」節點，為您省下大量的偵錯時間。

### 對話模擬器 (Dialogue Simulator)
- **用途**: 無需進入遊戲播放模式，直接在編輯器中快速預覽和偵錯您的對話流程。這能極大地加速開發和迭代效率。
- **如何使用**:
    1.  在「Graph」分頁中，確保您已經選取了要測試的 `DialogueGraph` 和 `GlobalStateAsset`。
    2.  切換到新的「**Simulator**」分頁。
    3.  點擊「**Start Simulation**」按鈕。
    4.  模擬器將會從 `DialogueGraph` 的 `Start Node` 開始執行。您會在面板上看到對話文字、說話者名稱和動態生成的選項按鈕。
    5.  點擊「**Next >**」按鈕或選擇選項，來推進對話流程。
    6.  您可以透過修改 `GlobalStateAsset` 中的變數，來測試 `ConditionNode` 的不同分支。
    7.  點擊「**Stop Simulation**」按鈕來結束模擬。
