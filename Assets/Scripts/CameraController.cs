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
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Read Mouse Delta (Supports both New Input System and Legacy Input)
        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
        {
            // Mouse delta is raw pixels (can be 50+ per frame), so we MUST scale it down
            mouseDelta = Mouse.current.delta.ReadValue() * 0.1f;
        }

        currentX += mouseDelta.x * sensitivity;
        currentY -= mouseDelta.y * sensitivity;
        currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);

        // 2. Calculate Desired Rotation
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // 3. Calculate Desired Position dynamically based on character size
        Vector3 targetPos = target.position + targetOffset;
        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null)
        {
            // TransformPoint gets the exact world position of the center
            Vector3 worldCenter = target.TransformPoint(cc.center);
            // Move up to the shoulders/head (approx 30% of total height above center)
            float worldHeight = cc.height * target.lossyScale.y;
            targetPos = worldCenter + Vector3.up * (worldHeight * 0.3f);
            
            // Adjust distance automatically based on height so big characters fit on screen
            distance = Mathf.Max(distance, worldHeight * 1.5f);
        }

        Vector3 direction = new Vector3(0, 0, -distance);
        Vector3 desiredPosition = targetPos + rotation * direction;

        // 4. Camera Collision Check
        float currentDistance = distance;
        RaycastHit hit;
        // SphereCast helps prevent camera from clipping into sharp corners
        if (Physics.SphereCast(targetPos, 0.2f, desiredPosition - targetPos, out hit, distance, collisionMask))
        {
            // Move camera inward if hitting a wall
            currentDistance = hit.distance - 0.1f;
        }

        // 5. Apply Final Position & Rotation
        Vector3 finalPosition = targetPos + rotation * new Vector3(0, 0, -currentDistance);

        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = rotation;
    }
}
