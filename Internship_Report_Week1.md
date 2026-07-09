# Week 1 Internship Report

## Andhkaar – Trial of the Yodha

| Field | Details |
|---|---|
| **Intern Name** | Mohit Pipaliya |
| **Project Title** | Andhkaar – Trial of the Yodha |
| **Week** | 1 |
| **Duration** | Day 1 – Day 6 |
| **Github Folder** | https://github.com/Mohit-Pipaliya/Andhkaar-Trial-of-the-Yodha |

---

## Objective

This week was about turning a rough idea into something I could actually build on. My goal was to lock down the concept, get Unity set up properly, gather and organize the assets I'd need, and get the core foundation of an action-adventure temple exploration game in place — player movement, combat, torch mechanics, quest progression with crystals and gates, and a third-person camera system.

---

## Daily Work Log

### Day 1 – Project Planning and Research

- Spent time exploring different game ideas that would work well for the internship timeline.
- Settled on **Andhkaar – Trial of the Yodha** as the project — a dark temple adventure where the player (Deva) explores ancient ruins, collects items, fights demons, and unlocks gates using special crystals.
- Mapped out the scope and thought through the core gameplay mechanics: third-person movement, melee combat, oil lamp exploration, and a 3-level gate puzzle system.
- Looked at existing action-adventure and souls-like games for inspiration and noted down the systems that seemed essential.

**Outcome:** Had a finalized concept and a rough development roadmap to work from.

---

### Day 2 – Asset Collection

- Sourced character models (Deva player character, Demon enemies), temple environment assets (Indian temples, Aztec temple, Mayan temple props, Jedi temple walkway).
- Imported weapons (Sword Asur 1, Sword Asur 2), oil lamp, oil can, wall models for 3 levels, gate models, crystal models (Blue, Red, White), and stand objects for special items.
- Imported character animations (running, walking with torch, running slide, skeleton run).
- Sorted everything into clearly named folders: `Characters`, `Objects/Model`, `Objects/Temple Models`, `Objects/Weapons`, `Animations`, `Map Prefeb` so the project wouldn't turn into a mess later.

**Outcome:** A usable asset library and a project structure that made sense.

---

### Day 3 – Project Structure and Environment Setup

- Built the main gameplay scene (`Assets/Scenes/SampleScene.unity`).
- Blocked out the temple map using walls for Level 1, Level 2, and Level 3, temple props, and environment pieces.
- Placed gate prefabs (Level 1 Gate, Level 2 Gate, Level 3 Gate) and special object stands at each gate location.
- Planned the architecture for the systems I'd need going forward: `PlayerController`, `CameraController`, weapon system, torch system, quest/gate progression, and animator state machine.

**Outcome:** The project structure was in place and ready for actual gameplay code.

---

### Day 4 – Core Gameplay Implementation

- Implemented `PlayerController.cs` using Unity's `CharacterController` for tight, responsive movement.
- Added walk, run, jump (dual-gravity system with coyote time and jump buffer), and crouch/slide mechanics.
- Built the third-person `CameraController.cs` with mouse look, camera collision detection, and smooth follow.
- Integrated Unity's new Input System with WASD/Arrow keys, mouse look, and action key bindings.

**Outcome:** Basic player movement and camera control were functional for the first time.

---

### Day 5 – Combat, Items & Quest Features

- Built the 3-hit melee combo attack system with combo window timing and attack animation speed control.
- Implemented weapon pickup/drop system for two swords (Sword 1 and Sword 2) with weapon switching (keys 1, 2, 3).
- Added oil lamp (torch) pickup, drop, and oil drain system — lamp light intensity decreases over time for realistic exploration tension.
- Added oil can refill mechanic to restore lamp brightness.
- Implemented special crystal object absorption — objects fly to player's chest and shrink when collected.
- Built the gate-opening quest sequence: player prays at trigger (M key), laser beams shoot from both hands to the stand, and the gate slides down to open.

**Outcome:** A working gameplay loop with combat, exploration, and quest progression came together.

---

### Day 6 – Integration, Testing & Polish

- Connected all 3 gate systems with pray triggers, place triggers, and slide-down gate animations.
- Set up animator controller (`Deva.controller`) with states for walking, running, jumping, sliding, attacking (3 combo steps), special magic action, and torch holding.
- Added debug logging and Gizmos for grounded check and pickup range visualization.
- Tested movement on slopes, jump feel, combo chaining, lamp drain/refill, and gate opening sequences.
- Wrote `DesignDocumentation.md` covering architectural choices and future improvements.
- Cleaned up the scene layout and organized prefabs.

**Outcome:** The first playable prototype was complete.

---

## Technologies Used

- Unity 6 (6000.3.10f1)
- C#
- Unity Input System
- Unity Animator & Animation System
- CharacterController & Physics (OverlapSphere, SphereCast, Raycast)
- LineRenderer (laser beam VFX)
- Git & GitHub
- Free 3D Asset Packs (Temple, Character, Weapon models)

---

## Features Completed

