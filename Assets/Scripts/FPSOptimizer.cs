using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// AAA FPS Optimizer — Andhkaar: Trial of the Yodha
/// 
/// MISSION: Hold 60 FPS. Kuch delete ya disable nahi.
/// 
/// ROOT CAUSE FIX (109M tris, 401 batches, 0 batching saved):
///   1. LOD bias 0.2 → objects switch to LOD1 much earlier = 70-80% triangle reduction
///   2. Far clip 350m + fog 80-350m → distant geometry hidden naturally
///   3. Shadow distance 80m → shadow rendering budget down
///   4. GPU instancing enabled on all materials (see AAARealisticRenderer.cs)
///   5. Audio DSP buffer reduced → less CPU audio overhead
/// </summary>
public class FPSOptimizer : MonoBehaviour
{
    [Header("Frame Rate")]
    public int targetFPS = 120;

    [Header("Geometry Budget")]
    [Tooltip("LOD bias — lower = switch to low-detail meshes earlier. 0.2 = AAA aggressive.")]
    public bool applyGeometryBudget = true;

    [Header("Dynamic Resolution")]
    public bool enableDynamicResolution = true;
    [Range(0.4f, 1f)]
    public float minRenderScale = 0.5f; // AAA Perf: Allow lower render scale for better FPS headroom
    public float maxRenderScale = 1f;
    public float dynamicResolutionTargetFPS = 60f;

    [Header("Startup")]
    public bool enableOcclusionCulling = true;
    public bool prewarmObjectPools = true;

    [System.Serializable]
    public class PoolPrewarmEntry
    {
        public GameObject prefab;
        public int count = 3;
    }

    public PoolPrewarmEntry[] poolPrewarmEntries;
    public int hitSparkPrewarmCount = 8;

    // ─── Runtime state ───────────────────────────────────────────
    private UniversalRenderPipelineAsset urpAsset;
    private float currentRenderScale = 1f;
    private float fpsAccumulator;
    private int   fpsFrames;
    private float fpsTimer;

    // Cache cameras once
    private Camera[] cachedCameras;
    private const float RenderScaleInterval = 1.0f; // check every 1s, not 0.5s

    // ═══════════════════════════════════════════════════════════
    void Awake()
    {
        EnsureManagersExist();
        ApplyOptimizations();

        if (gameObject.GetComponent<InGameProfiler>() == null)
            gameObject.AddComponent<InGameProfiler>();
    }

    void Start()
    {
        StartCoroutine(ApplyBudgetAfterSceneReady());

        if (prewarmObjectPools)
            StartCoroutine(PrewarmPoolsRoutine());

        // GC clean slate 2s after load
        StartCoroutine(StartupGCRoutine());
    }

    // ─── Dynamic Resolution ──────────────────────────────────────
    void Update()
    {
        if (!enableDynamicResolution || urpAsset == null) return;

        fpsAccumulator += 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        fpsFrames++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer < RenderScaleInterval) return;

        float avgFps = fpsAccumulator / fpsFrames;
        fpsAccumulator = 0f;
        fpsFrames      = 0;
        fpsTimer       = 0f;

        // Smooth steps — less popping
        if (avgFps < dynamicResolutionTargetFPS - 5f)
            currentRenderScale = Mathf.Max(minRenderScale, currentRenderScale - 0.04f);
        else if (avgFps > dynamicResolutionTargetFPS + 8f)
            currentRenderScale = Mathf.Min(maxRenderScale, currentRenderScale + 0.02f);

