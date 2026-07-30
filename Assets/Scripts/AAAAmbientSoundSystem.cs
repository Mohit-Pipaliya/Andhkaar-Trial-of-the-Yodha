using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// AAA Ambient Sound System — CLEAN VERSION (no procedural audio = no bip/click sounds)
/// Saari sounds ke liye external AudioClip assign karo Inspector mein.
/// Attach to Level 1 scene. DontDestroyOnLoad se teeno scenes mein kaam karega.
/// </summary>
public class AAAAmbientSoundSystem : MonoBehaviour
{
    public static AAAAmbientSoundSystem Instance { get; private set; }

    [Header("=== Wind / Ambient Loop ===")]
    [Tooltip("Real wind AudioClip assign karo \u2014 procedural generation hataya, woh click sound de raha tha")]
    public AudioClip windClip;
    public bool enableWind = false;
    [Range(0f, 1f)] public float windBaseVolume = 0.10f;
    [Range(0f, 1f)] public float windGustVolume = 0.20f;
    public float windGustInterval = 18f;

    [Header("=== Cave Drip Sounds ===")]
    [Tooltip("Real drip AudioClip assign karo")]
    public AudioClip dripClip;
    public bool enableCaveDrips = false;
    [Range(0f, 1f)] public float dripVolume = 0.20f;
    public float dripMinInterval = 10f;
    public float dripMaxInterval = 25f;

    [Header("=== Horror Ambience ===")]
    [Tooltip("Real horror ambience AudioClip assign karo")]
    public AudioClip horrorAmbienceClip;
    public bool enableHorrorAmbience = false;
    [Range(0f, 1f)] public float horrorVolume = 0.15f;
    public float horrorMinInterval = 35f;
    public float horrorMaxInterval = 80f;

    [Header("=== Heartbeat (Low Health) ===")]
    [Tooltip("Real heartbeat AudioClip assign karo")]
    public AudioClip heartbeatClip;
    [Range(0f, 1f)] public float heartbeatHealthThreshold = 0.30f;
    [Range(0f, 1f)] public float heartbeatMaxVolume = 0.55f;

    [Header("=== Boss Tension ===")]
    [Tooltip("Real boss tension music AudioClip assign karo")]
    public AudioClip bossTensionClip;
    public float bossTensionRadius = 30f;
    [Range(0f, 1f)] public float bossTensionVolume = 0.35f;

    // Internal audio sources
    private AudioSource windSource;
    private AudioSource heartbeatSource;
    private AudioSource bossTensionSource;
    private AudioSource sfxSource;

    // Cached references
    private PlayerController playerController;
    private Transform playerTransform;
    private BossEnemyAi bossEnemy;

    // Wind animation state
    private float windPhase = 0f;
    private float gustTimer = 0f;
    private bool isGusting = false;

