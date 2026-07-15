using UnityEngine;

/// <summary>
/// Special Object ke paas ki fog ko realistic banata hai.
/// Particle System pe attach karo. Player ke paas aane pe fog react karta hai,
/// Perlin noise se organic breathing hoti hai, aur object collect hone pe dissipate hota hai.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SpecialObjectFog : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    //  INSPECTOR SETTINGS
    // ══════════════════════════════════════════════════════

    [Header("=== Player Reference ===")]
    [Tooltip("Player Transform drag karo (auto-find bhi hoga agar null ho)")]
    public Transform playerTransform;

    [Header("=== Fog Breathing (Perlin Noise) ===")]
    [Tooltip("Kitni tezi se fog saans leta hai (0.3 = slow, 1.5 = fast)")]
    [Range(0.1f, 3f)]
    public float breathSpeed = 0.4f;

    [Tooltip("Breathing ka kitna asar hoga emission rate pe")]
    [Range(0f, 1f)]
    public float breathStrength = 0.35f;

    [Header("=== Emission Rates ===")]
    [Tooltip("Normal door se emission rate")]
    public float baseEmissionRate = 12f;

    [Tooltip("Player ke bilkul paas emission rate (intense fog)")]
    public float closeEmissionRate = 28f;

    [Tooltip("Player ke kitne unit door tak fog react kare")]
    public float reactionRadius = 6f;

    [Header("=== Particle Size Pulse ===")]
    [Tooltip("Particle size pulse ki speed")]
    [Range(0.1f, 2f)]
    public float sizePulseSpeed = 0.5f;

    [Tooltip("Size variation kitni hogi (0 = none, 0.3 = subtle)")]
    [Range(0f, 0.5f)]
    public float sizePulseAmount = 0.2f;

    [Header("=== Color Atmosphere ===")]
    [Tooltip("Jab player door ho tab fog ka color")]
    public Color farColor = new Color(0.45f, 0.30f, 0.70f, 0.35f);    // Mysterious purple

    [Tooltip("Jab player paas aaye tab fog ka color (warm glow)")]
    public Color nearColor = new Color(0.85f, 0.60f, 0.20f, 0.55f);   // Amber mystical

    [Tooltip("Jab player bilkul paas ho (pickup range) tab color")]
    public Color veryNearColor = new Color(1f, 0.90f, 0.50f, 0.70f);  // Bright golden

    [Header("=== Swirl / Rotation ===")]
    [Tooltip("Fog particles ka rotation speed (degrees/sec)")]
    public float rotationSpeed = 8f;

    [Tooltip("Rotation direction random ho? (realistic look)")]
    public bool randomRotationDirection = true;

    [Header("=== Object Collected Effect ===")]
    [Tooltip("Jab special object collect ho jaye tab fog kitni tezi se fade kare")]
    public float dissipateSpeed = 2.5f;

    [Tooltip("Kya fog collect hone pe bilkul band ho jaye?")]
    public bool dissipateOnCollect = true;

    // ══════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ══════════════════════════════════════════════════════
    private ParticleSystem ps;

    private float baseStartSize;
    private float noiseOffsetX;
    private float noiseOffsetY;
    private float noiseOffsetZ;
    private float rotDir = 1f;

    private bool isCollected = false;
    private float currentEmission;

    // ══════════════════════════════════════════════════════
    //  INIT
    // ══════════════════════════════════════════════════════
    void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        // Random Perlin offsets for unique feel per object
        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetY = Random.Range(0f, 100f);
        noiseOffsetZ = Random.Range(0f, 100f);

        baseStartSize    = ps.main.startSizeMultiplier;
        currentEmission  = baseEmissionRate;
        rotDir           = randomRotationDirection ? (Random.value > 0.5f ? 1f : -1f) : 1f;
    }

    void Start()
    {
        // Auto-find player agar assign nahi kiya
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        var em = ps.emission;
        em.rateOverTime = baseEmissionRate;
    }

    // ══════════════════════════════════════════════════════
    //  UPDATE — Every Frame
    // ══════════════════════════════════════════════════════
    void Update()
    {
        if (isCollected)
        {
            DissipateEffect();
            return;
        }

        float t = Time.time;

        // ─── 1. Player Distance ──────────────────────
        float dist = float.MaxValue;
        if (playerTransform != null)
            dist = Vector3.Distance(transform.position, playerTransform.position);

        float proximity = Mathf.Clamp01(1f - (dist / reactionRadius)); // 0 = door, 1 = bilkul paas

        // ─── 2. Perlin Noise Breathing ───────────────
        float breathNoise = Mathf.PerlinNoise(t * breathSpeed + noiseOffsetX, noiseOffsetY);
        float breathNoise2 = Mathf.PerlinNoise(noiseOffsetZ, t * breathSpeed * 0.7f + noiseOffsetY);

        // Combine two noise layers for organic feel
        float breathValue = (breathNoise * 0.6f + breathNoise2 * 0.4f); // 0..1

        // Modifying Particle System
        var em = ps.emission;
        var m  = ps.main;

        // ─── 3. Emission Rate ─────────────────────────
        float targetEmission = Mathf.Lerp(baseEmissionRate, closeEmissionRate, proximity);
        // Apply breathing on top
        float breathDelta = (breathValue - 0.5f) * 2f * breathStrength * targetEmission;
        targetEmission += breathDelta;
        targetEmission = Mathf.Max(targetEmission, 0f);

        currentEmission = Mathf.Lerp(currentEmission, targetEmission, Time.deltaTime * 3f);
        em.rateOverTime = currentEmission;

        // ─── 4. Particle Start Size Pulse ────────────
        float sizePulse = Mathf.Sin(t * sizePulseSpeed * Mathf.PI) * sizePulseAmount;
        float sizeNoise  = (Mathf.PerlinNoise(t * 0.3f + noiseOffsetX, noiseOffsetZ) - 0.5f) * sizePulseAmount;
        m.startSizeMultiplier = baseStartSize + sizePulse + sizeNoise;

        // ─── 5. Color Blend by Distance ──────────────
        Color targetColor;
        if (proximity > 0.75f)
            targetColor = Color.Lerp(nearColor, veryNearColor, (proximity - 0.75f) / 0.25f);
        else
            targetColor = Color.Lerp(farColor, nearColor, proximity / 0.75f);

        // Subtle alpha flicker using noise
        float alphaNoise = Mathf.PerlinNoise(t * 0.8f + noiseOffsetY, noiseOffsetX);
        targetColor.a *= Mathf.Lerp(0.85f, 1f, alphaNoise);

        m.startColor = targetColor;

        // ─── 6. Rotation over time ───────────────────
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z       = new ParticleSystem.MinMaxCurve(
            rotDir * (rotationSpeed - 3f) * Mathf.Deg2Rad,
            rotDir * (rotationSpeed + 3f) * Mathf.Deg2Rad
        );
    }

    // ══════════════════════════════════════════════════════
    //  COLLECT EFFECT — Smooth fade + scatter + destroy
    // ══════════════════════════════════════════════════════
    private void DissipateEffect()
    {
        // Update loop mein kuch nahi — sab coroutine handle karta hai
        // (yeh sirf safety ke liye hai agar koi update call ho)
    }

    // ══════════════════════════════════════════════════════
    //  PUBLIC — PlayerController se call karo jab object collect ho
    // ══════════════════════════════════════════════════════
    public void OnObjectCollected()
    {
        if (!dissipateOnCollect) return;
        isCollected = true;
        StartCoroutine(FadeAndDestroy());
    }

    private System.Collections.IEnumerator FadeAndDestroy()
    {
        var em = ps.emission;
        var m  = ps.main;

        // Step 1: Naye particles band karo — existing particles apni life jeeyenge
        em.rateOverTime = 0f;

        // Step 2: Existing particles thoda upar scatter karein (magical rise)
        float elapsed = 0f;
        float fadeDuration = 2.5f / dissipateSpeed; // dissipateSpeed se control

        // Particle speed badhao — scatter effect
        m.startSpeedMultiplier = Mathf.Max(m.startSpeedMultiplier, 0.8f);

        // Step 3: Existing particles apni lifetime khatam karne do + slow fade
        while (elapsed < fadeDuration)
        {
            // Time.unscaledDeltaTime use kiya taaki game freeze ho toh bhi yeh fade chalta rahe
            // ya phir ruk jaye agar Time.deltaTime 0 ho (dono fine hai, Time.deltaTime is safer)
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // Color alpha smoothly zero karo
            Color c = m.startColor.color;
            c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * dissipateSpeed * 1.5f);
            m.startColor = c;

            // Speed gently badhao — particles upar uthen
            m.startSpeedMultiplier = Mathf.Lerp(
                m.startSpeedMultiplier, 2.0f, Time.deltaTime * 1.2f);

            yield return null;
        }

        // Step 4: Particle system stop karo aur GameObject destroy karo
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Remaining particles finish honge, phir destroy
        yield return new WaitForSeconds(m.startLifetime.constantMax);

        Destroy(gameObject);
    }
}
