using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// AAA Ambient Sound System — Procedural wind, cave drips, horror ambience, heartbeat, boss tension.
/// Attach to Level 1 scene GameObject. DontDestroyOnLoad se teeno scenes mein kaam karega.
/// Existing AudioManager ke saath conflict nahi karega — sirf ambient layer add karta hai.
/// </summary>
public class AAAAmbientSoundSystem : MonoBehaviour
{
    public static AAAAmbientSoundSystem Instance { get; private set; }

    [Header("=== Wind Ambient ===")]
    [Tooltip("Continuous wind howl — procedurally generated using sine waves")]
    public bool enableWind = true;
    [Range(0f, 1f)] public float windBaseVolume = 0.18f;
    [Range(0f, 1f)] public float windGustVolume = 0.5f;
    public float windGustInterval = 12f;

    [Header("=== Cave Drip Sounds ===")]
    [Tooltip("Periodic water drip sounds for cave/dungeon atmosphere")]
    public bool enableCaveDrips = true;
    [Range(0f, 1f)] public float dripVolume = 0.35f;
    public float dripMinInterval = 4f;
    public float dripMaxInterval = 12f;

    [Header("=== Horror Ambience ===")]
    [Tooltip("Distant horror sounds — whispers, distant screams, creaks")]
    public bool enableHorrorAmbience = true;
    [Range(0f, 1f)] public float horrorVolume = 0.25f;
    public float horrorMinInterval = 20f;
    public float horrorMaxInterval = 45f;

    [Header("=== Heartbeat (Low Health) ===")]
    [Tooltip("Heartbeat starts playing when player health is below this threshold")]
    [Range(0f, 1f)] public float heartbeatHealthThreshold = 0.35f;
    [Range(0f, 1f)] public float heartbeatMaxVolume = 0.6f;

    [Header("=== Boss Tension ===")]
    [Tooltip("Distance within which boss tension sound layer kicks in")]
    public float bossTensionRadius = 30f;
    [Range(0f, 1f)] public float bossTensionVolume = 0.4f;

    // Audio sources
    private AudioSource windSource;
    private AudioSource heartbeatSource;
    private AudioSource bossTensionSource;
    private AudioSource sfxSource; // One-shot SFX

    // Player reference (cached)
    private PlayerController playerController;
    private Transform playerTransform;

    // Boss reference
    private BossEnemyAi bossEnemy;

