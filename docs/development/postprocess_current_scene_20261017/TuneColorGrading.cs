using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// In-memory Editor tool. Main only previews and restores; Apply saves the chosen revision.
public static class TuneColorGrading
{
    const string Dir = "docs/development/postprocess_current_scene_20261017/";
    const string ProfilePath = "Assets/Settings/CoreGuardianColorGrading.asset";
    static VolumeProfile Profile()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed || scene.isDirty || scene.path != "Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity")
            throw new Exception("Stopped, compiled, saved combat scene required.");
        return AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
    }
    static void Set(VolumeProfile p, float contrast, float saturation, float exposure)
    {
        p.TryGet<ColorAdjustments>(out var c); p.TryGet<SplitToning>(out var s);
        c.contrast.Override(contrast); c.saturation.Override(saturation); c.postExposure.Override(exposure);
        s.shadows.Override(new Color(.45f,.49f,.55f,1));
        s.highlights.Override(new Color(.50f,.50f,.50f,1));
        s.balance.Override(-10);
    }
    static void Capture(string name)
    {
        var camera = Camera.main; var d = camera.GetComponent<UniversalAdditionalCameraData>();
        var previous = camera.targetTexture; var active = RenderTexture.active;
        var rt = RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        Texture2D tex = null;
        try
        {
            camera.targetTexture = rt;
            VolumeManager.instance.Update(camera.transform,d.volumeLayerMask);
            camera.Render();camera.Render();RenderTexture.active=rt;
            tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
            File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());
        }
        finally {camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);}
    }
    public static object Main()
    {
        var original = Profile();
        var volume = UnityEngine.Object.FindAnyObjectByType<Volume>();
        var preview = UnityEngine.Object.Instantiate(original);
        preview.components = original.components.Select(c => UnityEngine.Object.Instantiate(c)).ToList();
        try
        {
            volume.sharedProfile = preview;
            Capture("revision2_previous");
            Set(preview,35,12,-.45f);Capture("revision2_clear");
            Set(preview,45,10,-.65f);Capture("revision2_crisp");
        }
        finally
        {
            volume.sharedProfile=original;
            foreach(var c in preview.components)UnityEngine.Object.DestroyImmediate(c);
            UnityEngine.Object.DestroyImmediate(preview);
        }
        // Temporary capture/property assignments can dirty the scene; all references restored first.
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return new {previewOnly=true,restored=true,files=new[]{"revision2_previous.png","revision2_clear.png","revision2_crisp.png"}};
    }
    public static object Apply()
    {
        var p=Profile();
        Undo.RecordObjects(p.components.ToArray(),"Strengthen approved color grading");
        Set(p,35,12,-.45f);
        foreach(var c in p.components)EditorUtility.SetDirty(c);
        EditorUtility.SetDirty(p);AssetDatabase.SaveAssetIfDirty(p);
        var result=new {contrast=35,saturation=12,exposure=-.45f,shadows=new[]{.45f,.49f,.55f},highlights=new[]{.50f,.50f,.50f},balance=-10,hdr=Camera.main.allowHDR,post=Camera.main.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing};
        File.WriteAllText(Dir+"revision2_setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
