using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float slideSpeed = 12f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -15f; 

    [Header("Mouse Rotation")]
    public float mouseSensitivityX = 150f; // Player rotate karne ki speed

    [Header("Slide Settings")]
    public float slideDuration = 0.75f;
    public float slideHeight = 0.8f; // Hitbox height during slide
    private float slideTimer;
    private bool isSliding = false;
    private Vector3 slideDirection; 
    private Quaternion slideRotation; // Slide ke dauran rotation lock karne ke liye 

    private CharacterController controller;
    private float originalHeight;
    private Vector3 originalCenter;
    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;

    public bool IsSliding => isSliding;

    // --- Input Actions ---
    private InputAction moveAction;
    private InputAction runAction;
    private InputAction jumpAction;
    private InputAction slideAction;
    private InputAction lookAction;
    private InputAction attackAction;

    void Awake()
    {
        moveAction = new InputAction("Move", type: InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        runAction = new InputAction("Run", type: InputActionType.Button, binding: "<Keyboard>/leftShift");
        jumpAction = new InputAction("Jump", type: InputActionType.Button, binding: "<Keyboard>/space");
        slideAction = new InputAction("Slide", type: InputActionType.Button, binding: "<Keyboard>/leftCtrl");
        attackAction = new InputAction("Attack", type: InputActionType.Button, binding: "<Mouse>/leftButton");
        
        // Mouse Look
        lookAction = new InputAction("Look", binding: "<Pointer>/delta");
    }

    void OnEnable()
    {
        moveAction.Enable();
        runAction.Enable();
        jumpAction.Enable();
        slideAction.Enable();
        lookAction.Enable();
        attackAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        runAction.Disable();
        jumpAction.Disable();
        slideAction.Disable();
        lookAction.Disable();
        attackAction.Disable();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        originalHeight = controller.height;
        originalCenter = controller.center;
    }

    void Update()
    {
        // ── 1. PLAYER ROTATION (Mouse X) ─────────
        // Player ko khud mouse se ghumao taaki movement lag na ho
        if (!isSliding)
        {
            Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
            transform.Rotate(Vector3.up * mouseDelta.x * mouseSensitivityX * Time.deltaTime);
        }

        // ── 2. MOVEMENT ─────────
        isGrounded = controller.isGrounded || Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector2 rawInput = moveAction.ReadValue<Vector2>();
        bool isRunning = runAction.ReadValue<float>() > 0.1f;
        Vector3 inputDir = new Vector3(rawInput.x, 0f, rawInput.y).normalized;
        bool hasInput = inputDir.magnitude >= 0.1f;

        // Slide Start
        if (slideAction.triggered && isGrounded && !isSliding)
        {
            isSliding  = true;
            slideTimer = slideDuration;
            
            // Slide hamesha forward hogi, aur rotation lock ho jayegi
            slideDirection = transform.forward;
            slideRotation  = transform.rotation;

            // Reduce hitbox height for passing under obstacles
            controller.height = slideHeight;
            controller.center = new Vector3(originalCenter.x, slideHeight / 2f, originalCenter.z);

            animator.SetBool("IsSliding", true);
        }

        // Slide Update
        if (isSliding)
        {
            // Root motion ya kisi bhi aur cheez se player ko ghoomne se roko
            transform.rotation = slideRotation;

            slideTimer -= Time.deltaTime;
            float slideProgress = 1f - (slideTimer / slideDuration);
            float currentSlideSpeed = Mathf.Lerp(slideSpeed, walkSpeed, Mathf.SmoothStep(0f, 1f, slideProgress));
            controller.Move(slideDirection * currentSlideSpeed * Time.deltaTime);

            // Check if we can stand up (no obstacle above) when timer ends
            if (slideTimer <= 0f)
            {
                bool canStand = !Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.up, originalHeight);
                if (canStand)
                {
                    isSliding = false;
                    animator.SetBool("IsSliding", false);
                    controller.height = originalHeight;
                    controller.center = originalCenter;
                }
                else
                {
                    // Extend slide slightly if blocked above
                    slideTimer = 0.1f; 
                }
            }
        }
        else // Normal Move
        {
            // Attack
            if (attackAction.triggered && isGrounded && !isSliding)
            {
                animator.SetTrigger("Attack");
            }

            // Jump
            if (jumpAction.triggered && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetTrigger("Jump");
            }

            if (hasInput)
            {
                // Simple forward/right movement, no camera needed
                Vector3 moveDir = transform.forward * inputDir.z + transform.right * inputDir.x;
                float speed = isRunning ? runSpeed : walkSpeed;
                controller.Move(moveDir.normalized * speed * Time.deltaTime);
            }
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Animation
        bool isActuallyMoving = (hasInput || isSliding) && isGrounded;
        animator.SetBool("IsWalking", isActuallyMoving && !isRunning && !isSliding);
        animator.SetBool("IsRunning", isActuallyMoving && isRunning && !isSliding);
    }
}
