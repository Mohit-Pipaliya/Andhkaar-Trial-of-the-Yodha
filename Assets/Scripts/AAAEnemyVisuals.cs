using UnityEngine;

[ExecuteAlways]
public class AAAEnemyVisuals : MonoBehaviour
{
    [Header("Scary Settings")]
    public Color eyeGlowColor = Color.red;
    public float eyeLightIntensity = 5f;
    public Color auraColor = new Color(0.1f, 0f, 0f, 1f); // Dark Red
    
    private void Start()
    {
        // 1. Make the materials look dark and scary (AAA Rim Light style)
        MakeMaterialsScary();

        // 2. Add glowing red eyes to the head
        if (transform.Find("DarkSmokeAura") == null) // Check if already created
        {
            AddGlowingEyes();
            CreateSmokeAura();
        }
    }

    private void MakeMaterialsScary()
    {
        SkinnedMeshRenderer[] meshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer smr in meshes)
        {
            foreach (Material mat in smr.materials)
            {
                // Pure Black color for a shadow demon look
                if (mat.HasProperty("_Color"))
                {
                    mat.color = Color.black; // Ekdam kaala (Pitch Black)
                }
                
                // Remove Emission so it doesn't glow red like a tomato
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }

    private void AddGlowingEyes()
    {
        // Try to find the Head bone
        Transform headBone = FindDeepChild(transform, "Head");
        if (headBone == null) headBone = FindDeepChild(transform, "mixamorig:Head");
        
        if (headBone != null)
        {
            // Left Eye Light
            GameObject leftEye = new GameObject("ScaryEye_Left");
            leftEye.transform.SetParent(headBone);
            leftEye.transform.localPosition = new Vector3(-0.05f, 0.05f, 0.1f); // Adjust based on model
            Light lLight = leftEye.AddComponent<Light>();
            lLight.type = LightType.Point;
            lLight.color = eyeGlowColor;
            lLight.intensity = eyeLightIntensity;
            lLight.range = 2f;

            // Right Eye Light
            GameObject rightEye = new GameObject("ScaryEye_Right");
            rightEye.transform.SetParent(headBone);
            rightEye.transform.localPosition = new Vector3(0.05f, 0.05f, 0.1f);
            Light rLight = rightEye.AddComponent<Light>();
            rLight.type = LightType.Point;
            rLight.color = eyeGlowColor;
            rLight.intensity = eyeLightIntensity;
            rLight.range = 2f;
        }
    }

    private void CreateSmokeAura()
    {
        GameObject aura = new GameObject("DarkSmokeAura");
        aura.transform.SetParent(transform);
        aura.transform.localPosition = new Vector3(0, 0.2f, 0);

        ParticleSystem ps = aura.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 3f;
        main.startSpeed = 0.5f;
        main.startSize = 2f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0f, 0f, 0f, 0.5f), new Color(0.2f, 0f, 0f, 0.5f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(auraColor, 0.0f), new GradientColorKey(Color.black, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.8f, 0.2f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        // Note: Default particle material is pink if we don't assign one, but we use the default Particle Material
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material defaultMat = new Material(Shader.Find("Particles/Standard Unlit"));
        
        // Setup material for transparent fading
        defaultMat.SetFloat("_Mode", 2); // Fade mode
        defaultMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        defaultMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        defaultMat.SetInt("_ZWrite", 0);
        defaultMat.DisableKeyword("_ALPHATEST_ON");
        defaultMat.EnableKeyword("_ALPHABLEND_ON");
        defaultMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        defaultMat.renderQueue = 3000;
        
        renderer.material = defaultMat;
    }

    // Helper to find bones recursively
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;
            
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
