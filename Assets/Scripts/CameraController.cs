using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Target Setup")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Distance Settings")]
    public float defaultDistance = 5f;
    public float minDistance     = 1f;
    public float cameraCollisionRadius = 0.2f;
    public LayerMask collisionMask;

    [Header("Mouse Y Sensitivity")]
    public float sensitivityY = 80f;
    public float minPitch = -20f;
    public float maxPitch = 60f;

    [Header("Follow Smoothing (SmoothDamp)")]
    public float positionSmoothTime  = 0.05f; // Extremely tight follow to prevent shake
    public float verticalSmoothTime  = 0.2f;  

    // Internal
    private float pitch = 10f;
    private float currentDistance;
    private float distanceVelocity;

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
            smoothedY = target.position.y + targetOffset.y;
            playerController = target.GetComponent<PlayerController>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isSliding = playerController != null && playerController.IsSliding;

        // ── 1. MOUSE Y INPUT (Pitch) ─────────
        Vector2 delta = lookAction.ReadValue<Vector2>();
        // pitch -= delta.y * sensitivityY * Time.deltaTime; // Trackpad/Mouse se up/down band kiya
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (isSliding) 
            pitch = Mathf.Lerp(pitch, 35f, Time.deltaTime * 8f); // Temple Run style: look down from top

        // ── 2. CAMERA YAW (Lock exactly to player) ─────────
        // Mouse X rotation is handled entirely by PlayerController now.
        // We just snap exactly to the player's rotation to prevent jitter.
        float yaw = target.eulerAngles.y;

        // ── 3. PIVOT POINT (Absorb jump/run bounce) ─────────
        float currentYOffset = isSliding ? (targetOffset.y - 1.2f) : targetOffset.y; // Camera low near the player
        float rawY = target.position.y + currentYOffset;

        float currentVerticalSmooth = isSliding ? 0.1f : verticalSmoothTime;
        smoothedY = Mathf.SmoothDamp(smoothedY, rawY, ref smoothedYVelocity, currentVerticalSmooth);

        Vector3 pivot = new Vector3(
            target.position.x + targetOffset.x,
            smoothedY,
            target.position.z + targetOffset.z
        );

        // ── 4. DESIRED POSITION & COLLISION ─────────
        Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 camDir = camRot * Vector3.back; 

        float targetDist = isSliding ? defaultDistance * 0.9f : defaultDistance; // Closer to player to show slide clearly
        RaycastHit hit;
        if (Physics.SphereCast(pivot, cameraCollisionRadius, camDir, out hit, targetDist, collisionMask))
            targetDist = Mathf.Max(hit.distance - 0.1f, minDistance);

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDist, ref distanceVelocity, 0.1f);
        Vector3 desiredPos = pivot + camDir * currentDistance;

        // ── 5. APPLY WITH TIGHT SMOOTHDAMP ─────────
        // Fast damp completely eliminates the 'trailing shake' effect
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVelocity, positionSmoothTime);
        transform.rotation = camRot;
    }
}
