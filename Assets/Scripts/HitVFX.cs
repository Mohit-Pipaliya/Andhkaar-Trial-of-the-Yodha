using UnityEngine;

/// <summary>
/// AAA Realistic Combat Particle and Audio Effects.
/// Handles stretched directional molten sparks, kinetic blade impact flashes,
/// audio feedback, and camera impulses for visceral sword fights.
/// </summary>
public static class HitVFX
{
    private static Material additiveMaterial;
    private static AudioClip clashAudioClip;

    private static Material GetSparkMaterial()
    {
        if (additiveMaterial == null)
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            additiveMaterial = new Material(shader);
        }
        return additiveMaterial;
    }

    /// <summary>
    /// Legacy alias for compatibility.
    /// </summary>
    public static void CreateSparks(Vector3 position)
    {
        CreateSwordClashSparks(position, Vector3.up);
    }

    /// <summary>
    /// Spawns realistic stretched molten sparks, impact flash, and audio feedback.
    /// </summary>
    public static void CreateSwordClashSparks(Vector3 position, Vector3 normal = default)
    {
        if (normal == default) normal = Vector3.up;

        // 1. Spawning Stretched Molten Blade Sparks
        GameObject sparkObj = new GameObject("AAA_SwordClash_Sparks");
        sparkObj.transform.position = position;
        sparkObj.transform.forward = normal;

        ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(9f, 22f); // High kinetic velocity
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.gravityModifier = 1.35f; // Sparks shower down naturally like molten metal
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 55) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.05f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(new Color(1f, 0.72f, 0.15f), 0.35f), // Glowing Molten Gold
                new GradientColorKey(new Color(1f, 0.25f, 0.02f), 0.75f), // Fiery Ember Red
                new GradientColorKey(new Color(0.3f, 0.05f, 0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch; // Real AAA sparks are elongated lines
        renderer.cameraVelocityScale = 0f;
        renderer.velocityScale = 0.22f;
        renderer.lengthScale = 2.6f;
        renderer.material = GetSparkMaterial();

        ps.Play();

        // 2. Kinetic Impact Light Flash (Bright 0.06s flash on impact)
        GameObject flashLightObj = new GameObject("Clash_FlashLight");
        flashLightObj.transform.position = position;
        Light flashLight = flashLightObj.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1f, 0.9f, 0.6f);
        flashLight.intensity = 18f;
        flashLight.range = 5.5f;
        flashLight.shadows = LightShadows.None;
        GameObject.Destroy(flashLightObj, 0.06f);

        // 3. Camera Micro-Impulse for visceral hit impact
        CameraController cam = Object.FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            cam.shakeOffset = Random.insideUnitSphere * 0.12f;
        }

        // 4. Hit-Stop for heavy impact feeling
        if (Managers.CombatFeelManager.Instance != null)
        {
            Managers.CombatFeelManager.Instance.HitStop(0.04f, 0.1f);
        }

        // 5. Procedural Metallic Blade Clash Sound
        PlayBladeClashAudio(position);

        GameObject.Destroy(sparkObj, 0.8f);
    }

    private static void PlayBladeClashAudio(Vector3 position)
    {
        if (clashAudioClip == null)
        {
            // Synthesize high-frequency metallic blade strike chime
            int sampleRate = 44100;
            float duration = 0.28f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 22f); // Sharp metallic decay
                // Inharmonic metallic frequencies (typical of steel striking steel)
                float wave = Mathf.Sin(2f * Mathf.PI * 1850f * t) * 0.4f
                           + Mathf.Sin(2f * Mathf.PI * 2760f * t) * 0.3f
                           + Mathf.Sin(2f * Mathf.PI * 4120f * t) * 0.2f
                           + (Random.value * 2f - 1f) * Mathf.Exp(-t * 45f) * 0.35f; // Initial friction noise
                data[i] = wave * env * 0.6f;
            }

            clashAudioClip = AudioClip.Create("BladeClashSound", samples, 1, sampleRate, false);
            clashAudioClip.SetData(data, 0);
        }

        if (clashAudioClip != null)
        {
            AudioSource.PlayClipAtPoint(clashAudioClip, position, 0.75f);
        }
    }
}
