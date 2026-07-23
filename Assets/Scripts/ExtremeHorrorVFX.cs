using UnityEngine;
using System.Collections;

[ExecuteAlways]
public class ExtremeHorrorVFX : MonoBehaviour
{
    private Light pulseLight;

    void Start()
    {
        if (transform.Find("HeartbeatPulseLight") != null) return; // Prevent duplicates in edit mode

        // 1. Ek daravani heartbeat jaisi dhadakti hui red light
        CreateHeartbeatLight();

        // 2. Enemy ke chaaro taraf ek bada, ghana (dense) kaala aur laal fog
        CreateCreepyFog();

        // 3. Narak (Hell) ki aag jaisi chingariyan (Embers) jo hawa me udengi
        CreateHellEmbers();

        // 4. Enemy ki body me achanak se jhatke (Glitch/Twitch) aayenge jaise koi bhoot ho
        if (Application.isPlaying) StartCoroutine(TwitchRoutine());
    }

    void CreateHeartbeatLight()
    {
        GameObject lightObj = new GameObject("HeartbeatPulseLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = new Vector3(0, 1.5f, 0);

        pulseLight = lightObj.AddComponent<Light>();
        pulseLight.type = LightType.Point;
        pulseLight.color = new Color(1f, 0f, 0f); // Khoon jaisa laal (Blood Red)
        pulseLight.range = 15f;
        pulseLight.intensity = 0f;

        StartCoroutine(PulseRoutine());
    }

    IEnumerator PulseRoutine()
    {
        while(true)
        {
            // Heartbeat thump 1 (Dhak)
            pulseLight.intensity = 8f;
            yield return new WaitForSeconds(0.08f);
            pulseLight.intensity = 2f;
            yield return new WaitForSeconds(0.08f);
            
            // Heartbeat thump 2 (Dhak)
            pulseLight.intensity = 15f; // Bohot tez chamak
            yield return new WaitForSeconds(0.15f);
            
            // Dheere dheere fade hona
            float t = 0;
            while(t < 1f)
            {
                t += Time.deltaTime * 2f;
                pulseLight.intensity = Mathf.Lerp(15f, 0f, t);
                yield return null;
            }
            
            // Agli dhadkan tak shanti (Tension build karne ke liye)
            yield return new WaitForSeconds(Random.Range(0.8f, 2.0f)); 
        }
    }

    void CreateCreepyFog()
    {
        GameObject fog = new GameObject("CreepyBlackFog");
        fog.transform.SetParent(transform);
        fog.transform.localPosition = new Vector3(0, 1f, 0);

        ParticleSystem ps = fog.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 4f;
        main.startSpeed = 0.5f;
        main.startSize = 10f; // Bohot bada dhuaan
        main.startColor = new Color(0.05f, 0f, 0.05f, 0.8f); // Pitch dark shadow
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;

        var emission = ps.emission;
        emission.rateOverTime = 30f; // Ghana dhuaan

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 4f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(new Color(0.3f, 0f, 0f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        ParticleSystemRenderer renderer = fog.GetComponent<ParticleSystemRenderer>();
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

    void CreateHellEmbers()
    {
        GameObject emberObj = new GameObject("HellFireEmbers");
        emberObj.transform.SetParent(transform);
        emberObj.transform.localPosition = new Vector3(0, 0.2f, 0);

        ParticleSystem ps = emberObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 3f;
        main.startSpeed = 2f;
        main.startSize = 0.15f;
        main.startColor = Color.red;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 2f;
        emberObj.transform.localRotation = Quaternion.Euler(-90, 0, 0); // Upar ki taraf udengi

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 2f; // Hawa me zig-zag udne ke liye
        noise.frequency = 1f;
        noise.scrollSpeed = 1f;

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(new Color(1f, 0.5f, 0f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorLife.color = grad;

        ParticleSystemRenderer renderer = emberObj.GetComponent<ParticleSystemRenderer>();
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.SetColor("_Color", Color.red);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.red * 10f); // Embers ko chamkane ke liye
        renderer.material = mat;
    }

    IEnumerator TwitchRoutine()
    {
        Vector3 originalScale = transform.localScale;
        
        while(true)
        {
            // 2 se 6 second ke beech achanak ek jhatka
            yield return new WaitForSeconds(Random.Range(2f, 6f));
            
            // Glitch / Twitch effect (Jaise kisi bhoot ki body ajeeb tarike se milti hai)
            int twitches = Random.Range(3, 8);
            for (int i = 0; i < twitches; i++)
            {
                transform.localScale = originalScale + new Vector3(
                    Random.Range(-0.1f, 0.1f),
                    Random.Range(-0.15f, 0.15f),
                    Random.Range(-0.1f, 0.1f)
                );
                
                yield return new WaitForSeconds(0.04f); // Ekdam fast
            }
            transform.localScale = originalScale;
        }
    }
}
