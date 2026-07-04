using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;
    public float slideSpeed = 8f;
    
    [Header("Jump & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Slide Settings")]
    public float slideDuration = 0.8f;
    private float slideTimer;
    private bool isSliding = false;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;

    // --- New Input System Actions ---
    private InputAction moveAction;
    private InputAction runAction;
    private InputAction jumpAction;
    private InputAction attackAction;

    void Awake()
    {
        // 1. Move Setup (WASD + Arrows)
        moveAction = new InputAction("Move", type: InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");

        // 2. Run Setup (Shift)
        runAction = new InputAction("Run", type: InputActionType.Button);
        runAction.AddBinding("<Keyboard>/leftShift");
        runAction.AddBinding("<Keyboard>/rightShift");

        // 3. Jump/Slide Setup (Spacebar)
        jumpAction = new InputAction("Jump", type: InputActionType.Button, binding: "<Keyboard>/space");

        // 4. Attack Setup (Left Click / Trackpad)
        attackAction = new InputAction("Attack", type: InputActionType.Button, binding: "<Mouse>/leftButton");
    }

    void OnEnable()
    {
        moveAction.Enable();
        runAction.Enable();
        jumpAction.Enable();
        attackAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        runAction.Disable();
        jumpAction.Disable();
        attackAction.Disable();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // GetComponentInChildren is use kiya taaki agar Animator model par ho to bhi detect ho jaye
        animator = GetComponentInChildren<Animator>();
        
        if (animator == null)
        {
            Debug.LogError("Animator nahi mila! Kripya player par ya uske andar wale model par Animator lagayein.");
        }
    }

    void Update()
    {
        HandleMovement();
        HandleAttack();
    }

    private void HandleMovement()
    {
        // 1. Check if grounded (with a small raycast backup just in case CharacterController isGrounded fails)
        isGrounded = controller.isGrounded || Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.25f);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Get Input using New Input System
        Vector2 inputDir = moveAction.ReadValue<Vector2>();
        float horizontal = inputDir.x;
        float vertical = inputDir.y;

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        
        if (move.magnitude > 1f)
            move = move.normalized;

        bool hasMovement = move.magnitude > 0.1f;

        // 3. Check for Run (ReadValue float is most reliable)
        bool isRunning = runAction.ReadValue<float>() > 0.1f;

        // 4. Jump & Slide Input (triggered is reliable for Button types)
        if (jumpAction.triggered && isGrounded)
        {
            if (isRunning && hasMovement && !isSliding)
            {
                StartSlide();
            }
            else if (!isSliding)
            {
                Jump();
            }
        }

        // 5. Calculate current speed
        float currentSpeed = walkSpeed;

        if (isSliding)
        {
            currentSpeed = slideSpeed;
            slideTimer -= Time.deltaTime;
            
            if (slideTimer <= 0)
                StopSlide();
        }
        else if (isRunning && hasMovement)
        {
            currentSpeed = runSpeed;
        }

        // 6. Apply horizontal movement
        if (hasMovement)
        {
            controller.Move(move * currentSpeed * Time.deltaTime);

            // Rotate the character model to face the direction of movement
            if (animator.transform != transform)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, targetRotation, Time.deltaTime * 15f);
            }
        }

        // 7. Apply gravity and vertical movement
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 8. Update Animation States
        UpdateAnimations(hasMovement, isRunning);
    }

    private void Jump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        animator.SetTrigger("Jump");
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        animator.SetBool("IsSliding", true);
    }

    private void StopSlide()
    {
        isSliding = false;
        animator.SetBool("IsSliding", false);
    }

    private void HandleAttack()
    {
        if (attackAction.WasPressedThisFrame())
        {
            animator.SetTrigger("Attack");
        }
    }

    private void UpdateAnimations(bool isMoving, bool isRunning)
    {
        animator.SetBool("IsWalking", isMoving && !isRunning);
        animator.SetBool("IsRunning", isMoving && isRunning);
    }

    public void TakeDamage(int damageAmount)
    {
        animator.SetTrigger("TakeDamage");
    }
}
