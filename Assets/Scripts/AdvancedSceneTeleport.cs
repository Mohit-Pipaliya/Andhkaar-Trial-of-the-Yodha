using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Magical / Dr. Strange Style Cross-Scene Teleportation
/// Features: Procedural Portal Rings, Levitation, Vortex Suck, and Magical Landing.
/// </summary>
public class AdvancedSceneTeleport : MonoBehaviour
{
    public static string PendingSpawnPortalID = "";

    [Header("Scene Routing Settings")]
    public string thisPortalID = "Gate1";
    public string targetSceneName = "Scene2";
    public string destinationPortalID = "Gate2";

    [Header("Player Settings")]
    public Transform player;
    public Vector3 actualPlayerSize = new Vector3(1f, 1f, 1f);

    [Header("Gate Triggers")]
    // [YELLOW] Player walks near this to trigger teleport (detection zone)
    public Transform portalIn;
    // [GREEN] Ring appears HERE — assign the gate door center / default child
    // If left empty, ring appears at portalIn position
    public Transform ringPosition;
    // [RED] Player lands here after teleporting in from the other scene
    public Transform endPoint;

    [Header("UI System")]
    public float triggerDistance = 3.0f;

    [Header("AAA Animation Timings")]
    public float levitationDuration = 1.0f;
    public float suckDuration = 0.5f;
    public float landingDuration = 0.8f;

    [Header("AAA VFX Settings")]
    public Color magicGlowColor = new Color(1f, 0.4f, 0f); // Fiery orange/magical
    public float maxGlowIntensity = 10.0f;

    [Header("AAA Camera Effects")]
    [Tooltip("How much the FOV increases during the vortex suck to create a warp speed effect")]
    public float maxFovWarp = 30f;

    [Header("Optional Event (Arrival)")]
    [Tooltip("Assign a stone here to make it slide down when arriving at this gate")]
    public Transform stoneToSlide;
    [Tooltip("The exact Y coordinate the stone should slide to")]
    public float stoneTargetY = 0f;
    public float stoneSlideDuration = 2.5f;
    public AudioClip stoneSlideSound;

    [Header("Optional Audio")]
    public AudioClip teleportSound;

    private bool isTeleporting = false;
    private bool uiActive = false;

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
        if (player == null)
        {
            GameObject p = GameObject.Find("PlayerArmature");
            if (p == null) p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

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
        if (mainCamera != null) originalFov = mainCamera.fieldOfView;

        if (PendingSpawnPortalID == thisPortalID)
        {
            PendingSpawnPortalID = ""; 
            StartCoroutine(WarpOutOnStart());
        }
    }

    void Update()
    {
        if (player == null || isTeleporting) return;
        if (portalIn == null || endPoint == null) return;

        float dist = Vector3.Distance(player.position, portalIn.position);
        // Detection uses portalIn — ring appears at ringPosition (gate door)

        if (dist <= triggerDistance)
        {
            if (!uiActive) uiActive = true;

            bool tPressed = false;
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) tPressed = true;

            if (tPressed)
            {
                StartCoroutine(WarpInAndLoadScene());
            }
        }
        else
        {
            if (uiActive) uiActive = false;
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

    IEnumerator WarpInAndLoadScene()
    {
        isTeleporting = true;
        uiActive = false;

        DisablePlayerPhysics();
        if (teleportSound != null) audioSource.PlayOneShot(teleportSound);

        // Ring exactly at ringPosition (gate door frame) — NOT at player detection zone
        // If ringPosition not assigned, fallback to portalIn
        Vector3 portalCenter = (ringPosition != null) ? ringPosition.position : portalIn.position;
        Debug.Log($"[SceneTeleport] Ring at: {portalCenter} | Detection zone: {portalIn.position}");

        // Phase 1: Open Portal Ring
        ParticleSystem portalRing = CreatePortalRing(portalCenter);
        Light glowLight = CreateMagicLight(portalCenter);

        Vector3 startPos = player.position;
        Vector3 levitationPos = startPos + Vector3.up * 1.2f;

        // Phase 2: Levitation Build Up
        float t = 0;
        // Float up with Ease-Out
        while (t < levitationDuration)
        {
            t += Time.deltaTime;
            float percent = t / levitationDuration;
            float ease = 1f - Mathf.Pow(1f - percent, 3); // Cubic Ease-Out
            
            player.position = Vector3.Lerp(startPos, levitationPos, ease);
            
            if (glowLight != null) 
            {
                glowLight.intensity = Mathf.Lerp(0, maxGlowIntensity, percent);
            }
            yield return null;
        }

        // Phase 2: Extreme Snap/Dash into Portal
        t = 0;
        while (t < suckDuration)
        {
            t += Time.deltaTime;
            float percent = t / suckDuration;
            // Quintic Ease-In for a massive speed burst at the end (AAA snap)
            float ease = Mathf.Pow(percent, 5); 

            player.position = Vector3.Lerp(levitationPos, portalCenter, ease);
            player.localScale = Vector3.Lerp(actualPlayerSize, Vector3.zero, ease);

            // FOV Warp
            if (mainCamera != null)
                mainCamera.fieldOfView = Mathf.Lerp(originalFov, originalFov + maxFovWarp, ease);

            yield return null;
        }

        player.position = portalCenter;
        player.localScale = Vector3.zero;

        if (portalRing != null) { var em = portalRing.emission; em.enabled = false; }
        
        yield return new WaitForSeconds(0.15f);

        if (glowLight != null) Destroy(glowLight.gameObject);
        if (portalRing != null) Destroy(portalRing.gameObject);

        // Tell the destination scene to skip main menu and go straight to gameplay
        PlayerPrefs.SetInt("SkipMenuOnRetry", 1);
        PlayerPrefs.Save();

        // Save Player's current health, weapons, and torch state before leaving this scene
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            GameSaveManager.SaveGameState(pc);
        }

        // Load the Next Scene
        PendingSpawnPortalID = destinationPortalID; 
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        if (asyncLoad == null)
        {
            PendingSpawnPortalID = ""; 
        }
        else
        {
            while (!asyncLoad.isDone) yield return null;
        }
    }

