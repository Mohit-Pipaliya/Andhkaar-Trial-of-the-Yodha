# Andhkaar – Trial of the Yodha: UI/HUD Kit

This document provides a comprehensive overview of the User Interface and HUD implementation for the game. The UI system was designed to meet the strict criteria of a dynamic, interactive, responsive, and highly optimized 60+ FPS experience using modern Event-Driven architecture in Unity.

---

## 1. Workflow & Design Decisions

### A. Event-Driven Architecture (Decoupled Design)
To fulfill the requirement of seamless interaction between game states and UI, the `PlayerController.cs` and `UIManager.cs` are completely decoupled using C# `Action` delegates (Events). 
- **How it works:** When the player takes damage or drains oil, the `PlayerController` doesn't manually search for the UI. Instead, it fires an event (`OnHealthChanged` / `OnOilChanged`). The `UIManager` listens to these events and updates itself.
- **Why this matters:** This prevents "Null Reference Exceptions", keeps code modular, and ensures that if the UI is missing from a scene, the core gameplay logic doesn't break.

### B. High-Performance Smooth Transitions (60+ FPS)
Instead of using Unity's `Animator` component (which is heavy and evaluates every frame for every panel), the UI uses code-based Coroutines and `CanvasGroup` components.
- Panels do not just snap `SetActive(true/false)`. They smoothly interpolate their `alpha` transparency via `Mathf.Lerp`.
- The Sliders for Health and Oil use `Time.unscaledDeltaTime` with `Lerp` to drain smoothly instead of instantly chunking down.

### C. "Juicy" Button Animations
A custom script `UIButtonAnimator.cs` was developed to provide a realistic, premium feel to UI interactions.
- Hovering over a button expands it slightly (`1.05x` scale).
- Clicking it compresses it (`0.95x` scale).
- This runs entirely on `Time.unscaledDeltaTime`, meaning the animations work perfectly even when the game is paused (Time.timeScale = 0).

### D. Game State Management
A global variable `UIManager.isGameActive` ensures that the `PlayerController` ignores player input (like movement and attacking) while the loading screen, main menu, or pause menus are active. This prevents the bug where the game "starts behind the scenes" before the user presses Play.

---

## 2. Usage Instructions

### Setting Up the UIManager
1. Create an Empty GameObject in your Canvas and name it `UIManager`.
2. Attach the `UIManager.cs` script.
3. Drag and drop your respective UI Panels (Main Menu, Pause, Game Play, etc.) into the appropriate slots.
4. Drag and drop your UI Sliders and TextMeshPro elements.

### Hooking up the Buttons
- **Play Button:** Add an OnClick event, drag the UIManager into the slot, and select `UIManager.PlayGame`.
- **Options Button:** Select `UIManager.OpenOptionsMenu`.
- **Resume Button:** Select `UIManager.ResumeGame`.
- **Quit Button:** Select `UIManager.QuitGame`.

### Applying Button Animations
1. Select any interactive UI Button in your Hierarchy (e.g., Play, Resume, Quit).
2. Click **Add Component** and search for `UIButtonAnimator`.
3. The script will automatically add a subtle scale animation when you play the game and interact with the button.

### Testing Player UI Updates
You can test the Health UI by calling `TakeDamage(20)` from the `PlayerController` (for instance, inside an `OnTriggerEnter` from an enemy). The health slider will automatically lerp down, and a red Damage Panel will flash briefly.

---

## 3. Evaluation Checklist Addressed
- [x] **Fully Functioning UI/HUD:** Loading, Menu, HUD, Settings, and Game Over states.
- [x] **C# Scripts Handling Logic:** Separate responsibilities between Player and UI.
- [x] **Dynamic Updates & Animations:** CanvasGroup fading, Lerp sliders, and custom button animations.
- [x] **Event-Driven Programming:** Implementation of C# `Action` delegates.
- [x] **Detailed Readme File:** This document.
