using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Unity.Profiling;

public static class CGRouteMeshProbe
{
    const string Folder="docs/development/route_mesh_20261022";
    static Color32[] Capture(Camera camera,string file)
    {
        var old=camera.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);Texture2D image=null;
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            image=new Texture2D(1920,1080,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            var pixels=image.GetPixels32();File.WriteAllBytes(Folder+"/"+file+".png",image.EncodeToPNG());return pixels;
        }
        finally{camera.targetTexture=old;RenderTexture.active=active;if(image!=null)UnityEngine.Object.DestroyImmediate(image);RenderTexture.ReleaseTemporary(rt);}
    }
    static object Difference(Color32[] a,Color32[] b)
    {
        int pixels=0,max=0;long sum=0;
        for(int i=0;i<a.Length;i++){int d=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)+Math.Abs(a[i].a-b[i].a);if(d>0)pixels++;sum+=d;max=Math.Max(max,d);}
        return new{differentPixels=pixels,maxChannelSum=max,totalChannelDelta=sum};
    }
    static object Measure(Camera camera)
    {
        var target=camera.targetTexture;var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);
        var rows=new List<object>();var marker=new ProfilerMarker(ProfilerCategory.Scripts,"CG.Route.CameraRender");
        try
        {
            camera.targetTexture=rt;
            for(int run=0;run<3;run++)
            {
                for(int i=0;i<10;i++)camera.Render();
                using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"CG.Route.CameraRender",128,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.CollectOnlyOnCurrentThread|ProfilerRecorderOptions.WrapAroundWhenCapacityReached))
                {
                    var samples=new List<object>();
                    for(int i=0;i<60;i++)
                    {
                        using(marker.Auto())camera.Render();
                        samples.Add(new{draw=UnityEditor.UnityStats.drawCalls,setpass=UnityEditor.UnityStats.setPassCalls,triangles=UnityEditor.UnityStats.triangles});
                    }
                    rows.Add(new{run,valid=recorder.Valid,unit=recorder.UnitType.ToString(),profilerSamples=recorder.ToArray().Select(s=>new{value=s.Value,count=s.Count}).ToArray(),samples});
                }
            }
        }
        finally{camera.targetTexture=target;RenderTexture.ReleaseTemporary(rt);}
        return rows;
    }
    public static object VerifyApplied()
    {
        var camera=Camera.main;float oldZoom=camera.orthographicSize;var rows=new List<object>();
        try
        {
            foreach(var zoom in new[]{8.5f,4.25f})
            {
                camera.orthographicSize=zoom;string key=zoom.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_');
                var before=new Texture2D(2,2);
                try{ImageConversion.LoadImage(before,File.ReadAllBytes(Folder+"/before_"+key+".png"));rows.Add(new{zoom,difference=Difference(before.GetPixels32(),Capture(camera,"applied_"+key))});}
                finally{UnityEngine.Object.DestroyImmediate(before);}
            }
        }
        finally{camera.orthographicSize=oldZoom;}
        File.WriteAllText(Folder+"/applied_pixels.json",JsonConvert.SerializeObject(rows,Formatting.Indented));return rows;
    }
    public static object BakeAndVerify()
    {
        var camera=Camera.main;float oldZoom=camera.orthographicSize;
        var before=new Dictionary<float,Color32[]>();var rows=new List<object>();
        try
        {
            foreach(var zoom in new[]{8.5f,4.25f}){camera.orthographicSize=zoom;before[zoom]=Capture(camera,"shipping_before_"+zoom.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_'));}
            camera.orthographicSize=oldZoom;
            if(!EditorApplication.ExecuteMenuItem("Tools/Core Guardian/Route Mesh/Combine Road Tiles"))throw new Exception("Bake menu did not execute");
            foreach(var zoom in new[]{8.5f,4.25f}){camera.orthographicSize=zoom;rows.Add(new{zoom,difference=Difference(before[zoom],Capture(camera,"shipping_after_"+zoom.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_')))});}
        }
        finally{camera.orthographicSize=oldZoom;}
        File.WriteAllText(Folder+"/shipping_pixels.json",JsonConvert.SerializeObject(rows,Formatting.Indented));return rows;
    }
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.Profiling.Profiler.enabled)throw new Exception("Idle Editor required");
        var root=GameObject.Find("CoreGuardian_MapArt_B/CoreGuardian_Route01_VisualOnly");var camera=Camera.main;
        var builder=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianRouteMeshBuilder")).First(t=>t!=null);
        var combine=builder.GetMethod("CombineRoad");var restore=builder.GetMethod("Restore");
        var originalZoom=camera.orthographicSize;var originalDirty=root.scene.isDirty;
        var rows=new List<object>();
        try
        {
            foreach(var zoom in new[]{8.5f,4.25f})
            {
                camera.orthographicSize=zoom;string key=zoom.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_');
                var before=Capture(camera,"before_"+key);var control=Capture(camera,"control_"+key);var beforeMeasure=zoom==8.5f?Measure(camera):null;
                var outputs=(MeshRenderer[])combine.Invoke(null,new object[]{root});
                var after=Capture(camera,"after_"+key);var afterMeasure=zoom==8.5f?Measure(camera):null;
                rows.Add(new{zoom,beforeControl=Difference(before,control),beforeAfter=Difference(before,after),beforeMeasure,afterMeasure,outputs=outputs.Select(r=>new{name=r.name,order=r.sortingOrder,vertices=r.GetComponent<MeshFilter>().sharedMesh.vertexCount,triangles=r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3}).ToArray()});
                restore.Invoke(null,new object[]{root});
            }
        }
        finally{restore.Invoke(null,new object[]{root});camera.orthographicSize=originalZoom;}
        var result=new{kind="Stopped Editor Camera.Render pixel comparison; Overlay HUD excluded",originalDirty,finalDirty=root.scene.isDirty,originalZoom,finalZoom=camera.orthographicSize,rows};
        File.WriteAllText(Folder+"/pixel_comparison.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
}
