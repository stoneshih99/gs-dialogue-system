# GitHub Copilot Instructions

This project is developed using Unity and C#. The goal is to build high-performance, readable, and scalable game systems. Please follow these coding guidelines and design expectations when assisting with code generation.

## 🧠 General Principles

- All scripts are written in **C#** for **Unity 2021+**.
- Follow Unity's **MonoBehaviour lifecycle** (`Awake`, `Start`, `Update`, etc.).
- Code must be clean, readable, and follow **C# naming conventions**.
- Prefer composition over inheritance. Avoid deep inheritance chains unless necessary.
- Always provide summaries using `///` for public methods and classes.

---

## 📦 Project Structure & Design Patterns

- Scripts are organized into folders like `Player`, `Input`, `UI`, `Systems`, `Managers`.
- Common patterns used:
  - **Singleton** (GameManager, AudioManager)
  - **Event system** (using `UnityEvent` or custom C# events)
  - **State Machine** for AI and player states
  - **ScriptableObject** for config and data assets
  - **Component-based design**: favor `MonoBehaviour` components over static classes

---

## 🎮 Input System

- We are using **Unity's new Input System (InputActionAsset)**.
- All player inputs are wrapped in a `PlayerInputActions` class generated from `.inputactions` asset.
- Avoid hardcoding input keys. Use `InputAction` references.
- For multiplayer, use `PlayerInputManager` + `PlayerInput`.

---

## 🔧 Unity-Specific Practices

- Use `SerializeField` for private variables that require inspector exposure.
- Use `TryGetComponent<T>()` instead of `GetComponent<T>()` where safety is needed.
- Avoid `FindObjectOfType` in performance-sensitive areas (use references).
- Use `Time.deltaTime` for frame-independent movement.
- Physics logic should go in `FixedUpdate()`; input and animation in `Update()`.

---

## 🧪 Testing & Debugging

- Use `[ContextMenu]` for in-editor testing methods.
- Use `Debug.LogWarning` or `Debug.LogError` for meaningful logs.
- Use Unity Test Framework when creating automated tests.

---

## ✨ Naming Conventions

- Class names: `PascalCase`
- Method names: `PascalCase`
- Local variables: `camelCase`
- Private fields: `_camelCase`
- Serialized fields: `[SerializeField] private camelCase`
- Constants: `PascalCase`
- Event handlers: prefix with `On` (e.g., `OnJump`, `OnTriggerEnter2D`)
- Async methods: suffix with `Async`
- Enum types and members: `PascalCase`

---

## ✅ Preferred Patterns & Features

- Use `Coroutines` for timed sequences.
- Use `UnityEvent` or `Action<T>` for decoupled communication.
- Use `ScriptableObject` for configs (e.g., player stats, enemy types).
- Use `enum` for state machines or behavior types.
- Prefer `transform.localPosition` over `position` when applicable.

---

## 🚫 Avoid

- Avoid magic numbers — use constants or config files.
- Avoid direct scene name strings — use centralized scene enum or manager.
- Avoid tight coupling between systems (e.g., UI directly controlling Player).

---

## 🧩 Example Use Cases for Copilot

- Generate a basic `MonoBehaviour` class with `Awake`, `Start`, `Update`.
- Write a coroutine that fades out a UI element over 1 second.
- Create a `Singleton` pattern for an `AudioManager`.
- Setup a 2D movement script using the new Input System with `Vector2` movement.
- Create a ScriptableObject for defining weapon stats.

---

## 📌 Final Notes

This project prioritizes maintainability and clarity over brevity. Always favor readable, self-documenting code with clear intention.

If unsure, prefer Unity's best practices and official guidelines.

