# SG Dialogue System - 使用手冊

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
        *   將 `Action Type` 設為 `Enter`。
        *   將 `Position` 設為 `Center`。
        *   將 `Render Mode` 設為 `Sprite` 並指定一個角色圖片。
    *   **TextNode**:
        *   將 `Speaker Name` 設為「英雄」。
        *   將 `Text` 設為「哈囉，世界！」。
4.  **連接節點**:
    *   從 `CharacterActionNode` 的「Next」輸出口點擊並拖曳，連接到 `TextNode` 的「In」輸入口。一條線會將它們連起來，定義了流程。
5.  **設定開始節點**:
    *   在 `CharacterActionNode` 上點擊右鍵，選擇 `Set as Start Node`。該節點的邊框會變為綠色，表示它是對話的入口。

### 步驟四：在場景中執行
1.  **設定控制器**:
    *   在您的場景中，建立一個空的 GameObject 並命名為 `DialogueSystem`。
    *   將 `DialogueController` 元件加入到這個物件上。
    *   將您的 `CH1_Intro_Graph.asset` 拖曳到 `DialogueController` 的 `Graph` 欄位。
    *   將您的 `Global_GameState.asset` 拖曳到 `Global State` 欄位。
    *   確保 `UI Manager`, `Visual Manager` 等欄位都已連結到它們各自的元件。
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

(此處節點說明與前一版本相同，為求簡潔省略)

---

## 4. 進階主題與最佳實踐

### 演出時間軸概念
本系統的核心優勢在於將複雜的演出拆解為一系列簡單、獨立的步驟。請始終以「時間軸」的思維來編排您的對話。

### 事件驅動架構 (Event-Driven Architecture)
`GameEventNode` 和 `PlayAudioNode` 是將對話與遊戲玩法深度整合的關鍵。請盡可能地使用它們，而不是在對話系統的程式碼中硬式編寫遊戲邏輯。
- **優點**: **解耦**。您的對話團隊可以專心編寫故事，而不需要知道程式設計師是如何實現「開門」或「播放特定音樂」的。他們只需要在正確的時機，觸發正確的 `EventName` 即可。
- **如何設定**:
    1. 在場景中建立一個 GameObject，並掛載 `GameEventManager` 或 `DialogueAudioEventManager`。
    2. 在對應的 Manager 上，設定 `Event Channel`。
    3. 在 `Event Mappings` 列表中，新增一個項目，填寫 `Event Name` (例如 `PlayerGetItem`)，並將希望觸發的 `UnityEvent` (例如 `Inventory.AddItem`) 拖曳到 `Response` 欄位中。
    4. 在對話圖中，使用 `GameEventNode` 或 `PlayAudioNode`，並在其 `Event Name` 欄位中輸入您剛剛設定的名稱。

### 變數系統

#### 全域變數 vs. 局部變數
- **全域變數 (`GlobalStateAsset`)**:
    - **用途**: 用於儲存需要**長期保留**、**跨場景**或**需要被存檔**的狀態。
    - **範例**: 玩家對某個陣營的好感度、是否完成了某個關鍵任務。
- **局部變數 (Local Variables)**:
    - **用途**: 用於儲存**僅在當前對話中**有意義的臨時狀態。這些變數在對話結束後會被清除。
    - **範例**: 在一次盤問中，玩家連續選擇了三次「說謊」選項。
- **使用方法**: 在任何節點的文字欄位中，使用 `{變數名}` 的格式來引用變數。在 `ConditionNode` 中，直接輸入變數名稱即可。

#### 外部變數解析：資料提供者模式
有時，您需要在對話中顯示不儲存在對話系統內部（`GlobalStateAsset` 或局部變數）的資料，例如玩家名稱、等級或金錢數量。本系統提供了一個基於「資料提供者模式」的框架，讓任何執行時的物件都能向對話系統提供動態變數。

- **運作原理**:
    1.  **`IVariableDataProvider` 介面**: 一個簡單的合約，任何想提供資料的類別都可以實現它。
    2.  **資料提供者 (Provider)**: 一個實現了該介面的 `MonoBehaviour`。它內部維護一個 `key -> value` 的字典，並在啟動時向解析器註冊自己。
    3.  **變數解析器 (Resolver)**: 一個中央樞紐 (`PlayerVariableResolver`)，負責管理所有註冊的資料提供者。當對話系統請求一個變數時，它會廣播這個請求給所有提供者，直到有一個能回應為止。

- **如何實現**:

    **步驟 1: 設定場景**
    1.  在您的 `DialogueSystem` 物件上，確保掛載了 `DialogueController` 和 `PlayerVariableResolver` 這兩個元件。
    2.  在場景的其他地方（例如，一個名為 `GameLogic` 的物件上），掛載 `RuntimeDataProvider` 元件。
    3.  在 `RuntimeDataProvider` 的 Inspector 中，將 `PlayerVariableResolver` 物件拖曳到 `Resolver` 欄位中。

    **步驟 2: 擴充您的資料提供者**
    打開 `RuntimeDataProvider.cs` 腳本。在 `Awake()` 方法中，您可以自由地新增、修改或刪除任何您想提供的變數。這是擴充自訂變數的**唯一需要修改的地方**。
    ```csharp
    // RuntimeDataProvider.cs
    private void Awake()
    {
        // ...
        // Key 是在對話中使用的名稱，Value 是一個返回目前值的函式。
        _dataMappings["PlayerName"] = () => PlayerProfile.PlayerName;
        _dataMappings["PlayerLevel"] = () => YourGameManager.Instance.PlayerLevel.ToString();
        _dataMappings["Gold"] = () => YourGameManager.Instance.Gold.ToString();
        _dataMappings["GuildName"] = () => YourGameManager.Instance.Guild.Name;
        // ...
    }
    ```

    **步驟 3: 在對話中使用**
    現在，您可以在任何對話文字中自由地使用 `{PlayerName}`、`{PlayerLevel}`、`{Gold}` 等您剛剛定義的變數。

- **優點**: 這個設計將對話系統與您的遊戲邏輯**完全解耦**。對話系統不需要知道 `PlayerProfile` 或 `YourGameManager` 的存在，它只與實現了 `IVariableDataProvider` 介面的物件溝通。這使得系統非常乾淨、模組化且易於擴展。

### 圖表驗證與執行高亮
- **圖表驗證**: 在完成一個複雜的對話圖後，請務必使用「Localization」分頁中的「**Validate Graph**」按鈕。這個「一鍵體檢」功能可以為您找出懸空的連線或無法到達的「孤島」節點。
- **執行高亮**: 當您在 Unity 編輯器中直接執行遊戲時，對話圖編輯器會即時高亮目前正在執行的節點。這對於追蹤和除錯複雜的對話流程非常有幫助。

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
