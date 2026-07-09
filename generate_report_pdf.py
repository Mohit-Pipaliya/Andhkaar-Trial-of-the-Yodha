#!/usr/bin/env python3
"""Generate Week 1 Internship Report PDF for Andhkaar – Trial of the Yodha."""

from fpdf import FPDF

OUTPUT = "Internship_Report_Week1.pdf"


class ReportPDF(FPDF):
    def header(self):
        pass

    def footer(self):
        self.set_y(-15)
        self.set_font("Helvetica", "I", 8)
        self.set_text_color(120, 120, 120)
        self.cell(0, 10, f"Page {self.page_no()}", align="C")

    def section_title(self, title):
        self.ln(4)
        self.set_font("Helvetica", "B", 13)
        self.set_text_color(30, 30, 30)
        self.multi_cell(0, 8, title)
        self.ln(2)

    def body_text(self, text):
        self.set_font("Helvetica", "", 10)
        self.set_text_color(50, 50, 50)
        self.multi_cell(0, 5.5, text)
        self.ln(2)

    def bullet(self, text, indent=8):
        self.set_font("Helvetica", "", 10)
        self.set_text_color(50, 50, 50)
        x = self.get_x()
        self.cell(indent, 5.5, chr(149))
        self.multi_cell(0, 5.5, text)
        self.ln(1)

    def outcome(self, text):
        self.set_font("Helvetica", "B", 10)
        self.set_text_color(30, 30, 30)
        self.multi_cell(0, 5.5, f"Outcome: {text}")
        self.ln(3)


