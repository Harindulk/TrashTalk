using UnityEditor;
using UnityEngine;

// Builds the itch.io WebGL folder into ../Builds/WebGL. Size/compression settings live in Project Settings.
public static class BuildWebGL
{
    [MenuItem("Trash Talk/Build WebGL")]
    public static void Build()
    {
        var scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path);
        var report = BuildPipeline.BuildPlayer(scenes, "../Builds/WebGL", BuildTarget.WebGL, BuildOptions.None);
        Debug.Log("WebGL build: " + report.summary.result + " " + report.summary.totalSize / (1024 * 1024) + " MB");
    }
}
