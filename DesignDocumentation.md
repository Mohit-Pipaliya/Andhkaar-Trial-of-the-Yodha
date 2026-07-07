# Andhkaar – Trial of the Yodha: Core Mechanics Design Document

This document outlines the core design decisions, architectural choices, and potential improvements for the "Andhkaar – Trial of the Yodha" Unity project.

## 1. Architectural & Design Choices

### Character Movement: `CharacterController` vs `Rigidbody`
For the core player movement, Unity's built-in `CharacterController` was chosen over a purely `Rigidbody`-based physics system. 
*   **Reasoning**: `CharacterController` provides tighter, more responsive control over player movement, which is essential for action and platforming games. Unlike `Rigidbody`, it does not suffer from unwanted physics-based sliding, bouncy collisions, or unpredictable rotation issues on slopes unless explicitly programmed. This allows for precise implementation of custom gravity and slope handling.

### Collision & Interaction: `Physics.OverlapSphere` vs `OnTriggerEnter`
For interacting with objects (e.g., picking up the Oil Lamp and Swords), `Physics.OverlapSphere` is used on a button press instead of passive `OnTriggerEnter` events.
*   **Reasoning**: This gives the player agency. Instead of automatically picking up weapons simply by walking over them (which can be frustrating if the player doesn't want to swap weapons), `OverlapSphere` checks for nearby interactable items *only* when the player explicitly presses the "Interact" key. This is a standard practice in modern Action-RPGs.

### Dual-Gravity Jump System
The jumping system implements a variable-height dual-gravity mechanic.
*   **Reasoning**: A standard, linear gravity jump often feels floaty. By calculating gravity dynamically based on `jumpHeight` and `timeToApex`, and applying a `fallGravityMultiplier` when the player is descending, the jump feels heavy, snappy, and realistic. Additionally, the `lowJumpMultiplier` allows the player to perform short hops by tapping the jump button, providing greater air control.

### Modular Weapon & State System
The weapon system utilizes a simple State pattern (`WeaponType` enum) coupled with a Combo System.
*   **Reasoning**: Using an enum to track `WeaponType.None`, `WeaponType.Sword1`, and `WeaponType.Sword2` allows for clean, centralized logic when updating Animator states and toggling the visibility of hand-held objects. The combo system incorporates a "combo window timer," making melee combat feel rhythmic and deliberate rather than allowing mindless button-mashing.

---

## 2. Potential Improvements & Future Scope

While the core module is robust, the following additions could elevate the gameplay experience:

*   **Object Pooling for Particles and Sounds**: Implementing an object pool for footstep sounds, attack swoosh effects, and landing dust particles would prevent garbage collection spikes during intense gameplay.
*   **Cinemachine Integration**: Replacing the basic `CameraController` with Unity Cinemachine would allow for dynamic camera shakes on impacts, target-lock-on systems, and smooth blending between exploration and combat camera angles.
*   **Interface / HUD**: Implementing a UI system using the Unity UI Canvas to display current health, active weapon icon, and contextual button prompts (e.g., "Press 'O' to Pick up Sword") when the player is within interaction range.
*   **Enemy AI Core**: Integrating a basic state machine for enemies (Idle, Chase, Attack) that interacts with the player's weapon hitboxes using Unity's Animation Events.
*   **Input System Refinement**: Transitioning fully to the new Unity Input System's event-driven callbacks rather than checking `inputAction.triggered` in the `Update` loop could marginally improve performance and clean up the `Update` logic.

## 3. Debugging Implementation
The current iteration includes robust `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` checks in the `PlayerController.cs`. Missing references (like the Animator or Character Controller) are flagged immediately upon initialization, and interaction events (picking up/dropping torches, collecting swords, switching weapons) print to the console to streamline debugging and playtesting.