def build_pdf():
    pdf = ReportPDF()
    pdf.set_auto_page_break(auto=True, margin=20)
    pdf.add_page()

    # Title
    pdf.set_font("Helvetica", "B", 18)
    pdf.set_text_color(20, 20, 20)
    pdf.cell(0, 10, "Week 1 Internship Report", align="C", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(2)
    pdf.set_font("Helvetica", "B", 14)
    pdf.cell(0, 8, "Andhkaar - Trial of the Yodha", align="C", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(6)

    # Meta info
    meta = [
        ("Intern Name:", "Mohit Pipaliya"),
        ("Project Title:", "Andhkaar - Trial of the Yodha"),
        ("Week:", "1"),
        ("Duration:", "Day 1 - Day 6"),
        ("Github Folder:", "https://github.com/Mohit-Pipaliya/Andhkaar-Trial-of-the-Yodha"),
    ]
    for label, value in meta:
        pdf.set_font("Helvetica", "B", 10)
        pdf.write(6, label + " ")
        pdf.set_font("Helvetica", "", 10)
        pdf.write(6, value + "\n")
    pdf.ln(4)

    # Objective
    pdf.section_title("Objective")
    pdf.body_text(
        "This week was about turning a rough idea into something I could actually build on. My goal was to "
        "lock down the concept, get Unity set up properly, gather and organize the assets I'd need, and get "
        "the core foundation of an action-adventure temple exploration game in place - player movement, combat, "
        "torch mechanics, quest progression with crystals and gates, and a third-person camera system."
    )

    # Daily Work Log
    pdf.section_title("Daily Work Log")

    days = [
        (
            "Day 1 - Project Planning and Research",
            [
                "Spent time exploring different game ideas that would work well for the internship timeline.",
                "Settled on Andhkaar - Trial of the Yodha as the project - a dark temple adventure where the player (Deva) explores ancient ruins, collects items, fights demons, and unlocks gates using special crystals.",
                "Mapped out the scope and thought through the core gameplay mechanics: third-person movement, melee combat, oil lamp exploration, and a 3-level gate puzzle system.",
                "Looked at existing action-adventure and souls-like games for inspiration and noted down the systems that seemed essential.",
            ],
            "Had a finalized concept and a rough development roadmap to work from.",
        ),
        (
            "Day 2 - Asset Collection",
            [
                "Sourced character models (Deva player character, Demon enemies), temple environment assets (Indian temples, Aztec temple, Mayan temple props, Jedi temple walkway).",
                "Imported weapons (Sword Asur 1, Sword Asur 2), oil lamp, oil can, wall models for 3 levels, gate models, crystal models (Blue, Red, White), and stand objects for special items.",
                "Imported character animations (running, walking with torch, running slide, skeleton run).",
                "Sorted everything into clearly named folders: Characters, Objects/Model, Objects/Temple Models, Objects/Weapons, Animations, Map Prefeb so the project wouldn't turn into a mess later.",
            ],
            "A usable asset library and a project structure that made sense.",
        ),
        (
            "Day 3 - Project Structure and Environment Setup",
            [
                "Built the main gameplay scene (Assets/Scenes/SampleScene.unity).",
                "Blocked out the temple map using walls for Level 1, Level 2, and Level 3, temple props, and environment pieces.",
                "Placed gate prefabs (Level 1 Gate, Level 2 Gate, Level 3 Gate) and special object stands at each gate location.",
                "Planned the architecture for the systems I'd need going forward: PlayerController, CameraController, weapon system, torch system, quest/gate progression, and animator state machine.",
            ],
            "The project structure was in place and ready for actual gameplay code.",
        ),
        (
            "Day 4 - Core Gameplay Implementation",
            [
                "Implemented PlayerController.cs using Unity's CharacterController for tight, responsive movement.",
                "Added walk, run, jump (dual-gravity system with coyote time and jump buffer), and crouch/slide mechanics.",
                "Built the third-person CameraController.cs with mouse look, camera collision detection, and smooth follow.",
                "Integrated Unity's new Input System with WASD/Arrow keys, mouse look, and action key bindings.",
            ],
            "Basic player movement and camera control were functional for the first time.",
        ),
        (
            "Day 5 - Combat, Items & Quest Features",
            [
                "Built the 3-hit melee combo attack system with combo window timing and attack animation speed control.",
                "Implemented weapon pickup/drop system for two swords (Sword 1 and Sword 2) with weapon switching (keys 1, 2, 3).",
                "Added oil lamp (torch) pickup, drop, and oil drain system - lamp light intensity decreases over time for realistic exploration tension.",
                "Added oil can refill mechanic to restore lamp brightness.",
                "Implemented special crystal object absorption - objects fly to player's chest and shrink when collected.",
                "Built the gate-opening quest sequence: player prays at trigger (M key), laser beams shoot from both hands to the stand, and the gate slides down to open.",
            ],
            "A working gameplay loop with combat, exploration, and quest progression came together.",
        ),
        (
            "Day 6 - Integration, Testing & Polish",
            [
                "Connected all 3 gate systems with pray triggers, place triggers, and slide-down gate animations.",
                "Set up animator controller (Deva.controller) with states for walking, running, jumping, sliding, attacking (3 combo steps), special magic action, and torch holding.",
                "Added debug logging and Gizmos for grounded check and pickup range visualization.",
                "Tested movement on slopes, jump feel, combo chaining, lamp drain/refill, and gate opening sequences.",
                "Wrote DesignDocumentation.md covering architectural choices and future improvements.",
                "Cleaned up the scene layout and organized prefabs.",
            ],
            "The first playable prototype was complete.",
        ),
    ]

    for day_title, bullets, outcome in days:
        pdf.set_font("Helvetica", "B", 11)
        pdf.set_text_color(30, 30, 30)
        pdf.multi_cell(0, 6, day_title)
        pdf.ln(1)
        for b in bullets:
            pdf.bullet(b)
        pdf.outcome(outcome)

    # Technologies
    pdf.add_page()
    pdf.section_title("Technologies Used")
    tech = [
        "Unity 6 (6000.3.10f1)",
        "C#",
        "Unity Input System",
        "Unity Animator & Animation System",
        "CharacterController & Physics (OverlapSphere, SphereCast, Raycast)",
        "LineRenderer (laser beam VFX)",
        "Git & GitHub",
        "Free 3D Asset Packs (Temple, Character, Weapon models)",
    ]
    for t in tech:
        pdf.bullet(t)

    # Features
    pdf.section_title("Features Completed")
    features = [
        "Third-person player movement (walk, run, jump, slide)",
        "Dual-gravity jump system with coyote time and jump buffer",
        "Third-person camera with collision detection and slide-mode adjustment",
        "3-hit melee combo attack system",
        "Dual sword weapon system (pickup, drop, switch)",
        "Oil lamp / torch system with light drain and oil can refill",
        "Special crystal object collection with fly-to-player absorption animation",
        "3-level gate quest system with pray trigger, laser beam VFX, and gate slide-down",
        "Character animations (walk, run, jump, slide, attack combos, special action, torch walk)",
        "Temple environment with walls, gates, stands, and props for 3 levels",
        "Debug tools (Gizmos, console logging)",
        "Design documentation",
    ]
    for f in features:
        pdf.bullet(f)

    # Challenges
    pdf.section_title("Challenges Faced")
    challenges = [
        "Figuring out a project scope that was realistic for the internship's timeframe.",
        "Keeping a large number of imported 3D assets (characters, temples, weapons, crystals) organized across multiple folders.",
        "Implementing a jump system that felt snappy and realistic rather than floaty.",
        "Designing the gate-opening sequence with synchronized animations, laser beams, and player lock during special actions.",
        "Preventing false OnTriggerExit events when centering the player at pray triggers.",
        "Balancing torch movement speeds so carrying the lamp felt heavier without being frustrating.",
    ]
    for c in challenges:
        pdf.bullet(c)

    # Solutions
    pdf.section_title("Solutions Implemented")
    solutions = [
        "Trimmed the scope to focus on core exploration, combat, and one complete quest loop across 3 gates.",
        "Organized assets into categorized folders (Characters, Objects, Animations, Map Prefeb) from the start.",
        "Used a dual-gravity jump system with kinematic equations and fall/low-jump multipliers for industry-standard feel.",
        "Built the special action as a coroutine sequence with player input lock.",
        "Used controller.Move for smooth centering at pray triggers instead of disabling CharacterController.",
        "Added separate torchWalkSpeed and torchRunSpeed values that are slower than normal movement.",
    ]
    for s in solutions:
        pdf.bullet(s)

    # Conclusion
    pdf.section_title("Conclusion")
    pdf.body_text(
        "By the end of the first week, Andhkaar - Trial of the Yodha had gone from a bare idea to a working "
        "prototype. There's now a structured temple environment, third-person movement with jump and slide, "
        "a 3-hit combo combat system, torch-based exploration with oil management, a crystal collection quest, "
        "and a 3-gate progression system with laser beam VFX. It's a solid base to build on for enemy AI, HUD/UI, "
        "sound effects, and more advanced gameplay features in the coming weeks."
    )

    # Screenshot Reference
    pdf.section_title("Screenshot Reference")
    pdf.body_text(
        "A few screenshots taken along the way, showing how the project moved from an early prototype to a "
        "more complete build. (Insert screenshots from Unity Editor / Game view before final submission.)"
    )
    screenshots = [
        ("Day 1", "Early project setup - Unity scene with imported Deva character model and basic terrain."),
        ("Day 2", "Asset organization - categorized folders with temple models, weapons, and crystals imported."),
        ("Day 3", "Environment layout - temple walls, gate prefabs, and special object stands placed in the scene."),
        ("Day 4", "Movement prototype - player walking, running, and jumping with third-person camera following."),
        ("Day 5", "Combat & items - sword combo attacks, torch pickup with point light, crystal absorption animation."),
        ("Day 6", "Integrated prototype - full quest loop with gate opening via laser beams and slide-down animation."),
    ]
    for day, desc in screenshots:
        pdf.set_font("Helvetica", "B", 10)
        pdf.cell(0, 5.5, day, new_x="LMARGIN", new_y="NEXT")
        pdf.set_font("Helvetica", "", 10)
        pdf.multi_cell(0, 5.5, desc)
        pdf.ln(2)

    # Expected Deliverables
    pdf.add_page()
    pdf.section_title("Expected Deliverables")
    pdf.body_text("For this submission, the following needs to be included:")
    deliverables = [
        "A Unity project folder containing a scene with the temple environment (Assets/Scenes/SampleScene.unity).",
        "Imported 3D models - player character (Deva), demons, temple props, gates, crystals, weapons, oil lamp.",
        "Proper asset organization in folders (Characters, Objects, Animations, Map Prefeb, Scripts).",
        "A README / report file explaining the project structure and the overall approach, submitted in PDF format.",
    ]
    for d in deliverables:
        pdf.bullet(d)

    # Key Steps
    pdf.section_title("Key Steps (How to Run the Project)")
    steps = [
        "Open the Project in Unity Hub (Unity 6 recommended).",
        "Navigate to Assets > Scenes > SampleScene and open the scene.",
        "Click the Play button in the Game window.",
        "Use controls: WASD/Arrows (Move), Shift (Run), Space (Jump), Ctrl (Slide), Mouse (Look), Left Click (Attack), O (Pick lamp), E (Pick sword/crystal), G (Drop weapon), L (Drop lamp), 1/2/3 (Switch weapon), M (Special action at gate).",
        "Explore the temple, collect the oil lamp, find swords and crystals, and unlock all 3 gates.",
    ]
    for i, step in enumerate(steps, 1):
        pdf.set_font("Helvetica", "", 10)
        pdf.multi_cell(0, 5.5, f"{i}. {step}")
        pdf.ln(1)

    # Project Structure
    pdf.section_title("Project Structure")
    structure = """Andhkaar - Trial of the Yodha/
  Assets/
    Animations/       - Character animations & Animator controllers
    Characters/       - Deva (player) and Demon models
    Map Prefeb/       - Gate, stand, and oil can prefabs
    Objects/
      Model/          - Walls, gates, crystals, oil lamp, stands
      Temple Models/  - Temple environment assets
      Weapons/        - Sword models
    Scenes/
      SampleScene.unity
    Scripts/
      PlayerController.cs
      CameraController.cs
  DesignDocumentation.md
  Internship_Report_Week1.pdf"""
    pdf.set_font("Courier", "", 9)
    pdf.multi_cell(0, 4.5, structure)

    pdf.ln(6)
    pdf.set_font("Helvetica", "I", 9)
    pdf.set_text_color(120, 120, 120)
    pdf.cell(0, 6, "Report prepared for Week 1 - Unity Game Development Trainee Internship", align="C")

    pdf.output(OUTPUT)
    print(f"PDF generated: {OUTPUT}")


if __name__ == "__main__":
    build_pdf()
