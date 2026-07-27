using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Magical / Dr. Strange Style Gate Teleportation System (Same Scene)
/// Features: Procedural Portal Ring, Levitation, Vortex Suck, Magical Landing
/// </summary>
public class AdvancedGateTeleport : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;
    public Vector3 actualPlayerSize = new Vector3(1f, 1f, 1f);

    [Header("Gate Triggers")]
    // Gate A side:
    public Transform trigger1; // [YELLOW] Player walks here to activate Gate A → RING does NOT appear here
    public Transform trigger2; // [GREEN]  RING appears here (Gate A portal mouth) → player sucked in here
    // Gate B side:
    public Transform trigger3; // [CYAN]   RING appears here (Gate B portal mouth) → player sucked in here
    public Transform trigger4; // [RED]    Player walks here to activate Gate B → RING does NOT appear here

    [Header("UI System")]
    public float triggerDistance = 3.0f;

    [Header("AAA Animation Timings")]
    [Tooltip("Time player floats up before being sucked in")]
    public float levitationDuration = 1.0f;
    [Tooltip("Time to suck the player into the portal vortex")]
    public float suckDuration = 0.5f;
    [Tooltip("Time for the player to land on the other side")]
    public float landingDuration = 0.8f;

    [Header("AAA VFX Settings")]
    public Color magicGlowColor = new Color(1f, 0.4f, 0f); // Fiery magical orange
    public float maxGlowIntensity = 10.0f;

    [Header("AAA Camera Effects")]
    [Tooltip("How much the FOV increases during the vortex suck to create a warp speed effect")]
    public float maxFovWarp = 30f; 

    [Header("Optional Custom VFX & Audio")]
    public GameObject teleportVFXPrefab;
    public AudioClip teleportSound;

    private int currentGateZone = 0;
    private bool uiActive = false;
    private bool isTeleporting = false;
    private AudioSource audioSource;
    private CharacterController _cc;
    private Rigidbody _rb;
    private NavMeshAgent _nma;

    private Camera mainCamera;
    private float originalFov;

    private MonoBehaviour tpc;
    private MonoBehaviour fpc;

    void Start()
    {
        // Auto-assign player by tag if missing
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // Auto-assign triggers by name if missing
        if (trigger1 == null) { GameObject t1 = GameObject.Find("Trigger 1"); if (t1 != null) trigger1 = t1.transform; }
        if (trigger2 == null) { GameObject t2 = GameObject.Find("Trigger 2"); if (t2 != null) trigger2 = t2.transform; }
        if (trigger3 == null) { GameObject t3 = GameObject.Find("Trigger 3"); if (t3 != null) trigger3 = t3.transform; }
        if (trigger4 == null) { GameObject t4 = GameObject.Find("Trigger 4"); if (t4 != null) trigger4 = t4.transform; }

        if (player == null)
            Debug.LogError("[GateTeleport] PLAYER is missing! Tag your player with 'Player' or drag it into the Inspector.");
        
        if (trigger1 == null || trigger2 == null || trigger3 == null || trigger4 == null)
            Debug.LogError("[GateTeleport] TRIGGERS are missing! Please name your empty GameObjects exactly: 'Trigger 1', 'Trigger 2', 'Trigger 3', and 'Trigger 4'.");

        if (player != null)
        {
            _cc = player.GetComponent<CharacterController>();
            _rb = player.GetComponent<Rigidbody>();
            _nma = player.GetComponent<NavMeshAgent>();

            tpc = player.GetComponent("ThirdPersonController") as MonoBehaviour;
            fpc = player.GetComponent("FirstPersonController") as MonoBehaviour;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        mainCamera = Camera.main;
        if (mainCamera != null)
            originalFov = mainCamera.fieldOfView;
    }

    void Update()
    {
        if (player == null || isTeleporting) return;

        float distTo1 = Vector3.Distance(player.position, trigger1.position);
        float distTo4 = Vector3.Distance(player.position, trigger4.position);

        if (distTo1 <= triggerDistance)
        {
            if (currentGateZone != 1) currentGateZone = 1;
            uiActive = true;

            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
                StartCoroutine(TeleportSequence(trigger1, trigger2, trigger3, trigger4));
        }
        else if (distTo4 <= triggerDistance)
        {
            if (currentGateZone != 2) currentGateZone = 2;
            uiActive = true;

            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
                StartCoroutine(TeleportSequence(trigger4, trigger3, trigger2, trigger1));
        }
        else
        {
            if (currentGateZone != 0) currentGateZone = 0;
            uiActive = false;
        }
    }

    void OnGUI()
    {
        if (uiActive && !isTeleporting)
        {
            GUIStyle shadowStyle = new GUIStyle();
            shadowStyle.fontSize = Screen.height / 15;
            shadowStyle.alignment = TextAnchor.LowerCenter;
            shadowStyle.fontStyle = FontStyle.Bold;
            shadowStyle.normal.textColor = Color.black;
            
            GUIStyle glowStyle = new GUIStyle(shadowStyle);
            glowStyle.normal.textColor = magicGlowColor;

            GUI.Label(new Rect(2, 0, Screen.width, Screen.height - 100 + 2), "Press [T] to Teleport", shadowStyle);
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height - 100), "Press [T] to Teleport", glowStyle);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    #region Magical Cinematic Sequence
    // ═══════════════════════════════════════════════════════════════════════

    IEnumerator TeleportSequence(Transform entryTrigger, Transform portalIn, Transform portalOut, Transform exitPoint)
    {
        isTeleporting = true;
        currentGateZone = 0;

        DisablePlayerPhysics();
        if (teleportSound != null) audioSource.PlayOneShot(teleportSound);

        // ── Debug: print positions in Console to verify correct triggers ──
        Debug.Log($"[GateTeleport] entryTrigger ({entryTrigger.name}) pos = {entryTrigger.position}");
        Debug.Log($"[GateTeleport] portalIN     ({portalIn.name})    pos = {portalIn.position}  ← RING appears here");
        Debug.Log($"[GateTeleport] portalOUT    ({portalOut.name})   pos = {portalOut.position} ← exit ring here");
        Debug.Log($"[GateTeleport] exitPoint    ({exitPoint.name})   pos = {exitPoint.position}");

        // Ring exactly at portalIn (trigger2 for Gate A, trigger3 for Gate B)
        Vector3 portalCenter  = portalIn.position;   // RING is here — must be at gate door
        Vector3 exitCenter    = portalOut.position;  // Exit RING here — opposite gate door
        Vector3 startPos      = player.position;
        Vector3 levitationPos = startPos + Vector3.up * 1.2f;

        // ── Phase 1: Portal Ring Appears + Player Floats Up ──
        ParticleSystem entryRing = CreatePortalRing(portalCenter);
        Light glowLight = CreateMagicLight(portalCenter);

        float t = 0;
        // Float up with Ease-Out (starts fast, slows down at top)
        while (t < levitationDuration)
        {
            t += Time.deltaTime;
            float percent = t / levitationDuration;
            float ease = 1f - Mathf.Pow(1f - percent, 3); // Cubic Ease-Out

            player.position = Vector3.Lerp(startPos, levitationPos, ease);
            if (glowLight != null) glowLight.intensity = Mathf.Lerp(0, maxGlowIntensity, percent);
            yield return null;
        }

        // ── Phase 2: Extreme Snap/Dash into Portal ──
        t = 0;
        while (t < suckDuration)
        {
            t += Time.deltaTime;
            float percent = t / suckDuration;
            // Quintic Ease-In for a massive speed burst at the end (AAA snap)
            float ease = Mathf.Pow(percent, 5); 

            player.position = Vector3.Lerp(levitationPos, portalCenter, ease);
            
            // Player gets pulled and shrinks smoothly without spinning
            player.localScale = Vector3.Lerp(actualPlayerSize, Vector3.zero, ease);

            // FOV Warp for speed effect
            if (mainCamera != null)
                mainCamera.fieldOfView = Mathf.Lerp(originalFov, originalFov + maxFovWarp, ease);

            yield return null;
        }

        // Snap to portal center (hidden)
        player.position = portalCenter;
        player.localScale = Vector3.zero;

        // Smoothly dissolve entry ring
        if (entryRing != null) { var em = entryRing.emission; em.enabled = false; }

        // ── Brief Transition (short, not jarring) ──
        yield return new WaitForSeconds(0.15f);

        // ── Phase 3: Exit Portal Opens, Player Moves There ──
        ParticleSystem exitRing = CreatePortalRing(exitCenter);
        Light exitGlow = CreateMagicLight(exitCenter);
        exitGlow.intensity = maxGlowIntensity;
        if (teleportSound != null) audioSource.PlayOneShot(teleportSound);

        // Move player to exit portal (still hidden)
        player.position = exitCenter;

        yield return new WaitForSeconds(0.15f);

        // ── Phase 4: Impactful Landing Dash ──
        t = 0;
        while (t < landingDuration)
        {
            t += Time.deltaTime;
            float percent = t / landingDuration;
            // Quintic Ease-Out (Starts with explosive speed, slows down smoothly)
            float ease = 1f - Mathf.Pow(1f - percent, 5); 

            player.position = Vector3.Lerp(exitCenter, exitPoint.position, ease);
            player.localScale = Vector3.Lerp(Vector3.zero, actualPlayerSize, ease);

            // Restore FOV smoothly
            if (mainCamera != null)
                mainCamera.fieldOfView = Mathf.Lerp(originalFov + maxFovWarp, originalFov, ease);

            if (exitGlow != null) exitGlow.intensity = Mathf.Lerp(maxGlowIntensity, 0, percent);
            yield return null;
        }

        // ── Cleanup ──
        if (mainCamera != null) mainCamera.fieldOfView = originalFov; // Ensure exact original
        player.position = exitPoint.position;
        player.rotation = exitPoint.rotation; // Snap rotation to face outward from exit gate
        player.localScale = actualPlayerSize;

        if (entryRing != null) Destroy(entryRing.gameObject, 1f);
        if (exitRing  != null) { var em = exitRing.emission; em.enabled = false; Destroy(exitRing.gameObject, 1f); }
        if (glowLight != null) Destroy(glowLight.gameObject);
        if (exitGlow  != null) Destroy(exitGlow.gameObject);

        EnablePlayerPhysics(exitPoint.position);
        isTeleporting = false;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    #region Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private Light CreateMagicLight(Vector3 pos)
    {
        GameObject lightObj = new GameObject("TeleportGlow_AAA");
        lightObj.transform.position = pos;
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = magicGlowColor;
        l.range = 20f;
        l.intensity = 0f;
        l.renderMode = LightRenderMode.ForcePixel;
        return l;
    }

    private ParticleSystem CreatePortalRing(Vector3 pos)
    {
        GameObject psObj = new GameObject("PortalRing_AAA");
        psObj.transform.position = pos;
        psObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Stand upright
        
        // --- 1. CORE RING (Thick, fiery energy) ---
        ParticleSystem corePs = psObj.AddComponent<ParticleSystem>();
        var main = corePs.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f); // THICKER PARTICLES
        main.startColor = magicGlowColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.maxParticles = 3000;

        var em = corePs.emission;
        em.rateOverTime = 1200f; // VERY DENSE

        var shape = corePs.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 4.8f; // BIGGER RING (Almost 10m diameter)
        shape.radiusThickness = 0.15f; 
        shape.arcMode = ParticleSystemShapeMultiModeValue.Loop;

        var vel = corePs.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalY = 10f; // FAST SPIN
        vel.radial = -2.5f;  // SUCK INWARD
        
        var noise = corePs.noise;
        noise.enabled = true;
        noise.strength = 1.5f;
        noise.frequency = 1.2f;
        noise.scrollSpeed = 2f;

        var col = corePs.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(magicGlowColor, 0.3f), new GradientColorKey(Color.black, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        ParticleSystemRenderer corePsr = corePs.GetComponent<ParticleSystemRenderer>();
        Shader validShader = GetValidParticleShader();
        if (validShader != null)
        {
            corePsr.material = new Material(validShader);
            if (corePsr.material.HasProperty("_BaseColor"))
                corePsr.material.SetColor("_BaseColor", magicGlowColor * 2.5f);
            else if (corePsr.material.HasProperty("_Color"))
                corePsr.material.SetColor("_Color", magicGlowColor * 2.5f);
        }

        // --- 2. SPARKS (Doctor Strange style flying embers) ---
        GameObject sparksObj = new GameObject("PortalSparks");
        sparksObj.transform.SetParent(psObj.transform, false);
        ParticleSystem sparksPs = sparksObj.AddComponent<ParticleSystem>();
        
        var sMain = sparksPs.main;
        sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 12f); // EXPLOSIVE SPEED
        sMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f); // SMALL DOTS
        sMain.startColor = new Color(1f, 0.9f, 0.4f, 1f); // Bright yellow/white sparks
        sMain.simulationSpace = ParticleSystemSimulationSpace.World;
        sMain.maxParticles = 1000;
        
        var sEm = sparksPs.emission;
        sEm.rateOverTime = 400f;
        
        var sShape = sparksPs.shape;
        sShape.shapeType = ParticleSystemShapeType.Circle;
        sShape.radius = 4.8f;
        sShape.radiusThickness = 0.01f;
        
        var sVel = sparksPs.velocityOverLifetime;
        sVel.enabled = true;
        sVel.orbitalY = 15f; // EXTREME SPIN
        sVel.radial = 4f; // FLY OUTWARD
        
        var sNoise = sparksPs.noise;
        sNoise.enabled = true;
        sNoise.strength = 4f;
        sNoise.frequency = 3f;

        var sCol = sparksPs.colorOverLifetime;
        sCol.enabled = true;
        Gradient sGrad = new Gradient();
        sGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f), new GradientColorKey(Color.black, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        sCol.color = sGrad;
        
        ParticleSystemRenderer sPsr = sparksPs.GetComponent<ParticleSystemRenderer>();
        Shader sparkShader = GetValidParticleShader();
        if (sparkShader != null)
        {
            sPsr.material = new Material(sparkShader);
        }
        sPsr.material.color = new Color(2f, 1.5f, 0.5f); // SUPER BRIGHT HDR
        sPsr.trailMaterial = sPsr.material;
        
        var sTrails = sparksPs.trails;
        sTrails.enabled = true;
        sTrails.ratio = 0.4f;
        sTrails.lifetimeMultiplier = 0.05f;

        corePs.Play();
        sparksPs.Play();
        
        return corePs;
    }

    private void SpawnVFX(Vector3 pos)
    {
        if (teleportVFXPrefab != null)
        {
            GameObject vfx = Instantiate(teleportVFXPrefab, pos, Quaternion.identity);
            Destroy(vfx, 3f);
        }
    }

    private System.Collections.IEnumerator DisableAfterTime(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null) obj.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    // Scene View Gizmos — Shows EXACTLY where each trigger is
    // Yellow = trigger1 (Gate A detection zone — NO ring here)
    // Green  = trigger2 (Gate A RING position)
    // Cyan   = trigger3 (Gate B RING position)
    // Red    = trigger4 (Gate B detection zone — NO ring here)
    // ─────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        DrawTriggerGizmo(trigger1, Color.yellow,  "T1: Gate A Entry\n(NO ring)",  0.4f);
        DrawTriggerGizmo(trigger2, Color.green,   "T2: Gate A RING\n(RING here)", 0.6f);
        DrawTriggerGizmo(trigger3, Color.cyan,    "T3: Gate B RING\n(RING here)", 0.6f);
        DrawTriggerGizmo(trigger4, Color.red,     "T4: Gate B Entry\n(NO ring)",  0.4f);

        // Draw lines connecting the gate pairs
        if (trigger1 != null && trigger2 != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(trigger1.position, trigger2.position);
        }
        if (trigger3 != null && trigger4 != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(trigger3.position, trigger4.position);
        }
        // Draw portal link (trigger2 <-> trigger3)
        if (trigger2 != null && trigger3 != null)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f); // orange
            Gizmos.DrawLine(trigger2.position, trigger3.position);
        }
    }

    private void DrawTriggerGizmo(Transform t, Color col, string label, float radius)
    {
#if UNITY_EDITOR
        if (t == null) return;
        Gizmos.color = col;
        Gizmos.DrawWireSphere(t.position, radius);
        UnityEditor.Handles.color = col;
        UnityEditor.Handles.Label(t.position + Vector3.up * (radius + 0.3f), label);
#endif
    }

    private bool originalKinematic = true;

    private void DisablePlayerPhysics()
    {
        if (tpc != null) tpc.enabled = false;
        if (fpc != null) fpc.enabled = false;

        if (_cc != null) _cc.enabled = false;
        if (_rb != null) 
        {
            originalKinematic = _rb.isKinematic;
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
        }
        if (_nma != null) _nma.enabled = false;
    }

    private void EnablePlayerPhysics(Vector3 finalPos)
    {
        if (_nma != null)
        {
            _nma.Warp(finalPos);
            _nma.enabled = true;
        }
        
        player.position = finalPos;
        
        if (_cc != null) _cc.enabled = true;
        if (_rb != null) _rb.isKinematic = originalKinematic;

        if (tpc != null) tpc.enabled = true;
        if (fpc != null) fpc.enabled = true;

        Animator anim = player.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("Grounded", true);
            anim.SetBool("FreeFall", false);
        }
    }

    private Shader GetValidParticleShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s != null) return s;
        
        s = Shader.Find("Particles/Standard Unlit");
        if (s != null) return s;
        
        s = Shader.Find("Legacy Shaders/Particles/Additive");
        if (s != null) return s;

        s = Shader.Find("Mobile/Particles/Additive");
        return s;
    }

    #endregion
}
