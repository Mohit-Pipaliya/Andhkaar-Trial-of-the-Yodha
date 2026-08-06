using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A robust 3rd person camera controller that follows the player, allows orbit using mouse,
/// and includes basic wall collision to prevent clipping.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The player to follow. If empty, it will auto-find by 'Player' tag.")]
    public Transform target;
    [Tooltip("Offset from the target (e.g., to look at the chest/head instead of feet).")]
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Camera Settings")]
    [Tooltip("Distance from the player.")]
    public float distance = 5.0f;
    [Tooltip("How smoothly the camera follows the player.")]
    public float smoothSpeed = 15.0f;
    [Tooltip("Layers that the camera will collide with (to avoid passing through walls).")]
    public LayerMask collisionMask;

    [Header("Orbit Settings")]
    [Tooltip("Mouse sensitivity for rotating the camera.")]
    public float sensitivity = 0.5f;
    [Tooltip("Minimum vertical angle (looking up).")]
    public float yMinLimit = -30f;
    [Tooltip("Maximum vertical angle (looking down).")]
    public float yMaxLimit = 70f;

    private float currentX = 0f;
    private float currentY = 20f;

    // External shake offset (set by other scripts e.g. FlyAndCollectMechanic)
    [HideInInspector] public Vector3 shakeOffset = Vector3.zero;

    void Start()
    {
        // UNPARENT the camera! If it's a child of the player, it will cause wild spinning.
        transform.parent = null;

        // Auto-find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                // Fallback: Agar tag nahi laga hai toh script se dhoondho
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null)
                {
                    target = pc.transform;
                }
                else
                {
                    Debug.LogWarning("CameraController: Player nahi mila. Please assign Target manually.");
                }
            }
        }

        // Default to Everything except Ignore Raycast and Player (layer 2 and whatever player is on)
        // A simple trick if user forgot to set collision mask
        if (collisionMask.value == 0)
        {
            collisionMask = Physics.DefaultRaycastLayers;
        }

        // Lock cursor to center for a true 3rd person feel
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Disable CinemachineBrain if it exists, because it will override this script and freeze the camera!
        Behaviour brain = GetComponent("CinemachineBrain") as Behaviour;
        if (brain != null)
        {
            brain.enabled = false;
            Debug.Log("CameraController: CinemachineBrain disabled to allow manual camera control.");
        }

        // Initialize rotation
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;

        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.farClipPlane  = 350f;  // AAA: Balance between visible + GPU (fog hides the edge)
            cam.nearClipPlane = 0.1f;  // Prevent z-fighting close-up
        }

        // AAA Perf: Cache CharacterController once — never fetch it in LateUpdate again
        CacheTargetComponents();
    }

    private void CacheTargetComponents()
    {
        if (target == null) return;
        cachedTargetCC = target.GetComponent<CharacterController>();
        if (cachedTargetCC != null)
        {
            cachedCCHeight     = cachedTargetCC.height;
            cachedCCCenter     = cachedTargetCC.center;
            cachedCCHeightHalf = cachedCCHeight * 0.5f;
        }
    }

    [Header("AAA Smoothing")]
    [Tooltip("How smooth the camera rotation feels (lower is snappier, higher is more floaty).")]
    public float rotationSmoothTime = 0.05f;
    [Tooltip("How smooth the camera follows the player position.")]
    public float positionSmoothTime = 0.03f;
    
    [Header("AAA Style Offsets")]
    [Tooltip("Push camera to the side for over-the-shoulder look (e.g. 0.8 on X)")]
    public Vector3 shoulderOffset = Vector3.zero;

    private float targetX;
    private float targetY;
    private float rotXVelocity;
    private float rotYVelocity;
    private Vector3 posVelocity;
    private float currentActualDistance;
    private float distanceVelocity;

    // AAA Perf: CharacterController cached once at Start — eliminates per-frame GetComponent (60x/sec)
    private CharacterController cachedTargetCC;
    private float cachedCCHeight;
    private float cachedCCHeightHalf;
    private Vector3 cachedCCCenter;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Read Mouse Delta
        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue() * 0.1f;
        }

        // Add to target rotation, not current directly (for smoothing)
        targetX += mouseDelta.x * sensitivity;
        targetY -= mouseDelta.y * sensitivity;
        targetY = Mathf.Clamp(targetY, yMinLimit, yMaxLimit);

        // 2. Smoothly damp the current rotation towards target rotation
        currentX = Mathf.SmoothDampAngle(currentX, targetX, ref rotXVelocity, rotationSmoothTime);
        currentY = Mathf.SmoothDampAngle(currentY, targetY, ref rotYVelocity, rotationSmoothTime);
        
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // 3. Calculate Target Center Base Position
        Vector3 targetPos = target.position + targetOffset;
        float dynamicDistance = distance;
        
        // AAA Perf: Use cached CharacterController — zero GetComponent cost
        if (cachedTargetCC == null) CacheTargetComponents();
        if (cachedTargetCC != null)
        {
            Vector3 worldCenter = target.TransformPoint(cachedTargetCC.center);
            float worldHeight = cachedTargetCC.height * target.lossyScale.y;
            targetPos = worldCenter + Vector3.up * (worldHeight * 0.1f);
            dynamicDistance = Mathf.Max(distance, worldHeight * 2.5f);
        }

        // Apply shoulder offset in local camera space
        targetPos += rotation * shoulderOffset;

        // 4. Camera Collision Check
        float desiredDistance = dynamicDistance;
        Vector3 direction = new Vector3(0, 0, -dynamicDistance);
        Vector3 desiredPositionForRay = targetPos + rotation * direction;
        
        RaycastHit hit;
        if (Physics.SphereCast(targetPos, 0.2f, desiredPositionForRay - targetPos, out hit, dynamicDistance, collisionMask))
        {
            if (hit.distance > 0.3f)
            {
                desiredDistance = hit.distance - 0.1f; // Keep a small buffer
            }
        }

        // Smoothly interpolate the distance to prevent sudden snapping when walking past trees/poles
        currentActualDistance = Mathf.SmoothDamp(currentActualDistance, desiredDistance, ref distanceVelocity, 0.1f);

        // 5. Calculate Final Position
        Vector3 finalPosition = targetPos + rotation * new Vector3(0, 0, -currentActualDistance);
        finalPosition += shakeOffset;

        // 6. Apply Final Position & Rotation with SmoothDamp (AAA standard for removing micro-jitters)
        transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref posVelocity, positionSmoothTime);
        transform.rotation = rotation;
    }
}
