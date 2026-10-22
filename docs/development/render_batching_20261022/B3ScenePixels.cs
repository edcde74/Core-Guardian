using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Newtonsoft.Json;
public static class CGB3ScenePixels
{
    public static object Run()
    {
        if(!EditorApplication.isPlaying||UnityEngine.Profiling.Profiler.enabled||FrameDebugger.enabled)throw new Exception("Own idle Play required");
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var camera=Camera.main;
        var begin=(Action<ScriptableRenderContext,Camera>)Delegate.CreateDelegate(typeof(Action<ScriptableRenderContext,Camera>),typeof(CoreGuardianMaskOutputBatch).GetMethod("Begin",BindingFlags.Static|BindingFlags.NonPublic));
        var oldTarget=camera.targetTexture;var oldSize=camera.orthographicSize;var oldActive=RenderTexture.active;var oldScale=Time.timeScale;bool oldGame=game.enabled;
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
        var read=new Texture2D(1920,1080,TextureFormat.RGBA32,false);var rows=new List<object>();bool subscribed=true;
        try
        {
            game.enabled=false;Time.timeScale=0;
            foreach(float zoom in new[]{8.5f,4.25f})
            {
                camera.orthographicSize=zoom;
                foreach(var shadow in UnityEngine.Object.FindObjectsByType<CoreGuardianPhotoShadow>())shadow.Present(camera);
                var emphasis=game.GetComponent<CoreGuardianObjectEmphasis>();if(emphasis!=null)emphasis.GetType().GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(emphasis,null);
                var cache=typeof(CoreGuardianSelectionOutline).Assembly.GetType("CoreGuardianBodyMaskCache");cache.GetMethod("Flush",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{camera});
                // Rebuild genuine legacy pixels independently; baseline must not read a mask made by B3.
                var seen=new HashSet<object>();const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
                foreach(var owner in Resources.FindObjectsOfTypeAll<CoreGuardianSelectionOutline>())
                {
                    if(!owner.IsVisible)continue;var binding=owner.GetType().GetField("maskBinding",hidden).GetValue(owner);if(binding==null)continue;
                    var record=binding.GetType().GetField("current",hidden).GetValue(binding);if(record==null||!seen.Add(record))continue;
                    var request=record.GetType().GetField("request",hidden).GetValue(record);if((Camera)request.GetType().GetField("camera",hidden).GetValue(request)!=camera)continue;
                    var rect=(Rect)request.GetType().GetField("rect",hidden).GetValue(request);var viewport=camera.pixelRect;
                    var a=(rect.min-viewport.position)/viewport.size*2-Vector2.one;var b=(rect.max-viewport.position)/viewport.size*2-Vector2.one;
                    var crop=Matrix4x4.identity;crop.m00=2/(b.x-a.x);crop.m11=2/(b.y-a.y);crop.m03=-(b.x+a.x)/(b.x-a.x);crop.m13=-(b.y+a.y)/(b.y-a.y);
                    var vp=GL.GetGPUProjectionMatrix(crop*camera.projectionMatrix,true)*camera.worldToCameraMatrix;
                    var mask=(RenderTexture)record.GetType().GetField("texture",hidden).GetValue(record);
                    var commands=(CommandBuffer)owner.GetType().GetField("commands",hidden).GetValue(owner);
                    commands.Clear();commands.SetRenderTarget(mask);commands.ClearRenderTarget(false,true,Color.clear);commands.SetViewport(new Rect(0,0,rect.width,rect.height));
                    owner.GetType().GetMethod("DrawMaskParts",hidden).Invoke(owner,new object[]{vp});Graphics.ExecuteCommandBuffer(commands);
                }
                Color32[] Capture(string suffix)
                {camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,1920,1080),0,0);read.Apply();File.WriteAllBytes("docs/development/render_batching_20261022/b3_scene_"+zoom.ToString(System.Globalization.CultureInfo.InvariantCulture)+"_"+suffix+".png",read.EncodeToPNG());return read.GetPixels32();}
                RenderPipelineManager.beginCameraRendering-=begin;subscribed=false;CoreGuardianMaskOutputBatch.Restore(camera);
                Capture("warmup");var control=Capture("control");var before=Capture("before");
                int preparedMasks=0;var copies=new CommandBuffer();
                foreach(var record in seen)
                {
                    var slot=(CoreGuardianMaskPage.Slot)record.GetType().GetField("page",hidden).GetValue(record);if(!CoreGuardianMaskPage.IsValid(slot))continue;
                    var texture=(RenderTexture)record.GetType().GetField("texture",hidden).GetValue(record);var rect=slot.Rect;
                    copies.CopyTexture(slot.Texture,0,0,rect.x,rect.y,rect.width,rect.height,texture,0,0,0,0);preparedMasks++;
                }
                Graphics.ExecuteCommandBuffer(copies);copies.Dispose();
                RenderPipelineManager.beginCameraRendering+=begin;subscribed=true;
                var after=Capture("after");int changed=0,max=0;
                for(int i=0;i<before.Length;i++)if(!before[i].Equals(after[i])){changed++;max=Mathf.Max(max,Mathf.Abs(before[i].r-after[i].r),Mathf.Abs(before[i].g-after[i].g),Mathf.Abs(before[i].b-after[i].b),Mathf.Abs(before[i].a-after[i].a));}
                rows.Add(new{zoom,changed,baselineRepeatChanged=control.Zip(before,(a,b)=>!a.Equals(b)).Count(x=>x),maxChannelDifference=max,preparedMasks,batched=CoreGuardianMaskOutputBatch.LastBatchedOutputs,draws=CoreGuardianMaskOutputBatch.LastDraws,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count,hudOverlayIncluded=false});
            }
            File.WriteAllText("docs/development/render_batching_20261022/b3_scene_pixels.json",JsonConvert.SerializeObject(rows,Formatting.Indented));return rows;
        }
        finally
        {
            if(!subscribed)RenderPipelineManager.beginCameraRendering+=begin;CoreGuardianMaskOutputBatch.Restore(camera);
            camera.targetTexture=oldTarget;camera.orthographicSize=oldSize;Time.timeScale=oldScale;game.enabled=oldGame;RenderTexture.active=oldActive;
            target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(read);
        }
    }
}
