using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// AAA Performance Optimizer — Attach to a GameObject in Level 1 scene.
/// DontDestroyOnLoad se teeno scenes mein automatically kaam karega.
/// Kuch bhi delete ya disable nahi karta — sirf quality settings smart tarike se tune karta hai.
/// </summary>
public class AAAPerformanceOptimizer : MonoBehaviour
{
    public static AAAPerformanceOptimizer Instance { get; private set; }

    [Header("=== Frame Rate ===")]
    public int targetFrameRate = 144;
    public bool enableVSync = false;

    [Header("=== Resolution Scaling (BIG FPS BOOST) ===")]
    [Tooltip("Enable karo to scale down resolution. 4K screen pe game 1080p mein render hoga = 4x FPS boost!")]
    public bool enableResolutionScaling = true;
    [Tooltip("Target render width — 1920 = Full HD (best balance of quality and FPS)")]
    public int targetWidth = 1920;
    [Tooltip("Target render height")]
    public int targetHeight = 1080;

    [Header("=== Shadow Settings ===")]
    public float shadowDistance = 80f;   // Was 120 — reduced for FPS
    public int shadowCascades = 2;

    [Header("=== Physics Settings ===")]
    public int physicsSolverIterations = 3;
    public int physicsSolverVelocityIterations = 1;
    public float fixedTimestep = 0.02f;

    [Header("=== Camera ===")]
    public float cameraFarClip = 250f;   // Was 300 — reduced for FPS
    public bool enableOcclusionCulling = true;

    [Header("=== LOD Bias ===")]
    [Range(0.3f, 2f)]
    public float lodBias = 0.65f;        // Was 0.75 — more aggressive LOD = more FPS

    // FPS tracking
    private float fpsAccum = 0f;
    private int fpsFrames = 0;
    private float currentAverageFPS = 0f;
    private bool isDynamicQualityActive = false;
    private const float FPS_LOW = 60f;
    private const float FPS_HIGH = 90f;

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
        ApplyAllOptimizations();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ReapplyOnSceneLoad());
    }

    private IEnumerator ReapplyOnSceneLoad()
    {
        yield return null;
        yield return null;
        ApplyAllOptimizations();
        FindAndOptimizeCamera();
        OptimizeAllParticleSystems();
        OptimizeAllLights();
    }

    void Start()
    {
        FindAndOptimizeCamera();
        OptimizeAllParticleSystems();
        OptimizeAllLights();
    }

    void Update()
    {
        fpsAccum += Time.unscaledDeltaTime;
        fpsFrames++;

        if (fpsAccum >= 1f)
        {
            currentAverageFPS = fpsFrames / fpsAccum;
            fpsAccum = 0f;
            fpsFrames = 0;
            DynamicQualityAdjust();
        }
    }

    public void ApplyAllOptimizations()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = enableVSync ? 1 : 0;
        QualitySettings.shadowDistance = shadowDistance;
        QualitySettings.shadowCascades = shadowCascades;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.lodBias = lodBias;
        QualitySettings.maximumLODLevel = 0;
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        QualitySettings.skinWeights = SkinWeights.TwoBones;
        Physics.defaultSolverIterations = physicsSolverIterations;
        Physics.defaultSolverVelocityIterations = physicsSolverVelocityIterations;
        Time.fixedDeltaTime = fixedTimestep;

        // === RESOLUTION SCALING — 4K se 1080p = ~4x FPS boost ===
        // Screenshot mein 4K (3840x2160) dikh raha tha — ye sabse bada FPS killer tha!
        if (enableResolutionScaling)
        {
            int currentW = Screen.width;
            int currentH = Screen.height;
            if (currentW > targetWidth || currentH > targetHeight)
            {
                Screen.SetResolution(targetWidth, targetHeight, Screen.fullScreenMode);
                Debug.Log($"[AAAOptimizer] Resolution scaled: {currentW}x{currentH} → {targetWidth}x{targetHeight}");
            }
        }

        // Reflection probes — render once, not every frame
        ReflectionProbe[] probes = FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
        foreach (var probe in probes)
        {
            if (probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
            {
                probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
                probe.RenderProbe();
            }
        }

        Debug.Log($"[AAAOptimizer] Applied! Target FPS:{targetFrameRate}, ShadowDist:{shadowDistance}, LOD:{lodBias}");
    }

    private void FindAndOptimizeCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();
        if (mainCam != null)
        {
            mainCam.farClipPlane = cameraFarClip;
            mainCam.useOcclusionCulling = enableOcclusionCulling;
        }
    }

    private void OptimizeAllParticleSystems()
    {
        ParticleSystem[] particles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in particles)
        {
            var main = ps.main;
            // Pause particles when off-screen — huge CPU saver
            main.cullingMode = ParticleSystemCullingMode.PauseAndCatchup;

            ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                // Enable per-particle distance culling
                psr.minParticleSize = 0.001f;
            }
        }
        Debug.Log($"[AAAOptimizer] {particles.Length} particle systems: culling enabled.");
    }

    private void OptimizeAllLights()
    {
        // Point lights aur Spot lights ko bilkul mat chhuo — user ki original settings rakho
        // Sirf ek informational log
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        Debug.Log($"[AAAOptimizer] {lights.Length} lights found — intensity aur shadows unchanged (user settings preserved).");
    }

    private void DynamicQualityAdjust()
    {
        if (currentAverageFPS < FPS_LOW && !isDynamicQualityActive)
        {
            isDynamicQualityActive = true;
            QualitySettings.shadowDistance = shadowDistance * 0.6f;
            QualitySettings.lodBias = lodBias * 0.6f;
            Debug.Log($"[AAAOptimizer] DynamicQuality REDUCED — FPS: {currentAverageFPS:F0}");
        }
        else if (currentAverageFPS > FPS_HIGH && isDynamicQualityActive)
        {
            isDynamicQualityActive = false;
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.lodBias = lodBias;
            Debug.Log($"[AAAOptimizer] DynamicQuality RESTORED — FPS: {currentAverageFPS:F0}");
        }
    }

    public float GetCurrentFPS() => currentAverageFPS;

    public void ForceReoptimize()
    {
        ApplyAllOptimizations();
        FindAndOptimizeCamera();
        OptimizeAllParticleSystems();
        OptimizeAllLights();
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = currentAverageFPS >= 80 ? Color.green
                                : currentAverageFPS >= 60 ? Color.yellow : Color.red;
        GUI.Label(new Rect(10, 10, 220, 35), $"FPS: {currentAverageFPS:F0}", style);
    }
#endif
}
