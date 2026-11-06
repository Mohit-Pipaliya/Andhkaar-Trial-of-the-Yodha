using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.AI;
using TMPro;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Testing & Debugging")]
    [Tooltip("Agar isko tick karoge toh har baar game start hone par purana save delete ho jayega (Fresh Start).")]
    public bool resetSaveOnPlay = false;
    // ═══════════════════════════════════════════════════════════
    //  MOVEMENT
    // ═══════════════════════════════════════════════════════════
    [Header("Movement Speeds")]
    public float walkSpeed   = 4f;
    public float runSpeed    = 8f;
    public float crouchSpeed = 2.5f;

    [Header("Torch Movement Speeds")]
    [Tooltip("Torch haath mein hone par walk speed (normal se thodi kam — realistic)")]
    public float torchWalkSpeed = 3f;   // Thodi slow — lamp sambhal ke chalna
    [Tooltip("Torch haath mein hone par run speed (normal run se thodi kam)")]
    public float torchRunSpeed  = 5.5f; // Lamp leke full sprint nahi ho sakti

    [Header("Turn Settings")]
    public float turnSmoothSpeed = 10f;

    // ═══════════════════════════════════════════════════════════
    //  JUMP & GRAVITY  (Industry-standard dual-gravity system)
    // ═══════════════════════════════════════════════════════════
    [Header("Jump Settings")]
    [Tooltip("Maximum jump height in Unity units")]
    public float jumpHeight = 1.4f;

    [Tooltip("Time (seconds) to reach jump apex — controls how snappy the jump feels")]
    public float timeToApex = 0.38f;           // Shorter = snappier, Longer = floaty

    [Tooltip("Multiplier applied to gravity while FALLING — makes landing feel heavy & realistic")]
    public float fallGravityMultiplier = 2.4f; // >= 1. Increase for heavier landings

    [Tooltip("Multiplier applied when player releases Space early (short jump)")]
    public float lowJumpMultiplier = 2.0f;     // Creates variable jump height on tap vs hold

    [Tooltip("Extra downward snap when landing (prevents tiny hops on slopes)")]
    public float groundSnapForce = 8f;

    // ═══════════════════════════════════════════════════════════
    //  ATTACK SYSTEM
    // ═══════════════════════════════════════════════════════════
    [Header("Attack Settings")]
    [Tooltip("Animation playback speed multiplier — 1.5 = 50% faster, 2.0 = double speed")]
    [Range(0.5f, 3f)]
    public float attackAnimSpeed = 1.6f;        // Snappy, responsive feel

    [Tooltip("How many seconds the combo window stays open after each hit")]
    public float comboWindowTime = 0.6f;        // Press again within this time to combo

    [Tooltip("Movement speed multiplied during attack (0 = frozen, 0.3 = slow walk)")]
    [Range(0f, 1f)]
    public float attackMovementFraction = 0.25f; // Character slows during strike = weight

    // ─── Private Attack State ──────────────────────────────────
    private bool  isAttacking       = false;    // Are we currently in an attack animation?
    private bool  isSpecialActionPlaying = false; // Are we locked in a cutscene/special action?
    private bool  comboQueued       = false;    // Next attack buffered during current swing?
    private float comboWindowTimer  = 0f;       // Countdown for combo window
    private int   currentComboStep  = 0;        // Which hit in the combo (1, 2, 3)
    private const int maxComboSteps = 3;        // 3-hit combo chain

    // ─── Coyote Time ───────────────────────────────────────────
    [Header("Coyote Time")]
    [Tooltip("Seconds after walking off a ledge where jump is still allowed")]
    public float coyoteTime = 0.18f;

    // ─── Jump Buffer ───────────────────────────────────────────
    [Header("Jump Buffer")]
    [Tooltip("Seconds before landing where pressing Space is remembered & auto-executes on touch")]
    public float jumpBufferTime = 0.2f;

    // ─── Air Control ───────────────────────────────────────────
    [Header("Air Control")]
    [Range(0f, 1f)]
    [Tooltip("0 = no air control (realistic), 1 = full control (arcade). 0.35 is a good balance.")]
    public float airControlAmount = 0.4f;

    // ═══════════════════════════════════════════════════════════
    //  CROUCH / SLIDE
    // ═══════════════════════════════════════════════════════════
    [Header("Crouch Settings")]
    [Range(0.1f, 1f)]
    public float slideHeightMultiplier = 0.5f;
    private bool isSliding = false;
    private bool isCrouchWalking = false; // Crouch karte hue movement

    // ═══════════════════════════════════════════════════════════
    //  QUEST & SPECIAL ABILITIES
    // ═══════════════════════════════════════════════════════════
    [Header("Quest Progression (Gates & Objects)")]
    [Tooltip("Drag the 3 specific objects from the scene here")]
    public GameObject[] specialObjects = new GameObject[3];
    
    [Tooltip("Drag the 3 Pray Trigger objects (where player presses M) here")]
    public Transform[] prayTriggers = new Transform[3];
    
    [Tooltip("Drag the 3 Placed Trigger objects (where object flies to) here")]
    public Transform[] placeTriggers = new Transform[3];
    
    [Tooltip("Drag the 3 Gate objects here (they will slide down when opened)")]
    public GameObject[] gates = new GameObject[3];

    [Header("Quest — Tuning")]
    [Tooltip("Crystal absorb hone mein kitna time lagega (seconds)")]
    public float absorbDuration = 1.5f;          // Slow absorption

    [Tooltip("Point Light ke fly hone ki speed (seconds)")]
    public float lightFlyDuration = 2.5f;        // Exact match with magic animation length

    [Tooltip("Jab Light haath se nikle tab intensity kitni hogi (bright glow)")]
    public float lightReleaseIntensity = 15f;

    [Tooltip("Jab Light placed ho jaye tab normal intensity kitni hogi")]
    public float lightNormalIntensity = 3f;

    [Tooltip("Gate 1 kitne units neeche slide karega")]
    public float gate1SlideDistance = 8f;
    [Tooltip("Gate 2 kitne units neeche slide karega")]
    public float gate2SlideDistance = 8f;
    [Tooltip("Gate 3 kitne units neeche slide karega")]
    public float gate3SlideDistance = 8f;

    // Hidden tracking states
    [HideInInspector] public bool[] hasSpecialObject = new bool[3];
    [HideInInspector] public bool[] inPrayTrigger = new bool[3];
    [HideInInspector] public Transform[] specialLights = new Transform[3];

    // ═══════════════════════════════════════════════════════════
    //  ITEM STATES & WEAPONS
    // ═══════════════════════════════════════════════════════════
    public enum WeaponType { None = 0, Sword1 = 1, Sword2 = 2 }

    [Header("Weapons")]
    public WeaponType currentWeapon = WeaponType.None;
    public bool hasSword1 = false;
    public bool hasSword2 = false;
    
    [Tooltip("Hand bone pe attached Sword 1 GameObject")]
    public GameObject handSword1Object;
    [Tooltip("Hand bone pe attached Sword 2 GameObject")]
    public GameObject handSword2Object;

    [Header("Torch States")]
    public bool hasTorch = false;
    [HideInInspector] public int activeCombatEngagements = 0;

    [Header("Torch — Dual Object Setup")]
    [Tooltip("Player ke haath ka lamp GameObject — Editor mein position set karo, script shuru mein hide kar degi")]
    public GameObject handLampObject;       // Haath waala lamp (pre-positioned)

    [Header("Dynamic Oil Cane Spawning")]
    [Tooltip("Prefab for the Oil Cane to spawn when light runs out")]
    public GameObject oilCanePrefab;
    [Tooltip("How far ahead of the player the Oil Cane should spawn")]
    public float oilCaneSpawnDistance = 15f;
    private bool hasSpawnedOilCane = false;

    [Tooltip("Hand Lamp ke andar jo Point Light hai, use yahan drag karein")]
    public Light handLampLight;

    [Tooltip("Kitni tezi se oil kam hoga (Bada number = jaldi khatam). 0.02 = bohot slow drain (~80 mins).")]
    public float lampDrainRate = 0.02f; 
    [Tooltip("Light ki starting intensity.")]
    public float maxLampIntensity = 100f;

    [Tooltip("Hand bone Transform jis par HandLamp ko attach karna hai (e.g. Left Hand bone)")]
    public Transform handBone;              // Inspector mein Left Hand bone drag karo

    [Tooltip("Dusre haath ka bone (e.g. Right Hand bone) dono haath se magic nikalne ke liye")]
    public Transform otherHandBone;

    [Tooltip("Player kitni door se lamp uthaa sakta hai (metres)")]
    public float pickupRange = 5.0f;        // Badhao to zyada dur se pickup ho

    [Header("Environment Lighting")]
    [Tooltip("Scene ka base ambient color")]
    public Color ambientBaseColor = new Color(0.22f, 0.24f, 0.31f, 1f);
    [Tooltip("Scene ka base ambient intensity")]
    public float ambientBaseIntensity = 1.15f;
    [Tooltip("Raat ki chaandni — path aage tak dikhe")]
    public float moonLightIntensity = 0.28f;
    [Tooltip("Torch ke saath ambient kitna warm hoga")]
    public float torchAmbientBoost = 0.18f;
    [Tooltip("Special action ke dauran environment ko thoda brighter karne ka effect")]
    public float specialAmbientBoost = 0.12f;


    [Header("Player Health & Events")]
    public float maxHealth = 1000f;
    public float currentHealth;
    
    // --- UI Events (Event-Driven Architecture) ---
    public static event Action<float> OnHealthChanged;
    public static event Action<float> OnOilChanged;
    public static event Action<bool> OnTorchStateChanged; // Naya event add kiya
    public static event Action OnPlayerDied;
    public static event Action OnPlayerRespawned; // R dabane par player respawn hone ka event

    [Header("Respawn & Checkpoint Settings")]
    [Tooltip("Scene ke banaye huye Spawn Points (Transforms) yahan drag karein.")]
    public Transform[] spawnPoints;

    [Tooltip("Spawn point ke kitne paas aane par use 'Crossed / Reached' mana jaye (meters)")]
    public float checkpointDetectionRadius = 6f;

    [Tooltip("Agar koi spawn point set na ho toh marne par kitne meter door fallback spawn hoga")]
    public float respawnDistance = 4.5f;

    private Vector3 deathPosition = Vector3.zero;
    private Coroutine deathCoroutine;
    private Coroutine revivalRoutine;
    // Track jo spawn points player ab tak cross kar chuka hai
    private readonly System.Collections.Generic.HashSet<Transform> crossedSpawnPoints = new System.Collections.Generic.HashSet<Transform>();
    // Breadcrumb trail: Player jaha se guzar chuka hai un jagaho ka track
    private readonly System.Collections.Generic.List<Vector3> traversedPathHistory = new System.Collections.Generic.List<Vector3>(600);

    [Header("UI Prompts")]
    [Tooltip("Drag the GameObject/Image that shows 'Press O'")]
    public GameObject pressOUI;
    [Tooltip("Drag the GameObject/Image that shows 'Press E'")]
    public GameObject pressEUI;
    [Tooltip("Drag the GameObject/Image that shows 'Press M' (Optional)")]
    public GameObject pressMUI;
    [Tooltip("Drag the GameObject/Image that shows 'Press 1 to Put Your Sword'")]
    public GameObject pressPut1UI;

    private GameObject activeGroundLamp;    // Jo lamp pick kiya gaya hai (drop reference)
    private Coroutine lampDrainCoroutine;   // Coroutine reference for lamp point light intensity drain
    private GameObject activeGroundSword1;  // Drop reference for sword 1
    private GameObject activeGroundSword2;  // Drop reference for sword 2

    // Performance caches (AAA: zero-allocation hot paths)
    private Transform cachedCameraTransform;
    private static readonly Collider[] overlapBuffer = new Collider[32];
    private int cachedOverlapCount;
    private float lastOverlapScanTime = -999f;
    private const float OverlapScanInterval = 0.12f;
    private float lastLightingUpdateTime = -999f;
    private const float LightingUpdateInterval = 0.15f;

    // AAA Perf: Dirty-write lighting cache — only push to GPU when state changes
    private bool  cachedHasTorch         = false;
    private bool  cachedIsSpecialPlaying  = false;
    private bool  cachedIsAttacking       = false;
    private float cachedLampIntensity     = -1f;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE — PHYSICS
    // ═══════════════════════════════════════════════════════════
    private CharacterController controller;
    private Animator animator;
    private Light sceneDirectionalLight;

    // ─── Animator dirty-write cache (avoid SetBool every frame when value unchanged) ───
    private bool _prevIsWalking  = false;
    private bool _prevIsRunning  = false;
    private bool _prevHasTorch   = false;
    private bool _prevIsGrounded = true;
    private bool isRunningAnim   = false; // Persistent across frames for hysteresis

    private float originalHeight;
    private Vector3 originalCenter;

    // Vertical velocity (Y only — horizontal handled separately for air control)
    private float verticalVelocity;

    // Horizontal velocity (X, Z) stored for momentum-based air control
    private Vector3 horizontalVelocity;

    // Derived from jumpHeight & timeToApex using kinematic equations
    private float jumpVelocity;   // v = 2h / t
    private float baseGravity;    // g = -2h / t²

    // ─── Grounded ──────────────────────────────────────────────
    private bool isGrounded;
    private float coyoteTimer;       // Counts down after leaving ground
    private bool wasGroundedLastFrame;

    // ─── Jump Buffer ───────────────────────────────────────────
    private float jumpBufferCounter; // Counts down after Space pressed

    // ─── Jump state ────────────────────────────────────────────
    private bool isJumping;          // True while in the air after a jump
    private bool jumpHeld;           // Is Space currently held?

    // ═══════════════════════════════════════════════════════════
    //  INPUT ACTIONS
    // ═══════════════════════════════════════════════════════════
    private InputAction moveAction;
    private InputAction runAction;
    private InputAction jumpAction;
    private InputAction slideAction;
    private InputAction lookAction;
    private InputAction attackAction;
    private InputAction interactWeaponAction;
    private InputAction interactLampAction;
    private InputAction dropWeaponAction;
    private InputAction dropLampAction;
    private InputAction specialAction;
    private InputAction prayAction;

    // ── Weapon Inputs ──────────────────────────────────────────
    private InputAction equipNoneAction;
    private InputAction equipSword1Action;
    private InputAction equipSword2Action;

    // ── Public property ─────────────────────────────────────────
    public bool IsSliding => isSliding;
    public bool IsAttacking => isAttacking;
    public bool IsSpecialActionPlaying => isSpecialActionPlaying;
    public bool IsBlocking => false; // Added to satisfy EnemyBase references
    public bool IsCrouchWalking => isCrouchWalking; // Camera zoom-out ke liye
    // Torch Idle: torch hai + koi movement nahi + grounded + attack nahi
    public bool IsTorchIdle => hasTorch && !isCrouchWalking && !isAttacking && !isSpecialActionPlaying;
    // Pray trigger me hai?
    public bool IsInPrayTrigger
    {
        get {
            for (int i = 0; i < inPrayTrigger.Length; i++)
                if (inPrayTrigger[i]) return true;
            return false;
        }
    }

    // Magic animation ke dauran active gate ki world position
    public Vector3 ActiveGatePosition
    {
        get {
            for (int i = 0; i < inPrayTrigger.Length; i++)
                if (inPrayTrigger[i] && gates[i] != null)
                    return gates[i].transform.position;
            return transform.position;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  AWAKE — Setup Input
    // ═══════════════════════════════════════════════════════════
    void Awake()
    {
        maxHealth = 1000f; // Forcefully set to 1000 in Awake taaki UIManager padhne se pehle update ho jaye
        moveAction = new InputAction("Move", type: InputActionType.Value);
        
        // Add WASD bindings
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w")
            .With("Down",  "<Keyboard>/s")
            .With("Left",  "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Add Arrow Key bindings
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        runAction      = new InputAction("Run",      type: InputActionType.Button, binding: "<Keyboard>/leftShift");
        jumpAction     = new InputAction("Jump",     type: InputActionType.Button, binding: "<Keyboard>/space");
        slideAction    = new InputAction("Slide",    type: InputActionType.Button, binding: "<Keyboard>/leftCtrl");
        attackAction         = new InputAction("Attack",         type: InputActionType.Button, binding: "<Mouse>/leftButton");
        interactWeaponAction = new InputAction("InteractWeapon", type: InputActionType.Button, binding: "<Keyboard>/e");
        interactLampAction   = new InputAction("InteractLamp",   type: InputActionType.Button, binding: "<Keyboard>/o");
        dropWeaponAction     = new InputAction("DropWeapon",     type: InputActionType.Button, binding: "<Keyboard>/g");
        dropLampAction       = new InputAction("DropLamp",       type: InputActionType.Button, binding: "<Keyboard>/l");
        lookAction           = new InputAction("Look",           binding: "<Pointer>/delta");
        specialAction        = new InputAction("Special",        type: InputActionType.Button, binding: "<Keyboard>/m");
        prayAction           = new InputAction("Pray",           type: InputActionType.Button, binding: "<Keyboard>/p");

        equipNoneAction   = new InputAction("EquipNone",   type: InputActionType.Button, binding: "<Keyboard>/1");
        equipSword1Action = new InputAction("EquipSword1", type: InputActionType.Button, binding: "<Keyboard>/2");
        equipSword2Action = new InputAction("EquipSword2", type: InputActionType.Button, binding: "<Keyboard>/3");

        // UI Prompts — Awake mein hi hide karo (Start se pehle) taaki ek bhi frame na dikhe
        if (pressPut1UI != null) pressPut1UI.SetActive(false);
        if (pressMUI != null)    pressMUI.SetActive(false);
        if (pressOUI != null)    pressOUI.SetActive(false);
        if (pressEUI != null)    pressEUI.SetActive(false);
    }

    void OnEnable()
    {
        moveAction.Enable(); runAction.Enable(); jumpAction.Enable();
        slideAction.Enable(); lookAction.Enable(); attackAction.Enable();
        interactWeaponAction.Enable(); interactLampAction.Enable(); dropWeaponAction.Enable(); dropLampAction.Enable();
        equipNoneAction.Enable(); equipSword1Action.Enable(); equipSword2Action.Enable();
        specialAction.Enable();
    }

    void OnDisable()
    {
        // Scene change ya script disable hone par equipment save karo taaki agle scene me retain rahe
        if (currentHealth > 0)
        {
            GameSaveManager.SaveGameState(this);
        }

        moveAction.Disable(); runAction.Disable(); jumpAction.Disable();
        slideAction.Disable(); lookAction.Disable(); attackAction.Disable();
        interactWeaponAction.Disable(); interactLampAction.Disable(); dropWeaponAction.Disable(); dropLampAction.Disable();
        equipNoneAction.Disable(); equipSword1Action.Disable(); equipSword2Action.Disable();
        specialAction.Disable();
    }

    // ═══════════════════════════════════════════════════════════
    //  START
    // ═══════════════════════════════════════════════════════════
    void Start()
    {
        // --- AUTO-SETUP CAMERA (Force correct 3rd person behavior) ---
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();
        if (mainCam != null)
        {
            mainCam.transform.parent = null; // Unparent to prevent spinning
            
            // Disable CinemachineBrain if it exists to allow manual control
            Behaviour brain = mainCam.GetComponent("CinemachineBrain") as Behaviour;
            if (brain != null) brain.enabled = false;

            // Destroy any conflicting or legacy TMPro camera controllers on mainCam
            MonoBehaviour[] allScripts = mainCam.GetComponents<MonoBehaviour>();
            for (int i = 0; i < allScripts.Length; i++)
            {
                MonoBehaviour mb = allScripts[i];
                if (mb == null) continue;
                string typeName = mb.GetType().FullName;
                if (typeName != null && (typeName.Contains("TMPro.Examples.CameraController") || (typeName.EndsWith("CameraController") && typeName != typeof(CameraController).FullName)))
                {
                    Debug.Log($"[PlayerController] Removed legacy conflicting camera controller: {typeName}");
                    Destroy(mb);
                }
            }

            // Add our CameraController if it's missing
            CameraController camController = mainCam.GetComponent<CameraController>();
            if (camController == null)
            {
                camController = mainCam.gameObject.AddComponent<CameraController>();
            }
            // Always ensure the target is this player and frame immediately!
            camController.target = this.transform;
            camController.SnapToTarget();
        }
        // -------------------------------------------------------------
        
        // --- SETUP VALIDATION (Helps find missing Inspector settings) ---
        if (pressOUI == null) Debug.LogError("SETUP ERROR: 'Press O UI' is not assigned in PlayerController!");
        if (pressEUI == null) Debug.LogError("SETUP ERROR: 'Press E UI' is not assigned in PlayerController!");
        
        GameObject lamp = GameObject.FindWithTag("OilLamp");
        if (lamp == null) Debug.LogWarning("SETUP WARNING: No object found with tag 'OilLamp'. Did you forget to set the tag on the lamp?");
        else if (lamp.GetComponent<Collider>() == null) Debug.LogWarning("SETUP WARNING: The object tagged 'OilLamp' does not have a Collider! Player won't detect it.");

        GameObject sword = GameObject.FindWithTag("Sword 1");
        if (sword == null) Debug.LogWarning("SETUP WARNING: No object found with tag 'Sword 1'. Did you forget to set the tag?");
        // ----------------------------------------------------------------

        controller           = GetComponent<CharacterController>();
        animator             = GetComponentInChildren<Animator>();

        if (controller == null) Debug.LogError("PlayerController: CharacterController is missing!");
        if (animator == null) Debug.LogWarning("PlayerController: Animator is missing!");
        if (Camera.main == null) Debug.LogWarning("PlayerController: Main Camera is missing!");

        currentHealth = maxHealth; // Setup health default
        
        // --- Cross-Scene Teleport & Fresh Start Logic ---
        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isCrossSceneTeleport = PlayerPrefs.GetInt("IsCrossSceneTeleport", 0) == 1;
        // Consume the transition flags immediately so normal reloads/restarts aren't stuck in teleport mode
        PlayerPrefs.SetInt("IsCrossSceneTeleport", 0);
        PlayerPrefs.SetInt("SkipMenuOnRetry", 0);
        PlayerPrefs.Save();

        // Level 1 fresh start (unless teleported into Level 1 from another level)
        if (activeSceneName == "Level 1" && !isCrossSceneTeleport)
        {
            GameSaveManager.ClearSave();
            hasTorch = false;
            hasSword1 = false;
            hasSword2 = false;
            currentWeapon = WeaponType.None;
        }

        // Auto-find missing weapon/torch objects if unassigned
        if (handLampObject == null || handLampLight == null || handSword1Object == null || handSword2Object == null)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                string lowerName = t.name.ToLower();
                if (handLampObject == null && (lowerName.Contains("lamp") || lowerName.Contains("torch")))
                {
                    handLampObject = t.gameObject;
                    if (handLampLight == null) handLampLight = t.GetComponentInChildren<Light>();
                }
                if (handSword1Object == null && (lowerName.Contains("sword 1") || lowerName.Contains("sword1")))
                    handSword1Object = t.gameObject;
                if (handSword2Object == null && (lowerName.Contains("sword 2") || lowerName.Contains("sword2")))
                    handSword2Object = t.gameObject;
            }
        }

        // Ensure both hand swords have trigger colliders & SwordDamage components
        EnsureSwordComponents(handSword1Object);
        EnsureSwordComponents(handSword2Object);

        // In Scene 2 / Scene 3 (or if teleported), load previously saved inventory
        if (activeSceneName != "Level 1" || isCrossSceneTeleport)
        {
            GameSaveManager.LoadGameState(this);
        }

        // Harmonize lamp drain rate across all levels so it never drains faster than Scene 1
        if (lampDrainRate > 3f)
        {
            lampDrainRate = 3f;
        }

        InitializeSpawnPoints();
        if (animator != null)
            animator.applyRootMotion = false;

        // Safe skin width — prevents sliding on slopes
        controller.skinWidth = controller.radius * 0.1f;

        originalHeight = controller.height;
        originalCenter = controller.center;

        // ── Hand Lamp visual sync ──────────────────────
        if (handLampObject != null)
            handLampObject.SetActive(hasTorch);

        if (handLampLight != null)
        {
            handLampLight.enabled = hasTorch;
            if (hasTorch)
            {
                if (lampDrainCoroutine != null) StopCoroutine(lampDrainCoroutine);
                lampDrainCoroutine = StartCoroutine(DrainLampOil());
            }
        }

        // Notify UI & Animator immediately about Torch state & Health
        OnHealthChanged?.Invoke(currentHealth);
        if (hasTorch)
        {
            OnTorchStateChanged?.Invoke(true);
            if (handLampLight != null)
            {
                OnOilChanged?.Invoke(handLampLight.intensity);
            }
            if (animator != null)
            {
                animator.SetBool("HasTorch", activeCombatEngagements == 0);
            }
        }
        else
        {
            OnTorchStateChanged?.Invoke(false);
            if (animator != null)
            {
                animator.SetBool("HasTorch", false);
            }
        }

        // ── Hand Sword visual sync ──────────────────────
        // Player starts with empty hands if neither sword was collected
        if (!hasSword1 && !hasSword2)
        {
            currentWeapon = WeaponType.None;
        }
        else if (currentWeapon == WeaponType.None)
        {
            if (hasSword2) currentWeapon = WeaponType.Sword2;
            else if (hasSword1) currentWeapon = WeaponType.Sword1;
        }
        UpdateWeaponState(currentWeapon);

        // ── UI Prompts shuru mein hide karo (sabhi bacchon ko chupa do) ──
        if (pressOUI != null)
        {
            Transform uiParent = pressOUI.transform.parent;
            if (uiParent != null)
            {
                foreach (Transform child in uiParent)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (pressEUI != null)    pressEUI.SetActive(false);
            if (pressMUI != null)    pressMUI.SetActive(false);
            if (pressPut1UI != null) pressPut1UI.SetActive(false);
        }

        // ── Derive physics values from designer-friendly inputs ──
        // Using kinematic equations:
        //   jumpVelocity = (2 * h) / t
        //   baseGravity  = -(2 * h) / t²
        RecalculateJumpPhysics();

        FindSceneDirectionalLight();
        ApplyEnvironmentLighting(true);

        CacheMainCamera();

        // --- SAFE FALLBACK: If UIManager is missing in the scene, spawn it from Resources ---
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            GameObject uiPrefab = Resources.Load<GameObject>("Game UI");
            if (uiPrefab != null)
            {
                GameObject spawnedUI = Instantiate(uiPrefab);
                spawnedUI.name = "Game UI";
                uiManager = spawnedUI.GetComponentInChildren<UIManager>();
                Debug.Log("[PlayerController] Automatically spawned Game UI from Resources.");
            }
        }

        if (uiManager == null)
        {
            Debug.LogWarning("PlayerController: UIManager scene mein nahi mila! Input by default unlock kar raha hu.");
            UIManager.isGameActive = true;
        }

        // Initialize traversed path breadcrumb history with starting point
        traversedPathHistory.Add(transform.position);
    }

    // Call this if you change jumpHeight or timeToApex at runtime
    void RecalculateJumpPhysics()
    {
        jumpVelocity = (2f * jumpHeight) / timeToApex;
        baseGravity  = -(2f * jumpHeight) / (timeToApex * timeToApex);
    }

    private void FindSceneDirectionalLight()
    {
        if (sceneDirectionalLight != null) return;

        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in allLights)
        {
            if (light.type == LightType.Directional)
            {
                sceneDirectionalLight = light;
                break;
            }
        }
    }

    private void CacheMainCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();
        cachedCameraTransform = mainCam != null ? mainCam.transform : null;
    }

    private int ScanNearbyColliders(bool forceRescan)
    {
        if (!forceRescan && Time.time < lastOverlapScanTime + OverlapScanInterval)
            return cachedOverlapCount;

        lastOverlapScanTime = Time.time;
        cachedOverlapCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            pickupRange,
            overlapBuffer,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);
        return cachedOverlapCount;
    }

    private void ApplyEnvironmentLighting(bool forceUpdate = false)
    {
        if (!forceUpdate && Time.time < lastLightingUpdateTime + LightingUpdateInterval)
            return;

        // AAA Perf: Dirty-write — only push to RenderSettings (GPU sync) when state actually changes
        float lampIntensity = (hasTorch && handLampLight != null) ? handLampLight.intensity : 0f;
        bool stateChanged = forceUpdate
            || hasTorch           != cachedHasTorch
            || isSpecialActionPlaying != cachedIsSpecialPlaying
            || isAttacking        != cachedIsAttacking
            || Mathf.Abs(lampIntensity - cachedLampIntensity) > 0.5f;

        lastLightingUpdateTime = Time.time;

        if (!stateChanged) return; // Skip GPU write entirely if nothing changed

        // Cache new state
        cachedHasTorch        = hasTorch;
        cachedIsSpecialPlaying = isSpecialActionPlaying;
        cachedIsAttacking     = isAttacking;
        cachedLampIntensity   = lampIntensity;

        float ambientIntensity = ambientBaseIntensity;
        Color ambientColor = ambientBaseColor;

        if (hasTorch && handLampLight != null && handLampLight.isActiveAndEnabled && handLampLight.intensity > 0f)
        {
            ambientIntensity += torchAmbientBoost;
            ambientColor = Color.Lerp(ambientColor, new Color(0.34f, 0.27f, 0.20f, 1f), 0.35f);
        }

        if (isSpecialActionPlaying || isAttacking)
        {
            ambientIntensity += specialAmbientBoost;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor    = ambientColor * (ambientIntensity * 1.05f);
        RenderSettings.ambientEquatorColor = ambientColor * (ambientIntensity * 0.75f);
        RenderSettings.ambientGroundColor  = ambientColor * (ambientIntensity * 0.45f);

        if (sceneDirectionalLight != null)
        {
            float moonIntensity = moonLightIntensity;
            if (hasTorch) moonIntensity *= 0.75f;
            sceneDirectionalLight.intensity = moonIntensity;
            sceneDirectionalLight.color = Color.Lerp(new Color(0.75f, 0.78f, 0.9f, 1f), new Color(0.95f, 0.82f, 0.65f, 1f), hasTorch ? 0.2f : 0.08f);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════════
    void Update()
    {
        // Agar player mar gaya hai, to bas usko zameen par girne do, aur R dabane par 10 meter door respawn karo!
        if (currentHealth <= 0)
        {
            if (controller != null && !controller.isGrounded)
            {
                verticalVelocity += baseGravity * Time.deltaTime;
                controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            }
            else
            {
                if (deathPosition == Vector3.zero)
                    deathPosition = transform.position;
            }

            // Marne par 'R' dabane se jaha mara tha waha se 10 meter door respawn ho
            if (IsRPressedThisFrame())
            {
                RespawnNearDeathPoint();
            }
            return;
        }

        ApplyEnvironmentLighting();

        if (cachedCameraTransform == null)
            CacheMainCamera();

        // ─── 1. READ INPUT ──────────────────────────────────────
        Vector2 rawInput     = moveAction.ReadValue<Vector2>();
        bool    isRunning    = runAction.ReadValue<float>()  > 0.1f;
        bool    isSlideHeld  = slideAction.ReadValue<float>() > 0.1f;
        bool    jumpPressed  = jumpAction.triggered;
               jumpHeld      = jumpAction.ReadValue<float>() > 0.1f;

        Vector3 inputDir = new Vector3(rawInput.x, 0f, rawInput.y).normalized;
        bool    hasInput = rawInput.sqrMagnitude > 0.01f;

        // ─── BREADCRUMB TRAIL (Record visited path points) ─────
        if (currentHealth > 0 && controller != null && controller.enabled && controller.isGrounded && Mathf.Abs(verticalVelocity) <= 1.0f && !isJumping)
        {
            if (traversedPathHistory.Count == 0 || Vector3.Distance(transform.position, traversedPathHistory[traversedPathHistory.Count - 1]) >= 0.8f)
            {
                traversedPathHistory.Add(transform.position);
                if (traversedPathHistory.Count > 600)
                {
                    traversedPathHistory.RemoveAt(0);
                }
            }
        }

        // ─── CHECKPOINT / SPAWN POINT CROSS TRACKING ─────────
        if (currentHealth > 0)
        {
            UpdateCrossedSpawnPoints();
        }

        // ─── IDLE CHECK & HEALTH REGENERATION ───────────────────
        bool isIdle = !hasInput && !isAttacking && !jumpPressed && controller.isGrounded && !isSpecialActionPlaying;
        if (isIdle)
        {
            idleTimer += Time.deltaTime;
        }
        else
        {
            idleTimer = 0f;
        }

        // Regen only if health is below the threshold AND player is idle
        float regenTargetHealth = maxHealth * lowHealthRegenThreshold;
        if (currentHealth < regenTargetHealth && currentHealth > 0)
        {
            if (Time.time >= lastDamageTime + healthRegenDelay && idleTimer >= idleTimeToRegen)
            {
                currentHealth += healthRegenRate * Time.deltaTime;
                if (currentHealth > regenTargetHealth) currentHealth = regenTargetHealth; // Limit to threshold
                
                // UI update smoothly
                OnHealthChanged?.Invoke(currentHealth);
            }
        }

        // If a special action is playing, player is dead, or game is not active (paused/loading), block all input
        if (isSpecialActionPlaying || isFrozen || !UIManager.isGameActive || currentHealth <= 0)
        {
            hasInput = false;
            isRunning = false;
            isSlideHeld = false;
            jumpPressed = false;
            jumpHeld = false;
            rawInput = Vector2.zero;
            inputDir = Vector3.zero;
        }

        // ─── 2. GROUNDED CHECK ──────────────────────────────────
        // AAA Perf: CheckSphere only when CharacterController isn't grounded — skips physics query ~50% frames
        float   capsuleRadius = controller.radius * transform.lossyScale.x;
        Vector3 capsuleBottom = transform.position + controller.center * transform.lossyScale.y
                                - Vector3.up * ((controller.height * 0.5f * transform.lossyScale.y) - capsuleRadius);

        bool groundedNow = controller.isGrounded ||
                           (!controller.isGrounded && Physics.CheckSphere(capsuleBottom, capsuleRadius + 0.05f,
                               Physics.AllLayers, QueryTriggerInteraction.Ignore));

        // Snap velocity to ground to prevent bouncing
        if (groundedNow && verticalVelocity < 0f)
            verticalVelocity = -groundSnapForce;

        // Landed this frame?
        bool justLanded = groundedNow && !wasGroundedLastFrame;
        wasGroundedLastFrame = groundedNow;

        // ─── Coyote Time ────────────────────────────────────────
        if (groundedNow)
        {
            coyoteTimer = coyoteTime;
            isJumping   = false;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        isGrounded = coyoteTimer > 0f;

        // ─── 3. JUMP BUFFER ─────────────────────────────────────
        // Player presses Space — start countdown
        if (jumpPressed)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // ─── 4. CROUCH / SLIDE ──────────────────────────────────
        // Only allow crouch/slide if player has collected the oil lamp (hasTorch is true)
        if (isSlideHeld && groundedNow && !isSliding && hasTorch)
        {
            isSliding = true;
            float targetHeight = originalHeight * slideHeightMultiplier;
            controller.height  = targetHeight;
            float bottom       = originalCenter.y - (originalHeight / 2f);
            controller.center  = new Vector3(originalCenter.x, bottom + targetHeight / 2f, originalCenter.z);
            animator.SetBool("IsSliding", true);
        }
        else if (!isSlideHeld && isSliding)
        {
            float worldH = originalHeight * transform.lossyScale.y;
            float origin = 0.1f * transform.lossyScale.y;
            controller.enabled = false;
            bool canStand = !Physics.Raycast(transform.position + Vector3.up * origin, Vector3.up, worldH);
            controller.enabled = true;

            if (canStand)
            {
                isSliding             = false;
                isCrouchWalking       = false;
                controller.height     = originalHeight;
                controller.center     = originalCenter;
                animator.SetBool("IsSliding", false);
            }
        }

        // Crouch+walk: jab slide chal raha ho aur player move kar raha ho
        if (isSliding)
            isCrouchWalking = hasInput;
        else
            isCrouchWalking = false;

        // ─── 5. ATTACK ──────────────────────────────────────────
        HandleAttack(groundedNow);

        // ─── 6. HORIZONTAL MOVEMENT (AAA Smooth) ────────────────
        Vector3 targetHorizontal = Vector3.zero;

        // Compute current speed once here — used by turn smoothing AND animation section below.
        // Uses last frame's horizontalVelocity which is fine since it's smoothed.
        float currentHorizSpeed = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z).magnitude;

        if (hasInput && !isSliding)
        {
            Vector3 moveDir = Vector3.zero;
            Transform camTransform = cachedCameraTransform;

            if (camTransform != null)
            {
                // Movement relative to camera
                Vector3 camF = camTransform.forward; camF.y = 0f; camF.Normalize();
                Vector3 camR = camTransform.right;   camR.y = 0f; camR.Normalize();
                moveDir = (camF * rawInput.y + camR * rawInput.x).normalized;

                // ─── AAA Turn Speed — scales with movement speed ─────────────────
                // Fast sprint = quick snappy turn. Slow walk = gentle lean.
                // This matches how real bodies (and Uncharted) handle turning momentum.
                float speedFactor = Mathf.Clamp01(currentHorizSpeed / runSpeed);
                float effectiveTurnSpeed = Mathf.Lerp(turnSmoothSpeed * 0.4f,
                                                       turnSmoothSpeed * 1.1f,
                                                       speedFactor);
                if (moveDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(moveDir), Time.deltaTime * effectiveTurnSpeed);
                }
            }
            else
            {
                moveDir = new Vector3(rawInput.x, 0f, rawInput.y).normalized;

                float speedFactor2 = Mathf.Clamp01(currentHorizSpeed / runSpeed);
                float effectiveTurnSpeed2 = Mathf.Lerp(turnSmoothSpeed * 0.4f,
                                                        turnSmoothSpeed * 1.1f,
                                                        speedFactor2);
                if (moveDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(moveDir), Time.deltaTime * effectiveTurnSpeed2);
                }
            }

            // Realistic heavy-lamp feel
            float currentSpeed;
            if (isSliding)
                currentSpeed = crouchSpeed;
            else if (hasTorch)
                currentSpeed = isRunning ? torchRunSpeed : torchWalkSpeed;
            else
                currentSpeed = isRunning ? runSpeed : walkSpeed;
                
            if (isAttacking) currentSpeed *= attackMovementFraction;

            targetHorizontal = moveDir * currentSpeed;
        }

        // Smooth acceleration and deceleration for heavy AAA feel
        if (groundedNow)
        {
            float groundAccel = hasInput ? 6f : 8f; 
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontal, Time.deltaTime * groundAccel);
        }
        else
        {
            float lerpSpeed = airControlAmount * 4f; 
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontal, Time.deltaTime * lerpSpeed);
        }

        if (controller != null && controller.enabled)
        {
            controller.Move(horizontalVelocity * Time.deltaTime);
        }

        // ─── 7. JUMP EXECUTION ──────────────────────────────────
        bool canJump = jumpBufferCounter > 0f && isGrounded && !isSliding;

        if (canJump)
        {
            verticalVelocity  = jumpVelocity;
            isJumping         = true;
            coyoteTimer       = 0f;
            jumpBufferCounter = 0f;
            animator.SetTrigger("Jump");
        }

        // ─── 8. DUAL GRAVITY ────────────────────────────────────
        float activeGravity = baseGravity;

        if (isJumping || !groundedNow)
        {
            if (verticalVelocity < 0f)
            {
                activeGravity = baseGravity * fallGravityMultiplier;
            }
            else if (verticalVelocity > 0f && !jumpHeld)
            {
                activeGravity = baseGravity * lowJumpMultiplier;
            }
        }

        verticalVelocity += activeGravity * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, baseGravity * 3f);
        if (controller != null && controller.enabled)
        {
            controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
        }

        // ─── 9. ANIMATIONS ──────────────────────────────────────
        // Animation ko direct input se na chalake, actual speed se chalayenge
        // Isse jab player rukega toh moonwalking (sliding) nahi hogi.
        // (currentHorizSpeed already computed above in section 6)
        bool isMovingAnim = currentHorizSpeed > 0.3f && groundedNow && !isSliding;

        // ─── AAA Hysteresis: Prevents walk↔run flicker at threshold boundary ─────
        // isRunningAnim becomes true only above (walkSpeed + 0.8) and
        // becomes false only below (walkSpeed - 0.4) — a dead-band prevents rapid flicker.
        if (!isRunningAnim)
            isRunningAnim = isMovingAnim && (isRunning || currentHorizSpeed > walkSpeed + 0.8f);
        else
            isRunningAnim = isMovingAnim && (isRunning || currentHorizSpeed > walkSpeed - 0.4f);

        bool isWalkingAnim = isMovingAnim && !isRunningAnim;

        // ─── Animator Speed float (drives Blend Trees & foot-sync) ──────────────
        // Normalized: 0=Idle, 0.5=Walk, 1=Run. Avoids foot-slide at any speed.
        float maxMoveSpeed = Mathf.Max(runSpeed, torchRunSpeed);
        float animSpeedNorm = Mathf.Clamp01(currentHorizSpeed / maxMoveSpeed);

        // Dirty-write: only push values that changed to avoid per-frame Animator overhead
        bool hasTorchAnim = hasTorch && activeCombatEngagements == 0;
        if (isWalkingAnim   != _prevIsWalking)  { animator.SetBool("IsWalking", isWalkingAnim);   _prevIsWalking  = isWalkingAnim; }
        if (isRunningAnim   != _prevIsRunning)  { animator.SetBool("IsRunning", isRunningAnim);   _prevIsRunning  = isRunningAnim; }
        if (hasTorchAnim    != _prevHasTorch)   { animator.SetBool("HasTorch",  hasTorchAnim);    _prevHasTorch   = hasTorchAnim; }
        if (groundedNow     != _prevIsGrounded) { animator.SetBool("IsGrounded", groundedNow);    _prevIsGrounded = groundedNow; }
        animator.SetFloat("Speed", animSpeedNorm, 0.1f, Time.deltaTime); // Damped — prevents snap

        // ─── 10. ITEM PICKUP (Torch & Swords) ───────────────────
        bool tryingToPickLamp = interactLampAction.triggered;
        bool tryingToPickWeapon = interactWeaponAction.triggered;

        if (tryingToPickLamp || tryingToPickWeapon)
        {
            int nearbyCount = ScanNearbyColliders(true);
            for (int c = 0; c < nearbyCount; c++)
            {
                Collider col = overlapBuffer[c];
                if (col == null) continue;
                if (tryingToPickLamp && col.CompareTag("OilLamp") && !hasTorch)
                {
                    EquipTorch(col.gameObject);
                    break;
                }
                else if (tryingToPickWeapon && col.CompareTag("Sword 1") && !hasSword1)
                {
                    CollectSword(col.gameObject, 1);
                    break;
                }
                else if (tryingToPickWeapon && col.CompareTag("Sword 2") && !hasSword2)
                {
                    CollectSword(col.gameObject, 2);
                    break;
                }
                else if (tryingToPickWeapon && col.CompareTag("OilCan") && hasTorch)
                {
                    RefillLamp(col.gameObject);
                    break;
                }
                else if (tryingToPickWeapon)
                {
                    bool pickedUpSpecial = false;
                    for (int i = 0; i < specialObjects.Length; i++)
                    {
                        if (specialObjects[i] != null && col.gameObject == specialObjects[i] && !hasSpecialObject[i])
                        {
                            hasSpecialObject[i] = true;
                            StartCoroutine(AbsorbObject(specialObjects[i]));
                            pickedUpSpecial = true;
                            break;
                        }
                    }
                    if (pickedUpSpecial) break;
                }
            }
        }

        if (dropLampAction.triggered && hasTorch)
            DropTorch();

        if (dropWeaponAction.triggered && currentWeapon != WeaponType.None)
            DropWeapon();

        // ─── 11. WEAPON SWITCHING ───────────────────────────────
        if (equipNoneAction.triggered)
            UpdateWeaponState(WeaponType.None);
        if (equipSword1Action.triggered && hasSword1)
            UpdateWeaponState(WeaponType.Sword1);
        if (equipSword2Action.triggered && hasSword2)
            UpdateWeaponState(WeaponType.Sword2);

        // ─── PRAY ACTION ───────────────────────────────────────────
        if (prayAction.triggered && groundedNow && !isSliding)
        {
            if (!isAttacking && !isSpecialActionPlaying && currentHealth > 0 && IsInPrayTrigger)
            {
                UpdateWeaponState(WeaponType.None); // Hath khali karo (weapons drop)
                animator.SetTrigger("Pray");
            }
        }

        // ─── 12. SPECIAL ACTION (3-step sword-put flow) ─────────────────────────
        if (specialAction.triggered && groundedNow && !isSliding)
        {
            // Step 3 — pressMUI wapas dikha hua hai, M dabane ka intezaar (sword rakh diya)
            if (waitingForMagicM)
            {
                // M dabaya → magic animation fire karo!
                waitingForMagicM = false;
                AAAPressPromptAnimator.AnimatePrompt(pressMUI, false);
                ExecuteSpecialAction(pendingSpecialIndex);
            }
            else
            {
                // Normal check — pray trigger zone mein hai?
                for (int i = 0; i < 3; i++)
                {
                    if (hasSpecialObject[i] && inPrayTrigger[i])
                    {
                        if (currentWeapon != WeaponType.None)
                        {
                            // Sword haath mein hai:
                            // Step 1 — M dabaya → pressMUI band karo, pressPut1UI dikhao
                            pendingSpecialIndex = i;
                            waitingForSwordPut  = true;
                            AAAPressPromptAnimator.AnimatePrompt(pressMUI, false);
                            AAAPressPromptAnimator.AnimatePrompt(pressPut1UI, true);
                        }
                        else
                        {
                            // Koi sword nahi — seedha magic
                            ExecuteSpecialAction(i);
                        }
                        break;
                    }
                }
            }
        }

        // Step 2 — pressPut1UI dikha hua hai, "1" dabane ka intezaar
        if (waitingForSwordPut && equipNoneAction.triggered)
        {
            UpdateWeaponState(WeaponType.None);   // Sword haath se hatao
            waitingForSwordPut = false;
            waitingForMagicM   = true;            // Ab M dabane ka wait karo

            // pressPut1UI band karo, pressMUI wapas dikhao — player M dabayega magic ke liye
            AAAPressPromptAnimator.AnimatePrompt(pressPut1UI, false);
            AAAPressPromptAnimator.AnimatePrompt(pressMUI, true);
        }

        UpdateInteractUI(groundedNow);
    }

    // ─── Helpers for sword-put flow ──────────────────────────────────────────
    private bool waitingForSwordPut = false;
    private bool waitingForMagicM   = false;  // 1 dabane ke baad M press ka intezaar
    private int  pendingSpecialIndex = -1;


    private System.Collections.IEnumerator AbsorbObject(GameObject obj)
    {
        Debug.Log("Absorbing Special Object!");

        // Find the index of this object to store its light
        int objIndex = -1;
        for (int i = 0; i < specialObjects.Length; i++)
        {
            if (specialObjects[i] == obj) { objIndex = i; break; }
        }

        // ── Step 1: Stop AND Clear ALL Particle Systems (including inactive children) ──
        // Check the object and its parent (in case the Particle System is a sibling inside a prefab)
        ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in particles)
        {
            // Sirf emit karna band karo taaki jo particles already hain wo dheere dheere fade ho jayein
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (obj.transform.parent != null)
        {
            ParticleSystem[] parentParticles = obj.transform.parent.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in parentParticles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // ── Step 2: Find Light and IMMEDIATELY TURN IT OFF (E press karte hi band ho jaye) ──
        Light specialLight = null;
        if (objIndex != -1)
        {
            specialLight = obj.GetComponentInChildren<Light>(true);
            if (specialLight != null)
            {
                // Turant band kar do - object collect hote hi light off
                specialLight.enabled = false;
                specialLights[objIndex] = specialLight.transform;
            }
        }

        // ── Step 3: Disable physics/colliders so it can fly smoothly ──
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // DO NOT SetParent yet! Fly in world space to avoid weird local scale/rotation issues
        // ── Step 4: Fly to player's chest ──
        Vector3 startScale = obj.transform.localScale;
        Vector3 startPos   = obj.transform.position;
        float time = 0f;

        // Calculate dynamic chest position based on CharacterController to support ANY player scale
        float worldHeight = controller.height * transform.lossyScale.y;
        float worldRadius = controller.radius * transform.lossyScale.x;
        float localChestY = controller.center.y + (controller.height * 0.25f);
        float localChestZ = controller.radius * 1.5f;

        while (time < absorbDuration)
        {
            time += Time.deltaTime;
            float smoothT = Mathf.SmoothStep(0f, 1f, time / absorbDuration);

            // Calculate chest position dynamically in world space every frame
            Vector3 centerWorld = transform.position + transform.up * (controller.center.y * transform.lossyScale.y);
            Vector3 chestPos = centerWorld + transform.up * (worldHeight * 0.25f) + transform.forward * worldRadius * 1.5f;
            
            obj.transform.position = Vector3.Lerp(startPos, chestPos, smoothT);
            
            // Shrink slowly but keep it visible (e.g. 10% size)
            obj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.1f, smoothT);

            yield return null;
        }

        // Light PERMANENTLY OFF - bas transform reference player ke paas rahega PlaceObject ke liye
        if (specialLight != null && specialLights[objIndex] != null)
        {
            // Sirf transform save karo, light band hi rahegi
            specialLight.transform.SetParent(this.transform);
            specialLight.transform.localPosition = new Vector3(0f, localChestY, localChestZ);
            // Light band hi rahegi - player ke seene mein sirf invisible orb hai
            specialLight.enabled = false;
        }

        // Fog ko dissipate karo — pehle detach karo taaki obj.SetActive(false) se band na ho
        SpecialObjectFog[] fogScripts = obj.GetComponentsInChildren<SpecialObjectFog>(true);
        if (fogScripts.Length == 0 && obj.transform.parent != null)
            fogScripts = obj.transform.parent.GetComponentsInChildren<SpecialObjectFog>(true);
        foreach (var fog in fogScripts)
        {
            fog.transform.SetParent(null); // Scene root me move karo (parent se alag)
            fog.OnObjectCollected();       // Fade-out + self-destroy trigger karo
        }

        obj.SetActive(false); // Main mesh hidden
    }

    private void ExecuteSpecialAction(int gateNumber)
    {
        if (isAttacking || isSpecialActionPlaying) return;

        Debug.Log($"Special Action Triggered for Gate {gateNumber + 1}!");

        // Face player towards the gate automatically
        if (gates[gateNumber] != null)
        {
            Vector3 dir = (gates[gateNumber].transform.position - transform.position);
            dir.y = 0f;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        isSpecialActionPlaying = true; // Complete lock
        isAttacking = true;
        animator.SetTrigger("Special");

        // Start the full sequence
        StartCoroutine(SpecialActionSequence(gateNumber));
    }

    private System.Collections.IEnumerator SpecialActionSequence(int gateNumber)
    {
        // 1. Point Light flies out and gate opens (this handles the whole 5.5s sequence)
        yield return StartCoroutine(PlaceObject(gateNumber));

        // 2. Sequence finished, unlock player
        isAttacking = false;
        isSpecialActionPlaying = false;
        
        // Consume the object state
        hasSpecialObject[gateNumber] = false;
        inPrayTrigger[gateNumber]    = false;
    }

    private System.Collections.IEnumerator PlaceObject(int index)
    {
        Transform lightTrans = specialLights[index];
        if (lightTrans == null || placeTriggers[index] == null) yield break;

        Light lt = lightTrans.GetComponent<Light>();

        // ── Object ki Point Light ka color pehle save karo, phir band karo ──
        Color objLightColor = lt != null ? lt.color : new Color(1f, 0.6f, 0.1f);
        if (lt != null) lt.enabled = false;
        lightTrans.SetParent(null);

        // Target: Trigger ki exact position
        Vector3 targetPos = placeTriggers[index].position;

        // ═══════════════════════════════════════════════════════════
        // LASER BEAMS — Object ke Point Light ke COLOUR ki
        // Dheere dheere haath se trigger tak pahochti hai
        // ═══════════════════════════════════════════════════════════
        float worldUnit = controller.height * transform.lossyScale.y * 0.015f;

        // Object ki exact light color se beam bana (thin hot core + soft outer halo)
        Color coreColor = new Color(
            Mathf.Min(objLightColor.r * 2.5f, 2f),
            Mathf.Min(objLightColor.g * 2.5f, 2f),
            Mathf.Min(objLightColor.b * 2.5f, 2f),
            1f
        );
        Color glowColor = new Color(objLightColor.r, objLightColor.g, objLightColor.b, 0.35f);

        Shader addShader = Shader.Find("Particles/Additive")
                        ?? Shader.Find("Legacy Shaders/Particles/Additive")
                        ?? Shader.Find("Sprites/Default");

        // ── BEAM THICKNESS (Ab jyada moti aur powerful hai) ──
        float coreW = worldUnit * 4.5f;   // Pehle 2.2 tha
        float glowW = worldUnit * 13.0f;  // Pehle 6.5 tha

        // ── Left hand — Core ──
        GameObject beam1Obj = new GameObject("LaserBeam1");
        LineRenderer beam1Core = beam1Obj.AddComponent<LineRenderer>();
        beam1Core.positionCount = 3;
        beam1Core.numCornerVertices = 8;
        beam1Core.numCapVertices = 8;
        beam1Core.startWidth = coreW * 0.8f; beam1Core.endWidth = coreW * 0.2f;
        beam1Core.useWorldSpace = true;
        beam1Core.material = new Material(addShader);
        beam1Core.startColor = coreColor;
        beam1Core.endColor = new Color(coreColor.r, coreColor.g, coreColor.b, 0.2f);
        beam1Core.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        

        // ── Left hand — Glow ──
        GameObject beam1GlowObj = new GameObject("LaserBeam1Glow");
        LineRenderer beam1Glow = beam1GlowObj.AddComponent<LineRenderer>();
        beam1Glow.positionCount = 3;
        beam1Glow.numCornerVertices = 8;
        beam1Glow.numCapVertices = 8;
        beam1Glow.startWidth = glowW * 0.85f; beam1Glow.endWidth = glowW * 0.04f;
        beam1Glow.useWorldSpace = true;
        beam1Glow.material = new Material(addShader);
        beam1Glow.startColor = glowColor;
        beam1Glow.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        beam1Glow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // ── Right hand — Core ──
        GameObject beam2Obj = new GameObject("LaserBeam2");
        LineRenderer beam2Core = beam2Obj.AddComponent<LineRenderer>();
        beam2Core.positionCount = 3;
        beam2Core.numCornerVertices = 8;
        beam2Core.numCapVertices = 8;
        beam2Core.startWidth = coreW * 0.8f; beam2Core.endWidth = coreW * 0.2f;
        beam2Core.useWorldSpace = true;
        beam2Core.material = new Material(addShader);
        beam2Core.startColor = coreColor;
        beam2Core.endColor = new Color(coreColor.r, coreColor.g, coreColor.b, 0.2f);
        beam2Core.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;


        // ── Right hand — Glow ──
        GameObject beam2GlowObj = new GameObject("LaserBeam2Glow");
        LineRenderer beam2Glow = beam2GlowObj.AddComponent<LineRenderer>();
        beam2Glow.positionCount = 3;
        beam2Glow.numCornerVertices = 8;
        beam2Glow.numCapVertices = 8;
        beam2Glow.startWidth = glowW * 0.85f; beam2Glow.endWidth = glowW * 0.04f;
        beam2Glow.useWorldSpace = true;
        beam2Glow.material = new Material(addShader);
        beam2Glow.startColor = glowColor;
        beam2Glow.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        beam2Glow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        float time = 0f;

        while (time < lightFlyDuration)
        {
            time += Time.deltaTime;
            // SmoothStep: beam dheere dheere badh ke trigger tak pahochti hai
            float t = Mathf.SmoothStep(0f, 1f, time / lightFlyDuration);

            // More natural energy pulse and slight beam drift (Realistic Flutter)
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 25f + 0.4f); // Faster, more aggressive pulse
            float sway = 0.15f * Mathf.Sin(Time.time * 18f + 0.2f); // Thoda zyada sway
            beam1Core.startWidth = coreW * pulse;
            beam1Glow.startWidth = glowW * pulse;
            beam2Core.startWidth = coreW * pulse;
            beam2Glow.startWidth = glowW * pulse;

            // Hand positions (animation se update hote hain)
            Vector3 chestFallback = transform.position + transform.up * (controller.height * transform.lossyScale.y * 0.75f);
            Vector3 handL = handBone      != null ? handBone.position      : (chestFallback - transform.right * (controller.radius * transform.lossyScale.x * 2f));
            Vector3 handR = otherHandBone != null ? otherHandBone.position : (chestFallback + transform.right * (controller.radius * transform.lossyScale.x * 2f));

            // Beam ka TIP dheere dheere haath se trigger ki taraf badhta hai
            Vector3 beamTip = Vector3.Lerp(handL, targetPos, t);
            Vector3 beam2Tip = Vector3.Lerp(handR, targetPos, t);

            Vector3 beamDir = (beamTip - handL).normalized;
            Vector3 beam2Dir = (beam2Tip - handR).normalized;
            Vector3 swayOffset = transform.forward * sway;
            Vector3 swayOffset2 = -transform.forward * sway;

            beam1Core.SetPosition(0, handL);
            beam1Core.SetPosition(1, beamTip + beamDir * 0.04f + swayOffset);
            beam1Core.SetPosition(2, beamTip + beamDir * 0.12f + swayOffset * 0.6f);
            beam1Glow.SetPosition(0, handL);
            beam1Glow.SetPosition(1, beamTip + beamDir * 0.04f + swayOffset);
            beam1Glow.SetPosition(2, beamTip + beamDir * 0.12f + swayOffset * 0.6f);

            beam2Core.SetPosition(0, handR);
            beam2Core.SetPosition(1, beam2Tip + beam2Dir * 0.04f + swayOffset2);
            beam2Core.SetPosition(2, beam2Tip + beam2Dir * 0.12f + swayOffset2 * 0.6f);
            beam2Glow.SetPosition(0, handR);
            beam2Glow.SetPosition(1, beam2Tip + beam2Dir * 0.04f + swayOffset2);
            beam2Glow.SetPosition(2, beam2Tip + beam2Dir * 0.12f + swayOffset2 * 0.6f);

            yield return null;
        }

        // Beam trigger pe pahochi — sab destroy
        Destroy(beam1Obj);
        Destroy(beam1GlowObj);
        Destroy(beam2Obj);
        Destroy(beam2GlowObj);

        // ── Open the gate ──
        if (gates[index] != null)
        {
            float slideDist = index == 0 ? gate1SlideDistance
                            : index == 1 ? gate2SlideDistance
                            : gate3SlideDistance;
            yield return StartCoroutine(OpenGateSlideDown(gates[index], slideDist));
            Debug.Log($"Gate {index + 1} Opened!");
        }
    }

    private System.Collections.IEnumerator OpenGateSlideDown(GameObject gate, float slideDistance)
    {
        Vector3 startPos  = gate.transform.position;
        Vector3 targetPos = startPos - new Vector3(0, slideDistance, 0);

        float duration = 2.5f;
        float time     = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / duration);
            gate.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        gate.transform.position = targetPos;
    }

    // ═══════════════════════════════════════════════════════════
    //  HEALTH SYSTEM
    // ═══════════════════════════════════════════════════════════
    public void TakeDamage(float damageAmount, Vector3 hitPosition)
    {
        TakeDamage(damageAmount);
    }

    public void TakeDamageWithKnockback(float damageAmount, Vector3 hitPosition)
    {
        TakeDamage(damageAmount);
    }

    public void Stun(float duration)
    {
    }

    private float lastDamageTime = 0f; // Track kab aakhri baar chot lagi thi
    public float healthRegenRate = 100f; // Har second kitni health badhegi (aap isko inspector me badal sakte hain)
    public float healthRegenDelay = 5f;  // Damage lene ke kitne second baad health badhna shuru hogi

    [Tooltip("Health percentage (0 to 1) below which auto-regen starts")]
    public float lowHealthRegenThreshold = 0.4f; // 40% health se kam hone par regen
    [Tooltip("Time the player must stand completely idle before regen starts")]
    public float idleTimeToRegen = 3f;
    private float idleTimer = 0f;

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth -= damageAmount;
        if (currentHealth < 0) currentHealth = 0;

        lastDamageTime = Time.time; // Record current time

        // Fire event to notify UI smoothly
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            deathPosition = transform.position;
            if (animator != null) animator.SetTrigger("Die"); // Marne ka animation
            if (deathCoroutine != null) StopCoroutine(deathCoroutine);
            deathCoroutine = StartCoroutine(DelayGameOver()); // Game over screen me delay taaki animation poora ho
        }
        else
        {
            // Player Zinda hai, to usko Damage reaction ka animation aana chahiye
            if (animator != null)
            {
                animator.ResetTrigger("TakeDamage");
                animator.SetTrigger("TakeDamage");
            }
            
            // Optional: Thodi der ke liye input block kar do (Stun effect)
            StartCoroutine(HitRecovery());
        }
    }

    private System.Collections.IEnumerator HitRecovery()
    {
        isFrozen = true; // Player ko thodi der ke liye freeze kar do (na bhage na maare)
        yield return new WaitForSeconds(0.4f); // 0.4 seconds ka flinch/stun
        isFrozen = false; // Wapas normal
    }

    private System.Collections.IEnumerator DelayGameOver()
    {
        yield return new WaitForSeconds(3.5f); // 3.5 second ka delay taaki death animation poora ho jaye
        if (currentHealth <= 0)
        {
            OnPlayerDied?.Invoke();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  RESPAWN SYSTEM (Press R to spawn 10m away from death point)
    // ═══════════════════════════════════════════════════════════
    private bool IsRPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) return true;
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.R)) return true;
#endif
        return false;
    }

    /// <summary>
    /// Player marne par sabse paas wale crossed spawn point par respawn karta hai (aur torch / weapons ko retain rakhta hai).
    /// </summary>
    public void RespawnNearDeathPoint()
    {
        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
            deathCoroutine = null;
        }

        Vector3 baseDeathPoint = deathPosition != Vector3.zero ? deathPosition : transform.position;

        // 1. Choose spawn point: Jo spawn point cross kiya hai aur marne ki jagah ke sabse paas hai
        Transform targetSpawn = GetNearestCrossedSpawnPoint(baseDeathPoint);
        Vector3 spawnPos;
        Quaternion spawnRot = transform.rotation;

        if (targetSpawn != null)
        {
            spawnPos = targetSpawn.position;
            spawnRot = targetSpawn.rotation;
        }
        else
        {
            // Fallback: Agar inspector me koi spawn point na dala ho toh safe ground calculation use karo
            spawnPos = FindTraversedRespawnPosition(baseDeathPoint, respawnDistance);
            Vector3 lookDir = (baseDeathPoint - spawnPos);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                spawnRot = Quaternion.LookRotation(lookDir);
            }
        }

        // Snap safely to ground so player doesn't hover or fall through
        int groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");
        RaycastHit groundHit;
        if (Physics.Raycast(spawnPos + Vector3.up * 1.5f, Vector3.down, out groundHit, 4.0f, groundMask, QueryTriggerInteraction.Ignore))
        {
            spawnPos.y = groundHit.point.y;
        }

        // 2. CharacterController disable karke firm ground position & rotation update karo
        if (controller != null) controller.enabled = false;
        transform.position = spawnPos;
        transform.rotation = spawnRot;

        if (controller != null)
        {
            controller.enabled = true;
            // Immediate ground snap: moves character down firmly so no floating/hovering occurs
            controller.Move(Vector3.down * 0.35f);
        }

        // 3. Physics & Velocities reset
        verticalVelocity = -groundSnapForce; // Firmly stick to ground on frame 1
        horizontalVelocity = Vector3.zero;

        // 4. Health restore
        currentHealth = maxHealth;
        deathPosition = Vector3.zero;
        OnHealthChanged?.Invoke(currentHealth);

        // 5. Action and combat states reset
        isAttacking = false;
        isSpecialActionPlaying = false;
        isSliding = false;
        activeCombatEngagements = 0;
        SetLampFreeze(false);

        // 6. WEAPON & TORCH PRESERVATION (Player ka collected weapon aur torch retain rahega)
        if (hasTorch)
        {
            if (handLampObject != null) handLampObject.SetActive(true);
            if (handLampLight != null)
            {
                handLampLight.enabled = true;
                handLampLight.intensity = maxLampIntensity;
            }
            OnTorchStateChanged?.Invoke(true);
            OnOilChanged?.Invoke(maxLampIntensity);

            if (lampDrainCoroutine != null) StopCoroutine(lampDrainCoroutine);
            lampDrainCoroutine = StartCoroutine(DrainLampOil());
        }
        else
        {
            if (handLampObject != null) handLampObject.SetActive(false);
            if (handLampLight != null) handLampLight.enabled = false;
            OnTorchStateChanged?.Invoke(false);
        }

        // Agar player ne koi sword pick ki thi toh usko hath me equip rakho, warna empty-handed
        if (!hasSword1 && !hasSword2)
        {
            currentWeapon = WeaponType.None;
        }
        else if (currentWeapon == WeaponType.None)
        {
            if (hasSword2) currentWeapon = WeaponType.Sword2;
            else if (hasSword1) currentWeapon = WeaponType.Sword1;
        }
        UpdateWeaponState(currentWeapon);

        // 7. Camera Snap to new position
        CameraController cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            cam.SnapToTarget();
        }

        // 8. COOLEST AAA REVIVAL ANIMATION & VFX SEQUENCE
        RevivalVFX.Play(transform, spawnPos);
        if (revivalRoutine != null) StopCoroutine(revivalRoutine);
        revivalRoutine = StartCoroutine(RevivalSequenceRoutine());

        // 9. Fire respawn event
        OnPlayerRespawned?.Invoke();

        Debug.Log($"[Respawn] Player successfully respawned at {(targetSpawn != null ? targetSpawn.name : "safe fallback")} ({spawnPos}).");
    }

    /// <summary>
    /// Start me scene me lagaye gaye spawn points ko initialize karta hai
    /// </summary>
    private void InitializeSpawnPoints()
    {
        crossedSpawnPoints.Clear();

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Shuru me player ke sabse paas wale spawn point ko crossed mark karo
            Transform initialSpawn = null;
            float minDist = float.MaxValue;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    float dist = Vector3.Distance(transform.position, spawnPoints[i].position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        initialSpawn = spawnPoints[i];
                    }
                }
            }

            if (initialSpawn != null)
            {
                crossedSpawnPoints.Add(initialSpawn);
            }
            else if (spawnPoints[0] != null)
            {
                crossedSpawnPoints.Add(spawnPoints[0]);
            }
        }
    }

    /// <summary>
    /// Gameplay ke dauran check karta hai ki player ne kaun-kaun se spawn points cross kar liye hain
    /// </summary>
    private void UpdateCrossedSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform sp = spawnPoints[i];
            if (sp != null && !crossedSpawnPoints.Contains(sp))
            {
                float dist = Vector3.Distance(transform.position, sp.position);
                if (dist <= checkpointDetectionRadius)
                {
                    crossedSpawnPoints.Add(sp);
                    Debug.Log($"[Checkpoint] Player crossed spawn point: {sp.name}");
                }
            }
        }
    }

    /// <summary>
    /// Player ke marne ki position ke sabse kareeb jo spawn point hai (aur jo player ne cross kiya hai), use dhoondhta hai.
    /// </summary>
    public Transform GetNearestCrossedSpawnPoint(Vector3 deathPos)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        Transform bestSpawn = null;
        float minDistance = float.MaxValue;

        // 1. Pehle sirf un spawn points me dhoondho jo player cross kar chuka hai
        if (crossedSpawnPoints != null && crossedSpawnPoints.Count > 0)
        {
            foreach (Transform sp in crossedSpawnPoints)
            {
                if (sp != null)
                {
                    float dist = Vector3.Distance(deathPos, sp.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestSpawn = sp;
                    }
                }
            }
        }

        // 2. Agar koi crossed spawn point nahi mila toh saare assigned spawn points me se sabse paas wala lo
        if (bestSpawn == null)
        {
            minDistance = float.MaxValue;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    float dist = Vector3.Distance(deathPos, spawnPoints[i].position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestSpawn = spawnPoints[i];
                    }
                }
            }
        }

        return bestSpawn;
    }

    private System.Collections.IEnumerator RevivalSequenceRoutine()
    {
        isFrozen = true;

        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.ResetTrigger("TakeDamage");
            animator.ResetTrigger("Hit");
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Jump");
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsSliding", false);
            animator.SetBool("IsGrounded", true);  // New param — grounded on revival
            animator.SetFloat("Speed", 0f);         // New param — stopped on revival
            // Play crisp Idle stance firmly on ground (no weird floating prayer pose)
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
            animator.SetInteger("WeaponType", (int)currentWeapon);
            animator.SetBool("HasTorch", hasTorch && activeCombatEngagements == 0);
        }

        // Reset dirty-write caches so the next Update() pushes all fresh values
        _prevIsWalking  = false;
        _prevIsRunning  = false;
        _prevHasTorch   = hasTorch && activeCombatEngagements == 0;
        _prevIsGrounded = true;
        isRunningAnim   = false;

        yield return new WaitForSeconds(0.25f);

        isFrozen = false;
    }

    /// <summary>
    /// Player jaha se guzar chuka hai (traversed path history), waha se theek 4-5 meter peeche ka safe ground point dhundhta hai.
    /// </summary>
    public Vector3 FindTraversedRespawnPosition(Vector3 deathPos, float targetDistance)
    {
        if (traversedPathHistory == null || traversedPathHistory.Count == 0)
        {
            return FindSafeRespawnPosition(deathPos, targetDistance);
        }

        Vector3 chosenPoint = deathPos;
        bool found = false;

        // Comfortable combat respawn distance: 4.0m to 5.0m back from death location
        float desiredDist = Mathf.Clamp(targetDistance, 3.5f, 5.0f);

        // Find the traversed point that is closest to desiredDist straight-line distance from deathPos
        float bestDiff = float.MaxValue;
        for (int i = traversedPathHistory.Count - 1; i >= 0; i--)
        {
            Vector3 pt = traversedPathHistory[i];
            float dist = Vector3.Distance(deathPos, pt);

            // Prefer points that are at least 3.0m away from death point
            if (dist >= 3.0f)
            {
                float diff = Mathf.Abs(dist - desiredDist);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    chosenPoint = pt;
                    found = true;
                    if (diff < 0.4f) break; // Perfect match found!
                }
            }
        }

        // If player hasn't walked 3.0m yet, pick the furthest point they walked, or fallback safe position
        if (!found)
        {
            if (traversedPathHistory.Count > 0)
            {
                float maxDist = 0f;
                Vector3 furthest = deathPos;
                for (int i = 0; i < traversedPathHistory.Count; i++)
                {
                    float d = Vector3.Distance(deathPos, traversedPathHistory[i]);
                    if (d > maxDist)
                    {
                        maxDist = d;
                        furthest = traversedPathHistory[i];
                    }
                }
                if (maxDist >= 2.0f)
                {
                    chosenPoint = furthest;
                    found = true;
                }
            }

            if (!found)
            {
                return FindSafeRespawnPosition(deathPos, 4.0f);
            }
        }

        // Snap directly to ground underneath (cast down from hip height only)
        int groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");
        RaycastHit groundHit;
        if (Physics.Raycast(chosenPoint + Vector3.up * 1.0f, Vector3.down, out groundHit, 3.0f, groundMask, QueryTriggerInteraction.Ignore))
        {
            chosenPoint.y = groundHit.point.y;
        }

        return chosenPoint;
    }

    /// <summary>
    /// Player jaha mara tha waha se approx distance (4m) door safe ground par spawn point dhundhta hai.
    /// </summary>
    public Vector3 FindSafeRespawnPosition(Vector3 deathPos, float distance)
    {
        distance = Mathf.Clamp(distance, 3.5f, 5.0f);
        float randomBaseAngle = UnityEngine.Random.Range(0f, 360f);
        int angleSteps = 16;
        float angleIncrement = 360f / angleSteps;
        int groundMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        Vector3 bestCandidate = deathPos;
        bool foundSafePosition = false;

        for (int i = 0; i < angleSteps; i++)
        {
            float currentAngle = (randomBaseAngle + i * angleIncrement) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(currentAngle), 0f, Mathf.Cos(currentAngle));
            Vector3 candidatePos = deathPos + direction * distance;

            // NavMesh check: kya ye candidate position NavMesh par walkable hai?
            bool hasNavMesh = false;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(candidatePos, out navHit, 3.0f, NavMesh.AllAreas))
            {
                candidatePos = navHit.position;
                hasNavMesh = true;
            }

            // Raycast downward from close above ground (not high up) to avoid ceiling/archway collision
            RaycastHit groundHit;
            Vector3 rayStart = candidatePos + Vector3.up * 1.5f;
            if (Physics.Raycast(rayStart, Vector3.down, out groundHit, 4.0f, groundMask, QueryTriggerInteraction.Ignore))
            {
                candidatePos.y = groundHit.point.y;
            }
            else if (!hasNavMesh)
            {
                continue;
            }

            bestCandidate = candidatePos;
            foundSafePosition = true;
            break;
        }

        if (!foundSafePosition)
        {
            bestCandidate = deathPos - transform.forward * distance;
            RaycastHit groundHit;
            if (Physics.Raycast(bestCandidate + Vector3.up * 1.5f, Vector3.down, out groundHit, 4.0f, groundMask, QueryTriggerInteraction.Ignore))
            {
                bestCandidate.y = groundHit.point.y;
            }
        }

        return bestCandidate;
    }

    // ═══════════════════════════════════════════════════════════
    //  TORCH METHODS  (Dual-Object System)
    // ═══════════════════════════════════════════════════════════
    private void EquipTorch(GameObject groundLamp)
    {
        Debug.Log("Equipped Torch!");
        hasTorch         = true;
        activeGroundLamp = groundLamp;

        // 1. Zameen wala lamp hide karo
        groundLamp.SetActive(false);

        // 2. Haath wala lamp dikhao
        if (handLampObject != null)
            handLampObject.SetActive(true);

        // 3. Turn on light instantly and start draining oil smoothly
        if (handLampLight != null)
        {
            handLampLight.enabled = true;
            handLampLight.intensity = maxLampIntensity;
            lampDrainCoroutine = StartCoroutine(DrainLampOil());
        }

        // 4. UI ko batao ki torch utha li hai
        OnTorchStateChanged?.Invoke(true);

        // State turant save karo taaki agle scene me Torch saath rahe
        GameSaveManager.SaveGameState(this);
    }

    public bool isLampFrozen = false; // Pray trigger me ya Arena me lamp drain freeze

    public void SetLampFreeze(bool freeze)
    {
        isLampFrozen = freeze;
    }

    private System.Collections.IEnumerator DrainLampOil()
    {
        while (hasTorch && handLampLight != null && handLampLight.intensity > 0)
        {
            bool isSlideHeld = slideAction != null && slideAction.ReadValue<float>() > 0.1f;
            
            // Pray trigger zone mein hain ya crouch control daba hua hai? Drain freeze karo
            // Oil lamp tab tak drain nahi hoga jab tak player ke paas koi sword na ho
            bool hasAnySword = hasSword1 || hasSword2;
            if (!isLampFrozen && !(isSlideHeld && isSliding) && hasAnySword)
            {
                handLampLight.intensity -= lampDrainRate * Time.deltaTime;

                if (handLampLight.intensity <= 0)
                {
                    handLampLight.intensity = 0;
                }
            }

            // UI slider update (frozen ya nahi, slider current value dikhaye)
            OnOilChanged?.Invoke(handLampLight.intensity);

            yield return null; // Next frame tak wait karo
        }

        // Agar loop se bahar aa gaye aur intensity <= 0 hai, to spawn karo
        Debug.Log($"DrainLampLight Ended. Checking spawn condition... hasTorch: {hasTorch}, light!=null: {handLampLight != null}, intensity: {(handLampLight != null ? handLampLight.intensity : -1)}, hasSpawnedOilCane: {hasSpawnedOilCane}");
        if (hasTorch && handLampLight != null && handLampLight.intensity <= 0 && !hasSpawnedOilCane)
        {
            Debug.Log("Condition met! Calling SpawnOilCane()...");
            SpawnOilCane();
        }
        else
        {
            Debug.LogWarning("Condition NOT met for spawning oil cane.");
        }
    }

    private void SpawnOilCane()
    {
        Debug.Log("SpawnOilCane() is triggered.");
        if (oilCanePrefab == null)
        {
            Debug.LogError("FAILED: Oil Cane Prefab is not assigned in PlayerController! Please assign it in the Inspector.");
            return;
        }

        hasSpawnedOilCane = true;

        Vector3 spawnDirection = transform.forward;
        float actualDistance = oilCaneSpawnDistance;

        // Deewar ke piche (behind wall) spawn na ho jaye isliye aage raycast karenge
        if (Physics.Raycast(transform.position + Vector3.up, spawnDirection, out RaycastHit wallHit, oilCaneSpawnDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            actualDistance = wallHit.distance - 2f; // Deewar se thoda pehle spawn karo
            if (actualDistance < 2f) actualDistance = 2f; // Kam se kam 2 meter door
        }

        // Spawn position nikalo
        Vector3 spawnPos = transform.position + spawnDirection * actualDistance;
        spawnPos.y += 10f; // Height se zameen dhoondhne ke liye

        if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            spawnPos = hit.point;
            spawnPos.y += 1.5f; // Hava me thoda terta hua spawn hoga
        }
        else
        {
            spawnPos.y -= 10f; // Fallback agar raycast fail ho jaye
        }

        GameObject spawnedCane = Instantiate(oilCanePrefab, spawnPos, Quaternion.identity);
        if (spawnedCane.GetComponent<FloatingItem>() == null)
        {
            spawnedCane.AddComponent<FloatingItem>(); // Tairne ka effect add karein
        }
        Debug.Log("SUCCESS: Dynamic Oil Cane spawned at " + spawnPos + " (Distance: " + actualDistance + ")");

        // Andhere me asani se dikhne ke liye ek tagdi (powerful) light lagate hain
        Light caneLight = spawnedCane.GetComponentInChildren<Light>();
        if (caneLight == null)
        {
            GameObject lightObj = new GameObject("CaneLight");
            lightObj.transform.SetParent(spawnedCane.transform);
            lightObj.transform.localPosition = Vector3.up * 1f; 
            caneLight = lightObj.AddComponent<Light>();
        }
        
        caneLight.type = LightType.Point;
        caneLight.range = 30f; // Range bohot zyada taaki door se dikhe
        caneLight.intensity = 15f; // Bohot chamkega
        caneLight.color = new Color(0f, 1f, 0.2f); // Neon Green color (taaki demon light se alag dikhe)
        caneLight.enabled = true;

        // Ek lamba laser jaisa pillar (indicator) banate hain taaki dur se bhi dikh jaye
        LineRenderer indicator = spawnedCane.AddComponent<LineRenderer>();
        indicator.useWorldSpace = false;
        indicator.positionCount = 2;
        indicator.SetPosition(0, Vector3.zero);
        indicator.SetPosition(1, Vector3.up * 10f); // 10 meter lamba pillar
        indicator.startWidth = 0.2f;
        indicator.endWidth = 0.2f;
        
        Shader defaultShader = Shader.Find("Sprites/Default");
        if (defaultShader != null)
        {
            Material glowMat = new Material(defaultShader);
            glowMat.color = new Color(0f, 1f, 0.2f, 0.5f); // Semi-transparent green
            indicator.material = glowMat;
        }
    }

    private void RefillLamp(GameObject oilCan)
    {
        Debug.Log("Refilled Lamp Oil!");
        oilCan.SetActive(false); // consume the oil can from the ground
        hasSpawnedOilCane = false; // Reset spawn flag so we can spawn another one later

        if (handLampLight != null && hasTorch)
        {
            handLampLight.enabled = true; // Make sure it's on
            handLampLight.intensity = maxLampIntensity;
            
            // Fire event instantly
            OnOilChanged?.Invoke(maxLampIntensity);
            
            // Restart drain process if stopped
            if (lampDrainCoroutine != null)
                StopCoroutine(lampDrainCoroutine);
                
            lampDrainCoroutine = StartCoroutine(DrainLampOil());
        }
    }

    private void DropTorch()
    {
        Debug.Log("Dropped Torch!");
        hasTorch = false;

        if (lampDrainCoroutine != null)
        {
            StopCoroutine(lampDrainCoroutine);
            lampDrainCoroutine = null;
        }

        if (handLampLight != null)
            handLampLight.enabled = false;

        // 1. Haath wala lamp chhupaao
        if (handLampObject != null)
            handLampObject.SetActive(false);

        // 2. Zameen wala lamp player ke paas wapas dikhao
        if (activeGroundLamp != null)
        {
            // Player ke pairo ke paas rakh do
            Vector3 dropPosition = transform.position + transform.forward * 0.5f;
            activeGroundLamp.transform.position = dropPosition;
            activeGroundLamp.SetActive(true);

            // Agar Rigidbody hai toh use physics drop karne do
            Rigidbody rb = activeGroundLamp.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(transform.forward * 1.5f + Vector3.up * 2f, ForceMode.Impulse);
            }
        }

        activeGroundLamp = null;
        
        // UI ko batao ki torch chhod di hai
        OnTorchStateChanged?.Invoke(false);

        // State update save karo
        GameSaveManager.SaveGameState(this);
    }

    public void SetCombatState(bool enteringCombat)
    {
        if (enteringCombat)
            activeCombatEngagements++;
        else
            activeCombatEngagements--;

        if (activeCombatEngagements < 0) activeCombatEngagements = 0;

        bool inCombat = activeCombatEngagements > 0;

        // Agar player ke pas torch hai to usko combat me chupana hai
        if (hasTorch)
        {
            if (inCombat)
            {
                // Fight shuru hui - lamp haath se gayab karo
                if (handLampObject != null) handLampObject.SetActive(false);
                if (handLampLight != null) handLampLight.enabled = false;
                
                // UI bar bhi hide kar do
                OnTorchStateChanged?.Invoke(false);
            }
            else
            {
                // Fight khatam - lamp wapas lao
                if (handLampObject != null) handLampObject.SetActive(true);
                if (handLampLight != null) handLampLight.enabled = true;
                
                // UI bar wapas dikhao
                OnTorchStateChanged?.Invoke(true);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  WEAPON METHODS
    // ═══════════════════════════════════════════════════════════
    private void CollectSword(GameObject groundSword, int swordType)
    {
        Debug.Log($"Collected Sword {swordType}!");
        groundSword.SetActive(false); // Hide the sword from ground
        
        if (swordType == 1)
        {
            hasSword1 = true;
            activeGroundSword1 = groundSword;
            UpdateWeaponState(WeaponType.Sword1);
        }
        else if (swordType == 2)
        {
            hasSword2 = true;
            activeGroundSword2 = groundSword;
            UpdateWeaponState(WeaponType.Sword2);
        }

        // State turant save karo taaki agle scene me Weapon saath rahe
        GameSaveManager.SaveGameState(this);
    }

    private void DropWeapon()
    {
        GameObject swordToDrop = null;

        if (currentWeapon == WeaponType.Sword1 && hasSword1)
        {
            hasSword1 = false;
            swordToDrop = activeGroundSword1;
            activeGroundSword1 = null;
        }
        else if (currentWeapon == WeaponType.Sword2 && hasSword2)
        {
            hasSword2 = false;
            swordToDrop = activeGroundSword2;
            activeGroundSword2 = null;
        }

        if (swordToDrop != null)
        {
            // Drop it near player's feet / front
            Vector3 dropPosition = transform.position + transform.forward * 0.5f;
            swordToDrop.transform.position = dropPosition;
            swordToDrop.SetActive(true);

            // Let physics handle the drop if it has a Rigidbody
            Rigidbody rb = swordToDrop.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(transform.forward * 1.5f + Vector3.up * 2f, ForceMode.Impulse);
            }
        }

        // Return to empty handed state after dropping weapon
        UpdateWeaponState(WeaponType.None);
        GameSaveManager.SaveGameState(this);
    }

    public void UpdateWeaponState(WeaponType newWeapon)
    {
        if (currentWeapon != newWeapon)
        {
            Debug.Log($"Switched weapon to: {newWeapon}");
        }
        currentWeapon = newWeapon;
        
        if (handSword1Object != null)
            handSword1Object.SetActive(currentWeapon == WeaponType.Sword1);
            
        if (handSword2Object != null)
            handSword2Object.SetActive(currentWeapon == WeaponType.Sword2);
            
        // Animator me weapon type pass kar rahe hain: 0 = None, 1 = Sword1, 2 = Sword2
        if (animator != null)
        {
            animator.SetInteger("WeaponType", (int)currentWeapon);
        }
    }

    /// <summary>
    /// Guarantees that the equipped sword has a trigger collider and SwordDamage script.
    /// This fixes sword attacks dealing 0 damage in Level 2 / Level 3 or any prefab instances.
    /// </summary>
    private void EnsureSwordComponents(GameObject swordObj)
    {
        if (swordObj == null) return;

        // 1. Collider check (trigger collider for weapon hits)
        Collider[] colliders = swordObj.GetComponentsInChildren<Collider>(true);
        if (colliders == null || colliders.Length == 0)
        {
            BoxCollider col = swordObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0f, 0.45f);
            col.size = new Vector3(0.35f, 0.35f, 1.4f);
        }
        else
        {
            // Ensure at least one collider is a trigger
            bool hasTrigger = false;
            foreach (var c in colliders)
            {
                if (c.isTrigger)
                {
                    hasTrigger = true;
                    break;
                }
            }
            if (!hasTrigger && colliders.Length > 0)
            {
                colliders[0].isTrigger = true;
            }
        }

        // 2. SwordDamage script check
        SwordDamage sd = swordObj.GetComponentInChildren<SwordDamage>(true);
        if (sd == null)
        {
            sd = swordObj.AddComponent<SwordDamage>();
            sd.damageAmount = 100;
        }
    }

    /// <summary>
    /// AAA Active melee sweep: sweeps in front of the player during the swing window.
    /// Prevents physics tunneling or missed trigger ticks on fast swings.
    /// </summary>
    private System.Collections.IEnumerator PerformMeleeHitSweep(float delay, float duration)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        var hitEnemiesThisSwing = new System.Collections.Generic.HashSet<object>();

        while (elapsed < duration)
        {
            Vector3 sweepCenter = transform.position + transform.forward * 1.5f + Vector3.up * 1f;
            float sweepRadius = 1.8f;
            Collider[] hits = Physics.OverlapSphere(sweepCenter, sweepRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col == null || col.transform.root == transform.root) continue;

                // Check DemonAi
                DemonAi demon = col.GetComponent<DemonAi>() ?? col.GetComponentInParent<DemonAi>();
                if (demon != null && !hitEnemiesThisSwing.Contains(demon))
                {
                    hitEnemiesThisSwing.Add(demon);
                    if (Time.time >= SwordDamage.LastSwordDamageTime + 0.35f)
                    {
                        demon.TakeDamage(100);
                        SwordDamage.LastSwordDamageTime = Time.time;
                        Vector3 hitPt = col.ClosestPoint(sweepCenter);
                        Vector3 norm = (transform.position - col.transform.position).normalized + Vector3.up * 0.4f;
                        HitVFX.CreateSwordClashSparks(hitPt, norm);
                        Debug.Log("<color=cyan>[PlayerController] Active Attack Sweep dealt 100 damage to DemonAi!</color>");
                    }
                    continue;
                }

                // Check EnemyAiNoPatrol
                EnemyAiNoPatrol noPatrol = col.GetComponent<EnemyAiNoPatrol>() ?? col.GetComponentInParent<EnemyAiNoPatrol>();
                if (noPatrol != null && !hitEnemiesThisSwing.Contains(noPatrol))
                {
                    hitEnemiesThisSwing.Add(noPatrol);
                    if (Time.time >= SwordDamage.LastSwordDamageTime + 0.35f)
                    {
                        noPatrol.TakeDamage(100);
                        SwordDamage.LastSwordDamageTime = Time.time;
                        Vector3 hitPt = col.ClosestPoint(sweepCenter);
                        Vector3 norm = (transform.position - col.transform.position).normalized + Vector3.up * 0.4f;
                        HitVFX.CreateSwordClashSparks(hitPt, norm);
                        Debug.Log("<color=cyan>[PlayerController] Active Attack Sweep dealt 100 damage to EnemyAiNoPatrol!</color>");
                    }
                    continue;
                }

                // Check BossEnemyAi
                BossEnemyAi boss = col.GetComponent<BossEnemyAi>() ?? col.GetComponentInParent<BossEnemyAi>();
                if (boss != null && !hitEnemiesThisSwing.Contains(boss))
                {
                    hitEnemiesThisSwing.Add(boss);
                    if (Time.time >= SwordDamage.LastSwordDamageTime + 0.35f)
                    {
                        boss.TakeDamage(300);
                        SwordDamage.LastSwordDamageTime = Time.time;
                        Vector3 hitPt = col.ClosestPoint(sweepCenter);
                        Vector3 norm = (transform.position - col.transform.position).normalized + Vector3.up * 0.4f;
                        HitVFX.CreateSwordClashSparks(hitPt, norm);
                        Debug.Log("<color=cyan>[PlayerController] Active Attack Sweep dealt 300 damage to BossEnemyAi!</color>");
                    }
                    continue;
                }
            }

            elapsed += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  ATTACK HANDLER
    // ═══════════════════════════════════════════════════════════
    private void HandleAttack(bool grounded)
    {
        // ─ Tick combo window down ─
        if (comboWindowTimer > 0f)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0f)
            {
                // Window expired — reset combo chain
                currentComboStep = 0;
                comboQueued      = false;
            }
        }

        // ─ Receive new click ─
        // Only allow attack if a sword is equipped (currentWeapon != None)
        if (attackAction.triggered && grounded && !isSliding && currentWeapon != WeaponType.None)
        {
            if (!isAttacking)
            {
                // Not currently swinging — start attack 1
                ExecuteAttack();
            }
            else if (comboWindowTimer > 0f && !comboQueued && currentComboStep < maxComboSteps)
            {
                // Mid-swing but inside combo window — buffer next hit
                comboQueued = true;
            }
        }
    }

    private void ExecuteAttack()
    {
        currentComboStep++;
        isAttacking      = true;
        comboQueued      = false;
        comboWindowTimer = 0f;

        // Set combo step integer so animator can pick Attack1 / Attack2 / Attack3
        animator.SetInteger("ComboStep", currentComboStep);

        // Control animation SPEED here — 1.6 = 60% faster than base clip
        animator.SetFloat("AttackSpeed", attackAnimSpeed);

        // Fire the trigger — animation will play at attackAnimSpeed
        animator.SetTrigger("Attack");

        // Active forward hit detection guarantee (covers fast swings & scenes with prefab differences)
        StartCoroutine(PerformMeleeHitSweep(0.08f, 0.28f));

        // Start auto-sampling the clip length for this combo step (if not yet cached)
        bool needsSample = (currentComboStep == 1 && _attackClipLen1 <= 0f)
                        || (currentComboStep == 2 && _attackClipLen2 <= 0f)
                        || (currentComboStep == 3 && _attackClipLen3 <= 0f);
        if (needsSample)
            StartCoroutine(SampleAttackClipLength(currentComboStep));

        // Use best-known clip length (cached or fallback)
        float clipLength = GetCurrentAttackClipLength();
        float attackDuration = clipLength / attackAnimSpeed;

        StartCoroutine(AttackRecovery(attackDuration));
    }

    /// <summary>
    /// Returns the length of the current Attack animation clip.
    /// Falls back to 0.7s if clip info isn't available yet.
    /// </summary>
    private float GetCurrentAttackClipLength()
    {
        // BUG FIX: Animator hasn't transitioned yet on the same frame as SetTrigger.
        // Reading GetCurrentAnimatorClipInfo(0) here always returns the PRE-attack state's clip.
        // Solution: Use known combo step to return pre-measured lengths instead.
        // Measure these once in the Unity Animation window and update below if clips change.
        switch (currentComboStep)
        {
            case 1: return _attackClipLen1 > 0f ? _attackClipLen1 : 0.65f; // Sword Attack 1
            case 2: return _attackClipLen2 > 0f ? _attackClipLen2 : 0.60f; // Sword Attack 2
            case 3: return _attackClipLen3 > 0f ? _attackClipLen3 : 0.80f; // Attack 3 (Sword Fight One)
            default: return 0.65f;
        }
    }

    // ─── Attack clip lengths (auto-sampled on first use per combo step) ────────────
    // These are measured once at runtime by reading the clip AFTER the transition completes.
    // They cache and never need to be set manually.
    private float _attackClipLen1 = 0f;
    private float _attackClipLen2 = 0f;
    private float _attackClipLen3 = 0f;

    private System.Collections.IEnumerator SampleAttackClipLength(int step)
    {
        // Wait 2 frames for the Animator to fully transition into the attack state
        yield return null;
        yield return null;

        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        foreach (var info in clips)
        {
            if (info.clip == null) continue;
            string name = info.clip.name.ToLower();
            if (name.Contains("attack") || name.Contains("sword") || name.Contains("slash") || name.Contains("fight"))
            {
                float len = info.clip.length;
                if (step == 1) _attackClipLen1 = len;
                else if (step == 2) _attackClipLen2 = len;
                else if (step == 3) _attackClipLen3 = len;
                break;
            }
        }
    }

    private System.Collections.IEnumerator AttackRecovery(float duration)
    {
        // Open combo window in the LAST 40% of the animation — feels natural
        float comboOpenAt = duration * 0.6f;
        yield return new WaitForSeconds(comboOpenAt);

        // Open the combo window
        comboWindowTimer = comboWindowTime;

        // Wait for rest of animation to finish
        yield return new WaitForSeconds(duration - comboOpenAt);

        isAttacking = false;

        // If next hit was buffered AND combo not expired, chain immediately
        if (comboQueued && comboWindowTimer > 0f && currentComboStep < maxComboSteps)
        {
            ExecuteAttack();
        }
        else
        {
            // Combo fully ended
            currentComboStep = 0;
            animator.SetInteger("ComboStep", 0);
            animator.SetFloat("AttackSpeed", 1f); // Reset speed back to normal
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  TRIGGERS (Gates)
    // ═══════════════════════════════════════════════════════════
    private void OnTriggerEnter(Collider other)
    {
        // Check if player entered a spawn point trigger
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null && (other.transform == spawnPoints[i] || other.transform.IsChildOf(spawnPoints[i])))
                {
                    if (crossedSpawnPoints.Add(spawnPoints[i]))
                    {
                        Debug.Log($"[Checkpoint] Trigger entered spawn point: {spawnPoints[i].name}");
                    }
                }
            }
        }

        for (int i = 0; i < prayTriggers.Length; i++)
        {
            if (prayTriggers[i] != null && other.transform == prayTriggers[i])
            {
                inPrayTrigger[i] = true;
                isLampFrozen = true;  // 🔒 Lamp drain freeze — pray trigger mein hai

                // Auto-center and face the gate smoothly when entering
                if (gates[i] != null)
                {
                    StartCoroutine(CenterAndFaceGate(prayTriggers[i].position, gates[i].transform.position));
                }
            }
        }
    }

    private System.Collections.IEnumerator CenterAndFaceGate(Vector3 targetCenter, Vector3 gatePos)
    {
        // Block player input while centering so they don't fight the movement
        isSpecialActionPlaying = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetCenter.x, transform.position.y, targetCenter.z);

        Quaternion startRot = transform.rotation;
        Vector3 dir = (gatePos - endPos);
        dir.y = 0f;
        Quaternion endRot = dir != Vector3.zero ? Quaternion.LookRotation(dir) : transform.rotation;

        float duration = 0.3f; // Smooth transition duration
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / duration);

            // Use controller.Move instead of disabling the controller
            // Disabling the controller causes OnTriggerExit to fire falsely!
            Vector3 nextPos = Vector3.Lerp(startPos, endPos, t);
            controller.Move(nextPos - transform.position);

            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        controller.Move(endPos - transform.position);
        transform.rotation = endRot;
        
        isSpecialActionPlaying = false;
    }

    private void OnTriggerExit(Collider other)
    {
        for (int i = 0; i < prayTriggers.Length; i++)
        {
            if (prayTriggers[i] != null && other.transform == prayTriggers[i])
            {
                inPrayTrigger[i] = false;

                // Check karo koi aur trigger active hai? Agar nahi, tab unfreeze karo
                bool anyTriggerActive = false;
                for (int j = 0; j < inPrayTrigger.Length; j++)
                    if (inPrayTrigger[j]) { anyTriggerActive = true; break; }

                if (!anyTriggerActive)
                    isLampFrozen = false; // 🔓 Bahar aaya — drain wapas chalu
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  UI
    // ═══════════════════════════════════════════════════════════
    private void UpdateInteractUI(bool groundedNow)
    {
        bool showO = false;
        bool showE = false;
        bool showM = false;

        if (groundedNow && !isSliding)
        {
            for (int i = 0; i < 3; i++)
            {
                if (hasSpecialObject[i] && inPrayTrigger[i])
                {
                    showM = true;
                    break;
                }
            }
        }

        if (!showM)
        {
            int nearbyCount = ScanNearbyColliders(false);
            for (int c = 0; c < nearbyCount; c++)
            {
                Collider col = overlapBuffer[c];
                if (col == null) continue;
                if (col.CompareTag("OilLamp") && !hasTorch)
                {
                    Debug.Log("SUCCESS: OilLamp detected in range! Setting showO = true");
                    showO = true;
                    break;
                }
                else if (col.CompareTag("Sword 1") && !hasSword1)
                {
                    showE = true;
                    break;
                }
                else if (col.CompareTag("Sword 2") && !hasSword2)
                {
                    showE = true;
                    break;
                }
                else if (col.CompareTag("OilCan") && hasTorch)
                {
                    showE = true;
                    break;
                }
                else
                {
                    bool specialFound = false;
                    for (int i = 0; i < specialObjects.Length; i++)
                    {
                        if (specialObjects[i] != null && col.gameObject == specialObjects[i] && !hasSpecialObject[i])
                        {
                            showE = true;
                            specialFound = true;
                            break;
                        }
                    }
                    if (specialFound) break;
                }
            }
        }

        AAAPressPromptAnimator.AnimatePrompt(pressOUI, showO);
        AAAPressPromptAnimator.AnimatePrompt(pressEUI, showE);
        // pressMUI: normal zone mein dikhe, ya jab sword rakh ke M press ka wait ho
        AAAPressPromptAnimator.AnimatePrompt(pressMUI, (showM && !waitingForSwordPut) || waitingForMagicM);
        // pressPut1UI: sirf sword-put wait mode mein dikhao, baaki time band
        AAAPressPromptAnimator.AnimatePrompt(pressPut1UI, waitingForSwordPut);
    }

    // ═══════════════════════════════════════════════════════════
    //  GIZMOS — Debug visualization in Scene view
    // ═══════════════════════════════════════════════════════════
    void OnDrawGizmosSelected()
    {
        if (controller == null) return;

        // ─ Grounded check sphere (green = grounded, red = airborne) ─
        float   r      = controller.radius * transform.lossyScale.x;
        Vector3 bottom = transform.position + controller.center * transform.lossyScale.y
                         - Vector3.up * ((controller.height * 0.5f * transform.lossyScale.y) - r);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(bottom, r + 0.05f);

        // ─ Pickup range sphere (yellow) ─
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, pickupRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);

        // ─ Spawn Points visualization ─
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    bool isCrossed = crossedSpawnPoints != null && crossedSpawnPoints.Contains(spawnPoints[i]);
                    Gizmos.color = isCrossed ? Color.green : new Color(0f, 0.8f, 1f, 0.7f);
                    Gizmos.DrawWireSphere(spawnPoints[i].position, 0.6f);
                    Gizmos.DrawRay(spawnPoints[i].position, spawnPoints[i].forward * 1.5f);
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  MISSING API METHODS (Added to fix compiler errors)
    // ═══════════════════════════════════════════════════════════
    [HideInInspector]
    public bool isFrozen = false;

    public void ShowInteractUI(bool show)
    {
        AAAPressPromptAnimator.AnimatePrompt(pressEUI, show);
    }

    public void HealFull()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void SetTalking(bool isTalking)
    {
        isSpecialActionPlaying = isTalking;
    }
}
