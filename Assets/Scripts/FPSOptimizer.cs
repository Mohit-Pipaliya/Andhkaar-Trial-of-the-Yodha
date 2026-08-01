using UnityEngine;
using System.Collections;

public class FPSOptimizer : MonoBehaviour
{
    [Header("Frame Rate Settings")]
    [Tooltip("Kitna FPS chahiye. 60 FPS stable ke liye.")]
    public int targetFPS = 60; 
    [Tooltip("VSync ko 0 karna zaroori hai warna FPS monitor ki hertz (mostly 60) par lock ho jayega.")]
    public bool disableVSync = true; 

    [Header("Graphics Optimization (80+ FPS ke liye Aggressive)")]
    public bool optimizeGraphics = true;
    public bool disableShadowsForMaxFPS = false; // Isko true karein agar shadows bilkul nahi chahiye
    
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
        Application.targetFrameRate = targetFPS; // Target 144 FPS for 80+ steady

        // 2. Graphics Tweaks (Aggressive for Max FPS)
        if (optimizeGraphics)
        {
            // Pixel lights kam karne se lighting calculations kam hoti hain (default 4 hota hai, 1 is optimized)
            QualitySettings.pixelLightCount = 1; 

            // Shadows sabse zyada FPS khaati hain.
            if (disableShadowsForMaxFPS) 
            {
                QualitySettings.shadows = ShadowQuality.Disable;
            }
            else 
            {
                QualitySettings.shadowResolution = ShadowResolution.Low; // Medium se Low
                QualitySettings.shadowCascades = 0; // Better performance
                QualitySettings.shadowDistance = 30f; // Aur paas tak shadows
                QualitySettings.shadows = ShadowQuality.HardOnly; 
            }

            // Anti-aliasing ko kam karna
            QualitySettings.antiAliasing = 0; 
            
            // Texture quality ko optimize kiya
            QualitySettings.globalTextureMipmapLimit = 1; // 1 (Half resolution textures)

            // Character animation bones calculation ko 4 se 1 ya 2 karna
            QualitySettings.skinWeights = SkinWeights.OneBone; // Max performance for characters

            // LODs ko jaldi switch karna
            QualitySettings.lodBias = 0.2f; 
            
            // Realtime reflections off
            QualitySettings.realtimeReflectionProbes = false;

            // Terrain optimizations
            if (Terrain.activeTerrain != null)
            {
                Terrain.activeTerrain.treeDistance = 150f; // Trees thode jaldi gayab honge
                Terrain.activeTerrain.treeBillboardDistance = 30f; // Jaldi 2D ped banenge
                Terrain.activeTerrain.detailObjectDistance = 80f; // Grass kam doori tak dikhegi
                Terrain.activeTerrain.treeMaximumFullLODCount = 5; // Sirf 5 ped high quality rahenge
                Terrain.activeTerrain.heightmapPixelError = 10; // Terrain mesh optimize
            }

            // Camera optimizations
            if (Camera.main != null)
            {
                if (Camera.main.farClipPlane > 400f)
                {
                    Camera.main.farClipPlane = 400f; // Max 400 to render less objects
                }
                Camera.main.allowHDR = false;
                Camera.main.allowMSAA = false;
            }

            // Physics calculation ko optimize (60 times/sec for stable 60 FPS)
            Time.fixedDeltaTime = 0.016666f; 
            
            // Background loading ko slow karo
            Application.backgroundLoadingPriority = ThreadPriority.Low;
            
            // Memory saaf karo shuru me
            System.GC.Collect();
        }
        
        Debug.Log("Aggressive FPS Optimizer Applied! Target FPS: " + Application.targetFrameRate);
    }
}
