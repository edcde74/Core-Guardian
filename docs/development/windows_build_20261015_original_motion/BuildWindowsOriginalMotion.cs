using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildWindowsOriginalMotion
{
    const string ScenePath = "Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity";
    const string Output = "Builds/Windows_20261015_OriginalMotion/CoreGuardian.exe";
    public static object Preflight()
    {
        return JsonConvert.SerializeObject(new {
            playing = EditorApplication.isPlaying,
            compiling = EditorApplication.isCompiling,
            scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i => {
                var s = SceneManager.GetSceneAt(i);
                return new { s.path, s.isDirty, s.isLoaded };
            }).ToArray(),
            activeTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
            windowsSupported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64),
            sceneExists = File.Exists(ScenePath),
            outputExists = File.Exists(Output)
        });
    }
    public static object Build()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            throw new Exception("Editor must be stopped and compilation complete.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: " + SceneManager.GetSceneAt(i).path);
        if (Directory.Exists(Path.GetDirectoryName(Output))) throw new Exception("Output folder already exists; preserve previous builds.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new Exception("Windows build support unavailable.");
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { ScenePath }, locationPathName = Output,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
        });
        var result = new {
            success = report.summary.result == BuildResult.Succeeded,
            result = report.summary.result.ToString(),
            output = report.summary.outputPath,
            scenes = new[] { ScenePath },
            errors = report.summary.totalErrors,
            warnings = report.summary.totalWarnings,
            bytes = report.summary.totalSize,
            seconds = report.summary.totalTime.TotalSeconds,
            messages = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception || m.type == LogType.Warning).Select(m => new { type = m.type.ToString(), m.content }).ToArray()
        };
        var json = JsonConvert.SerializeObject(result, Formatting.Indented);
        File.WriteAllText("docs/development/windows_build_20261015_original_motion/build_report.json", json);
        if (!result.success) throw new Exception(json);
        return json;
    }
}