    IEnumerator WarpOutOnStart()
    {
        isTeleporting = true;
        DisablePlayerPhysics();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null) originalFov = mainCamera.fieldOfView;
        }
        
        // Ring exactly at ringPosition (gate door frame) — NOT at player detection zone
        Vector3 portalCenter = (ringPosition != null) ? ringPosition.position : portalIn.position;
        Vector3 levitationPos = player.position + Vector3.up * 1.2f;
        player.position = portalCenter;

        ParticleSystem portalRing = CreatePortalRing(portalCenter);
        Light glowLight = CreateMagicLight(portalCenter);
        if (teleportSound != null) audioSource.PlayOneShot(teleportSound);
        
        // Brief pause just to let the ring spawn visually
        yield return new WaitForSeconds(0.1f);

        float t = 0;
        while (t < landingDuration)
        {
            t += Time.deltaTime;
            float percent = t / landingDuration;
            // Quintic Ease-Out (Starts with explosive speed, slows down smoothly)
            float ease = 1f - Mathf.Pow(1f - percent, 5); 

            player.localScale = Vector3.Lerp(Vector3.zero, actualPlayerSize, ease);
            player.position = Vector3.Lerp(portalCenter, endPoint.position, ease);

            // Restore FOV smoothly
            if (mainCamera != null)
                mainCamera.fieldOfView = Mathf.Lerp(originalFov + maxFovWarp, originalFov, ease);

            if (glowLight != null) glowLight.intensity = Mathf.Lerp(maxGlowIntensity, 0, percent);

            yield return null;
        }

        if (mainCamera != null) mainCamera.fieldOfView = originalFov; // Ensure exact original
        player.position = endPoint.position;
        player.localScale = actualPlayerSize; 
        
        if (portalRing != null) 
        {
            var em = portalRing.emission;
            em.enabled = false;
            Destroy(portalRing.gameObject, 1.5f);
        }
        if (glowLight != null) Destroy(glowLight.gameObject);
        
        EnablePlayerPhysics(endPoint.position);
        isTeleporting = false;

        // Slide the stone AFTER the player has safely landed
        if (stoneToSlide != null)
        {
            StartCoroutine(SlideStoneDown());
        }
    }

    IEnumerator SlideStoneDown()
    {
        if (stoneToSlide == null) yield break;
        
        // Play the sliding sound if assigned
        if (stoneSlideSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(stoneSlideSound);
        }

        Vector3 startPos = stoneToSlide.position;
        Vector3 endPos = new Vector3(startPos.x, stoneTargetY, startPos.z);
        float t = 0;
        
        while (t < stoneSlideDuration)
        {
            t += Time.deltaTime;
            float percent = Mathf.SmoothStep(0, 1, t / stoneSlideDuration);
            stoneToSlide.position = Vector3.Lerp(startPos, endPos, percent);
            yield return null;
        }
        stoneToSlide.position = endPos;
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
        GameObject psObj = new GameObject("PortalRing_AAA_Magical");
        psObj.transform.position = pos;
        // Keep the original rotation to match the gate orientation
        psObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Shader unlitShader = Shader.Find("Particles/Standard Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Legacy Shaders/Particles/Additive");

        // --- 1. DARK VOID (Black hole in the middle) ---
        GameObject voidObj = new GameObject("PortalVoid");
        voidObj.transform.SetParent(psObj.transform, false);
        ParticleSystem voidPs = voidObj.AddComponent<ParticleSystem>();
        var vMain = voidPs.main;
        vMain.startLifetime = 1.0f;
        vMain.startSpeed = 0f;
        vMain.startSize = 7.5f; // Big black circle
        vMain.startColor = Color.black;
        vMain.maxParticles = 5;
        var vEm = voidPs.emission;
        vEm.rateOverTime = 10f;
        var vShape = voidPs.shape;
        vShape.shapeType = ParticleSystemShapeType.Circle;
        vShape.radius = 0.1f;
        ParticleSystemRenderer vPsr = voidPs.GetComponent<ParticleSystemRenderer>();
        if (unlitShader != null) vPsr.material = new Material(unlitShader);
        SetMaterialColor(vPsr.material, Color.black);
        
        // Ensure void renders behind the sparks
        vPsr.sortingOrder = 1;

        // --- 2. THE CORE SPARK RING (Thin, ultra-bright, fast spin) ---
        ParticleSystem outerRingPs = psObj.AddComponent<ParticleSystem>();
        var main = outerRingPs.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startColor = new Color(1f, 0.8f, 0.2f, 1f); // Bright yellow/white core
        main.maxParticles = 5000;
        main.prewarm = true;
        var em = outerRingPs.emission;
        em.rateOverTime = 3000f; // Very dense
        var shape = outerRingPs.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 3.8f; // The ring radius
        shape.radiusThickness = 0.05f; // Very sharp edge
        var vel = outerRingPs.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalZ = 12f; // Spin around the ring
        var col = outerRingPs.colorOverLifetime;
        col.enabled = true;
        col.color = CreateFadeGradient(new Color(1f, 0.9f, 0.5f, 1f), magicGlowColor);
        ParticleSystemRenderer outerPsr = outerRingPs.GetComponent<ParticleSystemRenderer>();
        if (unlitShader != null) outerPsr.material = new Material(unlitShader);
        SetMaterialColor(outerPsr.material, new Color(3f, 2f, 0.5f)); // HDR intensity
        outerPsr.sortingOrder = 3;

        // --- 3. DR STRANGE SPARKS (Cascading outwards and falling) ---
        GameObject sparksObj = new GameObject("PortalSparks");
        sparksObj.transform.SetParent(psObj.transform, false);
        ParticleSystem sparksPs = sparksObj.AddComponent<ParticleSystem>();
        var sMain = sparksPs.main;
        sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f); // Fast explosion out
        sMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        sMain.startColor = new Color(1f, 0.7f, 0.1f, 1f);
        sMain.gravityModifier = 2.5f; // Gravity makes sparks fall like real sparklers!
        sMain.maxParticles = 2000;
        sMain.prewarm = true;
        var sEm = sparksPs.emission;
        sEm.rateOverTime = 800f;
        var sShape = sparksPs.shape;
        sShape.shapeType = ParticleSystemShapeType.Circle;
        sShape.radius = 3.8f;
        sShape.radiusThickness = 0.05f;
        var sVel = sparksPs.velocityOverLifetime;
        sVel.enabled = true;
        sVel.orbitalZ = 8f; // Spin while flying out
        sVel.radial = 5f; // Fly OUTWARDS tangentially
        var sCol = sparksPs.colorOverLifetime;
        sCol.enabled = true;
        sCol.color = CreateFadeGradient(new Color(1f, 0.9f, 0.2f, 1f), Color.red);
        
        // Add trails for sparkler effect
        var sTrails = sparksPs.trails;
        sTrails.enabled = true;
        sTrails.ratio = 0.5f; // 50% of particles have trails
        sTrails.lifetimeMultiplier = 0.15f;
        sTrails.colorOverLifetime = CreateFadeGradient(new Color(1f, 0.8f, 0.2f, 1f), Color.black);
        
        ParticleSystemRenderer sPsr = sparksPs.GetComponent<ParticleSystemRenderer>();
        if (unlitShader != null) sPsr.material = new Material(unlitShader);
        SetMaterialColor(sPsr.material, new Color(4f, 2.5f, 0.5f)); // Super bright HDR
        sPsr.trailMaterial = sPsr.material;
        sPsr.sortingOrder = 4;

        // --- 4. DARK MAGIC SMOKE (Billowing around the ring) ---
        GameObject smokeObj = new GameObject("PortalSmoke");
        smokeObj.transform.SetParent(psObj.transform, false);
        ParticleSystem smokePs = smokeObj.AddComponent<ParticleSystem>();
        var smMain = smokePs.main;
        smMain.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        smMain.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
        smMain.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        smMain.startColor = new Color(0.1f, 0.05f, 0.02f, 0.8f); // Dark ashy smoke
        smMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f); // Random rotation
        smMain.maxParticles = 200;
        smMain.prewarm = true;
        var smEm = smokePs.emission;
        smEm.rateOverTime = 80f;
        var smShape = smokePs.shape;
        smShape.shapeType = ParticleSystemShapeType.Circle;
        smShape.radius = 4.0f;
        smShape.radiusThickness = 0.2f;
        var smVel = smokePs.velocityOverLifetime;
        smVel.enabled = true;
        smVel.orbitalZ = -5f; // Slow reverse spin
        smVel.radial = 1.5f; // Slowly expand outward
        
        var smSize = smokePs.sizeOverLifetime;
        smSize.enabled = true;
        
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.5f);
        curve.AddKey(1f, 2f);
        smSize.size = new ParticleSystem.MinMaxCurve(1f, curve); // Grow over time
        
        var smCol = smokePs.colorOverLifetime;
        smCol.enabled = true;
        smCol.color = CreateFadeGradient(new Color(1f, 0.3f, 0f, 0.8f), new Color(0f, 0f, 0f, 0f));
        
        ParticleSystemRenderer smPsr = smokePs.GetComponent<ParticleSystemRenderer>();
        Shader alphaShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (alphaShader == null) alphaShader = unlitShader;
        if (alphaShader != null) smPsr.material = new Material(alphaShader);
        smPsr.sortingOrder = 2; // Behind sparks, in front of void

        voidPs.Play();
        outerRingPs.Play();
        sparksPs.Play();
        smokePs.Play();

        return outerRingPs;
    }

    private void SetMaterialColor(Material mat, Color col)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        else if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", col);
    }

    private Gradient CreateFadeGradient(Color start, Color end)
    {
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(start, 0f), new GradientColorKey(start, 0.7f), new GradientColorKey(end, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        return grad;
    }

    // ─────────────────────────────────────────────────────────────
    // Scene View Gizmos
    // Yellow = portalIn  (Player detection zone — NO ring here)
    // Green  = ringPosition (RING appears here — at gate door frame)
    // Red    = endPoint   (Player lands here)
    // ─────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // Detection zone - Yellow
        if (portalIn != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(portalIn.position, 0.4f);
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(portalIn.position + Vector3.up * 0.8f, "portalIn\n(Detection - NO ring)");
        }
        // Ring position - Green (this is where ring should be)
        if (ringPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(ringPosition.position, 0.6f);
            // Draw a circle to show ring size
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(ringPosition.position, Vector3.forward, 3.5f);
            UnityEditor.Handles.Label(ringPosition.position + Vector3.up * 1.0f, "ringPosition\n(RING HERE - gate door)");
        }
        else if (portalIn != null)
        {
            // Warn: no ringPosition set, ring will fall back to portalIn
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(portalIn.position + Vector3.up * 1.3f, "⚠ Assign ringPosition!\n(Ring falls back here)");
        }
        // End/landing point - Red
        if (endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPoint.position, 0.4f);
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(endPoint.position + Vector3.up * 0.8f, "endPoint\n(Player lands here)");
        }
        // Lines
        if (portalIn != null && ringPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(portalIn.position, ringPosition.position);
        }
        if (ringPosition != null && endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(ringPosition.position, endPoint.position);
        }
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
        }
        if (_nma != null) _nma.enabled = false;
    }

    private void EnablePlayerPhysics(Vector3 finalPos)
    {
        // Align rotation to the endpoint so we face the right way when exiting
        if (endPoint != null)
        {
            player.rotation = endPoint.rotation;
        }

        if (_nma != null)
        {
            _nma.Warp(finalPos);
            _nma.enabled = true;
        }
        
        player.position = finalPos;
        
        if (_cc != null) _cc.enabled = true;
        
        // HUGE FIX: Do NOT force isKinematic to false. Restore its original state!
        // Forcing it to false on a CharacterController causes violent vibration and ragdolling.
        if (_rb != null) _rb.isKinematic = originalKinematic;

        if (tpc != null) tpc.enabled = true;
        if (fpc != null) fpc.enabled = true;

        // Force animator to grounded so we don't play a falling animation upon landing
        Animator anim = player.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("Grounded", true);
            anim.SetBool("FreeFall", false);
        }
    }

    #endregion
}
