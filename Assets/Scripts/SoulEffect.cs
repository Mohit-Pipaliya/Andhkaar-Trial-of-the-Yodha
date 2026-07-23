using UnityEngine;

public class SoulEffect : MonoBehaviour
{
    [Header("Demon Fire Appearance")]
    public Color fireColor = new Color(1.0f, 0.3f, 0.0f, 1.0f); // Bright Magma Orange
    public Color smokeColor = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark Fog
    public Color eyeColor = new Color(1.0f, 0.0f, 0.0f, 1.0f); // Glowing Red
    
    [Header("AAA Giant Settings")]
    public float scaleMultiplier = 1.1f; // User requested 1100 scale (1000 * 1.1)
    public float coreLightIntensity = 8f;

    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.3f;

    private Material soulMaterial;

    void Start()
    {
        // 0. AAA Giant Scale Upgrade
        ApplyGiantScale();

        // 1. Create a glowing additive material for the magma overlay
        Shader soulShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (soulShader == null) soulShader = Shader.Find("Particles/Standard Unlit");
        if (soulShader == null) soulShader = Shader.Find("Sprites/Default");

        soulMaterial = new Material(soulShader);
        soulMaterial.SetColor("_TintColor", fireColor);
        soulMaterial.SetColor("_Color", fireColor);
        soulMaterial.SetColor("_EmissionColor", fireColor * 2.0f);
        soulMaterial.EnableKeyword("_EMISSION");

        // Material tinting removed to preserve original textures.

        // 3. Add Fire/Ember Particles
        AddEmberParticles();
        
        // 4. Add Dark Smoke/Fog
        AddSmokeFog();

        // 5. Add Glowing Red Eyes
        AddGlowingEyes();
        
        // 6. Add Inner Core Glow (AAA Lighting)
        AddCoreGlow();
    }

    private void ApplyGiantScale()
    {
        // Multiply the existing scale to preserve FBX import scales (like 1000x1000x1000)
        transform.localScale = transform.localScale * scaleMultiplier;
        
        // Note: We DO NOT manually scale CharacterController or NavMeshAgent properties.
        // Unity automatically scales them in world space when transform.localScale changes!
    }

    private void AddCoreGlow()
    {
        GameObject coreLightObj = new GameObject("FieryCoreGlow");
        coreLightObj.transform.SetParent(transform);
        coreLightObj.transform.localPosition = new Vector3(0, 1.2f * scaleMultiplier, 0); // Chest height
        
        Light coreLight = coreLightObj.AddComponent<Light>();
        coreLight.type = LightType.Point;
        coreLight.color = fireColor;
        coreLight.range = 8f * scaleMultiplier;
        coreLight.intensity = coreLightIntensity;
        coreLight.renderMode = LightRenderMode.ForcePixel; 
    }

    private void AddEmberParticles()
    {
        GameObject particlesObj = new GameObject("FieryEmbers");
        particlesObj.transform.SetParent(transform);
        particlesObj.transform.localPosition = new Vector3(0, 1f, 0); // Center of body
        
        ParticleSystem ps = particlesObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.startColor = new Color(fireColor.r, fireColor.g, fireColor.b, 1f); 
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f * scaleMultiplier, 0.2f * scaleMultiplier);
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
        main.gravityModifier = -0.1f; // Float upwards like sparks
        main.maxParticles = 300; // Increased for AAA density

        var emission = ps.emission;
        emission.rateOverTime = 80f;

        var shape = ps.shape;
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null)
        {
            shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
            shape.skinnedMeshRenderer = smr;
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.5f, 2f, 1.5f);
        }
        
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0.0f), new GradientColorKey(fireColor, 0.5f), new GradientColorKey(Color.red, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        ParticleSystemRenderer psr = particlesObj.GetComponent<ParticleSystemRenderer>();
        psr.material = soulMaterial;
    }

    private void AddSmokeFog()
    {
        GameObject fogObj = new GameObject("DarkSmokeFog");
        fogObj.transform.SetParent(transform);
        fogObj.transform.localPosition = new Vector3(0, 0.2f, 0); // Near feet
        
        ParticleSystem ps = fogObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.startColor = smokeColor;
        main.startSize = new ParticleSystem.MinMaxCurve(2.0f * scaleMultiplier, 4.0f * scaleMultiplier);
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.gravityModifier = -0.01f; // Barely float up
        main.maxParticles = 100; // Increased density

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 2.0f * scaleMultiplier;
        shape.rotation = new Vector3(-90, 0, 0); // Flat on ground
        
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(smokeColor, 0.0f), new GradientColorKey(Color.black, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(smokeColor.a, 0.2f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        // Use a default alpha blended material for smoke
        ParticleSystemRenderer psr = fogObj.GetComponent<ParticleSystemRenderer>();
        Shader smokeShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (smokeShader == null) smokeShader = Shader.Find("Particles/Standard Unlit");
        if (smokeShader != null)
        {
            Material smokeMat = new Material(smokeShader);
            if (smokeMat.HasProperty("_TintColor")) smokeMat.SetColor("_TintColor", smokeColor);
            psr.material = smokeMat;
        }
    }

    private void AddGlowingEyes()
    {
        // Try to find the head bone (commonly named "Head", "mixamorig:Head", etc)
        Transform headBone = null;
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name.ToLower().Contains("head"))
            {
                headBone = t;
                break;
            }
        }

        if (headBone != null)
        {
            // Add a glowing red light to the head
            GameObject eyeLightObj = new GameObject("DemonEyeGlow");
            eyeLightObj.transform.SetParent(headBone);
            eyeLightObj.transform.localPosition = new Vector3(0, 0.1f, 0.1f); // Approximate front of face
            
            Light eyeLight = eyeLightObj.AddComponent<Light>();
            eyeLight.type = LightType.Point;
            eyeLight.color = eyeColor;
            eyeLight.range = 3f;
            eyeLight.intensity = 5f;
            eyeLight.renderMode = LightRenderMode.ForcePixel; // Make it pop
        }
        else
        {
            Debug.LogWarning("SoulEffect: Could not find 'Head' bone to attach glowing eyes.");
        }
    }

    void Update()
    {
        // 6. Pulse the brightness of the magma material over time
        if (soulMaterial != null)
        {
            float pulse = 1f + Mathf.PingPong(Time.time * pulseSpeed, pulseIntensity);
            Color currentColor = fireColor * pulse;
            
            if (soulMaterial.HasProperty("_TintColor"))
                soulMaterial.SetColor("_TintColor", currentColor);
                
            if (soulMaterial.HasProperty("_EmissionColor"))
                soulMaterial.SetColor("_EmissionColor", currentColor * 2f);
        }
    }
}
