# Live2DActorKit （Runtime 封裝）

Live2DActorKit 是一組幫你在 Unity 裡更輕鬆控制 Live2D 角色的 Runtime 腳本封裝。  
它站在 **Live2D Cubism SDK for Unity** 之上，提供：

- 🎬 統一的「角色狀態」控制（動作 + 表情 + 呼吸）
- 🎤 自動語音嘴型同步（Lip Sync）
- 🫁 參數驅動的自然呼吸系統
- 👁️ 眼睛看向（LookAt），支援 Canvas / 3D 世界兩種模式
- 🎧 多角色語音管理（誰在講話、嘴型誰動）

> 本套件不包含 Cubism SDK，本身不處理匯入 .model3.json / .motion3.json，  
> 你需要先照官方流程把 Live2D 模型匯入 Unity 並能正常播放動作。

---

## 1. 相依性與支援環境

- Unity 2021 LTS 以上版本（建議）
- 已匯入 **Live2D Cubism SDK for Unity 4.x**（Core + Framework）
- Render Pipeline：Built-in / URP 皆可（本套件只用到 Script，不碰 Shader）

---

## 2. 資料夾結構（放到 Assets 下）

將整個 `Live2DActorKit` 資料夾放到專案的 `Assets/` 裡，結構如下：

```text
Assets/
  Live2DActorKit/
    Runtime/
      Core/
        ILive2DActor.cs                 # 高階角色介面
      Actors/
        Live2DActor.cs                  # 封裝 Cubism：Motion / Expression / LookAt / Breath / Voice
        Live2DActorStateController.cs   # 角色狀態機（Happy / Angry / Sleep...）
      Breath/
        Live2DBreathController.cs       # 呼吸參數波動
        Live2DBreathStateController.cs  # 呼吸節奏狀態（Idle / Nervous / Sleepy）
      Audio/
        Live2DLipSyncController.cs      # 語音音量 → 嘴型參數
        Live2DVoiceManager.cs           # 多角色語音管理
      Init/
        Live2DSpeakerBootstrap.cs       # 角色註冊到 VoiceManager
      Live2DActorKit.Runtime.asmdef     # 選用：若你不熟 asmdef，可直接刪掉
    Samples/
      Scripts/
        DialogueDemoRunner.cs           # 簡易對話示範（可選）
    README.md                           # 你現在看到的說明
```

> 如果你對 asmdef 不熟、或出現「找不到 Cubism 類型」的錯，  
> 可以 **直接刪掉 `Live2DActorKit.Runtime.asmdef`**，  
> 讓這些腳本編到 Unity 預設 Assembly 裡，最省事。

---

## 3. 各腳本職責總表

| 腳本 | 角色 | 功能重點 |
|------|------|----------|
| `ILive2DActor` | 介面 | 上層系統只依賴這個，不碰 Cubism 直接 API |
| `Live2DActor` | 角色核心 | PlayMotion / SetExpression / LookAt / 呼吸 / 語音 |
| `Live2DActorStateController` | 狀態機 | 用字串 State 控制動作 + 表情 + 呼吸 |
| `Live2DBreathController` | 呼吸底層 | 直接改 CubismParameter 模擬呼吸 |
| `Live2DBreathStateController` | 呼吸狀態 | Idle / Nervous / Sleepy 等節奏管理 |
| `Live2DLipSyncController` | 嘴型 | AudioSource → ParamMouthOpenY，含淡出 + OnVoiceFinished |
| `Live2DVoiceManager` | 全域語音 | 管理多角色誰在說話、呼叫 PlayLine |
| `Live2DSpeakerBootstrap` | 初始化 | 把角色註冊進 VoiceManager |
| `DialogueDemoRunner` | 範例 | 示範如何串對話資料與角色狀態 |

---

## 4. 安裝步驟（從零開始）

### 步驟 0：先讓 Cubism 模型在 Unity 裡正常動起來

1. 匯入 Live2D Cubism SDK for Unity。  
2. 依照官方流程匯入 `.model3.json`，產生對應 Prefab。  
3. 確認：
   - 可以用 `CubismFadeController` 播放 motion3
   - 有 `CubismExpressionController` + `.expressionList` 可以切換表情
   - 若要用眼睛看向，有掛 `CubismLookController` + `CubismLookTarget`

### 步驟 1：把 Live2DActorKit 丟進專案

- 整個 `Live2DActorKit` 資料夾放進 `Assets/`  
- 如果一開始只想快跑起來：  
  - 可以先刪掉 `Runtime/Live2DActorKit.Runtime.asmdef`，避免 asmdef 連結問題

### 步驟 2：在角色 Prefab 上掛組件

在你的 Live2D 模型 Prefab Root（以下稱 `HinaActor`）上：

