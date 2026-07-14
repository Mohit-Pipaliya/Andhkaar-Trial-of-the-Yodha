using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using TMPro;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
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

    [Header("Torch — Dual Object Setup")]
    [Tooltip("Player ke haath ka lamp GameObject — Editor mein position set karo, script shuru mein hide kar degi")]
    public GameObject handLampObject;       // Haath waala lamp (pre-positioned)

    [Tooltip("Hand Lamp ke andar jo Point Light hai, use yahan drag karein")]
    public Light handLampLight;

    [Tooltip("Kitni tezi se oil kam hoga (Bada number = jaldi khatam).")]
    public float lampDrainRate = 2f; // 100 intensity / 2 = 50 seconds mein khatam (dheere dheere)
    [Tooltip("Light ki starting intensity.")]
    public float maxLampIntensity = 100f;

    [Tooltip("Hand bone Transform jis par HandLamp ko attach karna hai (e.g. Left Hand bone)")]
    public Transform handBone;              // Inspector mein Left Hand bone drag karo

    [Tooltip("Dusre haath ka bone (e.g. Right Hand bone) dono haath se magic nikalne ke liye")]
    public Transform otherHandBone;

    [Tooltip("Player kitni door se lamp uthaa sakta hai (metres)")]
    public float pickupRange = 2.5f;        // Badhao to zyada dur se pickup ho

    [Header("Environment Lighting")]
    [Tooltip("Scene ka base ambient color")]
    public Color ambientBaseColor = new Color(0.22f, 0.24f, 0.31f, 1f);
    [Tooltip("Scene ka base ambient intensity")]
    public float ambientBaseIntensity = 0.95f;
    [Tooltip("Torch ke saath ambient kitna warm hoga")]
    public float torchAmbientBoost = 0.18f;
    [Tooltip("Special action ke dauran environment ko thoda brighter karne ka effect")]
    public float specialAmbientBoost = 0.12f;

    [Header("Player Health & Events")]
    public float maxHealth = 100f;
    private float currentHealth;
    
    // --- UI Events (Event-Driven Architecture) ---
    public static event Action<float> OnHealthChanged;
    public static event Action<float> OnOilChanged;
    public static event Action<bool> OnTorchStateChanged; // Naya event add kiya
    public static event Action OnPlayerDied;

    [Header("UI Prompts")]
    [Tooltip("Drag the GameObject/Image that shows 'Press O'")]
    public GameObject pressOUI;
    [Tooltip("Drag the GameObject/Image that shows 'Press E'")]
    public GameObject pressEUI;
    [Tooltip("Drag the GameObject/Image that shows 'Press M' (Optional)")]
    public GameObject pressMUI;

    private GameObject activeGroundLamp;    // Jo lamp pick kiya gaya hai (drop reference)
    private Coroutine lampDrainCoroutine;   // Coroutine reference for lamp point light intensity drain
    private GameObject activeGroundSword1;  // Drop reference for sword 1
    private GameObject activeGroundSword2;  // Drop reference for sword 2

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE — PHYSICS
    // ═══════════════════════════════════════════════════════════
    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;
    private Light sceneDirectionalLight;

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

    // ── Weapon Inputs ──────────────────────────────────────────
    private InputAction equipNoneAction;
    private InputAction equipSword1Action;
    private InputAction equipSword2Action;

    // ── Public property ─────────────────────────────────────────
    public bool IsSliding => isSliding;
    public bool IsAttacking => isAttacking;
    public bool IsSpecialActionPlaying => isSpecialActionPlaying;

    // ═══════════════════════════════════════════════════════════
    //  AWAKE — Setup Input
    // ═══════════════════════════════════════════════════════════
    void Awake()
    {
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

        equipNoneAction   = new InputAction("EquipNone",   type: InputActionType.Button, binding: "<Keyboard>/1");
        equipSword1Action = new InputAction("EquipSword1", type: InputActionType.Button, binding: "<Keyboard>/2");
        equipSword2Action = new InputAction("EquipSword2", type: InputActionType.Button, binding: "<Keyboard>/3");
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
        controller           = GetComponent<CharacterController>();
        animator             = GetComponentInChildren<Animator>();
        mainCameraTransform  = Camera.main.transform;

        if (controller == null) Debug.LogError("PlayerController: CharacterController is missing!");
        if (animator == null) Debug.LogWarning("PlayerController: Animator is missing!");
        if (mainCameraTransform == null) Debug.LogWarning("PlayerController: Main Camera is missing!");

        currentHealth = maxHealth; // Setup health

        if (animator != null)
            animator.applyRootMotion = false;

        // Safe skin width — prevents sliding on slopes
        controller.skinWidth = controller.radius * 0.1f;

        originalHeight = controller.height;
        originalCenter = controller.center;

        // ── Hand Lamp shuru mein hide karo ──────────────────────
        // Editor mein position adjust kar sakte ho, play pe automatically hidden hoga
        if (handLampObject != null)
            handLampObject.SetActive(false);

        if (handLampLight != null)
            handLampLight.enabled = false;

        if (handSword1Object != null)
            handSword1Object.SetActive(false);

        if (handSword2Object != null)
            handSword2Object.SetActive(false);
            
        UpdateWeaponState(WeaponType.None);

        // ── Derive physics values from designer-friendly inputs ──
        // Using kinematic equations:
        //   jumpVelocity = (2 * h) / t
        //   baseGravity  = -(2 * h) / t²
        RecalculateJumpPhysics();

        FindSceneDirectionalLight();
        ApplyEnvironmentLighting();
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

        Light[] allLights = FindObjectsOfType<Light>();
        foreach (Light light in allLights)
        {
            if (light.type == LightType.Directional)
            {
                sceneDirectionalLight = light;
                break;
            }
        }
    }

    private void ApplyEnvironmentLighting()
    {
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

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor * ambientIntensity;

        if (sceneDirectionalLight != null)
        {
            sceneDirectionalLight.intensity = Mathf.Lerp(0.8f, 1.25f, ambientIntensity * 0.5f);
            sceneDirectionalLight.color = Color.Lerp(new Color(0.75f, 0.78f, 0.9f, 1f), new Color(0.95f, 0.82f, 0.65f, 1f), hasTorch ? 0.2f : 0.08f);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════════
    void Update()
    {
        ApplyEnvironmentLighting();

        // ─── 1. READ INPUT ──────────────────────────────────────
        Vector2 rawInput     = moveAction.ReadValue<Vector2>();
        bool    isRunning    = runAction.ReadValue<float>()  > 0.1f;
        bool    isSlideHeld  = slideAction.ReadValue<float>() > 0.1f;
        bool    jumpPressed  = jumpAction.triggered;
               jumpHeld      = jumpAction.ReadValue<float>() > 0.1f;

        Vector3 inputDir = new Vector3(rawInput.x, 0f, rawInput.y).normalized;
        bool    hasInput = rawInput.sqrMagnitude > 0.01f;

        // If a special action is playing OR game is not active (paused/loading), block all input
        if (isSpecialActionPlaying || !UIManager.isGameActive)
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
        // Spherecast is more reliable than CharacterController.isGrounded
        float   capsuleRadius = controller.radius * transform.lossyScale.x;
        Vector3 capsuleBottom = transform.position + controller.center * transform.lossyScale.y
                                - Vector3.up * ((controller.height * 0.5f * transform.lossyScale.y) - capsuleRadius);

        bool groundedNow = controller.isGrounded ||
                           Physics.CheckSphere(capsuleBottom, capsuleRadius + 0.05f,
                               Physics.AllLayers, QueryTriggerInteraction.Ignore);

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
                controller.height     = originalHeight;
                controller.center     = originalCenter;
                animator.SetBool("IsSliding", false);
            }
        }

        // ─── 5. ATTACK ──────────────────────────────────────────
        HandleAttack(groundedNow);

        // ─── 6. JUMP EXECUTION ──────────────────────────────────
        // Jump fires when: buffer is active AND coyote time allows AND not sliding
        bool canJump = jumpBufferCounter > 0f && isGrounded && !isSliding;

        if (canJump)
        {
            // Apply velocity IMMEDIATELY — no coroutine delay, no hawa mein floating
            verticalVelocity  = jumpVelocity;
            isJumping         = true;
            coyoteTimer       = 0f;           // Consume coyote time
            jumpBufferCounter = 0f;           // Consume buffer

            // Fire animation trigger SAME frame as force — perfectly synced
            animator.SetTrigger("Jump");
        }

        // ─── 7. HORIZONTAL MOVEMENT ─────────────────────────────
        // Torch hone par alag speeds use karo — realistic heavy-lamp feel
        float currentSpeed;
        if (isSliding)
            currentSpeed = crouchSpeed;
        else if (hasTorch)
            currentSpeed = isRunning ? torchRunSpeed : torchWalkSpeed;
        else
            currentSpeed = isRunning ? runSpeed : walkSpeed;
        // Slow down movement during attack — adds physical weight to each strike
        if (isAttacking) currentSpeed *= attackMovementFraction;

        Vector3 targetHorizontal = Vector3.zero;
        if (hasInput)
        {
            Vector3 camF = mainCameraTransform.forward; camF.y = 0f; camF.Normalize();
            Vector3 camR = mainCameraTransform.right;   camR.y = 0f; camR.Normalize();
            Vector3 moveDir = (camF * inputDir.z + camR * inputDir.x).normalized;

            // Smooth rotation
            if (moveDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(moveDir), Time.deltaTime * turnSmoothSpeed);

            targetHorizontal = moveDir * currentSpeed;
        }

        // Air control: blend between current momentum and desired direction
        if (groundedNow)
        {
            horizontalVelocity = targetHorizontal;
        }
        else
        {
            // airControlAmount = 0 → no control (pure momentum)
            // airControlAmount = 1 → full instant control
            float lerpSpeed    = airControlAmount * 10f; // scale for good feel
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontal,
                                              Time.deltaTime * lerpSpeed);
        }

        controller.Move(horizontalVelocity * Time.deltaTime);

        // ─── 8. DUAL GRAVITY ────────────────────────────────────
        // This is the key to a REALISTIC jump feel:
        //   • Going up   → normal gravity
        //   • Going down → fallGravityMultiplier × gravity  (heavy landing)
        //   • Space released early → lowJumpMultiplier × gravity  (short hop)
        float activeGravity = baseGravity;

        if (isJumping || !groundedNow)
        {
            if (verticalVelocity < 0f)
            {
                // Falling — apply heavier gravity for satisfying landing
                activeGravity = baseGravity * fallGravityMultiplier;
            }
            else if (verticalVelocity > 0f && !jumpHeld)
            {
                // Rising but Space released — cut jump short (variable height)
                activeGravity = baseGravity * lowJumpMultiplier;
            }
        }

        verticalVelocity += activeGravity * Time.deltaTime;

        // Terminal velocity cap (prevents absurd fall speeds on tall drops)
        verticalVelocity = Mathf.Max(verticalVelocity, baseGravity * 3f);

        controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);

        // ─── 9. ANIMATIONS ──────────────────────────────────────
        animator.SetBool("IsWalking", hasInput && !isRunning && groundedNow && !isSliding);
        animator.SetBool("IsRunning",  hasInput &&  isRunning && groundedNow && !isSliding);
        animator.SetBool("HasTorch",  hasTorch);

        // ─── 10. ITEM PICKUP (Torch & Swords) ───────────────────
        bool tryingToPickLamp = interactLampAction.triggered;
        bool tryingToPickWeapon = interactWeaponAction.triggered;

        if (tryingToPickLamp || tryingToPickWeapon)
        {
            // Player ke aas-paas pickupRange mein objects dhoondo
            Collider[] nearby = Physics.OverlapSphere(transform.position, pickupRange);
            foreach (Collider col in nearby)
            {
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

        // ─── 12. SPECIAL ACTION ─────────────────────────────────
        if (specialAction.triggered && groundedNow && !isSliding)
        {
            for (int i = 0; i < 3; i++)
            {
                if (hasSpecialObject[i] && inPrayTrigger[i])
                {
                    // Player is automatically centered and rotated to the gate in OnTriggerEnter
                    // So we can just directly execute the action.
                    ExecuteSpecialAction(i);
                    break;
                }
            }
        }

        UpdateInteractUI(groundedNow);
    }

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

        float coreW = worldUnit * 2.2f;
        float glowW = worldUnit * 6.5f;

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

            // More natural energy pulse and slight beam drift
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 18f + 0.4f);
            float sway = 0.08f * Mathf.Sin(Time.time * 14f + 0.2f);
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
    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth -= damageAmount;
        if (currentHealth < 0) currentHealth = 0;

        // Fire event to notify UI smoothly
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            OnPlayerDied?.Invoke();
            // Yahan player marne ka animation ya ragdoll laga sakte ho
            animator.SetTrigger("Die"); // Agar marne ka animation hai toh
            this.enabled = false; // Disable controller
        }
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
    }

    private System.Collections.IEnumerator DrainLampOil()
    {
        while (hasTorch && handLampLight != null && handLampLight.intensity > 0)
        {
            // Time.deltaTime se update har frame pe thoda-thoda subtract hoga (realistic feel)
            handLampLight.intensity -= lampDrainRate * Time.deltaTime;
            
            if (handLampLight.intensity < 0)
                handLampLight.intensity = 0;
                
            // Fire event so UI slider moves smoothly with intensity
            OnOilChanged?.Invoke(handLampLight.intensity);
                
            yield return null; // Next frame tak wait karo
        }
    }

    private void RefillLamp(GameObject oilCan)
    {
        Debug.Log("Refilled Lamp Oil!");
        oilCan.SetActive(false); // consume the oil can from the ground

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
    }

    private void UpdateWeaponState(WeaponType newWeapon)
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

        // Calculate how long this attack animation actually takes at given speed
        // We use AnimatorStateInfo if we can, otherwise estimate from clip length
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
        // Give animator one frame to start the transition, then read
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        foreach (var info in clips)
        {
            string name = info.clip.name.ToLower();
            if (name.Contains("attack") || name.Contains("punch") || name.Contains("slash"))
                return info.clip.length;
        }
        return 0.65f; // Safe fallback — adjust if your clip is longer/shorter
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
        for (int i = 0; i < prayTriggers.Length; i++)
        {
            if (prayTriggers[i] != null && other.transform == prayTriggers[i])
            {
                inPrayTrigger[i] = true;

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
            Collider[] nearby = Physics.OverlapSphere(transform.position, pickupRange);
            foreach (Collider col in nearby)
            {
                if (col.CompareTag("OilLamp") && !hasTorch)
                {
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

        if (pressOUI != null) pressOUI.SetActive(showO);
        if (pressEUI != null) pressEUI.SetActive(showE);
        if (pressMUI != null) pressMUI.SetActive(showM);
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
    }
}
