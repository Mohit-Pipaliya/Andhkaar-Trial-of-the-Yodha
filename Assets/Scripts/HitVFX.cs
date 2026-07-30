using UnityEngine;

public static class HitVFX
{
    // Yeh function automatically bina kisi prefab ke game me chingari (sparks) generate karta hai
    public static void CreateSparks(Vector3 position)
    {
        GameObject sparkObj = new GameObject("HitSparks");
        sparkObj.transform.position = position;
        
        ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.3f; // Chingari kitni der zinda rahegi
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f); // Chingari kitni tez udxegi
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f); 
        main.startColor = new Color(1f, 0.6f, 0f, 1f); // Orange-yellow aag jaisi chingari
        main.loop = false;
        main.playOnAwake = true;
        
        var emission = ps.emission;
        emission.rateOverTime = 0;
        // Ek baar me 30 se 50 chingariyan nikalni chahiye
        emission.SetBursts(new ParticleSystem.Burst[]{ new ParticleSystem.Burst(0f, 40) });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        // Unlit material taaki andhere me bhi chingari chamke (glow kare)
        Shader defaultShader = Shader.Find("Sprites/Default");
        if (defaultShader != null)
        {
            renderer.material = new Material(defaultShader);
        }

        // Particle khatam hone ke baad object ko delete kar do taaki game lag na kare
        GameObject.Destroy(sparkObj, 1f);
    }
}
