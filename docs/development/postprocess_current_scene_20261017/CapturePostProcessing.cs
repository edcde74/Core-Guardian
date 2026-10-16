using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CapturePostProcessing
{
    public static object Main()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Stopped compiled Editor required");
        const string dir="Temp/postprocess_current_20261017/";Directory.CreateDirectory(dir);
        var source=Camera.main;var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var go=new GameObject("Postprocess study camera");go.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(go,scene);
        var cam=go.AddComponent<Camera>();cam.CopyFrom(source);cam.enabled=false;
        cam.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
        cam.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(source.gameObject.scene);
        var src=source.GetComponent<UniversalAdditionalCameraData>();var data=go.AddComponent<UniversalAdditionalCameraData>();
        data.SetRenderer(-1);data.antialiasing=AntialiasingMode.None;data.volumeLayerMask=1<<31;data.renderPostProcessing=false;
        var vg=new GameObject("Postprocess study volume");vg.hideFlags=HideFlags.HideAndDontSave;vg.layer=31;SceneManager.MoveGameObjectToScene(vg,scene);
        var volume=vg.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10000;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.hideFlags=HideFlags.HideAndDontSave;volume.sharedProfile=profile;
        var color=profile.Add<ColorAdjustments>(true);var split=profile.Add<SplitToning>(true);var bloom=profile.Add<Bloom>(true);var vig=profile.Add<Vignette>(true);
        var report=new System.Collections.Generic.List<object>();
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var prev=RenderTexture.active;Texture2D tex=null;
        try
        {
            cam.targetTexture=rt;
            foreach(string name in new[]{"original","color","gentle_glow","strong"})
            {
                bool on=name!="original";cam.allowHDR=on;data.renderPostProcessing=on;
                color.postExposure.value=0;color.contrast.value=name=="strong"?18:10;color.saturation.value=name=="strong"?10:6;
                split.shadows.value=new Color(.47f,.49f,.53f);split.highlights.value=new Color(.53f,.51f,.47f);split.balance.value=0;
                bloom.intensity.value=name=="gentle_glow"?.12f:name=="strong"?.5f:0;bloom.threshold.value=.9f;bloom.scatter.value=.35f;bloom.highQualityFiltering.value=true;
                vig.intensity.value=name=="strong"?.22f:0;vig.smoothness.value=.5f;vig.rounded.value=false;
                // First render updates the volume stack; second captures the settled state.
                VolumeManager.instance.Update(cam.transform,data.volumeLayerMask);cam.Render();cam.Render();RenderTexture.active=rt;
                tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
                File.WriteAllBytes(dir+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);tex=null;
                report.Add(new {name,hdr=cam.allowHDR,post=data.renderPostProcessing,contrast=color.contrast.value,saturation=color.saturation.value,bloom=bloom.intensity.value,threshold=bloom.threshold.value,vignette=vig.intensity.value});
            }
        }
        finally
        {
            RenderTexture.active=prev;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(vg);foreach(var c in profile.components)UnityEngine.Object.DestroyImmediate(c);UnityEngine.Object.DestroyImmediate(profile);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(source.gameObject.scene);
        }
        var result=new {source="Actual stopped scene via temporary camera and URP Volume, no runtime HUD or enemies",sourceCameraPost=src.renderPostProcessing,sourceCameraHDR=source.allowHDR,sourceSceneDirty=source.gameObject.scene.isDirty,report};
        File.WriteAllText(dir+"capture.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