- Third-person player movement (walk, run, jump, slide)
- Dual-gravity jump system with coyote time and jump buffer
- Third-person camera with collision detection and slide-mode adjustment
- 3-hit melee combo attack system
- Dual sword weapon system (pickup, drop, switch)
- Oil lamp / torch system with light drain and oil can refill
- Special crystal object collection with fly-to-player absorption animation
- 3-level gate quest system with pray trigger, laser beam VFX, and gate slide-down
- Character animations (walk, run, jump, slide, attack combos, special action, torch walk)
- Temple environment with walls, gates, stands, and props for 3 levels
- Debug tools (Gizmos, console logging)
- Design documentation

---

## Challenges Faced

- Figuring out a project scope that was realistic for the internship's timeframe.
- Keeping a large number of imported 3D assets (characters, temples, weapons, crystals) organized across multiple folders.
- Implementing a jump system that felt snappy and realistic rather than floaty.
- Designing the gate-opening sequence with synchronized animations, laser beams, and player lock during special actions.
- Preventing false `OnTriggerExit` events when centering the player at pray triggers (had to use `controller.Move` instead of disabling the controller).
- Balancing torch movement speeds so carrying the lamp felt heavier without being frustrating.

---

## Solutions Implemented

- Trimmed the scope to focus on core exploration, combat, and one complete quest loop across 3 gates.
- Organized assets into categorized folders (`Characters`, `Objects`, `Animations`, `Map Prefeb`) from the start.
- Used a dual-gravity jump system with kinematic equations (`jumpVelocity`, `baseGravity`) and fall/low-jump multipliers for industry-standard feel.
- Built the special action as a coroutine sequence (`SpecialActionSequence` → `PlaceObject` → `OpenGateSlideDown`) with player input lock.
- Used `controller.Move` for smooth centering at pray triggers instead of disabling `CharacterController`.
- Added separate `torchWalkSpeed` and `torchRunSpeed` values that are slower than normal movement.

---

## Conclusion

By the end of the first week, **Andhkaar – Trial of the Yodha** had gone from a bare idea to a working prototype. There's now a structured temple environment, third-person movement with jump and slide, a 3-hit combo combat system, torch-based exploration with oil management, a crystal collection quest, and a 3-gate progression system with laser beam VFX. It's a solid base to build on for enemy AI, HUD/UI, sound effects, and more advanced gameplay features in the coming weeks.

---

## Screenshot Reference

A few screenshots taken along the way, showing how the project moved from an early prototype to a more complete build.

> **Note:** Insert screenshots from your Unity Editor / Game view below before submitting.

| Day | Description |
|---|---|
| **Day 1** | Early project setup — Unity scene with imported Deva character model and basic terrain. |
| **Day 2** | Asset organization — categorized folders with temple models, weapons, and crystals imported. |
| **Day 3** | Environment layout — temple walls, gate prefabs, and special object stands placed in the scene. |
| **Day 4** | Movement prototype — player walking, running, and jumping with third-person camera following. |
| **Day 5** | Combat & items — sword combo attacks, torch pickup with point light, crystal absorption animation. |
| **Day 6** | Integrated prototype — full quest loop with gate opening via laser beams and slide-down animation. |

---

## Expected Deliverables

For this submission, the following needs to be included:

- A Unity project folder containing a scene with the temple environment (`Assets/Scenes/SampleScene.unity`).
- Imported 3D models — player character (Deva), demons, temple props, gates, crystals, weapons, oil lamp.
- Proper asset organization in folders (`Characters`, `Objects`, `Animations`, `Map Prefeb`, `Scripts`).
- A README / report file explaining the project structure and the overall approach, submitted in **PDF format**.

---

## Key Steps (How to Run the Project)

1. **Open the Project** in Unity Hub (Unity 6 recommended).
2. Navigate to `Assets → Scenes → SampleScene` and open the scene.
3. Click the **Play** button in the Game window.
4. Use the following controls:

| Key | Action |
|---|---|
| **W / A / S / D** or **Arrow Keys** | Move |
| **Left Shift** | Run |
| **Space** | Jump |
| **Left Ctrl** | Slide (requires torch) |
| **Mouse** | Look around |
| **Left Click** | Attack (requires sword) |
| **O** | Pick up / interact with Oil Lamp |
| **E** | Pick up sword / crystal / oil can |
| **G** | Drop weapon |
| **L** | Drop lamp |
| **1 / 2 / 3** | Switch weapon (None / Sword 1 / Sword 2) |
| **M** | Special action — place crystal and open gate (at pray trigger) |

5. Explore the temple, collect the oil lamp, find swords and crystals, and unlock all 3 gates.

---

## Project Structure

```
Andhkaar – Trial of the Yodha/
├── Assets/
│   ├── Animations/          # Character animations & Animator controllers
│   ├── Characters/          # Deva (player) and Demon models
│   ├── Map Prefeb/          # Gate, stand, and oil can prefabs
│   ├── Objects/
│   │   ├── Model/           # Walls, gates, crystals, oil lamp, stands
│   │   ├── Temple Models/   # Temple environment assets
│   │   └── Weapons/         # Sword models
│   ├── Scenes/
│   │   └── SampleScene.unity
│   └── Scripts/
│       ├── PlayerController.cs
│       └── CameraController.cs
├── DesignDocumentation.md
└── Internship_Report_Week1.md
```

---

*Report prepared for Week 1 — Unity Game Development Trainee Internship*
