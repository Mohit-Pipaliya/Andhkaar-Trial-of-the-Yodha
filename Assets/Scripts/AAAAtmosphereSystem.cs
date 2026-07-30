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
    [Tooltip("Level 1: Dark teal horror fog")]
    public Color level1FogColor = new Color(0.04f, 0.06f, 0.08f);
    [Tooltip("Level 2: Blood red mist")]
    public Color level2FogColor = new Color(0.12f, 0.01f, 0.01f);
    [Tooltip("Level 3: Pitch black void fog")]
    public Color level3FogColor = new Color(0.02f, 0.0f, 0.02f);

    [Header("=== Fog Settings ===")]
    public float fogDensityBase = 0.025f;
    [Tooltip("Fog 'breathes' — density oscillates between base and base+pulse")]
    public float fogPulseAmount = 0.008f;
    public float fogPulseSpeed = 0.3f;

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
    public float ambientIntensityBase = 0.2f;
    public float ambientPulseAmount = 0.05f;
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

        // Enable Unity's built-in fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensityBase;
        RenderSettings.fogColor = targetFogColor;
        currentFogColor = targetFogColor;

        // Ambient light — dark but not zero
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.05f, 0.07f);

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

        isInitialized = true;
        Debug.Log($"[AAAAtmosphere] Scene '{currentScene}' atmosphere applied. Fog: {targetFogColor}");
    }

    void Update()
    {
        if (!isInitialized) return;

        float dt = Time.deltaTime;
        fogPulseTimer += dt * fogPulseSpeed;
        ambientPulseTimer += dt * ambientPulseSpeed;

        // Smoothly transition fog color toward target
        currentFogColor = Color.Lerp(currentFogColor, targetFogColor, dt * 2f);

        // Breathing fog density
        float breathSin = (Mathf.Sin(fogPulseTimer) + 1f) * 0.5f;
        float currentDensity = fogDensityBase + breathSin * fogPulseAmount;
        RenderSettings.fogDensity = currentDensity;
        RenderSettings.fogColor = currentFogColor;

        // Breathing ambient light
        float ambSin = (Mathf.Sin(ambientPulseTimer) + 1f) * 0.5f;
        float ambIntensity = ambientIntensityBase + ambSin * ambientPulseAmount;
        Color baseAmb = new Color(0.08f, 0.05f, 0.07f);
        RenderSettings.ambientLight = baseAmb * ambIntensity;
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
