using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CaptureMapGrades
{
    const string Dir="docs/development/postprocess_map_options_20261018/";
    static void Capture(Camera camera,string name)
    {
        var previous=camera.targetTexture; var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        Texture2D tex=null;
        try {
            camera.targetTexture=rt;
            var d=camera.GetComponent<UniversalAdditionalCameraData>();
            VolumeManager.instance.Update(camera.transform,d.volumeLayerMask);
            camera.Render(); camera.Render(); RenderTexture.active=rt;
            tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,1920,1080),0,0); tex.Apply();
            File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());
        } finally {
            camera.targetTexture=previous; RenderTexture.active=active;
            RenderTexture.ReleaseTemporary(rt);
            if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);
        }
    }
    static void Set(VolumeProfile p,float contrast,float saturation,float exposure,Color shadows,Color highlights,float balance)
    {
        p.TryGet<ColorAdjustments>(out var c); p.TryGet<SplitToning>(out var s);
        c.contrast.Override(contrast);c.saturation.Override(saturation);c.postExposure.Override(exposure);
        s.shadows.Override(shadows);s.highlights.Override(highlights);s.balance.Override(balance);
    }
    public static object Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed||scene.isDirty||scene.path!="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity")
            throw new Exception("Stopped, compiled, saved combat scene required");
        var camera=Camera.main;
        var volume=UnityEngine.Object.FindObjectsByType<Volume>().Single(v=>v.isGlobal&&v.sharedProfile!=null);
        if(volume.HasInstantiatedProfile())throw new Exception("Unexpected instance profile; no preview performed");
        var original=volume.sharedProfile; var weight=volume.weight;
        var camJson=EditorJsonUtility.ToJson(camera);var volJson=EditorJsonUtility.ToJson(volume);
        var originalJson=EditorJsonUtility.ToJson(original);
        var componentsJson=original.components.Select(c=>EditorJsonUtility.ToJson(c)).ToArray();
        var preview=ScriptableObject.CreateInstance<VolumeProfile>();
        preview.hideFlags=HideFlags.HideAndDontSave;
        preview.components=original.components.Select(c=>UnityEngine.Object.Instantiate(c)).ToList();
        bool restored=false;
        try {
            Capture(camera,"00_current");
            volume.sharedProfile=preview;
            Set(preview,28,6,-.28f,new Color(.465f,.49f,.53f),new Color(.5f,.5f,.5f),-8);
            Capture(camera,"01_calm_stone");
            Set(preview,30,-5,-.40f,new Color(.43f,.48f,.57f),new Color(.49f,.50f,.52f),-15);
            Capture(camera,"02_cool_ruins");
            Set(preview,26,7,-.20f,new Color(.46f,.49f,.54f),new Color(.54f,.515f,.48f),5);
            Capture(camera,"03_warm_machinery");
        } finally {
            volume.sharedProfile=original;volume.weight=weight;
            VolumeManager.instance.Update(camera.transform,camera.GetComponent<UniversalAdditionalCameraData>().volumeLayerMask);
            foreach(var c in preview.components)UnityEngine.Object.DestroyImmediate(c);
            UnityEngine.Object.DestroyImmediate(preview);
            restored=EditorJsonUtility.ToJson(camera)==camJson&&EditorJsonUtility.ToJson(volume)==volJson&&EditorJsonUtility.ToJson(original)==originalJson&&original.components.Select(c=>EditorJsonUtility.ToJson(c)).SequenceEqual(componentsJson);
            if(restored&&scene.isDirty)UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        if(!restored)throw new Exception("Restore comparison failed; inspect before continuing");
        var report=new {previewOnly=true,restored,sceneDirty=scene.isDirty,playing=EditorApplication.isPlaying,hdr=camera.allowHDR,post=camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing,width=1920,height=1080,overlayIncluded=false,
            options=new[]{"00 current: 35/12/-0.45, shadows .45/.49/.55 highlights .5/.5/.5 balance -10","01 calm stone: 28/6/-0.28, shadows .465/.49/.53 highlights .5/.5/.5 balance -8","02 cool ruins: 30/-5/-0.40, shadows .43/.48/.57 highlights .49/.50/.52 balance -15","03 warm machinery: 26/7/-0.20, shadows .46/.49/.54 highlights .54/.515/.48 balance 5"}};
        File.WriteAllText(Dir+"capture.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
