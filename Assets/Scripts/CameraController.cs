using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Target Setup")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Distance & Collision")]
    public float defaultDistance = 4f;
    public float minDistance = 1f;
    public float cameraCollisionRadius = 0.3f;
    public LayerMask collisionMask;

    [Header("Look Settings")]
    public float mouseSensitivity = 1f; // Choti value kyonki ye raw delta use kar raha hai
    public float minPitch = -35f;
    public float maxPitch = 60f;
    
    [Header("Smoothness")]
    public float smoothSpeed = 10f;

    private float pitch = 0f;
    private float yaw = 0f;
    private float currentDistance;

    // --- New Input System Actions ---
    private InputAction lookAction;

    void Awake()
    {
        // Mouse Delta ko padhne ke liye Setup
        lookAction = new InputAction("Look", binding: "<Pointer>/delta");
    }

    void OnEnable() { lookAction.Enable(); }
    void OnDisable() { lookAction.Disable(); }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentDistance = defaultDistance;

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Mouse Input (New Input System se)
        Vector2 lookDelta = lookAction.ReadValue<Vector2>();
        
        float mouseX = lookDelta.x * mouseSensitivity * 0.1f;
        float mouseY = lookDelta.y * mouseSensitivity * 0.1f;

        // Player ko Left/Right ghumana mouse ke hisaab se
        target.Rotate(Vector3.up * mouseX);

        // Camera ka Pitch (Up/Down) aur Yaw (Left/Right)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        
        yaw = target.eulerAngles.y;

        // 2. Camera ka desired position
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 lookPosition = target.position + targetOffset;
        Vector3 desiredPosition = lookPosition - (rotation * Vector3.forward * defaultDistance);

        // 3. Collision Detection (Tunnel/Cave Logic)
        RaycastHit hit;
        Vector3 direction = desiredPosition - lookPosition;
        
        if (Physics.SphereCast(lookPosition, cameraCollisionRadius, direction.normalized, out hit, defaultDistance, collisionMask))
        {
            currentDistance = hit.distance;
        }
        else
        {
            currentDistance = defaultDistance;
        }

        // 4. Final Position lagana (Lerp for smoothness)
        Vector3 finalPosition = lookPosition - (rotation * Vector3.forward * currentDistance);
        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = rotation;
    }
}