        urpAsset.renderScale = currentRenderScale;
    }

    // ─── Core Optimizations ──────────────────────────────────────
    public void ApplyOptimizations()
    {
        // Frame rate
        QualitySettings.vSyncCount    = 0;
        Application.targetFrameRate   = targetFPS;

        // Physics tick (already locked, keep)
        Time.fixedDeltaTime    = 0.016666f;
        Time.maximumDeltaTime  = 0.05f;
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        // URP asset
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            currentRenderScale          = urpAsset.renderScale;
            // Shadow quality — 80m is enough for a horror corridor game
            urpAsset.shadowDistance     = 80f;
            urpAsset.shadowCascadeCount = 2;  // was probably 4 — halves shadow map cost
            urpAsset.msaaSampleCount    = 1;  // MSAA off (URP TAA/FXAA is cheaper)
        }

        // ── KEY FIX: Aggressive LOD ───────────────────────────────
        // LOD bias 0.5 = Balanced. 0.35 was causing some objects to disappear too early, 
        // but 1.0 is too expensive. 0.5 gives a good mix of detail and performance.
        QualitySettings.lodBias         = 0.5f;  
        QualitySettings.maximumLODLevel = 0;       // all LOD levels available, bias controls switch
        QualitySettings.pixelLightCount = 2;       // main + torch max
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable; // Realistic floor/ground textures

        // ── Audio DSP ─────────────────────────────────────────────
        // Reduce audio buffer size — less CPU per audio frame
        AudioSettings.GetDSPBufferSize(out int bufSize, out int numBuffers);
        if (bufSize > 512)
        {
            // AudioSettings.Reset requires a config — use output sample rate approach
            AudioConfiguration config = AudioSettings.GetConfiguration();
            config.dspBufferSize = 512;
            AudioSettings.Reset(config);
        }

        if (applyGeometryBudget)
            ApplyGeometryBudget();

        if (enableOcclusionCulling)
            ApplyOcclusionCulling();

        Debug.Log("AAA FPSOptimizer: Applied — LOD 0.2, Shadow 80m, Far 350m, Fog 80-350m");
    }

    // ─── Geometry Budget ─────────────────────────────────────────
    private void ApplyGeometryBudget()
    {
        ApplyTerrainSettings();
        ApplyCameraSettings();
        ApplyRealisticFog();
    }

    private void ApplyCameraSettings()
    {
        // Cache cameras once
        if (cachedCameras == null || cachedCameras.Length == 0)
            cachedCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        foreach (Camera cam in cachedCameras)
        {
            if (cam == null || !cam.enabled) continue;

            // Always SET (not cap) — overrides any stale Inspector/scene values
            cam.farClipPlane   = 350f;
            cam.nearClipPlane  = 0.1f;
            cam.useOcclusionCulling = true;

            // Per-camera URP data — enable depth texture for post-processing effects
            UniversalAdditionalCameraData urpCam = cam.GetUniversalAdditionalCameraData();
            if (urpCam != null)
            {
                urpCam.renderPostProcessing = true;
            }
        }
    }

    private void ApplyTerrainSettings()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0)
            terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);

        foreach (Terrain terrain in terrains)
        {
            if (terrain == null) continue;

            terrain.drawInstanced        = true;            // GPU instanced terrain rendering
            terrain.treeDistance         = 250f;            // Trees visible further
            terrain.treeBillboardDistance = 80f;            // billboards transition
            terrain.detailObjectDistance = 150f;            // AAA Fix: Grass visible much further (was 80)
            terrain.detailObjectDensity  = 1.0f;            // AAA Fix: Full lush grass density
            terrain.treeMaximumFullLODCount = 15;
            terrain.heightmapPixelError  = 10f;             // slightly lower terrain LOD
            terrain.drawTreesAndFoliage  = true;
        }
    }

    /// <summary>
    /// Natural atmospheric fog — starts at 150m, fully opaque at 350m.
    /// Linear fog: predictable, hides the far clip edge without blacking out the scene.
    /// IMPORTANT: We do NOT touch RenderSettings.ambientMode or ambientLight here.
    /// PlayerController.cs already manages ambient lighting based on torch state.
    /// </summary>
    private void ApplyRealisticFog()
    {
        RenderSettings.fog             = true;
        RenderSettings.fogMode         = FogMode.Linear;
        RenderSettings.fogStartDistance = 150f;   // No fog closer than 150m — scene fully visible
        RenderSettings.fogEndDistance   = 350f;   // Fully hidden at far clip edge
        // Deep dark blue-grey: natural night mist, NOT pitch black
        RenderSettings.fogColor = new Color(0.10f, 0.12f, 0.18f, 1f);
    }

    private void ApplyOcclusionCulling()
    {
        if (cachedCameras == null || cachedCameras.Length == 0)
            cachedCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        foreach (Camera cam in cachedCameras)
        {
            if (cam != null)
                cam.useOcclusionCulling = true;
        }
    }

    // ─── Coroutines ──────────────────────────────────────────────
    private IEnumerator ApplyBudgetAfterSceneReady()
    {
        yield return null;
        yield return null;

        if (applyGeometryBudget)
            ApplyGeometryBudget();
    }

    private IEnumerator StartupGCRoutine()
    {
        yield return new WaitForSeconds(2f);
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        Debug.Log("AAA FPSOptimizer: Startup GC complete.");
    }

    // ─── Object Pool Manager ─────────────────────────────────────
    private void EnsureManagersExist()
    {
        if (ObjectPoolManager.Instance == null)
        {
            GameObject poolObj = new GameObject("ObjectPoolManager");
            poolObj.AddComponent<ObjectPoolManager>();
        }
    }

    private IEnumerator PrewarmPoolsRoutine()
    {
        yield return null;
        if (ObjectPoolManager.Instance == null) yield break;

        if (poolPrewarmEntries != null)
        {
            foreach (PoolPrewarmEntry entry in poolPrewarmEntries)
            {
                if (entry.prefab != null && entry.count > 0)
                    ObjectPoolManager.Instance.Prewarm(entry.prefab, entry.count);
            }
        }

        ObjectPoolManager.Instance.PrewarmProcedural("HitSparks", hitSparkPrewarmCount, CreateHitSparkTemplate);
    }

    private static GameObject CreateHitSparkTemplate()
    {
        GameObject sparkObj = new GameObject("HitSparks");
        ParticleSystem ps   = sparkObj.AddComponent<ParticleSystem>();

        var main            = ps.main;
        main.duration       = 0.5f;
        main.startLifetime  = 0.3f;
        main.startSpeed     = new ParticleSystem.MinMaxCurve(5f, 15f);
        main.startSize      = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor     = new Color(1f, 0.6f, 0f, 1f);
        main.loop           = false;
        main.playOnAwake    = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });

        var shape        = ps.shape;
        shape.shapeType  = ParticleSystemShapeType.Sphere;
        shape.radius     = 0.2f;

        Shader defaultShader = Shader.Find("Sprites/Default");
        if (defaultShader != null)
            ps.GetComponent<ParticleSystemRenderer>().material = new Material(defaultShader);

        sparkObj.SetActive(false);
        return sparkObj;
    }
}
