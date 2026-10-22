using System.Collections;
using UnityEngine;

/// <summary>
/// AAA Sacred Rebirth & Soul Revival Visual Effect.
/// Generates a swirling golden ember aura, expanding sacred ground shockwave,
/// darkness-dissipating smoke, holy illumination burst, and subtle camera pulse.
/// </summary>
public class RevivalVFX : MonoBehaviour
{
    public static void Play(Transform playerTransform, Vector3 position)
    {
        GameObject fxObj = new GameObject("AAA_Revival_Effect");
        fxObj.transform.position = position;
        RevivalVFX vfx = fxObj.AddComponent<RevivalVFX>();
        vfx.StartEffect(playerTransform, position);
    }

    private void StartEffect(Transform playerTransform, Vector3 position)
    {
        StartCoroutine(RevivalRoutine(playerTransform, position));
    }

    private IEnumerator RevivalRoutine(Transform playerTransform, Vector3 position)
    {
        // 1. Holy Ambient Point Light Burst
        GameObject lightObj = new GameObject("Revival_Light");
        lightObj.transform.position = position + Vector3.up * 1.2f;
        Light pLight = lightObj.AddComponent<Light>();
        pLight.type = LightType.Point;
        pLight.color = new Color(1.0f, 0.72f, 0.22f); // Divine Amber Gold
        pLight.range = 14f;
        pLight.intensity = 22f;
        pLight.shadows = LightShadows.None;

        // 2. Ascending Divine Soul Embers
        GameObject emberObj = new GameObject("Revival_Embers");
        emberObj.transform.position = position + Vector3.up * 0.1f;
        ParticleSystem emberPS = emberObj.AddComponent<ParticleSystem>();

        var main = emberPS.main;
        main.duration = 1.2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = new Color(1f, 0.85f, 0.35f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = emberPS.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 45),
            new ParticleSystem.Burst(0.2f, 35)
        });

        var shape = emberPS.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.2f;
        shape.arc = 360f;
        shape.rotation = new Vector3(-90f, 0f, 0f); // Face upwards

        var velOverLifetime = emberPS.velocityOverLifetime;
        velOverLifetime.enabled = true;
        velOverLifetime.space = ParticleSystemSimulationSpace.World;
        velOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        velOverLifetime.y = new ParticleSystem.MinMaxCurve(2.5f, 6.0f); // Ascend upwards into the warrior
        velOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

        var colOverLifetime = emberPS.colorOverLifetime;
        colOverLifetime.enabled = true;
        Gradient emberGrad = new Gradient();
        emberGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0.0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.1f), 0.6f),
                new GradientColorKey(new Color(0.9f, 0.15f, 0.05f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.2f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colOverLifetime.color = emberGrad;

        var emberRenderer = emberObj.GetComponent<ParticleSystemRenderer>();
        Shader addShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (addShader == null) addShader = Shader.Find("Particles/Standard Unlit");
        if (addShader == null) addShader = Shader.Find("Sprites/Default");
        emberRenderer.material = new Material(addShader);

        emberPS.Play();

        // 3. Ground Sacred Energy Shockwave Ring
        GameObject ringObj = new GameObject("Revival_Shockwave");
        ringObj.transform.position = position + Vector3.up * 0.05f;
        ParticleSystem ringPS = ringObj.AddComponent<ParticleSystem>();

        var rMain = ringPS.main;
        rMain.duration = 0.8f;
        rMain.loop = false;
        rMain.startLifetime = 0.7f;
        rMain.startSpeed = 0f;
        rMain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 4.5f); // Expands outwards
        rMain.startColor = new Color(1f, 0.75f, 0.2f, 0.85f);
        rMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var rEmission = ringPS.emission;
        rEmission.rateOverTime = 0;
        rEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 1) });

        var rShape = ringPS.shape;
        rShape.shapeType = ParticleSystemShapeType.Hemisphere;
        rShape.radius = 0.1f;

        var rRenderer = ringPS.GetComponent<ParticleSystemRenderer>();
        rRenderer.material = new Material(addShader);

        ringPS.Play();

        // 4. Darkness Dissipation Smoke (Andhkaar expelled from body)
        GameObject smokeObj = new GameObject("Revival_DarknessExpelled");
        smokeObj.transform.position = position + Vector3.up * 0.3f;
        ParticleSystem smokePS = smokeObj.AddComponent<ParticleSystem>();

        var sMain = smokePS.main;
        sMain.duration = 0.8f;
        sMain.loop = false;
        sMain.startLifetime = 0.9f;
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        sMain.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        sMain.startColor = new Color(0.08f, 0.05f, 0.12f, 0.7f); // Deep smoky purple-black

        var sEmission = smokePS.emission;
        sEmission.rateOverTime = 0;
        sEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.05f, 25) });

        var sShape = smokePS.shape;
        sShape.shapeType = ParticleSystemShapeType.Sphere;
        sShape.radius = 0.4f;

        var sRenderer = smokePS.GetComponent<ParticleSystemRenderer>();
        Shader alphaShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (alphaShader == null) alphaShader = Shader.Find("Sprites/Default");
        sRenderer.material = new Material(alphaShader);

        smokePS.Play();

        // 5. Camera impulse shake
        CameraController cam = Object.FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            cam.shakeOffset = Random.insideUnitSphere * 0.22f;
        }

        // 6. Audio Feedback (Holy Resonance Whoosh)
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.85f;
        AudioClip chime = CreateProceduralHolyChime();
        if (chime != null)
        {
            audioSource.PlayOneShot(chime);
        }

        // 7. Light Decay & Camera Shake settle
        float totalTime = 1.6f;
        float elapsed = 0f;
        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / totalTime;

            if (pLight != null)
            {
                pLight.intensity = Mathf.Lerp(22f, 0f, t * t);
            }

            if (cam != null && cam.shakeOffset != Vector3.zero)
            {
                cam.shakeOffset = Vector3.Lerp(cam.shakeOffset, Vector3.zero, Time.deltaTime * 6f);
            }

            yield return null;
        }

        if (cam != null) cam.shakeOffset = Vector3.zero;

        // Cleanup procedural visual objects
        Destroy(lightObj);
        Destroy(emberObj);
        Destroy(ringObj);
        Destroy(smokeObj);
        Destroy(gameObject);
    }

    /// <summary>
    /// Generates a synthesized warm harmonic chime sound for instant AAA audio feedback.
    /// </summary>
    private static AudioClip CreateProceduralHolyChime()
    {
        int sampleRate = 44100;
        float duration = 1.4f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float freq1 = 432f; // Sacred resonant A
        float freq2 = 648f; // Fifth harmonic (E)
        float freq3 = 864f; // Octave

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Exponential volume envelope with smooth attack
            float attack = Mathf.Clamp01(t * 25f);
            float decay = Mathf.Exp(-t * 2.8f);
            float envelope = attack * decay;

            float wave = Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.5f +
                         Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.3f +
                         Mathf.Sin(2f * Mathf.PI * freq3 * t) * 0.2f;

            samples[i] = wave * envelope * 0.45f;
        }

        AudioClip clip = AudioClip.Create("HolyRevivalChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
