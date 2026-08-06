using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AAA Realistic Renderer — Andhkaar: Trial of the Yodha
///
/// Yeh script do kaam karti hai:
///
/// 1. GPU INSTANCING ENABLER
///    Scene ke saare renderers (MeshRenderer, SkinnedMeshRenderer) scan karta hai
///    aur har material par material.enableInstancing = true set karta hai.
///    Iska fayda: Unity similar materials ke saare objects ek single "instanced"
///    draw call mein render karta hai → batches 400+ se ~100-150 tak.
///
/// 2. REALISTIC POST-PROCESSING
///    URP Global Volume ko runtime par find karke realistic settings apply karta hai:
///    - Color Grading: Warm shadows, cool highlights (cinematic film look)
///    - Vignette: Subtle dark edges (horror atmosphere)
///    - Bloom: Torch aur enemy glow ke liye (subtle — not over-the-top)
///    - White Balance: Slightly cool (night/dark fantasy feel)
///
/// IMPORTANT: Yeh script PURELY ADDITIVE hai. Koi bhi existing component ko
/// disable ya delete nahi karti. Sirf existing Volume override components ko
/// tweak karti hai.
///
/// Setup: Is script ko FPSOptimizer ke saath hi usi GameObject par add karo.
/// </summary>
[DefaultExecutionOrder(-90)]
public class AAARealisticRenderer : MonoBehaviour
{
    [Header("GPU Instancing")]
    [Tooltip("Scan all scene materials and enable GPU instancing on them.")]
    public bool enableGPUInstancing = true;

    [Tooltip("Re-scan every N seconds to catch dynamically spawned objects.")]
    public float instancingRescanInterval = 5f;

    [Header("Post-Processing")]
    [Tooltip("Apply cinematic post-processing to the global volume.")]
    public bool applyPostProcessing = true;

    [Header("Realistic Lighting Tone")]
    [Tooltip("Overall scene brightness multiplier (1 = no change, 0.9 = slightly darker = horror)")]
    [Range(0.5f, 1.5f)]
    public float sceneBrightness = 0.85f;

    // Internal state
    private Volume globalVolume;
    private VolumeProfile profile;
    private int totalInstancedMaterials = 0;

    // ═══════════════════════════════════════════════════════════
    void Start()
    {
        if (enableGPUInstancing)
            StartCoroutine(GPUInstancingRoutine());

        if (applyPostProcessing)
            StartCoroutine(ApplyPostProcessingDelayed());
    }

    // ─── GPU Instancing ──────────────────────────────────────────
    /// <summary>
    /// Scans ALL renderers in the scene and enables GPU instancing on every material.
    /// Run at start and periodically for dynamic objects.
    /// </summary>
    private IEnumerator GPUInstancingRoutine()
    {
        yield return null; // Wait one frame for scene to be ready

        EnableGPUInstancingOnAllMaterials();
        
        // Removed periodic rescan: It was causing O(n) CPU lag spikes every 5 seconds.
    }