1. 確認原本就有：
   - `CubismFadeController`
   - `CubismExpressionController`
   - （若要用眼睛看向）`CubismLookController`  
     - 並在某個子物件上掛 `CubismLookTarget`（或你自訂的 ICubismLookTarget 實作）  
     - 在 `CubismLookController.Target` 欄位指定該元件

2. 再加上這些：
   - `Live2DActor`
   - `Live2DBreathController`
   - `Live2DBreathStateController`
   - `Live2DLipSyncController`
   - `Live2DActorStateController`
   - `Live2DSpeakerBootstrap`

3. Inspector 基本設定重點：
   - `Live2DActor.expressionController` 指向該角色的 `CubismExpressionController`  
   - `Live2DActor.lookController` 指向該角色的 `CubismLookController`  
   - 嘴型：模型必須有 `ParamMouthOpenY`，`Live2DLipSyncController` 會自動尋找  
   - 呼吸：若模型有 `ParamBreath / ParamBodyY / ParamBustY / ParamAngleZ`，`Live2DBreathController` 會自動綁定  
   - 若角色在 Canvas 下，建議把：
     - `Live2DActor.rectTransform` 指向角色的 RectTransform  
     - `Live2DActor.parentCanvas` 指向所在 Canvas  
     - `Live2DActor.uiCamera` 設成 Canvas 使用的 Camera（ScreenSpaceCamera / WorldSpace 時）

### 步驟 3：建立全域 VoiceManager

在任意場景建立一個空物件 `Live2DVoiceManager`，掛上：

- `Live2DVoiceManager` 腳本

`Awake()` 內已經 `DontDestroyOnLoad`，會跨場景存在。

### 步驟 4：設定 Speaker Id

在每個角色的 `Live2DSpeakerBootstrap` 上設定：

- `Speaker Id`：例如 `"Hina"`、`"Ryo"`  
- 之後對話系統就用這個 Id 來叫：

```csharp
Live2DVoiceManager.Instance.PlayLine("Hina", hinaClip);
```

---

## 5. Expression（表情）對應規則

`Live2DActor.SetExpression(string expressionId)` 的實作是：

- 從 `CubismExpressionController.ExpressionList.CubismExpressionObjects` 取出所有 exp3.asset  
- 以 **exp3.asset 的名稱** 作為 `expressionId` 的對應 key  

例如：

- 你的 `expressionList` 裡有：
  - `Hina_Neutral.exp3.asset`
  - `Hina_Happy.exp3.asset`
  - `Hina_Angry.exp3.asset`

那在程式裡可以這樣用：

```csharp
actor.SetExpression("Hina_Happy");
actor.SetExpression("Hina_Angry");
actor.ClearExpression(); // CurrentExpressionIndex = -1
```

如果你想要用比較短的 key（例如 `"Happy"`），可以自己在外層做一層 `Dictionary<string, string>` mapping。

---

## 6. LookAt / 眼睛看向 的正確用法

### 6.1 Target 型別說明

`CubismLookController.Target` 型別是 `UnityEngine.Object`，  
而且被限制為「必須實作 `ICubismLookTarget` 的元件」，**不是 `Vector3`**。

所以正確用法是：

- 在角色階層中建立一個子物件 `LookTarget`  
- 掛上官方 `CubismLookTarget`（或你自己的 ICubismLookTarget 實作）  
- 將這個元件填入 `CubismLookController.Target` 欄位  
- 程式只需要「移動這個 LookTarget 物件的位置」，眼睛就會跟著看

本套件的 `Live2DActor.LookAt()` 做的事情是：

1. 從 `lookController.Target` 取得真正的 `Component`（`_lookTargetComponent`）  
2. 根據角色是否在 Canvas 下，選擇：
   - Canvas 模式：使用 `RectTransformUtility.ScreenPointToWorldPointInRectangle()`  
   - 3D 世界模式：使用 `uiCamera.ScreenToWorldPoint()`  
3. 把換算後的世界座標指定給 `_lookTargetComponent.transform.position`

### 6.2 使用範例

```csharp
// 讓角色眼睛跟著滑鼠
void Update()
{
    if (Input.GetMouseButton(0))
        actor.LookAt(Input.mousePosition);
    else
        actor.ResetLookAt();
}
```

如果你的角色在 Canvas 底下，請務必設定：

- `Live2DActor.rectTransform`
- `Live2DActor.parentCanvas`
- `Live2DActor.uiCamera`（若 Canvas 用 ScreenSpaceCamera / WorldSpace）

否則會自動視為 3D 世界模式，只用 `uiCamera.ScreenToWorldPoint()`。

---

## 7. 語音 + 嘴型 + 狀態整合

### 7.1 嘴型控制（Live2DLipSyncController）

- 會從 `AudioSource` 取樣，計算 RMS 音量  
- 映射到 `ParamMouthOpenY`（自動尋找）  
- 音檔播完後，嘴型會以 `mouthFadeOutSpeed` 漸漸收回  
- 收到嘴型幾乎關閉時觸發 `OnVoiceFinished` 事件

