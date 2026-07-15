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
    [Tooltip("Camera distance during gate special action — auto-calculated, yeh minimum hai")]
    public float gateSpecialActionDistance = 22f;
    [Tooltip("Player + Gate dono dikhane ke liye kitna extra buffer add karo (metres)")]
    public float magicZoomBuffer = 10f;
    [Tooltip("Magic ke dauran camera ka pitch angle (degree) — dono dikhe")]
    public float magicPitch = 30f;
    [Tooltip("Camera distance jab player crouch karke walk kare — pura player dikhe")]
    public float crouchWalkDistance = 12f;
    [Tooltip("Camera pitch (upar se neeche ka angle) jab crouch+walk ho")]
    public float crouchWalkPitch = 5f;
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

    [Header("Torch Idle — Camera Auto Follow")]
    [Tooltip("Torch idle me camera kitni tezi se player ke direction me ghoomega (0=nahi, 1=turant)")]
    [Range(0f, 1f)]
    public float torchIdleFollowStrength = 0.6f;
    [Tooltip("Yaw align hone ki smoothness (bada = dheere)")]
    public float torchIdleFollowSpeed = 3f;
    [Tooltip("Mouse input kitna ho tab torch follow override ho jaye")]
    public float mouseOverrideThreshold = 1.5f;

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
    private float   torchIdleBlend = 0f; // 0=no follow, 1=full follow

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
        // Crouch+walk detection: player sliding (crouching) aur move kar raha hai
        bool isCrouchWalking = isSliding && playerController.IsCrouchWalking;

        // ── 1. MOUSE Y INPUT (Pitch) ─────────
        Vector2 delta = lookAction.ReadValue<Vector2>();
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Dynamic pitch based on state
        float targetPitch = 10f;
        if (isCrouchWalking)
            targetPitch = crouchWalkPitch;
        else if (isSliding)
            targetPitch = 35f;
        else if (isSpecialAction)
            targetPitch = magicPitch; // Slightly elevated — player + gate dono dikhe

        pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, 0.15f);

        // ── 2. CAMERA YAW (Mouse X + Player Follow) ─────────
        bool mouseMoving = Mathf.Abs(delta.x) > mouseOverrideThreshold;

        // Mouse yaw — normal rotation
        float mouseYaw = yaw + delta.x * sensitivityX * Time.deltaTime;

        // Player direction follow — camera player ke peeche dhire dhire aati hai
        float playerYaw = target != null ? target.eulerAngles.y : yaw;

        // Blend: mouse chalte waqt mouse control kare, ruke to camera player ke peeche jaye
        float blendTarget = mouseMoving ? 0f : 1f;
        torchIdleBlend    = Mathf.Lerp(torchIdleBlend, blendTarget, Time.deltaTime * torchIdleFollowSpeed);

        float followYaw   = Mathf.LerpAngle(yaw, playerYaw, torchIdleFollowStrength * Time.deltaTime * torchIdleFollowSpeed);
        float targetYaw   = Mathf.LerpAngle(mouseYaw, followYaw, torchIdleBlend);

        yaw = Mathf.SmoothDamp(yaw, targetYaw, ref yawVelocity, mouseMoving ? 0.08f : 0.3f);

        // ── 3. PIVOT POINT ─────────
        float currentYOffset = targetOffset.y;

        Vector3 pivotXZ;
        if (isSpecialAction && playerController != null)
        {
            // Magic: pivot = player aur gate ke beech ka midpoint (dono frame ho jayein)
            Vector3 gatePos   = playerController.ActiveGatePosition;
            Vector3 playerPos = target.position;
            Vector3 midPoint  = Vector3.Lerp(playerPos, gatePos, 0.5f);
            pivotXZ           = new Vector3(midPoint.x, 0f, midPoint.z);
            currentYOffset    = targetOffset.y + 1.5f; // Camera thoda aur upar jaye
        }
        else if (isCrouchWalking)
        {
            pivotXZ = new Vector3(
                target.position.x + targetOffset.x,
                0f,
                target.position.z + targetOffset.z
            );
            currentYOffset = targetOffset.y - 0.5f; // Crouch walk me camera thoda neeche aaye (tunnel ke andar rahe)
        }
        else
        {
            pivotXZ = new Vector3(
                target.position.x + targetOffset.x,
                0f,
                target.position.z + targetOffset.z
            );
            if (isSliding)
                currentYOffset = targetOffset.y - 1.2f; // Normal slide me camera neeche jaye
        }

        float rawY = target.position.y + currentYOffset;
        float currentVerticalSmooth = isSliding ? 0.1f : verticalSmoothTime;
        smoothedY = Mathf.SmoothDamp(smoothedY, rawY, ref smoothedYVelocity, currentVerticalSmooth);

        Vector3 pivot = new Vector3(pivotXZ.x, smoothedY, pivotXZ.z);

        // ── 4. DESIRED DISTANCE ─────────
        float targetDist = defaultDistance;
        if (isCrouchWalking)
        {
            targetDist = crouchWalkDistance;
        }
        else if (isSliding)
        {
            targetDist = slideDistance;
        }
        else if (isSpecialAction && playerController != null)
        {
            // Dynamic: player se gate ki distance + buffer — dono frame ho jayein
            float playerGateDist = Vector3.Distance(
                target.position, playerController.ActiveGatePosition);
            targetDist = Mathf.Max(gateSpecialActionDistance, playerGateDist + magicZoomBuffer);
        }

        // ── 5. COLLISION CHECK ─────────
        Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 camDir = camRot * Vector3.back;

        RaycastHit hit;
        // Cinematic views (magic) ke dauran camera wall collide hoke aage na aaye.
        // Tunnel me collision zaroori hai taaki camera wall ke aar-paar na jaye.
        bool ignoreCollision = isSpecialAction;

        if (!ignoreCollision && Physics.SphereCast(pivot, cameraCollisionRadius, camDir, out hit, targetDist, collisionMask))
            targetDist = Mathf.Max(hit.distance - 0.1f, minDistance);

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDist, ref distanceVelocity, distanceSmoothTime);
        Vector3 desiredPos = pivot + camDir * currentDistance;

        // ── 6. APPLY CAMERA TRANSFORM ─────────
        transform.position = desiredPos;
        transform.rotation = camRot;
    }
}
