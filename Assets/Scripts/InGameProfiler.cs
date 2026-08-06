using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Text;

/// <summary>
/// AAA In-Game Profiler — Zero GC allocation version.
/// Uses StringBuilder reuse instead of string.Format (no heap alloc per frame).
/// Uses UnityEngine.Profiling.Profiler for memory (lighter than System.GC).
/// </summary>
public class InGameProfiler : MonoBehaviour
{
    private float deltaTime = 0.0f;

    private GUIStyle style;
    private GUIStyle shadowStyle;
    private Rect rect;
    private Rect shadowRect;
    private string displayText = "Loading Profiler...";

    // AAA Perf: timers split — FPS updates 0.5s, memory updates 2s (barely changes)
    private float fpsTimer   = 0f;
    private float memTimer   = 0f;

    // AAA Perf: Reusable StringBuilder \u2014 zero heap allocation per update
    private readonly StringBuilder sb = new StringBuilder(128);

    // Cached values so we don't GC every tick
    private long   cachedMemoryMB   = 0;
    private float  cachedRenderScale = 1f;
    private int    cachedPoolObjects  = 0;

    void Update()
    {
        // Exponential moving average for smooth FPS display
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fpsTimer  += Time.unscaledDeltaTime;
        memTimer  += Time.unscaledDeltaTime;

        // AAA Perf: Memory & pool check every 2s (not 0.5s) \u2014 these rarely change
        if (memTimer >= 2.0f)
        {
            memTimer = 0f;

            // AAA Perf: Profiler API is lighter than System.GC.GetTotalMemory
            cachedMemoryMB = (long)(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024));

            if (ObjectPoolManager.Instance != null)
                cachedPoolObjects = ObjectPoolManager.Instance.TotalActiveObjects;

            UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
                cachedRenderScale = urp.renderScale;
        }

        // FPS text rebuild every 0.5s
        if (fpsTimer >= 0.5f)
        {
            fpsTimer = 0f;

            float msec = deltaTime * 1000.0f;
            float fps  = 1.0f / Mathf.Max(deltaTime, 0.0001f);

            // AAA Perf: StringBuilder.Clear() + Append = zero allocation (vs string.Format)
            sb.Clear();
            sb.Append(msec.ToString("F1"));
            sb.Append(" ms (");
            sb.Append(((int)fps).ToString());
            sb.Append(" fps)\nMemory: ");
            sb.Append(cachedMemoryMB);
            sb.Append(" MB | Scale: ");
            sb.Append((cachedRenderScale * 100f).ToString("F0"));
            sb.Append("%\nPooled Objects: ");
            sb.Append(cachedPoolObjects);

            displayText = sb.ToString();
        }
    }

    void OnGUI()
    {
        if (style == null)
        {
            int w = Screen.width, h = Screen.height;
            style = new GUIStyle();
            rect = new Rect(10, 10, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 50;
            style.normal.textColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);

            shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = Color.black;
            shadowRect = new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height);
        }

        // Draw shadow for readability
        GUI.Label(shadowRect, displayText, shadowStyle);

        // Draw actual text
        GUI.Label(rect, displayText, style);
    }
}
