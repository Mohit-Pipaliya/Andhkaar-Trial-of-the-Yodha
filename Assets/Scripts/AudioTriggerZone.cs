using UnityEngine;
using System.Collections;

/// <summary>
/// AudioTriggerZone: Jab player trigger area me enter karta hai to audio play hota hai,
/// aur jab trigger area se bahar nikalta hai to audio stop ya pause ho jata hai.
/// </summary>
public class AudioTriggerZone : MonoBehaviour
{
    [Header("=== Audio Setup ===")]
    [Tooltip("AudioSource component. Agar khali chhodoge toh isi GameObject ka AudioSource use hoga.")]
    public AudioSource targetAudioSource;

    [Tooltip("Play karne ke liye AudioClip. Agar AudioSource me pehle se clip assigned hai toh ise khali chhod sakte hain.")]
    public AudioClip audioClip;

    [Header("=== Trigger Settings ===")]
    [Tooltip("Player ka Tag (Default: 'Player')")]
    public string playerTag = "Player";

    [Header("=== Playback Settings ===")]
    [Tooltip("Kya audio loop me continuous bajna chahiye?")]
    public bool loopAudio = true;

    [Range(0f, 1f)]
    [Tooltip("Audio ka normal volume (0 se 1 ke beech)")]
    public float targetVolume = 1f;

    [Tooltip("Bahar nikalne par: \nTrue = Pause hoga (wapas aane par wahi se continue karega)\nFalse = Stop hoga (agali baar starting se play hoga)")]
    public bool pauseOnExit = false;

    [Header("=== Smooth Fade Settings ===")]
    [Tooltip("Khatam/Shuru hote waqt smooth sound fade chahiye? (Recommended taaki sound jhatke se cut na ho)")]
    public bool useSmoothFade = true;

    [Tooltip("Fade In / Fade Out ka duration (Seconds me)")]
    public float fadeDuration = 0.8f;

    // Private variables
    private Coroutine fadeCoroutine;
    private bool isPlayerInside = false;

    private void Awake()
    {
        // 1. Agar 3D ya 2D Collider hai toh use Trigger ensure karo
        Collider col3D = GetComponent<Collider>();
        if (col3D != null && !col3D.isTrigger)
        {
            col3D.isTrigger = true;
        }

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null && !col2D.isTrigger)
        {
            col2D.isTrigger = true;
        }

        // 2. AudioSource setup
        if (targetAudioSource == null)
        {
            targetAudioSource = GetComponent<AudioSource>();
            if (targetAudioSource == null)
            {
                targetAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // 3. Clip assign karna
        if (audioClip != null)
        {
            targetAudioSource.clip = audioClip;
        }

        targetAudioSource.loop = loopAudio;
        targetAudioSource.playOnAwake = false;

        // Initial volume setting
        if (useSmoothFade)
        {
            targetAudioSource.volume = 0f;
        }
        else
        {
            targetAudioSource.volume = targetVolume;
        }
    }

    // ─────────────────────────────────────────────
    // 3D TRIGGER DETECTIONS
    // ─────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other.gameObject))
        {
            OnPlayerEnteredZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other.gameObject))
        {
            OnPlayerExitedZone();
        }
    }

    // ─────────────────────────────────────────────
    // 2D TRIGGER DETECTIONS (Agar 2D use ho raha ho)
    // ─────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other.gameObject))
        {
            OnPlayerEnteredZone();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other.gameObject))
        {
            OnPlayerExitedZone();
        }
    }

    // ─────────────────────────────────────────────
    // PLAYER CHECK LOGIC
    // ─────────────────────────────────────────────
    private bool IsPlayer(GameObject obj)
    {
        // Tag check (khud ka tag ya parent root ka tag)
        if (obj.CompareTag(playerTag)) return true;
        if (obj.transform.root != null && obj.transform.root.CompareTag(playerTag)) return true;

        // Component check (Agar PlayerController script lagi hai)
        if (obj.GetComponentInParent<PlayerController>() != null) return true;

        return false;
    }

    // ─────────────────────────────────────────────
    // AUDIO CONTROLS
    // ─────────────────────────────────────────────
    private void OnPlayerEnteredZone()
    {
        isPlayerInside = true;

        if (targetAudioSource == null || targetAudioSource.clip == null) return;

        // Audio start / unpause
        if (!targetAudioSource.isPlaying)
        {
            if (pauseOnExit)
            {
                targetAudioSource.UnPause();
                if (!targetAudioSource.isPlaying)
                {
                    targetAudioSource.Play();
                }
            }
            else
            {
                targetAudioSource.Play();
            }
        }

        // Fade in volume
        if (useSmoothFade)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeVolume(targetVolume, null));
        }
        else
        {
            targetAudioSource.volume = targetVolume;
        }
    }

    private void OnPlayerExitedZone()
    {
        isPlayerInside = false;

        if (targetAudioSource == null) return;

        // Fade out volume or immediate stop
        if (useSmoothFade)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeVolume(0f, () =>
            {
                if (pauseOnExit)
                {
                    targetAudioSource.Pause();
                }
                else
                {
                    targetAudioSource.Stop();
                }
            }));
        }
        else
        {
            if (pauseOnExit)
            {
                targetAudioSource.Pause();
            }
            else
            {
                targetAudioSource.Stop();
            }
            targetAudioSource.volume = 0f;
        }
    }

    private IEnumerator FadeVolume(float toVolume, System.Action onComplete)
    {
        if (fadeDuration <= 0f)
        {
            targetAudioSource.volume = toVolume;
            onComplete?.Invoke();
            yield break;
        }

        float startVolume = targetAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            targetAudioSource.volume = Mathf.Lerp(startVolume, toVolume, elapsed / fadeDuration);
            yield return null;
        }

        targetAudioSource.volume = toVolume;
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────
    // SCENE VIEW GIZMOS (Area visualize karne ke liye)
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = isPlayerInside ? new Color(0f, 1f, 0.2f, 0.25f) : new Color(0.1f, 0.6f, 1f, 0.2f);
            
            if (col is BoxCollider box)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = isPlayerInside ? Color.green : new Color(0.1f, 0.6f, 1f, 0.8f);
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                Gizmos.color = isPlayerInside ? Color.green : new Color(0.1f, 0.6f, 1f, 0.8f);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}

