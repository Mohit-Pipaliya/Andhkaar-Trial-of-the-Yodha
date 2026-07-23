using UnityEngine;

public class LightningArena : MonoBehaviour
{
    private float radius = 10f;
    private float height = 10f;
    private int segments = 64;
    private Material arenaMaterial;

    public void Initialize(float arenaRadius)
    {
        radius = arenaRadius;
        
        Mesh mesh = GenerateDoubleSidedCylinderMesh(radius, height, segments);
        
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        arenaMaterial = new Material(shader);
        
        Texture2D divineTexture = Resources.Load<Texture2D>("DivineArenaTexture");
        if (divineTexture != null)
        {
            arenaMaterial.mainTexture = divineTexture;
        }

        Color goldColor = new Color(1f, 0.75f, 0.2f, 0.8f);
        if (arenaMaterial.HasProperty("_Color")) arenaMaterial.SetColor("_Color", goldColor);
        if (arenaMaterial.HasProperty("_TintColor")) arenaMaterial.SetColor("_TintColor", goldColor);
        
        arenaMaterial.EnableKeyword("_EMISSION");
        if (arenaMaterial.HasProperty("_EmissionColor"))
            arenaMaterial.SetColor("_EmissionColor", goldColor * 1.5f);
            
        mr.material = arenaMaterial;

        SetupColliders();
    }

    private Mesh GenerateDoubleSidedCylinderMesh(float r, float h, int seg)
    {
        Mesh m = new Mesh();
        
        int vertexCount = (seg + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        
        float angleStep = 360f / seg;
        
        for (int i = 0; i <= seg; i++)
        {
            float angle = i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            float x = Mathf.Sin(rad) * r;
            float z = Mathf.Cos(rad) * r;
            
            vertices[i * 2] = new Vector3(x, 0, z);
            vertices[i * 2 + 1] = new Vector3(x, h, z);
            
            float u = (float)i / seg * 4f; 
            uvs[i * 2] = new Vector2(u, 0f);
            uvs[i * 2 + 1] = new Vector2(u, 1f);
        }
        
        int[] triangles = new int[seg * 12];
        int t = 0;
        for (int i = 0; i < seg; i++)
        {
            int baseIndex = i * 2;
            
            triangles[t++] = baseIndex;
            triangles[t++] = baseIndex + 1;
            triangles[t++] = baseIndex + 3;
            
            triangles[t++] = baseIndex;
            triangles[t++] = baseIndex + 3;
            triangles[t++] = baseIndex + 2;

            triangles[t++] = baseIndex;
            triangles[t++] = baseIndex + 3;
            triangles[t++] = baseIndex + 1;
            
            triangles[t++] = baseIndex;
            triangles[t++] = baseIndex + 2;
            triangles[t++] = baseIndex + 3;
        }
        
        m.vertices = vertices;
        m.uv = uvs;
        m.triangles = triangles;
        m.RecalculateNormals();
        
        return m;
    }

    private void SetupColliders()
    {
        int numColliders = 36;
        float collAngle = 0f;
        for (int i = 0; i < numColliders; i++)
        {
            GameObject wall = new GameObject("ArenaWall_" + i);
            wall.transform.SetParent(transform);
            
            float x = Mathf.Sin(Mathf.Deg2Rad * collAngle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * collAngle) * radius;
            wall.transform.localPosition = new Vector3(x, 5f, z);
            wall.transform.LookAt(transform.position + new Vector3(0, 5f, 0));
            
            BoxCollider col = wall.AddComponent<BoxCollider>();
            col.size = new Vector3((2f * Mathf.PI * radius) / numColliders + 1f, 20f, 1f);
            
            collAngle += (360f / numColliders);
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.up * 8f * Time.deltaTime);

        if (arenaMaterial != null)
        {
            float pulse = 0.6f + Mathf.PingPong(Time.time * 1.5f, 0.4f);
            Color goldColor = new Color(1f, 0.75f, 0.2f, pulse);
            if (arenaMaterial.HasProperty("_TintColor")) arenaMaterial.SetColor("_TintColor", goldColor);
            if (arenaMaterial.HasProperty("_Color")) arenaMaterial.SetColor("_Color", goldColor);
            if (arenaMaterial.HasProperty("_EmissionColor"))
                arenaMaterial.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.2f) * pulse * 1.5f);
        }
    }
}
