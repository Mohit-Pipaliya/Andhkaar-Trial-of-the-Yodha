using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Builder
{
    private static readonly string[] Scenes = new string[]
    {
        "Assets/Scenes/Level 1.unity",
        "Assets/Scenes/Level 2.unity",
        "Assets/Scenes/Level 3.unity"
    };

    public static void BuildMac()
    {
        string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Mac");
        Directory.CreateDirectory(buildDir);
        string outputPath = Path.Combine(buildDir, "Andhkaar - Trial of the Yodha.app");

        Build(outputPath, BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone);
    }

    public static void BuildWindows()
    {
        string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Windows");
        Directory.CreateDirectory(buildDir);
        string outputPath = Path.Combine(buildDir, "Andhkaar - Trial of the Yodha.exe");

        Build(outputPath, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone);
    }

    private static void Build(string outputPath, BuildTarget target, BuildTargetGroup group)
    {
        Debug.Log($"[Builder] Starting build for {target} -> {outputPath}...");

        // Ensure scenes exist
        foreach (var scene in Scenes)
        {
            if (!File.Exists(scene))
            {
                Debug.LogError($"[Builder] Scene file not found: {scene}");
                EditorApplication.Exit(1);
                return;
            }
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outputPath,
            target = target,
            targetGroup = group,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"<color=green>[Builder] Build SUCCEEDED!</color>");
            Debug.Log($"[Builder] Output: {summary.outputPath}");
            Debug.Log($"[Builder] Size: {summary.totalSize / (1024 * 1024)} MB");
            Debug.Log($"[Builder] Time: {summary.totalTime.TotalSeconds:F1} seconds");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"<color=red>[Builder] Build FAILED with result: {summary.result}</color>");
            Debug.LogError($"[Builder] Errors: {summary.totalErrors}");
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                    {
                        Debug.LogError($"[Builder Step Error] {msg.content}");
                    }
                }
            }
            EditorApplication.Exit(1);
        }
    }
}
