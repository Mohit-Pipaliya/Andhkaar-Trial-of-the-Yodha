using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class LightningEffect : MonoBehaviour
{
    [Header("Timing Settings")]
    [Tooltip("Minimum time (in seconds) between lightning bursts")]
    public float minTimeBetweenFlashes = 3f;
    [Tooltip("Maximum time (in seconds) between lightning bursts")]
    public float maxTimeBetweenFlashes = 10f;
    
    [Header("Intensity Settings")]
    [Tooltip("Target intensity during a flash")]
    public float flashIntensity = 5f;
    [Tooltip("Should the light completely turn off after a flash? (Useful for dedicated lightning lights)")]
    public bool turnOffAfterFlash = false;
    public float flashDuration = 0.1f;
    public int maxFlashesPerBurst = 3;

    [Header("Audio Settings (Optional)")]
    public AudioSource thunderAudioSource;
    public AudioClip[] thunderSounds;
    public float minThunderDelay = 0.2f; // To simulate distance of lightning
    public float maxThunderDelay = 1.0f;

    private Light targetLight;
    private float baseIntensity;
    private bool isFlashing = false;

    void Start()
    {
        targetLight = GetComponent<Light>();
        baseIntensity = targetLight.intensity;

        if (turnOffAfterFlash)
        {
            targetLight.intensity = 0f;
            targetLight.enabled = false;
        }

        if (thunderAudioSource == null)
        {
            thunderAudioSource = gameObject.AddComponent<AudioSource>();
            thunderAudioSource.spatialBlend = 1f; // 3D sound so it feels like it's coming from somewhere
            thunderAudioSource.minDistance = 50f;
            thunderAudioSource.maxDistance = 500f;
        }
        
        StartCoroutine(LightningRoutine());
    }

    IEnumerator LightningRoutine()
    {
        while (true)
        {
            // Random time wait karo agli lightning burst ke liye
            float waitTime = Random.Range(minTimeBetweenFlashes, maxTimeBetweenFlashes);
            yield return new WaitForSeconds(waitTime);

            if (!isFlashing)
            {
                StartCoroutine(FlashBurstRoutine());
            }
        }
    }

    IEnumerator FlashBurstRoutine()
    {
        isFlashing = true;
        int flashCount = Random.Range(1, maxFlashesPerBurst + 1);
        
        for (int i = 0; i < flashCount; i++)
        {
            // Flash on
            if (turnOffAfterFlash) targetLight.enabled = true;
            targetLight.intensity = flashIntensity;
            
            // Choti si der ke liye ruko
            yield return new WaitForSeconds(Random.Range(0.02f, flashDuration));
            
            // Flash off
            if (turnOffAfterFlash) 
            {
                targetLight.intensity = 0f;
                targetLight.enabled = false;
            }
            else
            {
                targetLight.intensity = baseIntensity;
            }
            
            // Agar aur flashes baaki hain toh thoda delay do (multiple strikes illusion)
            if (i < flashCount - 1)
            {
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }
        }
        
        // Agar audio lagaya hai toh thodi der me thunder sound play karo (distance simulation)
        if (thunderAudioSource != null && thunderSounds != null && thunderSounds.Length > 0)
        {
            float thunderDelay = Random.Range(minThunderDelay, maxThunderDelay);
            yield return new WaitForSeconds(thunderDelay);
            
            AudioClip clip = thunderSounds[Random.Range(0, thunderSounds.Length)];
            thunderAudioSource.PlayOneShot(clip);
        }
        
        isFlashing = false;
    }
}
