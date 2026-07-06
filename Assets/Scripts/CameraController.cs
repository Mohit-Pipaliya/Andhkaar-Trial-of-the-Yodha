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

    [Header("Mouse Sensitivity")]
    public float sensitivityX = 150f;
    public float sensitivityY = 80f;
    public float minPitch = -20f;
    public float maxPitch = 60f;

    [Header("Follow Smoothing")]
    public float verticalSmoothTime  = 0.2f;  

    // Internal
    private float yaw = 0f;
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
            yaw = target.eulerAngles.y;
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

        // ── 2. CAMERA YAW (Mouse X) ─────────
        yaw += delta.x * sensitivityX * Time.deltaTime;

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

        // ── 5. APPLY DIRECTLY ─────────
        // Horizontal smooth damp hatana zaroori hai nahi toh player screen par vibrate/shake karta hua dikhta hai
        transform.position = desiredPos;
        transform.rotation = camRot;
    }
}