### 7.2 狀態自動回 Idle（Live2DActorStateController）

`Live2DActorStateController` 會在 `OnEnable` 時訂閱：

```csharp
_lipSync.OnVoiceFinished += HandleVoiceFinished;
```

當語音播放 + 嘴型淡出結束後：

```csharp
private void HandleVoiceFinished()
{
    ResetToIdle();
}
```

如不希望自動回 Idle，可以直接把這行改成空實作或註解掉。

### 7.3 多角色語音管理（Live2DVoiceManager）

`Live2DVoiceManager` 用字串 `speakerId` 管理多個角色的 `Live2DLipSyncController`：

- `RegisterSpeaker(id, lipSync)`：由 `Live2DSpeakerBootstrap` 在 Start 時自動呼叫  
- `PlayLine(id, clip, volume, onFinished)`：播放語音 + 嘴型同步  
- `StopSpeaker(id)` / `StopAll()`：可強制停止語音  

預設策略 `VoiceConflictPolicy.StopOthers`：

- 新的 `PlayLine()` 啟動時，會把其他角色的語音停止（嘴型直接關閉）

---

## 8. 快速範例：對話 + 語音 + 嘴型 + 狀態

### 資料結構

```csharp
[System.Serializable]
public class DialogueEntry
{
    public string SpeakerId;    // 例如 "Hina"
    public string ActorState;   // 例如 "Happy"
    public string Text;
    public AudioClip VoiceClip;
}
```

### 使用 `DialogueDemoRunner`（簡化版）

```csharp
using UnityEngine;
using Live2DActorKit.Actors;
using Live2DActorKit.Audio;

public class MyDialogueRunner : MonoBehaviour
{
    public DialogueEntry[] entries;
    public UnityEngine.UI.Text textUI;
    public Live2DActorStateController hinaActor;

    private int _index;

    void Start()
    {
        PlayNext();
    }

    public void PlayNext()
    {
        if (_index >= entries.Length)
        {
            if (textUI != null) textUI.text = "(End)";
            return;
        }

        var line = entries[_index++];

        if (!string.IsNullOrEmpty(line.ActorState))
            hinaActor.PlayState(line.ActorState);

        if (textUI != null)
            textUI.text = $"{line.SpeakerId}: {line.Text}";

        Live2DVoiceManager.Instance.PlayLine(
            line.SpeakerId,
            line.VoiceClip,
            1f,
            onFinished: PlayNext
        );
    }
}
```

---

## 9. 常見坑位與排錯建議

1. **找不到 Cubism 類別（Compiler Error）**
   - 刪掉 `Live2DActorKit.Runtime.asmdef` 最快
   - 或在 asmdef 的 `references` 加上 Cubism 的 asmdef 名稱

2. **表情切換沒反應**
   - 確認 `CubismExpressionController.ExpressionList` 有填
   - 確認 exp3.asset 名稱與你傳進去的 `expressionId` 一致
   - 看 Console 有沒有出現 `[Live2DActor] Expression 'xxx' not found...`

3. **LookAt 沒有作用**
   - `CubismLookController.Target` 必須指定一個有實作 `ICubismLookTarget` 的 Component（例如 `CubismLookTarget`）
   - `Live2DActor.uiCamera` / `rectTransform` / `parentCanvas` 是否設定合理
   - 確認 `LookAt()` 確實在 Update 或事件裡被呼叫

4. **嘴型不動**
   - 確認模型有 `ParamMouthOpenY`
   - `Live2DLipSyncController` 是否成功自動找到該 Parameter
   - `AudioSource` 有輸出，且 `GetOutputData` 能取到波形（注意是否有 Mute / Volume = 0）

---

## 10. 授權與用途

這套腳本預設可以自由修改、商用、內部專案使用。  
你可以：

- 直接整包放進你們團隊的共用 Unity Template 專案  
- 改名成你們自己的命名空間 / 產品名稱  
- 依專案需求砍掉你不需要的部分（例如語音系統，保留呼吸與表情即可）

如果你有實際專案結構、命名習慣，  
也可以再給我一版「你們正式專案的目錄與 coding style」，  
我可以幫你把這個 Kit 重排成「正式版 v1.0」專用 Layout。


---

## 附錄：Motion 播放（PlayMotion）設定提醒

- `Live2DActor` 內部使用 `CubismMotionController.PlayAnimation(AnimationClip clip, bool isLoop)` 播放動作。
- 你需要在 `Live2DActor.motionClips` 欄位中，把想要透過程式控制的 `AnimationClip` 全部拖進去。
- `PlayMotion("Idle")` 會去 `motionClips` 裡找 `clip.name == "Idle"` 的那一個來播放。
- Cubism 的淡入淡出由同一個 Prefab 上的 `CubismFadeController` + `CubismFadeMotionList` 自動處理，本套件不直接呼叫 `CubismFadeController` 的任何 Play API。
