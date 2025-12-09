# SG Dialogue System

![Unity](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

**SG Dialogue System** 是一個為 Unity 設計的、基於節點的、可擴充的對話系統。其核心設計理念是讓敘事設計師能夠以最少的程式碼，透過視覺化的方式創作出複雜、動態且具有電影感的對話演出。

本系統的核心是一個強大的 **Graph Editor (圖形編輯器)**，它將對話流程轉化為類似「演出時間軸」的概念。每一個節點都是一個獨立、具體的動作（如顯示文字、角色登場、播放動畫），讓您可以精準地控制演出的每一個細節。

---

## ✨ 核心功能 (Core Features)

*   **視覺化節點編輯器**: 提供強大直觀的圖形介面，透過拖拉和連線即可編排複雜的對話流與事件序列。
*   **時間軸式流程**: 將對話視為一場演出。每個節點都是時間軸上的一個動作，讓您能精準控制節奏。
*   **高度可擴充**: 採用單一職責原則，新增功能（如新的角色動作、鏡頭效果）就像建立一個新的節點類型一樣簡單。
*   **動態變數系統**:
    *   支援**全域變數** (跨場景保存) 和**局部變數** (僅限當前對話)。
    *   獨特的**資料提供者 (Data Provider)** 模式，可以從任何遊戲邏_輯（如玩家等級、金錢）動態讀取數值並顯示在對話中，實現完美解耦。
*   **事件驅動架構**: 透過 `GameEventNode`，可以在對話的任何時間點觸發遊戲中的任何事件（如開門、獲得道具），無需撰寫任何硬式編碼。
*   **內建開發者工具**:
    *   **對話模擬器 (Simulator)**: 無需進入 Play Mode，直接在編輯器中快速預覽和偵錯對話流程。
    *   **圖表驗證 (Validator)**: 一鍵找出懸空的連線或無法到達的「孤島」節點。
    *   **執行高亮 (Live Highlight)**: 在 Play Mode 中即時高亮顯示正在執行的節點，便於追蹤流程。
*   **第三方整合**:
    *   內建對 **Spine** 和 **Live2D** 的支援，只需定義 Scripting Define Symbol 即可啟用相關功能。

---

## 🚀 安裝 (Installation)

本系統透過 Unity Package Manager (UPM) 進行安裝。

**1. 安裝相依套件 (Dependencies)**

**重要:** 在安裝本系統前，您需要手動安裝以下所有相依套件。請打開 Unity 的 `Package Manager` 視窗，選擇 `Add package from git URL...`，然後依序加入以下 URL：

*   **LitMotion**: `https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion`
*   **LitMotion.Animation**: `https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion.Animation`
*   **Editor Toolbox**: `https://github.com/arimger/Unity-Editor-Toolbox.git#upm`
*   **Spine C# Runtime**: `https://github.com/EsotericSoftware/spine-runtimes.git?path=spine-csharp/src#4.2`
*   **Spine Unity Runtime**: `https://github.com/EsotericSoftware/spine-runtimes.git?path=spine-unity/Assets/Spine#4.2`
*   **NuGetForUnity**: `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity`

**2. 安裝對話系統 (Install Dialogue System)**

在所有相依套件都安裝完畢後，使用相同的方式加入本系統的 URL：

```
https://github.com/stoneshih99/gs-dialogue-system.git?path=Packages/com.sg.dialogue
```

---

## 📖 快速入門與文件

安裝完成後，您可以從 Package Manager 視窗匯入 **Basic Dialogue Example** 範例專案來快速了解系統的運作方式。

詳細的使用手冊、節點參考和進階教學，都包含在 package 本身之中。您可以在專案的 `Packages/SG Dialogue System` 資料夾中找到 `README.md` 文件。

或者，您可以直接在 GitHub 上瀏覽這份詳細文件：

[**>> 點此前往詳細使用手冊 <<**](Packages/com.sg.dialogue/README.md)

---

## 📄 授權 (License)

本專案採用 [MIT License](LICENSE.md) 授權。