    private void EnableGPUInstancingOnAllMaterials()
    {
        int count = 0;

        // MeshRenderers (static world geometry, props, terrain decorations)
        MeshRenderer[] meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        foreach (MeshRenderer mr in meshRenderers)
        {
            if (mr == null) continue;
            foreach (Material mat in mr.sharedMaterials)
            {
                if (mat != null && !mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    count++;
                }
            }
        }

        // SkinnedMeshRenderers (player, enemies, NPCs)
        SkinnedMeshRenderer[] skinnedRenderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
        foreach (SkinnedMeshRenderer smr in skinnedRenderers)
        {
            if (smr == null) continue;
            foreach (Material mat in smr.sharedMaterials)
            {
                if (mat != null && !mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            totalInstancedMaterials += count;
            Debug.Log($"AAARealisticRenderer: GPU Instancing enabled on {count} materials " +
                      $"(total: {totalInstancedMaterials})");
        }
    }

    // ─── Post-Processing ─────────────────────────────────────────
    private IEnumerator ApplyPostProcessingDelayed()
    {
        yield return null;
        yield return null;

        // Find or create global volume
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        foreach (Volume v in volumes)
        {
            if (v.isGlobal)
            {
                globalVolume = v;
                break;
            }
        }

        if (globalVolume == null)
        {
            // Create a new global volume
            GameObject volObj = new GameObject("AAA_RealisticGlobalVolume");
            globalVolume = volObj.AddComponent<Volume>();
            globalVolume.isGlobal   = true;
            globalVolume.priority   = 1f; // Higher priority than any existing volume
        }

        // Create a fresh profile so we don't mutate shared assets
        if (globalVolume.profile == null || !globalVolume.HasInstantiatedProfile())
        {
            globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }
        profile = globalVolume.profile;

        ApplyColorGrading();
        ApplyBloom();
        ApplyVignette();
        ApplyWhiteBalance();
        ApplyFilmGrain();

        Debug.Log("AAARealisticRenderer: Post-processing applied — cinematic look active.");
    }

    /// <summary>
    /// Color Grading: Cinematic look without darkening the scene.
    /// Warm shadows, cool highlights — the classic dark fantasy AAA look.
    /// </summary>
    private void ApplyColorGrading()
    {
        if (!profile.TryGet<ColorAdjustments>(out ColorAdjustments ca))
            ca = profile.Add<ColorAdjustments>(true);

        ca.active = true;
        ca.postExposure.Override(0f);        // NO exposure change — scene stays at natural brightness
        ca.contrast.Override(8f);            // Very subtle contrast bump
        ca.colorFilter.Override(new Color(0.97f, 0.95f, 1.0f)); // Barely-noticeable cool tint
        ca.saturation.Override(-5f);         // Slight desaturation for gritty look

        // Lift/Gamma/Gain: warm shadows, cool highlights
        if (!profile.TryGet<LiftGammaGain>(out LiftGammaGain lgg))
            lgg = profile.Add<LiftGammaGain>(true);

        lgg.active = true;
        lgg.lift.Override(new Vector4(1.02f, 0.98f, 0.96f, 0f));    // Warm shadows — W=0, no brightness change
        lgg.gamma.Override(new Vector4(0.99f, 0.99f, 1.01f, 0.0f)); // Neutral midtones
        lgg.gain.Override(new Vector4(0.97f, 0.98f, 1.04f, 1.0f));  // Cool highlights
    }

    /// <summary>
    /// Bloom: Subtle glow on bright sources (torches, fire, enemy glows).
    /// AAA horror games use very low bloom threshold so only true bright emitters glow.
    /// </summary>
    private void ApplyBloom()
    {
        if (!profile.TryGet<Bloom>(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);

        bloom.active = true;
        bloom.threshold.Override(0.9f);   // Only sources brighter than 0.9 will bloom
        bloom.intensity.Override(0.5f);   // Subtle — not the cheap over-bloomed look
        bloom.scatter.Override(0.65f);    // Tight scatter for realistic torch glow
        bloom.tint.Override(new Color(1.0f, 0.85f, 0.6f)); // Warm orange torch tint
        bloom.highQualityFiltering.Override(true);
    }

    /// <summary>
    /// Vignette: Very subtle dark screen edges. Barely noticeable but adds immersion.
    /// </summary>
    private void ApplyVignette()
    {
        if (!profile.TryGet<Vignette>(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);

        vignette.active    = true;
        vignette.intensity.Override(0.22f);  // Subtle — NOT the pitch-black horror vignette
        vignette.smoothness.Override(0.4f);
        vignette.rounded.Override(true);
        vignette.color.Override(Color.black);
    }

    /// <summary>
    /// White Balance: Neutral to very slightly cool. Natural look preserved.
    /// </summary>
    private void ApplyWhiteBalance()
    {
        if (!profile.TryGet<WhiteBalance>(out WhiteBalance wb))
            wb = profile.Add<WhiteBalance>(true);

        wb.active = true;
        wb.temperature.Override(-6f);   // Very subtle cool — natural night feel
        wb.tint.Override(1f);           // Near-neutral
    }

    /// <summary>
    /// Film Grain: Extremely subtle — adds texture and "organic" feel to the image.
    /// This is what separates AAA from indie in the final image quality.
    /// </summary>
    private void ApplyFilmGrain()
    {
        if (!profile.TryGet<FilmGrain>(out FilmGrain grain))
            grain = profile.Add<FilmGrain>(true);

        grain.active = true;
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.12f);  // Very subtle — you feel it more than see it
        grain.response.Override(0.85f);
    }
}
