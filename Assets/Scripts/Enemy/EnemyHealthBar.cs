using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnemyHealthBar : MonoBehaviour
{
    private Canvas canvas;
    private List<Image> healthSegments = new List<Image>();

    [Header("Raji Ground Circle Settings")]
    [Header("Raji Ground Circle Settings")]
    public int segmentCount = 12; // Perfect circle of runes
    public float radius = 0.6f; // Tighter radius around the feet
    public Vector2 segmentSize = new Vector2(0.25f, 0.06f); // Thicker, pill-shaped dashescker and long)

    public Color activeColor = new Color(0.2f, 0.9f, 1f, 1f); 
    public Color inactiveColor = new Color(0f, 0f, 0f, 0f); // Invisible when health is lost
    
    private float targetFill = 1f;
    private bool isInitialized = false;

    void Start()
    {
        if (!isInitialized)
        {
            Initialize(activeColor);
        }
    }

    public void Initialize(Color glowColor)
    {
        if (isInitialized) return;
        
        // Multiply RGB by a sensible HDR value (x2 instead of x4 so it doesn't wash out)
        activeColor = new Color(glowColor.r * 2f, glowColor.g * 2f, glowColor.b * 2f, 1f);

        // Dynamically adjust size based on the model's CharacterController radius
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            radius = cc.radius * 1.5f; // Add padding around feet
            float scaleFactor = radius / 0.6f; 
            segmentSize = new Vector2(0.25f * scaleFactor, 0.06f * scaleFactor);
        }

        // Create a CRISP, Anti-Aliased "Pill" (Rounded Rectangle) texture
        int texWidth = 128;
        int texHeight = 32;
        Texture2D pillTex = new Texture2D(texWidth, texHeight);
        float r = texHeight / 2f;
        
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                float dist = 0f;
                // Left half-circle
                if (x < r) 
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                // Right half-circle
                else if (x > texWidth - r) 
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(texWidth - r, r));
                // Middle rectangle
                else 
                    dist = Mathf.Abs(y - r); // Distance to horizontal center

                // Smooth AA edge
                float alpha = Mathf.Clamp01(r - dist);
                pillTex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        pillTex.Apply();
        Sprite finalSprite = Sprite.Create(pillTex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f), 100f);

        // 1. Create Canvas at the Enemy's feet
        GameObject canvasGO = new GameObject("GroundHealthCanvas");
        canvasGO.transform.SetParent(this.transform);
        canvasGO.transform.localPosition = new Vector3(0, 0.1f, 0); 
        
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3f, 3f);
        canvasRect.localScale = Vector3.one;

        // 2. Create the segmented circular dashes
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segGO = new GameObject("Segment_" + i);
            segGO.transform.SetParent(canvasGO.transform);
            
            float angleDeg = i * (360f / segmentCount);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            
            Vector3 pos = new Vector3(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius, 0);
            
            Image img = segGO.AddComponent<Image>();
            img.sprite = finalSprite; // Use the crisp pill texture
            img.color = activeColor;
            
            RectTransform rect = segGO.GetComponent<RectTransform>();
            rect.localPosition = pos;
            rect.sizeDelta = segmentSize; 
            rect.localScale = Vector3.one;
            
            // Rotate the dash so it points outwards
            rect.localRotation = Quaternion.Euler(0, 0, angleDeg);

            healthSegments.Add(img);
        }
        
        isInitialized = true;
    }

    void LateUpdate()
    {
        if (canvas != null)
        {
            // Force the canvas to ALWAYS lay flat on the ground
            canvas.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            
            // Keep at feet, completely bypassing any external offsets
            canvas.transform.position = transform.position + new Vector3(0, 0.1f, 0);
        }
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        targetFill = Mathf.Clamp01((float)currentHealth / maxHealth);
        int activeCount = Mathf.CeilToInt(targetFill * segmentCount);

        for (int i = 0; i < segmentCount; i++)
        {
            if (i < activeCount)
            {
                healthSegments[i].color = activeColor; 
            }
            else
            {
                healthSegments[i].color = inactiveColor; 
            }
        }
    }

    public void HideBar()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }
    }
}
