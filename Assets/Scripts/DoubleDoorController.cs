using UnityEngine;

public class DoubleDoorController : MonoBehaviour
{
    [Header("Door Transforms")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Rotation Settings")]
    public float leftDoorOpenAngle = 90f;
    public float rightDoorOpenAngle = -90f;
    public float openSpeed = 3f;

    [Header("Detection Settings")]
    [Tooltip("Drag your Trigger Area (Box Collider) object here. Iske andar aane pe door khulega.")]
    public Collider triggerArea;
    [Tooltip("Agar upar Trigger Area khali hai, toh kitni door (distance) se khulna chahiye")]
    public float interactionDistance = 4f;

    [Header("UI Settings")]
    public GameObject openDoorUI;

    [Header("Audio Settings")]
    public AudioSource doorAudioSource;
    public AudioClip doorOpenSound;

    private bool isPlayerNear = false;
    private bool isOpen = false;
    private Transform playerTransform;

    private Quaternion leftDoorClosedRot;
    private Quaternion rightDoorClosedRot;

    // We will track the current rotation angles explicitly
    private float currentLeftAngle = 0f;
    private float currentRightAngle = 0f;

    void Start()
    {
        if (openDoorUI != null) openDoorUI.SetActive(false);

        if (leftDoor != null)
        {
            leftDoorClosedRot = leftDoor.localRotation;
        }
        if (rightDoor != null)
        {
            rightDoorClosedRot = rightDoor.localRotation;
        }

        // Automatically find the player using the Tag "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        // 1. Detection Logic
        if (playerTransform != null && !isOpen)
        {
            bool playerInZone = false;

            // Agar user ne koi Trigger Area assign kiya hai inspector me
            if (triggerArea != null)
            {
                // Check if player is inside the assigned Trigger Area's bounds
                if (triggerArea.bounds.Contains(playerTransform.position))
                {
                    playerInZone = true;
                }
            }
            else
            {
                // Agar Trigger Area khali hai, toh distance se check karega
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance <= interactionDistance)
                {
                    playerInZone = true;
                }
            }

            // UI Show/Hide Logic
            if (playerInZone)
            {
                if (!isPlayerNear)
                {
                    isPlayerNear = true;
                    if (openDoorUI != null) openDoorUI.SetActive(true);
                }
            }
            else
            {
                if (isPlayerNear)
                {
                    isPlayerNear = false;
                    if (openDoorUI != null) openDoorUI.SetActive(false);
                }
            }
        }

        // 2. Interaction Logic (Press O to open) using New Input System
        if (isPlayerNear && !isOpen)
        {
            // NEW INPUT SYSTEM: Keyboard.current.oKey.wasPressedThisFrame
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.oKey.wasPressedThisFrame)
            {
                Debug.Log("[Door] O key pressed! Opening the door now.");
                isOpen = true;
                if (doorAudioSource != null && doorOpenSound != null) doorAudioSource.PlayOneShot(doorOpenSound);
                if (openDoorUI != null) openDoorUI.SetActive(false);
            }
        }

        // 3. Smooth Door Animation
        if (isOpen)
        {
            float speedInDegrees = openSpeed * 50f; // Speed of opening

            // Explicitly move the angle towards the target
            currentLeftAngle = Mathf.MoveTowards(currentLeftAngle, leftDoorOpenAngle, speedInDegrees * Time.deltaTime);
            currentRightAngle = Mathf.MoveTowards(currentRightAngle, rightDoorOpenAngle, speedInDegrees * Time.deltaTime);
            
            // Uncomment to debug animation frames
            // Debug.Log($"[Door] Animating... LeftAngle: {currentLeftAngle}, RightAngle: {currentRightAngle}");
            
            // Apply the exact angle to the doors
            if (leftDoor != null) 
                leftDoor.localRotation = leftDoorClosedRot * Quaternion.Euler(0, currentLeftAngle, 0);
            else
                Debug.LogError("[Door ERROR] Left Door is missing (not assigned in inspector)!");
                
            if (rightDoor != null) 
                rightDoor.localRotation = rightDoorClosedRot * Quaternion.Euler(0, currentRightAngle, 0);
            else
                Debug.LogError("[Door ERROR] Right Door is missing (not assigned in inspector)!");
        }
    }
}