    // Scene tracking
    private string currentScene = "";
    private Coroutine dripCoroutine;
    private Coroutine horrorCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        CreateAudioSources();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ReinitAfterSceneLoad());
    }

    private IEnumerator ReinitAfterSceneLoad()
    {
        yield return null;
        yield return null;
        currentScene = SceneManager.GetActiveScene().name;
        FindSceneReferences();
        RestartAmbientCoroutines();
    }

    void Start()
    {
        currentScene = SceneManager.GetActiveScene().name;
        FindSceneReferences();
        RestartAmbientCoroutines();

        // Wind loop — only if clip assigned
        if (enableWind && windClip != null)
        {
            windSource.clip = windClip;
            windSource.volume = windBaseVolume;
            windSource.loop = true;
            windSource.Play();
        }
    }

    void Update()
    {
        AnimateWind();
        UpdateHeartbeat();
        UpdateBossTension();
    }

    // =================== AUDIO SOURCE SETUP ===================

    private void CreateAudioSources()
    {
        windSource        = MakeSource("WindSource",        windBaseVolume, true,  0);
        heartbeatSource   = MakeSource("HeartbeatSource",   0f,            true,  128);
        bossTensionSource = MakeSource("BossTensionSource", 0f,            true,  64);
        sfxSource         = MakeSource("SFXSource",         1f,            false, 256);
    }

    private AudioSource MakeSource(string name, float vol, bool loop, int priority)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        AudioSource src = go.AddComponent<AudioSource>();
        src.volume = vol;
        src.loop = loop;
        src.priority = priority;
        src.spatialBlend = 0f; // 2D ambient
        src.playOnAwake = false;
        src.dopplerLevel = 0f;
        return src;
    }

    private void FindSceneReferences()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
        bossEnemy = FindFirstObjectByType<BossEnemyAi>();
    }

    private void RestartAmbientCoroutines()
    {
        if (dripCoroutine   != null) StopCoroutine(dripCoroutine);
        if (horrorCoroutine != null) StopCoroutine(horrorCoroutine);

        if (enableCaveDrips    && dripClip            != null) dripCoroutine   = StartCoroutine(DripRoutine());
        if (enableHorrorAmbience && horrorAmbienceClip != null) horrorCoroutine = StartCoroutine(HorrorRoutine());
    }

    // =================== WIND ===================

    private void AnimateWind()
    {
        if (!enableWind || windSource == null || !windSource.isPlaying) return;

        windPhase += Time.deltaTime;
        gustTimer += Time.deltaTime;

        float breathe   = Mathf.Sin(windPhase * 0.35f) * 0.5f + 0.5f;
        float targetVol = Mathf.Lerp(windBaseVolume * 0.6f, windBaseVolume, breathe);

        if (gustTimer >= windGustInterval)
        {
            gustTimer = 0f;
            isGusting = true;
        }

        if (isGusting)
        {
            windSource.volume = Mathf.MoveTowards(windSource.volume, windGustVolume, Time.deltaTime * 0.6f);
            if (windSource.volume >= windGustVolume * 0.96f) isGusting = false;
        }
        else
        {
            windSource.volume = Mathf.Lerp(windSource.volume, targetVol, Time.deltaTime * 0.4f);
        }

        windSource.pitch = 0.92f + Mathf.Sin(windPhase * 0.18f) * 0.08f;
    }

    // =================== CAVE DRIP ===================

    private IEnumerator DripRoutine()
    {
        yield return new WaitForSeconds(Random.Range(3f, 8f)); // Initial delay
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(dripMinInterval, dripMaxInterval));
            if (!Application.isPlaying || dripClip == null) yield break;

            sfxSource.volume = dripVolume * Random.Range(0.7f, 1.2f);
            sfxSource.pitch  = Random.Range(0.85f, 1.35f);
            sfxSource.PlayOneShot(dripClip);
        }
    }

    // =================== HORROR AMBIENCE ===================

    private IEnumerator HorrorRoutine()
    {
        yield return new WaitForSeconds(10f);
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(horrorMinInterval, horrorMaxInterval));
            if (!Application.isPlaying || horrorAmbienceClip == null) yield break;

            sfxSource.volume = horrorVolume * Random.Range(0.5f, 1f);
            sfxSource.pitch  = Random.Range(0.75f, 1.05f);
            sfxSource.PlayOneShot(horrorAmbienceClip);
        }
    }

    // =================== HEARTBEAT ===================

    private void UpdateHeartbeat()
    {
        // No clip assigned = nothing to do
        if (heartbeatClip == null || heartbeatSource == null) return;
        if (playerController == null) return;

        // Safe division
        float maxHP = Mathf.Max(playerController.maxHealth, 1f);
        float healthFraction = playerController.currentHealth / maxHP;
        bool shouldBeat = healthFraction <= heartbeatHealthThreshold && healthFraction > 0f;

        if (shouldBeat)
        {
            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.clip = heartbeatClip;
                heartbeatSource.Play();
            }
            float factor = 1f - (healthFraction / Mathf.Max(heartbeatHealthThreshold, 0.01f));
            heartbeatSource.pitch  = Mathf.Lerp(0.85f, 1.55f, factor);
            heartbeatSource.volume = Mathf.Lerp(0.1f, heartbeatMaxVolume, factor);
        }
        else
        {
            if (heartbeatSource.isPlaying)
            {
                heartbeatSource.volume = Mathf.MoveTowards(heartbeatSource.volume, 0f, Time.deltaTime * 0.8f);
                if (heartbeatSource.volume <= 0.01f) heartbeatSource.Stop();
            }
        }
    }

    // =================== BOSS TENSION ===================

    private float bossTensionPhase = 0f;

    private void UpdateBossTension()
    {
        if (bossTensionClip == null || bossEnemy == null || bossEnemy.isDead) return;
        if (playerTransform == null || bossTensionSource == null) return;

        float dist      = Vector3.Distance(playerTransform.position, bossEnemy.transform.position);
        float proximity = 1f - Mathf.Clamp01(dist / Mathf.Max(bossTensionRadius, 1f));
        float targetVol = proximity * bossTensionVolume;

        if (targetVol > 0.05f && !bossTensionSource.isPlaying)
        {
            bossTensionSource.clip = bossTensionClip;
            bossTensionSource.Play();
        }

        bossTensionSource.volume = Mathf.Lerp(bossTensionSource.volume, targetVol, Time.deltaTime * 1.5f);

        bossTensionPhase += Time.deltaTime;
        bossTensionSource.pitch = 0.82f + Mathf.Sin(bossTensionPhase * 0.45f) * 0.12f;
    }

    // =================== PUBLIC API ===================

    public void StopBossTension()
    {
        if (bossTensionSource != null)
            StartCoroutine(FadeOut(bossTensionSource, 2f));
    }

    private IEnumerator FadeOut(AudioSource src, float dur)
    {
        float startVol = src.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, t / dur);
            yield return null;
        }
        src.Stop();
        src.volume = startVol;
    }
}
