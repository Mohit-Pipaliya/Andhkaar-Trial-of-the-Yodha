using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Target Setup")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Distance Settings")]
    public float defaultDistance = 9f;
    [Tooltip("Camera distance during regular slide")]
    public float slideDistance = 8f;
    [Tooltip("Camera distance during tunnel/narrow passage slide (ducking)")]
    public float tunneSlideDistance = 7f;
    [Tooltip("Camera distance during gate special action (magic animation)")]
    public float gateSpecialActionDistance = 15f;
    public float minDistance     = 1f;
    public float cameraCollisionRadius = 0.2f;
    public LayerMask collisionMask;

    [Header("Mouse Sensitivity")]
    public float sensitivityX = 60f;    // Reduced for smooth look
    public float sensitivityY = 40f;    // Reduced for smooth look
    public float minPitch = -20f;
    public float maxPitch = 60f;

    [Header("Follow Smoothing")]
    public float verticalSmoothTime  = 0.2f;
    public float distanceSmoothTime = 0.15f;

    // Internal
    private float yaw = 0f;
    private float pitch = 10f;
    private float currentDistance;
    private float distanceVelocity;
    private float pitchVelocity = 0f;
    private float yawVelocity = 0f;

    private Vector3 posVelocity = Vector3.zero;
    private float   smoothedY;
    private float   smoothedYVelocity;

    private PlayerController playerController;
    private InputAction lookAction;

    void Awake()
    {
        lookAction = new InputAction("Look", binding: "<Pointer>/delta");
    }

    void OnEnable()  => lookAction.Enable();
    void OnDisable() => lookAction.Disable();

    void Start()
    {
        currentDistance = defaultDistance;
        if (target != null)
        {
            yaw = target.eulerAngles.y;
            smoothedY = target.position.y + targetOffset.y;
            playerController = target.GetComponent<PlayerController>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isSliding = playerController != null && playerController.IsSliding;
        bool isSpecialAction = playerController != null && 
                               (playerController.IsSpecialActionPlaying || playerController.IsAttacking);

        // ── 1. MOUSE Y INPUT (Pitch) ─────────
        Vector2 delta = lookAction.ReadValue<Vector2>();
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Dynamic pitch based on state
        float targetPitch = 10f;
        if (isSliding)
            targetPitch = 35f; // Temple Run style: look down from top
        else if (isSpecialAction)
            targetPitch = 20f; // Slightly elevated to see player and gate/trigger

        pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, 0.15f);

        // ── 2. CAMERA YAW (Mouse X) ─────────
        float targetYaw = yaw + delta.x * sensitivityX * Time.deltaTime;
        yaw = Mathf.SmoothDamp(yaw, targetYaw, ref yawVelocity, 0.1f);

        // ── 3. PIVOT POINT ─────────
        float currentYOffset = isSliding ? (targetOffset.y - 1.2f) : targetOffset.y;
        if (isSpecialAction)
            currentYOffset = targetOffset.y + 0.5f; // Lift camera slightly for special action view
            
        float rawY = target.position.y + currentYOffset;
        float currentVerticalSmooth = isSliding ? 0.1f : verticalSmoothTime;
        smoothedY = Mathf.SmoothDamp(smoothedY, rawY, ref smoothedYVelocity, currentVerticalSmooth);

        Vector3 pivot = new Vector3(
            target.position.x + targetOffset.x,
            smoothedY,
            target.position.z + targetOffset.z
        );

        // ── 4. DESIRED DISTANCE ─────────
        float targetDist = defaultDistance;
        if (isSliding)
            targetDist = slideDistance;  // Regular slide or tunnel slide
        else if (isSpecialAction)
            targetDist = gateSpecialActionDistance;  // Gate magic animation

        // ── 5. COLLISION CHECK ─────────
        Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 camDir = camRot * Vector3.back;

        RaycastHit hit;
        if (Physics.SphereCast(pivot, cameraCollisionRadius, camDir, out hit, targetDist, collisionMask))
            targetDist = Mathf.Max(hit.distance - 0.1f, minDistance);

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDist, ref distanceVelocity, distanceSmoothTime);
        Vector3 desiredPos = pivot + camDir * currentDistance;

        // ── 6. APPLY CAMERA TRANSFORM ─────────
        transform.position = desiredPos;
        transform.rotation = camRot;
    }
}
