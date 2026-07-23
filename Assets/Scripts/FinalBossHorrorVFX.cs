using UnityEngine;
using System.Collections;

[ExecuteAlways]
public class FinalBossHorrorVFX : MonoBehaviour
{
    private Light lightningLight;
    
    void Start()
    {
        // Model ko kaala aur darawana banane ke liye
        MakeMaterialsScary();

        if (transform.Find("BossLightningStrike") != null) return; // Prevent duplicates in edit mode

        // 1. Aasman se girne wali Daravani Bijli (Lightning Flashes)
        CreateLightningLight();
        
        // 2. Boss ke pairo ke neeche ek bahot bada maut ka kaala dhuaan (Abyss Fog)
        CreateAbyssFog();
        
        // 3. Khooni dhuaan jo boss ke chaaro taraf chakkar lagayega (Swirling Blood Mist)
        CreateSwirlingMist();
        
        // 4. Aankhon se nikalne wala khoon ka dhuaan (Bleeding Eyes Trail)
        CreateBleedingEyes();
        
        // 5. Zameen par aag ka ek bada ring (Hellfire Ring)
        CreateHellfireRing();
        
        // 6. Boss ki heavy saans lene ki animation (Breathing Scale effect)
        if (Application.isPlaying) StartCoroutine(HeavyBreathingRoutine());
        
        // 7. Bijli chamkane wala routine
        if (Application.isPlaying) StartCoroutine(LightningStormRoutine());
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
                    mat.color = Color.black; 
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

    void CreateLightningLight()
    {
        GameObject lightObj = new GameObject("BossLightningStrike");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = new Vector3(0, 5f, 0);

        lightningLight = lightObj.AddComponent<Light>();
        lightningLight.type = LightType.Point;
        lightningLight.color = new Color(0.8f, 0.2f, 1f); // Purple / White lightning
        lightningLight.range = 40f;
        lightningLight.intensity = 0f;
        lightningLight.shadows = LightShadows.Soft; 
    }

    IEnumerator LightningStormRoutine()
    {
        while(true)
        {
            // 3 se 8 second ke beech kabhi bhi bijli karkegi
            yield return new WaitForSeconds(Random.Range(3f, 8f));
            
            // Pehla jhatka (Flash 1)
            lightningLight.intensity = 25f;
            yield return new WaitForSeconds(0.05f);
            lightningLight.intensity = 0f;
            yield return new WaitForSeconds(0.05f);
            
            // Dusra lamba jhatka (Flash 2)
            lightningLight.intensity = 50f; // Extremely bright
            yield return new WaitForSeconds(0.1f);
            
            // Dheere dheere fade hona
            float t = 0;
            while(t < 1f)
            {
                t += Time.deltaTime * 4f;
                lightningLight.intensity = Mathf.Lerp(50f, 0f, t);
                yield return null;
            }
        }
    }

    void CreateAbyssFog()
    {
        GameObject fog = new GameObject("MassiveAbyssFog");
        fog.transform.SetParent(transform);
        fog.transform.localPosition = new Vector3(0, 0f, 0);

        ParticleSystem ps = fog.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 10f;
        main.loop = true;
        main.startLifetime = 6f;
        main.startSpeed = 0.2f;
        main.startSize = 18f; // Bohot bada dhuaan (MASSIVE)
        main.startColor = new Color(0f, 0f, 0f, 0.95f); // Pure pitch black
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;

        var emission = ps.emission;
        emission.rateOverTime = 40f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 5f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(new Color(0.05f, 0f, 0.05f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        SetupParticleMaterial(fog);
    }

    void CreateSwirlingMist()
    {
        GameObject mist = new GameObject("SwirlingBloodMist");
        mist.transform.SetParent(transform);
        mist.transform.localPosition = new Vector3(0, 2f, 0);

        ParticleSystem ps = mist.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 3f;
        main.startSpeed = 3f;
        main.startSize = 6f;
        main.startColor = new Color(0.6f, 0f, 0f, 0.7f); // Khoon jaisa dhuaan
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 3f;

        // Ye effect dhue ko boss ke chaaro taraf ek tornado ki tarah ghumayega
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalZ = 3f; 
        vel.orbitalY = 1f;
        vel.radial = -1f; // Andar ki taraf khinchega

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.black, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        SetupParticleMaterial(mist);
    }

    void CreateHellfireRing()
    {
        GameObject ring = new GameObject("HellfireGroundRing");
        ring.transform.SetParent(transform);
        ring.transform.localPosition = new Vector3(0, 0.1f, 0);
        ring.transform.localRotation = Quaternion.Euler(-90, 0, 0);

        ParticleSystem ps = ring.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1f;
        main.startSpeed = 0f; // Ek hi jagah jalega
        main.startSize = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.rateOverTime = 80f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 4f; // Boss ke chaaro taraf aag ka ghera
        
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.red, 0.5f), new GradientColorKey(Color.black, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        ParticleSystemRenderer renderer = ring.GetComponent<ParticleSystemRenderer>();
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.SetColor("_Color", Color.white);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.red * 8f); // Aag ko chamkane ke liye
        mat.SetFloat("_Mode", 2); 
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        renderer.material = mat;
    }

    void CreateBleedingEyes()
    {
        Transform head = FindDeepChild(transform, "Head");
        if (head == null) head = FindDeepChild(transform, "mixamorig:Head");
        
        if (head != null)
        {
            // Aankhon se laal trail niklegi jo peeche chutege jab boss move karega
            CreateEyeTrail(head, new Vector3(-0.06f, 0.05f, 0.12f), "LeftEyeBleed");
            CreateEyeTrail(head, new Vector3(0.06f, 0.05f, 0.12f), "RightEyeBleed");
        }
    }

    void CreateEyeTrail(Transform parent, Vector3 localPos, string name)
    {
        GameObject eye = new GameObject(name);
        eye.transform.SetParent(parent);
        eye.transform.localPosition = localPos;

        TrailRenderer tr = eye.AddComponent<TrailRenderer>();
        tr.time = 0.5f; // Kitni der tak trail dikhega
        tr.startWidth = 0.05f;
        tr.endWidth = 0f;
        
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.SetColor("_Color", Color.red);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.red * 10f); // Chamakti hui laal trail
        tr.material = mat;
    }

    IEnumerator HeavyBreathingRoutine()
    {
        Vector3 baseScale = transform.localScale;
        while(true)
        {
            // Saans andar lena (Expand)
            float t = 0;
            while(t < 1f)
            {
                t += Time.deltaTime * 0.8f; 
                transform.localScale = Vector3.Lerp(baseScale, baseScale * 1.03f, t); // Body 3% phulegi
                yield return null;
            }
            
            // Saans bahar chhodna (Shrink)
            t = 0;
            while(t < 1f)
            {
                t += Time.deltaTime * 1.2f; 
                transform.localScale = Vector3.Lerp(baseScale * 1.03f, baseScale, t);
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
    }

    void SetupParticleMaterial(GameObject go)
    {
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        Material defaultMat = new Material(Shader.Find("Particles/Standard Unlit"));
        defaultMat.SetFloat("_Mode", 2); // Fade
        defaultMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        defaultMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        defaultMat.SetInt("_ZWrite", 0);
        defaultMat.DisableKeyword("_ALPHATEST_ON");
        defaultMat.EnableKeyword("_ALPHABLEND_ON");
        defaultMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        defaultMat.renderQueue = 3000;
        renderer.material = defaultMat;
    }

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
