using UnityEngine;
using UnityEditor;

public class LightGeneratorTool : EditorWindow
{
    [MenuItem("Tools/Lighting Generator")]
    public static void ShowWindow()
    {
        GetWindow<LightGeneratorTool>("Lighting Generator");
    }

    private LightType lightType = LightType.Point;
    private Color lightColor = Color.white;
    private float lightIntensity = 1f;
    private float lightRange = 10f;

    private void OnGUI()
    {
        GUILayout.Label("Light Settings", EditorStyles.boldLabel);
        
        lightType = (LightType)EditorGUILayout.EnumPopup("Light Type", lightType);
        lightColor = EditorGUILayout.ColorField("Color", lightColor);
        lightIntensity = EditorGUILayout.FloatField("Intensity", lightIntensity);
        
        if (lightType == LightType.Point || lightType == LightType.Spot)
        {
            lightRange = EditorGUILayout.FloatField("Range", lightRange);
        }

        GUILayout.Space(15);
        GUILayout.Label("Generate Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Single Light at Center", GUILayout.Height(30)))
        {
            CreateLightAtCenter();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Add 1 Light to Each Selected Object", GUILayout.Height(30)))
        {
            CreateLightForSelectedObjects();
        }

        GUILayout.Space(20);
        GUILayout.Label("Mac/M-Series Crash Fixes", EditorStyles.boldLabel);
        GUI.color = new Color(1f, 0.6f, 0.6f); // Reddish tint
        
        if (GUILayout.Button("Stop Current Baking (Cancel)", GUILayout.Height(25)))
        {
            Lightmapping.Cancel();
            Debug.Log("Baking Canceled.");
        }

        if (GUILayout.Button("Clear Corrupted Baked Data", GUILayout.Height(25)))
        {
            Lightmapping.Clear();
            Debug.Log("Baked GI data cleared.");
        }

        GUI.color = new Color(0.6f, 1f, 0.6f); // Greenish tint
        if (GUILayout.Button("Apply Safe Mac Settings (Use CPU)", GUILayout.Height(35)))
        {
            ApplySafeMacLightingSettings();
        }
        GUI.color = Color.white; // Reset color
    }

    private void ApplySafeMacLightingSettings()
    {
        // Cancel ongoing bakes first
        Lightmapping.Cancel();

        // Switch to CPU to prevent GPU memory crashes on Apple Silicon
        // LightmapEditorSettings.lightmapper = LightmapEditorSettings.Lightmapper.ProgressiveCPU;
        
        // Lower sample counts for faster baking and less memory usage
        // LightmapEditorSettings.directSampleCount = 16;
        // LightmapEditorSettings.indirectSampleCount = 128;
        // LightmapEditorSettings.environmentSampleCount = 128;

        Debug.Log("Please manually set Lightmapper to 'Progressive CPU' in the Lighting window.");
        EditorUtility.DisplayDialog("Settings Applied", "Please open Window > Rendering > Lighting and manually set 'Lightmapper' to 'Progressive CPU' to prevent crashes.", "OK");
    }

    private void CreateLightAtCenter()
    {
        GameObject lightObj = new GameObject("Generated " + lightType.ToString() + " Light");
        SetupLightComponent(lightObj);

        // Scene view ke center me light place karne ke liye
        if (SceneView.lastActiveSceneView != null)
        {
            lightObj.transform.position = SceneView.lastActiveSceneView.pivot;
        }

        Undo.RegisterCreatedObjectUndo(lightObj, "Create Single Light");
        Selection.activeGameObject = lightObj;
        
        Debug.Log("Created a single light at scene center.");
    }

    private void CreateLightForSelectedObjects()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects in the scene to add lights to them.", "OK");
            return;
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            GameObject lightObj = new GameObject(obj.name + " _Light");
            lightObj.transform.SetParent(obj.transform);
            lightObj.transform.localPosition = Vector3.zero; // Set position to center of the selected object
            
            SetupLightComponent(lightObj);
            
            Undo.RegisterCreatedObjectUndo(lightObj, "Create Lights For Selected");
        }
        
        Debug.Log($"Created 1 light for each of the {Selection.gameObjects.Length} selected objects.");
    }

    private void SetupLightComponent(GameObject lightObj)
    {
        Light lightComp = lightObj.AddComponent<Light>();
        
        lightComp.type = lightType;
        lightComp.color = lightColor;
        lightComp.intensity = lightIntensity;
        
        if (lightType == LightType.Point || lightType == LightType.Spot)
        {
            lightComp.range = lightRange;
        }
    }
}
