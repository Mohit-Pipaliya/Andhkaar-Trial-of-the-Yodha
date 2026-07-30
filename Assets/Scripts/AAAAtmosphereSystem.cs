using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// AAA Atmosphere System — Dynamic Fog, Lightning, Volumetric Mist, Ambient Color shifts.
/// Attach to a GameObject in Level 1. DontDestroyOnLoad se teeno scenes mein kaam karega.
/// Har scene ka atmosphere alag aur immersive hoga.
/// </summary>
public class AAAAtmosphereSystem : MonoBehaviour
{
    public static AAAAtmosphereSystem Instance { get; private set; }

    [Header("=== Level Fog Presets ===")]
    [Tooltip("Level 1: Dark teal horror fog — visible but eerie")]
    public Color level1FogColor = new Color(0.12f, 0.14f, 0.20f);  // Brighter teal
    [Tooltip("Level 2: Blood red mist — dark but visible")]
    public Color level2FogColor = new Color(0.22f, 0.06f, 0.06f);  // Warmer red
    [Tooltip("Level 3: Void fog — deep dark purple")]
    public Color level3FogColor = new Color(0.10f, 0.04f, 0.14f);  // Visible purple

    [Header("=== Fog Settings ===")]
    [Tooltip("Fog ON/OFF toggle")]
    public bool enableFog = true;  // ON — realistic linear fog
    [Tooltip("Linear Fog: Is distance tak kuch nahi (clear visibility near player)")]
    public float fogStartDistance = 30f;   // 30 unit tak sab clear dikhega
    [Tooltip("Linear Fog: Is distance par poori tarah fog ho jaayega")]
    public float fogEndDistance = 100f;    // 100 unit se door sab dhak jaayega
    [Tooltip("Fog color ka multiplier — bahut zyada mat badhao")]
    [Range(0.5f, 2f)] public float fogColorBrightness = 1.0f;

    [Header("=== Lightning Settings ===")]
    public bool enableLightning = true;
    public float lightningMinInterval = 8f;
    public float lightningMaxInterval = 20f;
    public Color lightningColor = new Color(0.7f, 0.5f, 1f); // Purple-white
    public float lightningIntensityPeak = 35f;

    [Header("=== Ground Mist Particles ===")]
    public bool enableGroundMist = true;
    public float mistParticleCount = 80f;

    [Header("=== Ambient Settings ===")]
    public float ambientIntensityBase = 1.2f;   // Was 0.85 — still too dark, now properly lit
    public float ambientPulseAmount = 0.08f;
    public float ambientPulseSpeed = 0.15f;

