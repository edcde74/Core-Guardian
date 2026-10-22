using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class CGB3MaskPixels
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Field(object owner,string name)=>owner.GetType().GetField(name,Hidden).GetValue(owner);
    public static object Run()
    {
        if(!EditorApplication.isPlaying||UnityEngine.Profiling.Profiler.enabled||FrameDebugger.enabled)throw new Exception("Own idle Play required");
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var camera=Camera.main;var oldZoom=camera.orthographicSize;var oldGame=game.enabled;float oldScale=Time.timeScale;
        var rows=new List<object>();var slots=new List<CoreGuardianMaskPage.Slot>();
        try
        {
            game.enabled=false;Time.timeScale=0;
            foreach(float zoom in new[]{8.5f,4.25f})
            {
                camera.orthographicSize=zoom;
                foreach(var shadow in UnityEngine.Object.FindObjectsByType<CoreGuardianPhotoShadow>())shadow.Present(camera);
                var emphasis=game.GetComponent<CoreGuardianObjectEmphasis>();if(emphasis!=null)emphasis.GetType().GetMethod("LateUpdate",Hidden).Invoke(emphasis,null);
                var seen=new HashSet<object>();var records=new List<(RenderTexture texture,CoreGuardianMaskPage.Slot slot,string name)>();slots.Clear();
                foreach(var owner in Resources.FindObjectsOfTypeAll<CoreGuardianSelectionOutline>())
                {
                    if(!owner.IsVisible)continue;var binding=Field(owner,"maskBinding");var record=binding==null?null:Field(binding,"current");if(record==null||!seen.Add(record))continue;
                    var request=Field(record,"request");if((Camera)Field(request,"camera")!=camera)continue;
                    var source=(RenderTexture)Field(record,"texture");var rect=(Rect)Field(request,"rect");var viewport=camera.pixelRect;
                    if(!CoreGuardianMaskPage.TryAcquire(camera,Time.frameCount,source.width,source.height,out var slot))throw new Exception("Page allocation refused");slots.Add(slot);
                    var a=(rect.min-viewport.position)/viewport.size*2-Vector2.one;var b=(rect.max-viewport.position)/viewport.size*2-Vector2.one;
                    var crop=Matrix4x4.identity;crop.m00=2/(b.x-a.x);crop.m11=2/(b.y-a.y);crop.m03=-(b.x+a.x)/(b.x-a.x);crop.m13=-(b.y+a.y)/(b.y-a.y);
                    var vp=GL.GetGPUProjectionMatrix(crop*camera.projectionMatrix,true)*camera.worldToCameraMatrix;
                    foreach(var part in (List<SpriteRenderer>)Field(request,"sources"))
                    {
                        var shape=owner.GetType().GetMethod("Geometry",Hidden).Invoke(owner,new object[]{part.sprite});var texture=(Texture)shape.GetType().GetField("texture").GetValue(shape);
                        var mesh=(Mesh)owner.GetType().GetMethod("SpriteMesh",Hidden).Invoke(owner,new object[]{part.sprite});var matrix=part.transform.localToWorldMatrix*Matrix4x4.Scale(new Vector3(part.flipX?-1:1,part.flipY?-1:1,1));
                        if(!CoreGuardianMaskPage.Queue(slot,mesh,(Material)Field(owner,"maskMaterial"),matrix,vp,texture,part.color.a,(float)Field(request,"threshold"),rect.size))throw new Exception("Producer rejected original source");
                    }
                    records.Add((source,slot,owner.CurrentTarget==null?owner.name:owner.CurrentTarget.name));
                }
                CoreGuardianMaskPage.Flush(camera);int changed=0,failed=0;var failures=new List<object>();
                foreach(var item in records)
                {
                    var rect=item.slot.Rect;var copy=new RenderTexture(rect.width,rect.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);copy.Create();
                    try{Graphics.CopyTexture(item.slot.Texture,0,0,rect.x,rect.y,rect.width,rect.height,copy,0,0,0,0);var expected=Pixels(item.texture);var actual=Pixels(copy);int difference=expected.Zip(actual,(a,b)=>a.Equals(b)?0:1).Sum();changed+=difference;if(difference!=0){failed++;failures.Add(new{item.name,width=rect.width,height=rect.height,difference});}}
                    finally{copy.Release();UnityEngine.Object.Destroy(copy);}
                }
                rows.Add(new{zoom,masks=records.Count,changed,failed,draws=CoreGuardianMaskPage.LastProducerDraws,pages=CoreGuardianMaskPage.PageCount,pageBytes=CoreGuardianMaskPage.RuntimeBytes,failures});
                foreach(var slot in slots)CoreGuardianMaskPage.Release(slot);slots.Clear();
            }
            File.WriteAllText("docs/development/render_batching_20261022/b3_actual_masks.json",JsonConvert.SerializeObject(rows,Formatting.Indented));return rows;
        }
        finally{foreach(var slot in slots)CoreGuardianMaskPage.Release(slot);camera.orthographicSize=oldZoom;game.enabled=oldGame;Time.timeScale=oldScale;}
    }
    static Color32[] Pixels(RenderTexture texture)
    {
        var old=RenderTexture.active;var read=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=texture;read.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);read.Apply();return read.GetPixels32();}
        finally{RenderTexture.active=old;UnityEngine.Object.Destroy(read);}
    }
}
