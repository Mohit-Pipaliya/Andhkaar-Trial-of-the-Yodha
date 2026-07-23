using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; 
    public float distance = 6.0f; 
    public float heightOffset = 1.5f; 
    public float rightOffset = 1.5f; // Over the shoulder offset

    [Header("Orbit Settings")]
    public float sensitivityX = 30.0f; 
    
    [Header("Camera Fixed Height Angle")]
    public float fixedAngleY = 15f; 

    [Header("Lens Settings")]
    public float fieldOfView = 50f;

    [Header("Smoothness Settings")]
    public float positionDamping = 10f;
    public float rotationDamping = 15f;

    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private float currentX = 0f;

    // Cinematic variables
    private bool isCinematic = false;
    private Transform cinematicEnemy;
    private float cinematicTransitionSpeed = 2f;
    private Vector3 cinematicTargetPosition;
    private Quaternion cinematicTargetRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (Camera.main != null)
        {
            Camera.main.fieldOfView = fieldOfView;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Agar cutscene chal raha hai toh cinematic logic chalega
        if (isCinematic && cinematicEnemy != null)
        {
            HandleCinematicCamera();
        }
        else
        {
            HandleNormalCamera();
        }
    }

    void HandleNormalCamera()
    {
        if (Mouse.current != null)
        {
            currentX += Mouse.current.delta.x.ReadValue() * sensitivityX * Time.unscaledDeltaTime;
        }

        Quaternion targetRotation = Quaternion.Euler(fixedAngleY, currentX, 0);
        
        // Base center position (above player)
        Vector3 targetCenter = target.position + Vector3.up * heightOffset;
        
        // Offset to the right (over the shoulder view)
        Vector3 cameraRight = targetRotation * Vector3.right;
        targetCenter += cameraRight * rightOffset;

        Vector3 targetPosition = targetCenter - (targetRotation * Vector3.forward * distance);

        if (shakeDuration > 0)
        {
            targetPosition += Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.unscaledDeltaTime; 
        }

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionDamping * Time.unscaledDeltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationDamping * Time.unscaledDeltaTime);
    }

    void HandleCinematicCamera()
    {
        // Dono characters ke beech ka center point
        Vector3 midPoint = (target.position + cinematicEnemy.position) / 2f;
        midPoint.y += heightOffset; // Thoda upar dekhe

        // Dono characters ke beech ki line nikali
        Vector3 lineBetween = (cinematicEnemy.position - target.position).normalized;

        // Us line se 90 degree ghoom gaye (side view ke liye)
        Vector3 sideDirection = Vector3.Cross(Vector3.up, lineBetween).normalized;
        
        // Agar angle theek na aye, toh aap direction invert kar sakte hain sideDirection *= -1;
        
        // Side view position calculate ki
        cinematicTargetPosition = midPoint + sideDirection * (distance * 0.8f);

        // Center point ki taraf dekhna hai
        cinematicTargetRotation = Quaternion.LookRotation(midPoint - cinematicTargetPosition);

        // Smoothly wahan jana
        transform.position = Vector3.Slerp(transform.position, cinematicTargetPosition, cinematicTransitionSpeed * Time.unscaledDeltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, cinematicTargetRotation, cinematicTransitionSpeed * Time.unscaledDeltaTime);
    }

    public void StartCinematic(Transform enemyTransform)
    {
        isCinematic = true;
        cinematicEnemy = enemyTransform;
    }

    public void StopCinematic()
    {
        isCinematic = false;
        cinematicEnemy = null;
        
        // Jab cinematic khatam ho, toh camera dobara player ke peeche aane lage uske liye currentX set kar diya
        if (target != null)
        {
            currentX = target.eulerAngles.y; 
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}
