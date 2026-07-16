using UnityEngine;
using UnityEditor;

public class SetupDemonAnimations : EditorWindow
{
    [MenuItem("Tools/Setup Demon Attack Events")]
    public static void SetupEvents()
    {
        string[] attackFiles = {
            "Assets/Animations/Character Animation/Demon/Attack.fbx",
            "Assets/Animations/Character Animation/Demon/Attack 2.fbx"
        };

        bool anyModified = false;

        foreach (string path in attackFiles)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                
                // If clips are empty, we need to extract them from takeinfo
                if (clips.Length == 0)
                {
                    clips = importer.clipAnimations;
                }

                if (clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        // Calculate times based on frames
                        float totalFrames = clips[i].lastFrame - clips[i].firstFrame;
                        float frameRate = 30f; // Default FBX framerate
                        float duration = totalFrames / frameRate;

                        AnimationEvent[] events = new AnimationEvent[3];
                        
                        // Event 1: EnableHitbox at 30% of animation
                        events[0] = new AnimationEvent();
                        events[0].functionName = "EnableHitbox";
                        events[0].time = duration * 0.3f; 

                        // Event 2: DisableHitbox at 60% of animation
                        events[1] = new AnimationEvent();
                        events[1].functionName = "DisableHitbox";
                        events[1].time = duration * 0.6f;

                        // Event 3: FinishAttack at 95% of animation
                        events[2] = new AnimationEvent();
                        events[2].functionName = "FinishAttack";
                        events[2].time = duration * 0.95f;

                        clips[i].events = events;
                    }

                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                    anyModified = true;
                    Debug.Log("Successfully added events to: " + path);
                }
                else
                {
                    Debug.LogWarning("Could not find clip info for " + path);
                }
            }
            else
            {
                Debug.LogWarning("Could not find file at: " + path);
            }
        }

        if (anyModified)
        {
            EditorUtility.DisplayDialog("Success", "Demon Attack Events have been automatically set up!", "OK");
        }
    }
}
