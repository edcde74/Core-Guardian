using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class CGSceneLegacyMaterialPixels
{
    public static object Run()
    {
        if(!EditorApplication.isPlaying||UnityEngine.Profiling.Profiler.enabled||FrameDebugger.enabled)throw new Exception("Own idle Play required");
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var camera=Camera.main;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSpriteBatching")).First(t=>t!=null);
        var map=(Dictionary<Material,Material>)type.GetField("mapped",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var reverse=map.ToDictionary(p=>p.Value,p=>p.Key);
        var affected=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.sharedMaterial!=null&&reverse.ContainsKey(r.sharedMaterial)).Select(r=>new{renderer=r,mapped=r.sharedMaterial,original=reverse[r.sharedMaterial]}).ToArray();
        var oldTarget=camera.targetTexture;var oldSize=camera.orthographicSize;var oldActive=RenderTexture.active;var oldScale=Time.timeScale;bool oldGame=game.enabled;
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
        var read=new Texture2D(1920,1080,TextureFormat.RGBA32,false);var rows=new List<object>();
        try
        {
            game.enabled=false;Time.timeScale=0;
            foreach(float zoom in new[]{8.5f,4.25f})
            {
                camera.orthographicSize=zoom;
                foreach(var shadow in UnityEngine.Object.FindObjectsByType<CoreGuardianPhotoShadow>())shadow.Present(camera);
                var emphasis=game.GetComponent<CoreGuardianObjectEmphasis>();if(emphasis!=null)emphasis.GetType().GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(emphasis,null);
                Color32[] Capture(string suffix)
                {camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,1920,1080),0,0);read.Apply();File.WriteAllBytes("docs/development/render_batching_20261022/a1_legacy_full_scene_"+zoom.ToString(System.Globalization.CultureInfo.InvariantCulture)+"_"+suffix+".png",read.EncodeToPNG());return read.GetPixels32();}
                foreach(var r in affected)r.renderer.sharedMaterial=r.original;
                var before=Capture("before");
                foreach(var r in affected)r.renderer.sharedMaterial=r.mapped;
                var after=Capture("after");int changed=0,max=0;
                for(int i=0;i<before.Length;i++)if(!before[i].Equals(after[i])){changed++;max=Mathf.Max(max,Mathf.Abs(before[i].r-after[i].r),Mathf.Abs(before[i].g-after[i].g),Mathf.Abs(before[i].b-after[i].b),Mathf.Abs(before[i].a-after[i].a));}
                rows.Add(new{zoom,changed,maxChannelDifference=max,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count,kind="same-pose full camera render, overlay HUD excluded"});
            }
            var result=new{rows,affected=affected.Length};File.WriteAllText("docs/development/render_batching_20261022/a1_legacy_full_scene_pixels.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
        }
        finally
        {
            foreach(var r in affected)if(r.renderer!=null)r.renderer.sharedMaterial=r.mapped;
            camera.targetTexture=oldTarget;camera.orthographicSize=oldSize;Time.timeScale=oldScale;game.enabled=oldGame;RenderTexture.active=oldActive;
            target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(read);
        }
    }
}
