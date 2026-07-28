using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RoomObject
{
    [Tooltip("Object ki position (Jiske paas jane par light jalegi)")]
    public Transform objectPosition;
    
    [Tooltip("Is object ki Point Light")]
    public Light pointLight;
    
    // Internal variables (Inspector me nahi dikhenge)
    [HideInInspector] public float maxIntensity = 1f;
    [HideInInspector] public float targetIntensity = 0f;
}

public class ProximityTrigger : MonoBehaviour
{
    [Header("Player Settings")]
    [Tooltip("Player ka object yaha dale")]
    public Transform player;

    [Header("Main Room (Main Trigger Area)")]
    [Tooltip("Wo main bada Trigger Area jiske andar 4 objects hain")]
    public Transform mainTriggerArea;
    
    [Tooltip("Jab player is main area me aayega toh ye Audio bajega")]
    public AudioSource mainAudio;
    
    [Tooltip("Main Trigger Area ki distance (Agar Collider nahi lagaya hai toh)")]
    public float mainAreaDistance = 15f;

    public float audioFadeSpeed = 2f;
    private float maxVolume = 1f;
    private float targetVolume = 0f;

    [Header("Room Objects (4 Objects)")]
    [Tooltip("Player object ke kitne paas aaye tab uski Point Light jale?")]
    public float objectActivationDistance = 4f;
    public float lightFadeSpeed = 5f;

    [Tooltip("Apne 4 objects aur unki Point Lights yaha dale")]
    public List<RoomObject> roomObjects = new List<RoomObject>();

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.Find("PlayerArmature");
            if (p == null) p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (mainAudio != null)
        {
            maxVolume = mainAudio.volume;
            mainAudio.volume = 0f;
            targetVolume = 0f;
        }

        foreach (var obj in roomObjects)
        {
            if (obj.pointLight != null)
            {
                obj.maxIntensity = obj.pointLight.intensity;
                obj.pointLight.intensity = 0f;
                obj.targetIntensity = 0f;
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        // ============================================
        // 1. MAIN AREA CHECK (For Audio)
        // ============================================
        bool inMainArea = false;
        if (mainTriggerArea != null)
        {
            Collider mainCol = mainTriggerArea.GetComponent<Collider>();
            if (mainCol != null)
            {
                // Agar Bada BoxCollider (Is Trigger) lagaya hai
                if (mainCol.bounds.Contains(player.position) || mainCol.bounds.Contains(player.position + Vector3.up))
                {
                    inMainArea = true;
                }
            }
            
            if (!inMainArea) // Agar collider se nahi mila, toh distance check karo
            {
                float dist = Vector3.Distance(player.position, mainTriggerArea.position);
                if (dist <= mainAreaDistance) inMainArea = true;
            }
        }

        targetVolume = inMainArea ? maxVolume : 0f;

        // Audio Fade
        if (mainAudio != null)
        {
            mainAudio.volume = Mathf.MoveTowards(mainAudio.volume, targetVolume, maxVolume * audioFadeSpeed * Time.deltaTime);

            if (inMainArea && !mainAudio.isPlaying)
            {
                mainAudio.Play();
            }
            else if (!inMainArea && mainAudio.volume == 0f && mainAudio.isPlaying)
            {
                mainAudio.Pause();
            }
        }

        // ============================================
        // 2. 4 OBJECTS CHECK (For Point Lights)
        // ============================================
        foreach (var obj in roomObjects)
        {
            if (obj.objectPosition == null) continue;

            bool isNearObject = false;

            Collider objCol = obj.objectPosition.GetComponent<Collider>();
            if (objCol != null)
            {
                if (objCol.bounds.Contains(player.position) || objCol.bounds.Contains(player.position + Vector3.up))
                {
                    isNearObject = true;
                }
            }

            if (!isNearObject)
            {
                float objDist = Vector3.Distance(player.position, obj.objectPosition.position);
                if (objDist <= objectActivationDistance) isNearObject = true;
            }

            // Light Target Set karo
            obj.targetIntensity = isNearObject ? obj.maxIntensity : 0f;

            // Light Fade
            if (obj.pointLight != null)
            {
                obj.pointLight.intensity = Mathf.MoveTowards(obj.pointLight.intensity, obj.targetIntensity, obj.maxIntensity * lightFadeSpeed * Time.deltaTime);
            }
        }
    }
}
