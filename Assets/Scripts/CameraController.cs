using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// AAA 3rd person camera controller that follows the player, allows smooth orbital mouse rotation,
/// automatically adapts its height and follow distance to match any character scale,
/// includes spherical obstacle collision detection, and eliminates micro-jitter and shaking.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The player to follow. If empty, it will auto-find by 'Player' tag or PlayerController.")]
    public Transform target;
    [Tooltip("Additional custom offset added to the look target.")]
    public Vector3 targetOffset = Vector3.zero;

    [Header("Camera Settings")]
    [Tooltip("Base distance from the player. Scaled automatically if player has non-unit scale.")]
    public float distance = 40.0f;
    [Tooltip("Minimum allowed distance so camera never clips into player.")]
    public float minDistance = 14.0f;
    [Tooltip("How smoothly the camera follows the player.")]
    public float smoothSpeed = 15.0f;
    [Tooltip("Layers that the camera will collide with (to avoid passing through walls/terrain).")]
    public LayerMask collisionMask;

    [Header("Orbit Settings")]
    [Tooltip("Mouse sensitivity for rotating the camera.")]
    public float sensitivity = 1.5f;
    [Tooltip("Minimum vertical angle (looking up).")]
    public float yMinLimit = -20f;
    [Tooltip("Maximum vertical angle (looking down).")]
    public float yMaxLimit = 75f;

    private float currentX = 0f;
    private float currentY = 14f;

    [Header("AAA Cinematic Feel")]
    [Tooltip("Enable subtle camera bobbing/breathing. Off by default to prevent micro-jitter.")]
    public bool enableCameraBob = false;
    public float bobSpeed = 1.5f;
    public float bobAmount = 0.05f;
    
    [Tooltip("Dynamic FOV based on player speed.")]
    public bool enableDynamicFOV = true;
    public float baseFOV = 60f;
    public float maxSpeedFOV = 70f;
    public float fovSmoothTime = 0.25f;

    [Header("AAA Smoothing")]
    [Tooltip("How smooth the camera rotation feels (lower is snappier, higher is more floaty).")]
    public float rotationSmoothTime = 0.04f;
    [Tooltip("How smooth the camera follows the player position.")]
    public float positionSmoothTime = 0.03f;
    
    [Header("AAA Style Offsets")]
    [Tooltip("Push camera to the side for over-the-shoulder look (e.g. 0.8 on X)")]
    public Vector3 shoulderOffset = Vector3.zero;

    // External shake offset (set by HitVFX, RevivalVFX, etc.)
    [HideInInspector] public Vector3 shakeOffset = Vector3.zero;

    // Internal state
    private Camera camComponent;
    private float bobTimer = 0f;
    private float currentFOVVelocity = 0f;
    private float targetX;
    private float targetY = 14f;
    private float rotXVelocity;
    private float rotYVelocity;
    private Vector3 posVelocity;
    private float currentActualDistance;
    private float distanceVelocity;

    // Target tracking cache
    private CharacterController cachedTargetCC;
    private float characterWorldHeight = 2.0f;

    void Awake()
    {
        // Destroy any legacy or conflicting TMPro camera controllers on the same GameObject
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        for (int i = 0; i < scripts.Length; i++)
        {
            MonoBehaviour mb = scripts[i];
            if (mb == null || mb == this) continue;
            string tName = mb.GetType().FullName;
            if (tName != null && (tName.Contains("TMPro.Examples.CameraController") || 
                                 (tName.EndsWith("CameraController") && tName != typeof(CameraController).FullName)))
            {
                Debug.Log($"[CameraController] Removing conflicting legacy script: {tName}");
                Destroy(mb);
            }
        }
    }

    void Start()
    {
        // Unparent the camera to avoid inherited rotation spinning
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
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) target = pc.transform;
            }
        }

        if (collisionMask.value == 0)
        {
            collisionMask = Physics.DefaultRaycastLayers;
        }

        // Lock cursor for clean 3rd-person mouse control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Disable CinemachineBrain if present to avoid dual-camera override fight
        Behaviour brain = GetComponent("CinemachineBrain") as Behaviour;
        if (brain != null) brain.enabled = false;

        // Initialize rotation angles cleanly (never leave targetX/targetY at 0)
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        float pitch = angles.x > 180f ? angles.x - 360f : angles.x;
        currentY = Mathf.Clamp(pitch, yMinLimit, yMaxLimit);
        if (currentY < 6f) currentY = 14f; // Default natural look-down angle at upper torso

        targetX = currentX;
        targetY = currentY;

        camComponent = GetComponent<Camera>();
        if (camComponent != null)
        {
            camComponent.farClipPlane  = 1000f;
            camComponent.nearClipPlane = 0.2f;
            camComponent.fieldOfView   = baseFOV;
        }

        CacheTargetComponents();
        ScaleFramingForTarget();
        currentActualDistance = distance;
    }

    private void CacheTargetComponents()
    {
        if (target == null) return;
        cachedTargetCC = target.GetComponent<CharacterController>();
        ScaleFramingForTarget();
    }

    /// <summary>
    /// Computes true world dimensions and automatically scales follow distance & look height.
    /// Works seamlessly whether character scale is 1, 100, or 2000!
    /// </summary>
    private void ScaleFramingForTarget()
    {
        if (target == null) return;

        if (cachedTargetCC != null)
        {
            characterWorldHeight = cachedTargetCC.height * target.lossyScale.y;
        }
        else
        {
            characterWorldHeight = 2.0f * target.lossyScale.y;
        }

        // If character world height is large (e.g. Deva with height ~20):
        if (characterWorldHeight > 5f)
        {
            // Auto-scale distance and minDistance if left at small human defaults
            if (distance < 15f)
            {
                distance = characterWorldHeight * 2.0f; // 40m for 20m warrior
            }
            else
            {
                distance = Mathf.Clamp(distance, characterWorldHeight * 1.5f, characterWorldHeight * 2.5f);
            }

            if (minDistance < 6f)
            {
                minDistance = characterWorldHeight * 0.65f; // 13m
            }
            else
            {
                minDistance = Mathf.Clamp(minDistance, characterWorldHeight * 0.5f, characterWorldHeight * 0.8f);
            }
        }
        else
        {
            // Standard human character (~1.8m)
            if (distance > 15f) distance = 4.8f;
            if (minDistance > 5f) minDistance = 2.0f;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Read Mouse Delta (supports New Input System with legacy fallback)
        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue() * 0.1f;
        }
        else
        {
            try
            {
                mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2f;
            }
            catch { }
        }

        // Add to target rotation with damping
        targetX += mouseDelta.x * sensitivity;
        targetY -= mouseDelta.y * sensitivity;
        targetY = Mathf.Clamp(targetY, yMinLimit, yMaxLimit);

        // 2. Smoothly damp the current rotation towards target rotation (eliminates mouse micro-jitter)
        currentX = Mathf.SmoothDampAngle(currentX, targetX, ref rotXVelocity, rotationSmoothTime);
        currentY = Mathf.SmoothDampAngle(currentY, targetY, ref rotYVelocity, rotationSmoothTime);
        
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // 3. Calculate Target Center Base Position (aimed at chest/shoulders, never feet or grass)
        if (cachedTargetCC == null && target != null) CacheTargetComponents();

        float lookHeight = characterWorldHeight * 0.70f;
        Vector3 targetPos = target.position + Vector3.up * lookHeight + targetOffset;

        // Apply shoulder offset in local camera space
        targetPos += rotation * shoulderOffset;

        // 4. AAA Spherical Obstacle Collision Check
        float desiredDistance = distance;
        Vector3 cameraDirection = rotation * Vector3.back;

        // Filter out Player and Ignore Raycast layers
        int effectiveMask = collisionMask.value != 0 ? collisionMask.value : Physics.DefaultRaycastLayers;
        effectiveMask &= ~LayerMask.GetMask("Player", "Ignore Raycast");

        // Dynamic collision sphere radius proportional to character scale
        float sphereRadius = Mathf.Clamp(characterWorldHeight * 0.035f, 0.2f, 0.8f);
        RaycastHit[] hits = Physics.SphereCastAll(targetPos, sphereRadius, cameraDirection, distance, effectiveMask, QueryTriggerInteraction.Ignore);
        float closestHitDist = distance;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit h = hits[i];
            if (h.collider == null || h.collider.isTrigger) continue;
            // Ignore player and player children (sword, lamp, bones, etc.)
            if (h.transform == target || h.transform.IsChildOf(target)) continue;
            // Ignore hits directly against player's own inner buffer
            if (h.distance < sphereRadius * 2f) continue;

            if (h.distance < closestHitDist)
            {
                closestHitDist = h.distance;
            }
        }

        if (closestHitDist < distance)
        {
            float clearance = sphereRadius * 1.5f;
            desiredDistance = Mathf.Clamp(closestHitDist - clearance, minDistance, distance);
        }
        else
        {
            desiredDistance = distance;
        }

        // Fast response when approaching obstacle (0.05s), smooth graceful pull-out when leaving obstacle (0.20s)
        float dampTime = desiredDistance < currentActualDistance ? 0.05f : 0.20f;
        currentActualDistance = Mathf.SmoothDamp(currentActualDistance, desiredDistance, ref distanceVelocity, dampTime);

        // 5. External Shake Offset Decay (ensures shake impulse settles cleanly to zero in 0.15s)
        if (shakeOffset.sqrMagnitude > 0.00001f)
        {
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * 12f);
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        // Camera Bobbing (optional)
        Vector3 bobOffset = Vector3.zero;
        if (enableCameraBob)
        {
            float speed = (cachedTargetCC != null) ? cachedTargetCC.velocity.magnitude : 0f;
            float currentBobSpeed = (speed > 1f) ? bobSpeed * 2.5f : bobSpeed;
            float currentBobAmount = (speed > 1f) ? bobAmount * 1.5f : bobAmount;
            
            bobTimer += Time.deltaTime * currentBobSpeed;
            bobOffset = new Vector3(0f, Mathf.Sin(bobTimer) * currentBobAmount, 0f);
        }

        // 6. Calculate Final Camera Position
        Vector3 finalPosition = targetPos + rotation * new Vector3(0, 0, -currentActualDistance);
        finalPosition += shakeOffset + (rotation * bobOffset);

        // Ground Clearance Protection: Never let camera drop into grass/ground on steep upward slopes
        RaycastHit groundCheck;
        float probeDist = Mathf.Clamp(characterWorldHeight * 0.25f, 1.5f, 5f);
        if (Physics.Raycast(finalPosition + Vector3.up * probeDist, Vector3.down, out groundCheck, probeDist * 2f, effectiveMask, QueryTriggerInteraction.Ignore))
        {
            float minAllowedY = groundCheck.point.y + sphereRadius + 0.4f;
            if (finalPosition.y < minAllowedY)
            {
                finalPosition.y = minAllowedY;
            }
        }

        // 7. Apply Final Position & Rotation with SmoothDamp (buttery smooth motion with zero jitter)
        transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref posVelocity, positionSmoothTime);
        transform.rotation = rotation;

        // 8. AAA Dynamic FOV
        if (enableDynamicFOV && camComponent != null)
        {
            float speed = (cachedTargetCC != null) ? cachedTargetCC.velocity.magnitude : 0f;
            float maxSpeed = (characterWorldHeight > 5f) ? 50f : 8f;
            float speedFactor = Mathf.Clamp01(speed / maxSpeed);
            float targetFOV = Mathf.Lerp(baseFOV, maxSpeedFOV, speedFactor);
            camComponent.fieldOfView = Mathf.SmoothDamp(camComponent.fieldOfView, targetFOV, ref currentFOVVelocity, fovSmoothTime);
        }
    }

    /// <summary>
    /// Instantly teleports the camera to the player's new position without interpolating across obstacles or lagging.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        posVelocity = Vector3.zero;
        distanceVelocity = 0f;
        rotXVelocity = 0f;
        rotYVelocity = 0f;
        shakeOffset = Vector3.zero;

        if (cachedTargetCC == null) CacheTargetComponents();
        ScaleFramingForTarget();

        targetX = currentX;
        targetY = currentY;

        float lookHeight = characterWorldHeight * 0.70f;
        Vector3 targetPos = target.position + Vector3.up * lookHeight + targetOffset;
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        targetPos += rotation * shoulderOffset;

        currentActualDistance = distance;
        transform.position = targetPos + rotation * new Vector3(0, 0, -distance);
        transform.rotation = rotation;
    }
}
