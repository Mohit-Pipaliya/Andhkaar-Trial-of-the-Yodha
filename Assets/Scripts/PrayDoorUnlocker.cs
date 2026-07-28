using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PrayDoorUnlocker : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("UI GameObject for 'Pray to Unlock the Door'")]
    public GameObject infoUI;     
    [Tooltip("UI GameObject for 'Press P to Pray'")]
    public GameObject actionUI;   

    [Header("Door Settings")]
    public Transform door1;
    public Transform door2;
    [Tooltip("Kitne degree rotate karna hai (Y-axis)")]
    public float door1RotationY = 90f;
    [Tooltip("Dusre door ka rotation (opposite bhi ho sakta hai)")]
    public float door2RotationY = -90f;
    [Tooltip("Door open hone ki speed")]
    public float rotationSpeed = 1.5f;

    [Header("Animation Settings")]
    public Animator playerAnimator;
    [Tooltip("Pray animation kitne seconds ka hai")]
    public float prayAnimationDuration = 4f;

    [Header("Trigger Areas (Drag & Drop here)")]
    [Tooltip("Trigger 1 ka Collider (Bada area jaha 'Pray to Unlock' dikhega)")]
    public Collider trigger1Area;
    [Tooltip("Trigger 2 ka Collider (Chhota area jaha 'Press P' dikhega)")]
    public Collider trigger2Area;

    // Internal states
    private bool inTrigger1 = false;
    private bool inTrigger2 = false;
    private bool puzzleCompleted = false;
    private bool isPraying = false;
    private PlayerController playerController;

    void Start()
    {
        // UI hide kardo shuru mein
        if (infoUI) infoUI.SetActive(false);
        if (actionUI) actionUI.SetActive(false);

        // Animator assign nahi kiya to khud dhoondh lega
        if (!playerAnimator) playerAnimator = GetComponentInChildren<Animator>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (puzzleCompleted || isPraying) return;

        // Jab Trigger 2 mein ho aur P dabaye
        if (inTrigger2)
        {
            // Sirf New Input System use karenge error se bachne ke liye
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                StartCoroutine(PrayAndOpenDoorsSequence());
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (puzzleCompleted) return;

        if (trigger1Area != null && other == trigger1Area)
        {
            inTrigger1 = true;
            // Agar pehle se trigger 2 mein nahi hai, tabhi Info UI dikhao
            if (!inTrigger2 && infoUI) infoUI.SetActive(true);
        }
        else if (trigger2Area != null && other == trigger2Area)
        {
            inTrigger2 = true;
            // Info band karo aur Action (Press P) UI dikhao
            if (infoUI) infoUI.SetActive(false);
            if (actionUI) actionUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (puzzleCompleted) return;

        if (trigger1Area != null && other == trigger1Area)
        {
            inTrigger1 = false;
            if (infoUI) infoUI.SetActive(false);
        }
        else if (trigger2Area != null && other == trigger2Area)
        {
            inTrigger2 = false;
            if (actionUI) actionUI.SetActive(false);
            
            // Agar Trigger 2 se bahar aaye par abhi bhi Trigger 1 mein hai, toh wapas Info UI dikhao
            if (inTrigger1 && infoUI) infoUI.SetActive(true);
        }
    }

    private IEnumerator PrayAndOpenDoorsSequence()
    {
        isPraying = true;
        
        // UI band kardo
        if (infoUI) infoUI.SetActive(false);
        if (actionUI) actionUI.SetActive(false);

        // Agar hath mein koi weapon hai toh use drop karo taaki animation sahi chale
        if (playerController != null)
        {
            playerController.UpdateWeaponState(PlayerController.WeaponType.None);
        }

        // 1. Pray Animation start karo
        if (playerAnimator)
        {
            playerAnimator.SetTrigger("Pray");
        }

        // 2. Animation khatam hone ka intezaar karo
        yield return new WaitForSeconds(prayAnimationDuration);

        // 3. Doors ko rotate karo
        if (door1) StartCoroutine(RotateDoor(door1, door1RotationY));
        if (door2) StartCoroutine(RotateDoor(door2, door2RotationY));

        puzzleCompleted = true; // Puzzle solve ho gaya
    }

    private IEnumerator RotateDoor(Transform door, float angleY)
    {
        Quaternion startRot = door.rotation;
        Quaternion endRot = door.rotation * Quaternion.Euler(0, angleY, 0);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            door.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        door.rotation = endRot;
    }
}
