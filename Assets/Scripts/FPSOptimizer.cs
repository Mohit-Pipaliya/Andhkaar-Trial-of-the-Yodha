using UnityEngine;
using System.Collections;

public class FPSOptimizer : MonoBehaviour
{
    [Header("Frame Rate Settings")]
    [Tooltip("Kitna FPS chahiye. 80+ ke liye 120 rakhna theek hai.")]
    public int targetFPS = 120; 
    [Tooltip("VSync ko 0 karna zaroori hai warna FPS monitor ki hertz (mostly 60) par lock ho jayega.")]
    public bool disableVSync = true; 

    [Header("Graphics Optimization (Bina delete kiye)")]
    public bool optimizeGraphics = true;
    
    void Awake()
    {
        ApplyOptimizations();
    }

    public void ApplyOptimizations()
    {
        // 1. VSync Off aur Framerate Unlock
        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0; // VSync completely disable
        }
        Application.targetFrameRate = targetFPS; // Target 120 FPS

        // 2. Graphics Tweaks (FPS badhane ke liye quality thodi optimize hogi, par kuch delete/disable nahi hoga)
        if (optimizeGraphics)
        {
            // Pixel lights kam karne se lighting calculations kam hoti hain (default 4 hota hai, 2 is optimized)
            QualitySettings.pixelLightCount = 2; 

            // Shadows sabse zyada FPS khaati hain. Hum inko disable nahi kar rahe, bas optimize kar rahe hain.
            QualitySettings.shadowResolution = ShadowResolution.Medium; // High ki jagah Medium
            QualitySettings.shadowCascades = 2; // 4 cascades ki jagah 2 (better performance)
            QualitySettings.shadowDistance = 50f; // Kitni door tak shadows dikhengi
            QualitySettings.shadows = ShadowQuality.HardOnly; // Soft shadows bohot heavy hoti hain

            // Anti-aliasing ko kam karna (Edges thode sharp honge par FPS badhega)
            QualitySettings.antiAliasing = 0; 
            
            // Texture quality ko thoda optimize kiya (No blur, just memory saving)
            QualitySettings.globalTextureMipmapLimit = 1; // 0 (Full) se 1 (Half) kiya, isse VRAM aur bandwidth bachegi

            // Character animation bones calculation ko 4 se 2 karna (performance boost for characters)
            QualitySettings.skinWeights = SkinWeights.TwoBones;

            // LODs ko jaldi switch karna taaki poly count kam ho
            QualitySettings.lodBias = 0.3f; // 0.5 se aur kam karke 0.3 kiya for max FPS

            // 77M Triangles usually Terrain trees/grass ki wajah se hote hain. 
            if (Terrain.activeTerrain != null)
            {
                Terrain.activeTerrain.treeDistance = 250f; // 200+ still visible, but slightly shorter
                Terrain.activeTerrain.treeBillboardDistance = 70f; // Jaldi 2D ped banenge
                Terrain.activeTerrain.detailObjectDistance = 150f; // Grass 150 door tak dikhegi
                Terrain.activeTerrain.treeMaximumFullLODCount = 10; // Sirf paas ke 10 ped high quality rahenge
            }

            // Camera ka viewing distance 
            if (Camera.main != null)
            {
                if (Camera.main.farClipPlane > 600f)
                {
                    Camera.main.farClipPlane = 600f; // Max 600
                }
            }

            // Physics calculation ko adha kar diya! (Bohat bada CPU boost)
            // Default 0.02 (50 times/sec) hota hai. Isey 0.04 (25 times/sec) karne se physics CPU load half ho jata hai
            Time.fixedDeltaTime = 0.04f; 
            
            // Background loading ko slow karo taaki main game fast chale
            Application.backgroundLoadingPriority = ThreadPriority.Low;
            
            // Memory saaf karo shuru me
            System.GC.Collect();
        }
        
        Debug.Log("FPS Optimizer Applied! Target FPS: " + Application.targetFrameRate);
    }
}