    // Wind state
    private float windTimer = 0f;
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
        FindSceneReferences();
        StartAmbientCoroutines();
        currentScene = SceneManager.GetActiveScene().name;
    }

    void Start()
    {
        currentScene = SceneManager.GetActiveScene().name;
        FindSceneReferences();
        StartAmbientCoroutines();

        // Start wind
        if (enableWind) StartCoroutine(WindRoutine());
    }

    void Update()
    {
        UpdateHeartbeat();
        UpdateBossTension();
        AnimateWind();
    }

    // =================== SETUP ===================

    private void CreateAudioSources()
    {
        // Wind — continuous loop
        windSource = CreateAudioSource("WindSource", windBaseVolume, true, 0);

        // Heartbeat — loop
        heartbeatSource = CreateAudioSource("HeartbeatSource", 0f, true, 128);
        heartbeatSource.pitch = 1f;

        // Boss tension — loop
        bossTensionSource = CreateAudioSource("BossTensionSource", 0f, true, 64);

        // One-shot SFX
        sfxSource = CreateAudioSource("SFXSource", 1f, false, 256);
    }

    private AudioSource CreateAudioSource(string name, float volume, bool loop, int priority)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        AudioSource src = go.AddComponent<AudioSource>();
        src.volume = volume;
        src.loop = loop;
        src.priority = priority;
        src.spatialBlend = 0f; // 2D — ambient sounds fill the world
        src.playOnAwake = false;
        return src;
    }

    private void FindSceneReferences()
    {
        // Cache player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        // Cache boss
        bossEnemy = FindFirstObjectByType<BossEnemyAi>();

        Debug.Log($"[AAAAmbient] Scene refs found — Player: {playerTransform != null}, Boss: {bossEnemy != null}");
    }

    private void StartAmbientCoroutines()
    {
        if (dripCoroutine != null) StopCoroutine(dripCoroutine);
        if (horrorCoroutine != null) StopCoroutine(horrorCoroutine);

        if (enableCaveDrips) dripCoroutine = StartCoroutine(CaveDripRoutine());
        if (enableHorrorAmbience) horrorCoroutine = StartCoroutine(HorrorAmbienceRoutine());
    }

    // =================== WIND ===================

    private float windPhase = 0f;
    private float gustTimer = 0f;

    private IEnumerator WindRoutine()
    {
        // Generate procedural wind sound using AudioClip synthesis
        AudioClip windClip = GenerateWindClip(4f, 44100);
        windSource.clip = windClip;
        windSource.volume = windBaseVolume;
        windSource.Play();
        yield break;
    }

    private void AnimateWind()
    {
        if (!enableWind || windSource == null) return;

        windPhase += Time.deltaTime;
        gustTimer += Time.deltaTime;

        // Gentle wind breathing
        float breathe = Mathf.Sin(windPhase * 0.4f) * 0.5f + 0.5f;
        float targetVol = Mathf.Lerp(windBaseVolume * 0.6f, windBaseVolume, breathe);

        // Random gust
        if (gustTimer >= windGustInterval + Random.Range(-3f, 3f))
        {
            gustTimer = 0f;
            isGusting = true;
        }

        if (isGusting)
        {
            targetVol = Mathf.MoveTowards(windSource.volume, windGustVolume, Time.deltaTime * 0.8f);
            if (windSource.volume >= windGustVolume * 0.95f) isGusting = false;
        }
        else
        {
            windSource.volume = Mathf.Lerp(windSource.volume, targetVol, Time.deltaTime * 0.5f);
        }

        // Subtle pitch variation
        windSource.pitch = 0.9f + Mathf.Sin(windPhase * 0.2f) * 0.1f;
    }

    // =================== CAVE DRIP ===================

    private IEnumerator CaveDripRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(dripMinInterval, dripMaxInterval);
            yield return new WaitForSeconds(waitTime);

            if (!Application.isPlaying) yield break;

            // Play procedural drip sound
            AudioClip dripClip = GenerateDripClip();
            sfxSource.volume = dripVolume * Random.Range(0.7f, 1.3f);
            sfxSource.pitch = Random.Range(0.8f, 1.4f);
            sfxSource.PlayOneShot(dripClip);
        }
    }

    // =================== HORROR AMBIENCE ===================

    private IEnumerator HorrorAmbienceRoutine()
    {
        yield return new WaitForSeconds(5f); // Initial delay

        while (true)
        {
            float waitTime = Random.Range(horrorMinInterval, horrorMaxInterval);
            yield return new WaitForSeconds(waitTime);

            if (!Application.isPlaying) yield break;

            // Random horror sounds
            int choice = Random.Range(0, 3);
            AudioClip clip = null;

            switch (choice)
            {
                case 0: clip = GenerateWhisperClip(); break;     // Whisper
                case 1: clip = GenerateCreakClip(); break;        // Creak
                case 2: clip = GenerateDistantHowlClip(); break;  // Distant howl
            }

            if (clip != null)
            {
                sfxSource.volume = horrorVolume * Random.Range(0.5f, 1f);
                sfxSource.pitch = Random.Range(0.7f, 1.1f);
                sfxSource.PlayOneShot(clip);
            }
        }
    }

    // =================== HEARTBEAT ===================

    private float heartbeatPhase = 0f;

    private void UpdateHeartbeat()
    {
        if (playerController == null || heartbeatSource == null) return;

        float healthFraction = playerController.currentHealth / playerController.maxHealth;
        bool shouldBeat = healthFraction <= heartbeatHealthThreshold;

        if (shouldBeat)
        {
            if (!heartbeatSource.isPlaying)
            {
                AudioClip hbClip = GenerateHeartbeatClip();
                heartbeatSource.clip = hbClip;
                heartbeatSource.Play();
            }

            // Faster beat when lower health
            float lowHealthFactor = 1f - (healthFraction / heartbeatHealthThreshold);
            heartbeatSource.pitch = Mathf.Lerp(0.8f, 1.6f, lowHealthFactor);
            heartbeatSource.volume = Mathf.Lerp(0.1f, heartbeatMaxVolume, lowHealthFactor);
        }
        else
        {
            if (heartbeatSource.isPlaying)
            {
                heartbeatSource.volume = Mathf.MoveTowards(heartbeatSource.volume, 0f, Time.deltaTime * 0.5f);
                if (heartbeatSource.volume <= 0.01f) heartbeatSource.Stop();
            }
        }
    }

    // =================== BOSS TENSION ===================

    private float bossTensionPhase = 0f;

    private void UpdateBossTension()
    {
        if (bossEnemy == null || bossEnemy.isDead || bossTensionSource == null) return;
        if (playerTransform == null) return;

        float dist = Vector3.Distance(playerTransform.position, bossEnemy.transform.position);
        float proximity = 1f - Mathf.Clamp01(dist / bossTensionRadius);

        float targetVol = proximity * bossTensionVolume;

        if (targetVol > 0.05f && !bossTensionSource.isPlaying)
        {
            AudioClip tensionClip = GenerateTensionDroneClip();
            bossTensionSource.clip = tensionClip;
            bossTensionSource.Play();
        }

        bossTensionSource.volume = Mathf.Lerp(bossTensionSource.volume, targetVol, Time.deltaTime * 1.5f);

        // Pitch variation for tension feel
        bossTensionPhase += Time.deltaTime;
        bossTensionSource.pitch = 0.8f + Mathf.Sin(bossTensionPhase * 0.5f) * 0.15f;
    }

    // =================== PROCEDURAL AUDIO CLIP GENERATORS ===================
    // These generate AudioClips at runtime using raw PCM data — no external audio files needed!

    private AudioClip GenerateWindClip(float duration, int sampleRate)
    {
        int samples = Mathf.RoundToInt(duration * sampleRate);
        float[] data = new float[samples];

        // Layered noise — multiple frequencies for realistic wind
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float noise = 0f;
            noise += Mathf.PerlinNoise(t * 2.3f, 0.5f) * 0.6f;
            noise += Mathf.PerlinNoise(t * 5.7f, 1.2f) * 0.25f;
            noise += Mathf.PerlinNoise(t * 11.3f, 2.7f) * 0.1f;
            noise += (Random.value - 0.5f) * 0.05f; // Very subtle white noise
            data[i] = (noise - 0.5f) * 2f * 0.5f; // Normalize to [-1, 1]
        }

        AudioClip clip = AudioClip.Create("ProceduralWind", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateDripClip()
    {
        int sampleRate = 44100;
        float duration = 0.3f;
        int samples = Mathf.RoundToInt(duration * sampleRate);
        float[] data = new float[samples];

        // Drip = short ping with exponential decay + slight resonance
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float decay = Mathf.Exp(-t * 18f);
            float tone = Mathf.Sin(2 * Mathf.PI * 680f * t); // Mid-high frequency drip tone
            float resonance = Mathf.Sin(2 * Mathf.PI * 340f * t) * 0.3f;
            data[i] = (tone + resonance) * decay * 0.7f;
        }

        AudioClip clip = AudioClip.Create("ProceduralDrip", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateWhisperClip()
    {
        int sampleRate = 44100;
        float duration = 1.5f;
        int samples = Mathf.RoundToInt(duration * sampleRate);
        float[] data = new float[samples];

        // Whisper = filtered noise with soft attack and decay
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t / duration);
            float noise = (Random.value - 0.5f) * 2f;
            // Band-pass simulation: keep only mid frequencies
            float filtered = noise * 0.4f + (i > 0 ? data[i-1] * 0.6f : 0f);
            data[i] = filtered * envelope * 0.3f;
        }

        AudioClip clip = AudioClip.Create("ProceduralWhisper", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateCreakClip()
    {
        int sampleRate = 44100;
        float duration = 0.8f;
        int samples = Mathf.RoundToInt(duration * sampleRate);
        float[] data = new float[samples];

        float freq = Random.Range(120f, 200f); // Low creak frequency
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t / duration);
            float creak = Mathf.Sin(2 * Mathf.PI * freq * t * (1f + t * 0.5f)); // Pitch glide
            float noise = (Random.value - 0.5f) * 0.3f;
            data[i] = (creak * 0.7f + noise) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("ProceduralCreak", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateDistantHowlClip()
    {
        int sampleRate = 44100;
        float duration = 2f;
        int samples = Mathf.RoundToInt(duration * sampleRate);
        float[] data = new float[samples];

        float baseFreq = Random.Range(180f, 280f);
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t / duration) * Mathf.Exp(-t * 0.5f);
            float howl = Mathf.Sin(2 * Mathf.PI * baseFreq * t);
            float harmonic = Mathf.Sin(2 * Mathf.PI * baseFreq * 2f * t) * 0.3f;
            float vibrato = Mathf.Sin(2 * Mathf.PI * 5f * t) * 0.02f;
            data[i] = (howl + harmonic + vibrato) * envelope * 0.25f;
        }

        AudioClip clip = AudioClip.Create("ProceduralHowl", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateHeartbeatClip()
    {
        int sampleRate = 44100;
        float duration = 2.5f;
        int samples = Mathf.RoundToInt(duration * sampleRate);
        float[] data = new float[samples];

        // Two thumps: lub-DUB pattern
        float thump1Time = 0.1f;
        float thump2Time = 0.35f;
        float thumpFreq = 60f; // Very low frequency thump

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float val = 0f;

            // Thump 1 (lub)
            if (t >= thump1Time && t < thump1Time + 0.15f)
            {
                float local = t - thump1Time;
                float env = Mathf.Exp(-local * 25f);
                val += Mathf.Sin(2 * Mathf.PI * thumpFreq * local) * env * 0.7f;
            }
            // Thump 2 (DUB) — slightly louder
            if (t >= thump2Time && t < thump2Time + 0.2f)
            {
                float local = t - thump2Time;
                float env = Mathf.Exp(-local * 18f);
                val += Mathf.Sin(2 * Mathf.PI * thumpFreq * 0.85f * local) * env * 1.0f;
            }
            data[i] = val * 0.8f;
        }

        AudioClip clip = AudioClip.Create("ProceduralHeartbeat", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateTensionDroneClip()
    {
        int sampleRate = 22050; // Lower sample rate for drone = saves memory
        float duration = 8f;
        int samples = Mathf.RoundToInt(duration * sampleRate);
        float[] data = new float[samples];

        float rootFreq = 55f; // Deep A1 note — sinister drone

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float root = Mathf.Sin(2 * Mathf.PI * rootFreq * t);
            float fifth = Mathf.Sin(2 * Mathf.PI * rootFreq * 1.5f * t) * 0.5f;
            float detuned = Mathf.Sin(2 * Mathf.PI * (rootFreq + 0.5f) * t) * 0.3f; // Slight detune = tension
            float sub = Mathf.Sin(2 * Mathf.PI * rootFreq * 0.5f * t) * 0.6f;
            float tremolo = 1f + Mathf.Sin(2 * Mathf.PI * 4.5f * t) * 0.15f; // 4.5 Hz tremolo
            data[i] = (root + fifth + detuned + sub) * tremolo * 0.2f;
        }

        AudioClip clip = AudioClip.Create("ProceduralTensionDrone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // =================== PUBLIC API ===================

    public void PlayBossRoarTension()
    {
        if (bossTensionSource != null)
        {
            bossTensionSource.volume = bossTensionVolume;
        }
    }

    public void StopBossTension()
    {
        if (bossTensionSource != null)
        {
            StartCoroutine(FadeOutAudio(bossTensionSource, 2f));
        }
    }

    private IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        source.Stop();
        source.volume = startVol;
    }
}
