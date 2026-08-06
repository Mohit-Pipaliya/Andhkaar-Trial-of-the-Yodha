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
    public AudioClip doorCloseSound;
    [Tooltip("Slight random pitch adds realism by avoiding repetitive sounds.")]
    [Range(0f, 0.2f)]
    public float pitchRandomness = 0.1f;

    private bool isPlayerNear = false;
    private bool isOpen = false;
    private Transform playerTransform;

    private Quaternion leftDoorClosedRot;
    private Quaternion rightDoorClosedRot;

    // We will track the current rotation angles explicitly
    private float currentLeftAngle = 0f;
    private float currentRightAngle = 0f;
    
    // Smooth movement velocities
    private float leftVelocity;
    private float rightVelocity;

    void Start()
    {
        if (openDoorUI != null) openDoorUI.SetActive(false);

        if (doorAudioSource == null)
        {
            doorAudioSource = gameObject.GetComponent<AudioSource>();
            if (doorAudioSource == null && (doorOpenSound != null || doorCloseSound != null))
            {
                doorAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (doorAudioSource != null)
        {
            // Force 2D sound so it is 100% audible everywhere and doesn't get muffled by weird listener distances
            doorAudioSource.spatialBlend = 0f; 
            doorAudioSource.volume = 1f;
            doorAudioSource.bypassEffects = true;
        }

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
        if (playerTransform == null) return;

        // 1. Detection Logic
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

        // UI Show/Hide and Close Logic
        if (playerInZone)
        {
            if (!isPlayerNear)
            {
                isPlayerNear = true;
                if (!isOpen && openDoorUI != null) openDoorUI.SetActive(true);
            }
        }
        else
        {
            if (isPlayerNear)
            {
                isPlayerNear = false;
                if (openDoorUI != null) openDoorUI.SetActive(false);
            }

            // Close the door automatically when the player leaves the zone
            if (isOpen)
            {
                Debug.Log("[Door] Player left the zone, closing the door.");
                isOpen = false;
                if (doorAudioSource != null && doorCloseSound != null) 
                {
                    doorAudioSource.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
                    doorAudioSource.PlayOneShot(doorCloseSound);
                }
                else if (doorAudioSource != null && doorAudioSource.clip != null)
                {
                    doorAudioSource.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
                    doorAudioSource.Play();
                }
                else if (doorCloseSound == null)
                {
                    Debug.LogError("[DoubleDoorController] DOOR CLOSE SOUND MISSING! Please assign 'doorCloseSound' in the Inspector or add a clip to the AudioSource.");
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
                if (doorAudioSource != null && doorOpenSound != null) 
                {
                    doorAudioSource.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
                    doorAudioSource.PlayOneShot(doorOpenSound);
                }
                else if (doorAudioSource != null && doorAudioSource.clip != null)
                {
                    doorAudioSource.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
                    doorAudioSource.Play();
                }
                else
                {
                    Debug.LogError("[DoubleDoorController] DOOR OPEN SOUND MISSING! Please assign 'doorOpenSound' in the Inspector or add a clip to the AudioSource.");
                }
                if (openDoorUI != null) openDoorUI.SetActive(false);
            }
        }

        // 3. Smooth Door Animation (Realistic Physics Feel)
        float targetLeftAngle = isOpen ? leftDoorOpenAngle : 0f;
        float targetRightAngle = isOpen ? rightDoorOpenAngle : 0f;

        // Easing smoothing using SmoothDampAngle instead of linear MoveTowards
        float smoothTime = 1f / Mathf.Max(openSpeed, 0.1f);
        currentLeftAngle = Mathf.SmoothDampAngle(currentLeftAngle, targetLeftAngle, ref leftVelocity, smoothTime);
        currentRightAngle = Mathf.SmoothDampAngle(currentRightAngle, targetRightAngle, ref rightVelocity, smoothTime);
        
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
