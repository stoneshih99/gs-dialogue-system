# Architecture Decision Records (ADR)

本文件紀錄專案中的關鍵技術決策、考量因素以及當時的時空背景，這有助於後續維護者理解為何系統採用現有架構。

## 決策格式
每個紀錄應包含：
- **Status**: Proposed / Accepted / Superseded
- **Context**: 當時面臨什麼問題？
- **Decision**: 最終選擇了什麼方案？
- **Consequences**: 該決策帶來的優缺點。

---

## [ADR-001] 使用 Unity GraphView 作為編輯器基礎
- **Status**: Accepted
- **Context**: 需要一個視覺化、節點化的對話編輯器。
- **Decision**: 使用 Unity 實驗性的 `UnityEditor.Experimental.GraphView`。
- **Consequences**: 
    - **優點**: 深度整合 Unity UI 元件，支援 Zoom、拖動、框選等原生體驗。
    - **缺點**: API 標記為 Experimental，未來可能有變動風險；資料與視圖同步邏輯需自行實作。

## [ADR-002] 採用 Event Channel (ScriptableObject) 解耦
- **Status**: Accepted
- **Context**: 對話系統需要觸發音訊、粒子、UI 演出，但核心 Package 不應強耦合於特定場景物件。
- **Decision**: 使用 ScriptableObject 作為通訊管道，Node 發送 Request，Managers 訂閱事件。
- **Consequences**: 
    - **優點**: 極高擴充性、支援 Mock 測試、即使場景缺少 Manager 也不會報錯。
    - **缺點**: 需要管理多個 ScriptableObject 資產。

## [ADR-003] 使用 LitMotion 作為動畫引擎
- **Status**: Accepted
- **Context**: 系統需要高效、流暢且對 GC 友善的數值插值 (Tweening) 工具。
- **Decision**: 選用 LitMotion 代替傳統的 DOTween。
- **Consequences**: 
    - **優點**: 極致效能 (Zero GC)、基於 Struct 設計、原生支援 `UniTask`。
    - **缺點**: 與舊有 Unity 專案的整合需額外引入相依套件。

## [ADR-004] 嚴格分離 Runtime 與 Editor 程式碼
- **Status**: Accepted
- **Context**: 避免在建置遊戲 (Build) 時因為引用到 `UnityEditor` API 而失敗。
- **Decision**: 在 `AGENTS.md` 中明確規定「禁止 Runtime 引用 Editor API」，並透過 `asmdef` 強制隔離。
- **Consequences**: 
    - **優點**: 確保 Build 穩定性、架構層次分明。
    - **缺點**: 需要實作更多的資料傳輸層 (DTO) 來在 Editor 與 Runtime 間傳遞 UI 配置。

---
*Last Updated: 2026-01-07*
