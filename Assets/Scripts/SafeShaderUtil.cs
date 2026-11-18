using UnityEngine;

/// <summary>
/// Safe Shader Utility to ensure materials and particles never turn magenta/pink 
/// or throw ArgumentNullException in standalone builds when shaders are stripped.
/// </summary>
public static class SafeShaderUtil
{
    private static Shader _cachedParticleShader;
    private static Shader _cachedColoredShader;

    public static Shader GetParticleShader()
    {
        if (_cachedParticleShader != null) return _cachedParticleShader;

        // Try Universal Render Pipeline Particle Unlit first
        _cachedParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (_cachedParticleShader == null) _cachedParticleShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (_cachedParticleShader == null) _cachedParticleShader = Shader.Find("Particles/Standard Unlit");
        if (_cachedParticleShader == null) _cachedParticleShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (_cachedParticleShader == null) _cachedParticleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (_cachedParticleShader == null) _cachedParticleShader = Shader.Find("Sprites/Default");
        if (_cachedParticleShader == null) _cachedParticleShader = Shader.Find("UI/Default");
        if (_cachedParticleShader == null) _cachedParticleShader = Shader.Find("Standard");

        return _cachedParticleShader;
    }

    public static Shader GetColoredShader()
    {
        if (_cachedColoredShader != null) return _cachedColoredShader;

        _cachedColoredShader = Shader.Find("Hidden/Internal-Colored");
        if (_cachedColoredShader == null) _cachedColoredShader = Shader.Find("UI/Default");
        if (_cachedColoredShader == null) _cachedColoredShader = Shader.Find("Sprites/Default");
        if (_cachedColoredShader == null) _cachedColoredShader = GetParticleShader();

        return _cachedColoredShader;
    }

    public static Material CreateParticleMaterial()
    {
        Shader s = GetParticleShader();
        Material mat = new Material(s);
        // Setup transparent alpha blending
        if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 2); // Fade
        if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        return mat;
    }

    public static Material CreateColoredMaterial(Color color)
    {
        Shader s = GetColoredShader();
        Material mat = new Material(s);
        mat.color = color;
        return mat;
    }
}
