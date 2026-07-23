using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class AutoFillAnimations : EditorWindow
{
    [MenuItem("Tools/Auto-Fill Missing Animations")]
    public static void FillMissingAnimations()
    {
        AnimatorController controller = Selection.activeObject as AnimatorController;

        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an Animator Controller in the Project window first.", "OK");
            return;
        }

        // Find all Animation Clips in the project
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
        Dictionary<string, AnimationClip> clipDict = new Dictionary<string, AnimationClip>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            if (clip != null && !path.StartsWith("Packages/")) // Ignore built-in packages
            {
                string clipName = clip.name.ToLower();
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();

                // Map by clip name
                if (!clipDict.ContainsKey(clipName)) 
                {
                    clipDict[clipName] = clip;
                }
                
                // Map by file name (super useful for Mixamo FBX files where clip is named "mixamo.com")
                if (!clipDict.ContainsKey(fileName)) 
                {
                    clipDict[fileName] = clip;
                }
            }
        }

        int filledCount = 0;

        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            filledCount += ProcessStateMachine(layer.stateMachine, clipDict);
        }

        if (filledCount > 0)
        {
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"Successfully auto-filled {filledCount} missing animations in '{controller.name}'!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "No missing animations were filled.\nEither all states already have animations, or no matching clip/file names were found in your project.", "OK");
        }
    }

    private static int ProcessStateMachine(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> clipDict)
    {
        int count = 0;

        // Process states in this state machine
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            if (childState.state.motion == null)
            {
                string stateName = childState.state.name.ToLower();
                
                // 1. Try exact match (State Name == Clip Name or File Name)
                if (clipDict.ContainsKey(stateName))
                {
                    childState.state.motion = clipDict[stateName];
                    count++;
                    Debug.Log($"[Auto-Fill] Assigned '{clipDict[stateName].name}' to State '{childState.state.name}' (Exact Match)");
                }
                else 
                {
                    // 2. Try fuzzy match (State Name contains Clip/File Name or vice versa)
                    foreach (var kvp in clipDict)
                    {
                        // Ignore generic names for fuzzy matching to avoid wrong assignments
                        if (kvp.Key == "mixamo.com" || kvp.Key.Contains("take 001")) continue;

                        if (kvp.Key.Contains(stateName) || stateName.Contains(kvp.Key))
                        {
                            childState.state.motion = kvp.Value;
                            count++;
                            Debug.Log($"[Auto-Fill] Assigned '{kvp.Value.name}' to State '{childState.state.name}' (Fuzzy Match)");
                            break;
                        }
                    }
                }
            }
        }

        // Process sub-state machines recursively
        foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
        {
            count += ProcessStateMachine(childStateMachine.stateMachine, clipDict);
        }

        return count;
    }
}
