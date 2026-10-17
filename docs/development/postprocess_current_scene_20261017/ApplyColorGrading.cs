using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Editor-only configuration tool; compiled in memory, never added to the runtime.
public static class ApplyColorGrading
{
    const string ScenePath = "Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity";
    const string ProfilePath = "Assets/Settings/CoreGuardianColorGrading.asset";
    public static object Main()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed || scene.isDirty || scene.path != ScenePath)
            throw new Exception("Stopped, compiled, clean combat scene required.");
        var camera = Camera.main;
        var data = camera.GetComponent<UniversalAdditionalCameraData>();
        if (UniversalRenderPipeline.asset == null || data == null || camera.allowHDR || data.renderPostProcessing)
            throw new Exception("Unexpected pipeline/camera baseline.");
        if (UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include).Length != 0 || AssetDatabase.LoadMainAssetAtPath(ProfilePath) != null)
            throw new Exception("Existing Volume/profile requires review; will not overwrite.");

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "CoreGuardianColorGrading";
        AssetDatabase.CreateAsset(profile, ProfilePath);
        var color = profile.Add<ColorAdjustments>();
        color.name = "ColorAdjustments";
        color.contrast.Override(10f);
        color.saturation.Override(6f);
        var split = profile.Add<SplitToning>();
        split.name = "SplitToning";
        split.shadows.Override(new Color(.47f, .49f, .53f, 1));
        split.highlights.Override(new Color(.53f, .51f, .47f, 1));
        split.balance.Override(0f);
        foreach (var component in profile.components)
        {
            component.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(component);
        }
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);

        var go = new GameObject("Color Grading");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Apply subtle color grading");
        go.layer = 0;
        var volume = Undo.AddComponent<Volume>(go);
        volume.isGlobal = true;
        volume.priority = 0;
        volume.weight = 1;
        volume.sharedProfile = profile;
        Undo.RecordObject(data, "Enable approved color grading");
        data.renderPostProcessing = true;
        data.volumeLayerMask |= 1;
        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed.");
        return Inspect();
    }

    public static object Inspect()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var camera = Camera.main;
        var data = camera.GetComponent<UniversalAdditionalCameraData>();
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null || !profile.TryGet<ColorAdjustments>(out var color) || !profile.TryGet<SplitToning>(out var split))
            throw new Exception("Saved color grading profile missing.");
        var result = new { scene = scene.path, dirty = scene.isDirty, profile = ProfilePath,
            components = profile.components.Select(c => c.GetType().Name).ToArray(),
            contrast = color.contrast.value, saturation = color.saturation.value,
            shadows = new[] { split.shadows.value.r, split.shadows.value.g, split.shadows.value.b },
            highlights = new[] { split.highlights.value.r, split.highlights.value.g, split.highlights.value.b },
            hdr = camera.allowHDR, post = data.renderPostProcessing, volumeMask = data.volumeLayerMask.value,
            note = "Only ColorAdjustments and SplitToning. No Bloom, HDR change, tonemapping, blur, vignette or antialiasing change." };
        File.WriteAllText("docs/development/postprocess_current_scene_20261017/applied_setup.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