    // Internal state
    private string currentScene = "";
    private Color targetFogColor;
    private Color currentFogColor;
    private Light lightningLight;
    private ParticleSystem groundMistPS;
    private Coroutine lightningRoutine;
    private float fogPulseTimer = 0f;
    private float ambientPulseTimer = 0f;
    private bool isInitialized = false;

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
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        SetupAtmosphere();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(InitAfterSceneLoad());
    }

    private IEnumerator InitAfterSceneLoad()
    {
        yield return null;
        yield return null;
        SetupAtmosphere();
    }

    private void SetupAtmosphere()
    {
        currentScene = SceneManager.GetActiveScene().name;

        // Select fog color based on scene
        if (currentScene == "Level 1")
            targetFogColor = level1FogColor;
        else if (currentScene == "Level 2")
            targetFogColor = level2FogColor;
        else if (currentScene == "Level 3")
            targetFogColor = level3FogColor;
        else
            targetFogColor = level1FogColor;

        // Fog — Linear mode: nearby = clear, distance = fog (real feel)
        RenderSettings.fog = enableFog;
        if (enableFog)
        {
            RenderSettings.fogMode    = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance   = fogEndDistance;
            RenderSettings.fogColor   = targetFogColor * fogColorBrightness;
        }
        currentFogColor = targetFogColor;

        // Ambient light — horror atmosphere but VISIBLE
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        // Brighter ambient so player can see — still dark/cool tone for horror
        Color ambBase = currentScene == "Level 2" ? new Color(0.22f, 0.10f, 0.10f)   // Level 2: warm red tint
                      : currentScene == "Level 3" ? new Color(0.12f, 0.08f, 0.18f)   // Level 3: purple tint
                      :                             new Color(0.16f, 0.18f, 0.22f);   // Level 1: cool blue-grey
        RenderSettings.ambientLight = ambBase;

        // Setup lightning light if not already created
        if (lightningLight == null)
        {
            GameObject lightObj = new GameObject("AAALightningLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = new Vector3(0, 30f, 0);
            lightningLight = lightObj.AddComponent<Light>();
            lightningLight.type = LightType.Directional;
            lightningLight.color = lightningColor;
            lightningLight.intensity = 0f;
            lightningLight.shadows = LightShadows.None; // No shadow = much faster
        }

        // Setup ground mist
        if (enableGroundMist && groundMistPS == null)
        {
            CreateGroundMist();
        }
        else if (groundMistPS != null)
        {
            // Update mist color to match scene
            var main = groundMistPS.main;
            Color mistColor = Color.Lerp(targetFogColor, Color.black, 0.3f);
            mistColor.a = 0.4f;
            main.startColor = mistColor;
        }

        // Start lightning coroutine
        if (lightningRoutine != null) StopCoroutine(lightningRoutine);
        if (enableLightning)
        {
            lightningRoutine = StartCoroutine(LightningRoutine());
        }

        // === Directional Light Intensity — Visible + Horror feel ===
        float dirIntensity = currentScene == "Level 3" ? 0.70f   // Level 3: darkest but visible
                           : currentScene == "Level 2" ? 0.75f   // Level 2: slightly darker
                           :                             0.85f;  // Level 1: properly lit
        SetDirectionalLightIntensity(dirIntensity);

        isInitialized = true;
        Debug.Log($"[AAAAtmosphere] Scene '{currentScene}' atmosphere applied. Fog: {targetFogColor}");
    }

    /// <summary>
    /// Scene ke sabhi Directional Lights ki intensity set karta hai.
    /// Lightning light (jo script khud banati hai) ko skip karta hai.
    /// </summary>
    private void SetDirectionalLightIntensity(float intensity)
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        int count = 0;
        foreach (Light light in allLights)
        {
            if (light.type == LightType.Directional)
            {
                // Apni khud ki lightning light ko skip karo
                if (light == lightningLight) continue;

                light.intensity = intensity;
                count++;
            }
        }
        Debug.Log($"[AAAAtmosphere] {count} Directional Light(s) set to intensity {intensity}.");
    }

    void Update()
    {
        if (!isInitialized) return;

        float dt = Time.deltaTime;
        ambientPulseTimer += dt * ambientPulseSpeed;

        // Linear fog: sirf color update karo, start/end runtime me change nahi karein
        if (enableFog)
        {
            currentFogColor = Color.Lerp(currentFogColor, targetFogColor, dt * 1.5f);
            RenderSettings.fogColor = currentFogColor * fogColorBrightness;
        }

        // Ambient light breathing — scene-based color, clearly visible
        float ambSin = (Mathf.Sin(ambientPulseTimer) + 1f) * 0.5f;
        float ambMult = ambientIntensityBase + ambSin * ambientPulseAmount;
        Color baseAmb = currentScene == "Level 2" ? new Color(0.30f, 0.15f, 0.12f)
                      : currentScene == "Level 3" ? new Color(0.18f, 0.12f, 0.25f)
                      :                             new Color(0.22f, 0.24f, 0.30f);
        RenderSettings.ambientLight = baseAmb * ambMult;
    }

    // =================== LIGHTNING ===================

    private IEnumerator LightningRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(lightningMinInterval, lightningMaxInterval);
            yield return new WaitForSeconds(waitTime);

            if (!Application.isPlaying) yield break;

            // Triple flash sequence for realism
            yield return StartCoroutine(FlashLightning(0.04f, lightningIntensityPeak * 0.4f));
            yield return new WaitForSeconds(0.06f);
            yield return StartCoroutine(FlashLightning(0.02f, lightningIntensityPeak * 0.2f));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(FlashLightning(0.12f, lightningIntensityPeak));

            // Slow fade out
            float t = 0f;
            float startIntensity = lightningLight.intensity;
            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                lightningLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
                yield return null;
            }
            lightningLight.intensity = 0f;
        }
    }

    private IEnumerator FlashLightning(float duration, float intensity)
    {
        lightningLight.intensity = intensity;
        yield return new WaitForSeconds(duration);
        lightningLight.intensity = 0f;
    }

    // =================== GROUND MIST PARTICLES ===================

    private void CreateGroundMist()
    {
        GameObject mistObj = new GameObject("AAAGroundMist");
        mistObj.transform.SetParent(transform);
        mistObj.transform.localPosition = Vector3.zero;

        groundMistPS = mistObj.AddComponent<ParticleSystem>();
        var main = groundMistPS.main;
        main.duration = 10f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(10f, 22f);
        Color mistColor = targetFogColor;
        mistColor.a = 0.35f;
        main.startColor = mistColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = (int)mistParticleCount;
        main.gravityModifier = 0f;

        var emission = groundMistPS.emission;
        emission.rateOverTime = 6f;

        var shape = groundMistPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(60f, 0.5f, 60f); // Wide ground coverage

        var vel = groundMistPS.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        var colorLife = groundMistPS.colorOverLifetime;
        colorLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(targetFogColor * 1.5f, 0.5f),
                new GradientColorKey(Color.black, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.4f, 0.4f),
                new GradientAlphaKey(0.35f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorLife.color = grad;

        // Follow player camera so mist always covers nearby area
        var trigger = groundMistPS.trigger;
        trigger.enabled = false;

        SetupMistMaterial(mistObj);
        Debug.Log("[AAAAtmosphere] Ground mist particle system created.");
    }

    private void SetupMistMaterial(GameObject go)
    {
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer == null) return;

        // Soft additive for ethereal look
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
        {
            mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        }
        mat.SetFloat("_Mode", 2); // Fade
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3001;
        renderer.material = mat;
        renderer.sortingFudge = 10f; // Render behind other transparents
    }

    // =================== PUBLIC API ===================

    /// <summary>Instantly snap fog to a custom color (e.g. on boss trigger)</summary>
    public void SetFogColor(Color newColor, float transitionSpeed = 2f)
    {
        targetFogColor = newColor;
    }

    /// <summary>Trigger a manual lightning flash — use for jump scares</summary>
    public void TriggerLightningFlash()
    {
        StartCoroutine(FlashLightning(0.15f, lightningIntensityPeak));
    }
}
