using UnityEngine;
using UnityEngine.InputSystem;

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
    //  ITEM STATES
    // ═══════════════════════════════════════════════════════════
    [Header("Item States")]
    public bool hasTorch = false;

    [Header("Torch — Dual Object Setup")]
    [Tooltip("Player ke haath ka lamp GameObject — Editor mein position set karo, script shuru mein hide kar degi")]
    public GameObject handLampObject;       // Haath waala lamp (pre-positioned)

    [Tooltip("Hand bone Transform jis par HandLamp ko attach karna hai (e.g. Left Hand bone)")]
    public Transform handBone;              // Inspector mein Left Hand bone drag karo

    [Tooltip("Player kitni door se lamp uthaa sakta hai (metres)")]
    public float pickupRange = 2.5f;        // Badhao to zyada dur se pickup ho

    private GameObject activeGroundLamp;    // Jo lamp pick kiya gaya hai (drop reference)

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE — PHYSICS
    // ═══════════════════════════════════════════════════════════
    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;

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
    private InputAction interactAction;
    private InputAction dropAction;

    // ── Public property ─────────────────────────────────────────
    public bool IsSliding => isSliding;

    // ═══════════════════════════════════════════════════════════
    //  AWAKE — Setup Input
    // ═══════════════════════════════════════════════════════════
    void Awake()
    {
        moveAction = new InputAction("Move", type: InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w")
            .With("Down",  "<Keyboard>/s")
            .With("Left",  "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        runAction      = new InputAction("Run",      type: InputActionType.Button, binding: "<Keyboard>/leftShift");
        jumpAction     = new InputAction("Jump",     type: InputActionType.Button, binding: "<Keyboard>/space");
        slideAction    = new InputAction("Slide",    type: InputActionType.Button, binding: "<Keyboard>/leftCtrl");
        attackAction   = new InputAction("Attack",   type: InputActionType.Button, binding: "<Mouse>/leftButton");
        interactAction = new InputAction("Interact", type: InputActionType.Button, binding: "<Keyboard>/o");
        dropAction     = new InputAction("Drop",     type: InputActionType.Button, binding: "<Keyboard>/g");
        lookAction     = new InputAction("Look",     binding: "<Pointer>/delta");
    }

    void OnEnable()
    {
        moveAction.Enable(); runAction.Enable(); jumpAction.Enable();
        slideAction.Enable(); lookAction.Enable(); attackAction.Enable();
        interactAction.Enable(); dropAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable(); runAction.Disable(); jumpAction.Disable();
        slideAction.Disable(); lookAction.Disable(); attackAction.Disable();
        interactAction.Disable(); dropAction.Disable();
    }

    // ═══════════════════════════════════════════════════════════
    //  START
    // ═══════════════════════════════════════════════════════════
    void Start()
    {
        controller           = GetComponent<CharacterController>();
        animator             = GetComponentInChildren<Animator>();
        mainCameraTransform  = Camera.main.transform;

        animator.applyRootMotion = false;

        // Safe skin width — prevents sliding on slopes
        controller.skinWidth = controller.radius * 0.1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        originalHeight = controller.height;
        originalCenter = controller.center;

        // ── Hand Lamp shuru mein hide karo ──────────────────────
        // Editor mein position adjust kar sakte ho, play pe automatically hidden hoga
        if (handLampObject != null)
            handLampObject.SetActive(false);

        // ── Derive physics values from designer-friendly inputs ──
        // Using kinematic equations:
        //   jumpVelocity = (2 * h) / t
        //   baseGravity  = -(2 * h) / t²
        RecalculateJumpPhysics();
    }

    // Call this if you change jumpHeight or timeToApex at runtime
    void RecalculateJumpPhysics()
    {
        jumpVelocity = (2f * jumpHeight) / timeToApex;
        baseGravity  = -(2f * jumpHeight) / (timeToApex * timeToApex);
    }

    // ═══════════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════════
    void Update()
    {
        // ─── 1. READ INPUT ──────────────────────────────────────
        Vector2 rawInput     = moveAction.ReadValue<Vector2>();
        bool    isRunning    = runAction.ReadValue<float>()  > 0.1f;
        bool    isSlideHeld  = slideAction.ReadValue<float>() > 0.1f;
        bool    jumpPressed  = jumpAction.triggered;
               jumpHeld      = jumpAction.ReadValue<float>() > 0.1f;

        Vector3 inputDir = new Vector3(rawInput.x, 0f, rawInput.y).normalized;
        bool    hasInput = rawInput.sqrMagnitude > 0.01f;

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
        if (isSlideHeld && groundedNow && !isSliding)
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

        // ─── 10. TORCH PICKUP (OverlapSphere — trigger collider size matter nahi karta) ─
        if (interactAction.triggered && !hasTorch)
        {
            // Player ke aas-paas pickupRange mein koi OilLamp hai?
            Collider[] nearby = Physics.OverlapSphere(transform.position, pickupRange);
            foreach (Collider col in nearby)
            {
                if (col.CompareTag("OilLamp"))
                {
                    EquipTorch(col.gameObject);
                    break;
                }
            }
        }

        if (dropAction.triggered && hasTorch)
            DropTorch();
    }

    // ═══════════════════════════════════════════════════════════
    //  TORCH METHODS  (Dual-Object System)
    // ═══════════════════════════════════════════════════════════
    private void EquipTorch(GameObject groundLamp)
    {
        hasTorch         = true;
        activeGroundLamp = groundLamp;

        // 1. Zameen wala lamp hide karo
        groundLamp.SetActive(false);

        // 2. Haath wala lamp dikhao
        // NOTE: HandLamp pehle se hi hand bone ka CHILD hona chahiye (Unity hierarchy mein)
        // Script sirf SetActive karta hai — position/parenting editor mein set hoti hai
        if (handLampObject != null)
            handLampObject.SetActive(true);
    }

    private void DropTorch()
    {
        hasTorch = false;

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
        if (attackAction.triggered && grounded && !isSliding)
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

    // OnTrigger methods hata diye — ab OverlapSphere use hoti hai (pickupRange se control)

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
