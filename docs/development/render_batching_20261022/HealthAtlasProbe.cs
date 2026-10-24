using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class CGHealthAtlasProbe
{
    public static object Run()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSpriteAtlas")).First(t=>t!=null);
        var root=new GameObject("Temporary health atlas pixels");root.layer=31;root.transform.position=new Vector3(10000,10000,0);
        var health=root.AddComponent<CoreGuardian.SegmentedEnemyHealthBar>();health.Configure(false);var renderer=root.GetComponent<SpriteRenderer>();
        var material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));renderer.sharedMaterial=material;
        var cameraRoot=new GameObject("Temporary health atlas camera");var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1;camera.transform.position=new Vector3(10000,10000,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;var read=new Texture2D(128,128,TextureFormat.RGBA32,false);var old=RenderTexture.active;
        var rows=new List<object>();
        try
        {
            for(int filled=0;filled<=8;filled++)
            {
                health.SetHealth(filled,8);var source=renderer.sprite;var args=new object[]{source,null};type.GetMethod("TryMap").Invoke(null,args);var mapped=(Sprite)args[1];
                Color32[] Capture(Sprite sprite,string label){renderer.sprite=sprite;camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();File.WriteAllBytes("docs/development/render_batching_20261022/health_"+filled+"_"+label+".png",read.EncodeToPNG());return read.GetPixels32();}
                var before=Capture(source,"before");var after=Capture(mapped,"after");int differences=before.Where((p,i)=>!p.Equals(after[i])).Count();
                rows.Add(new{filled,source=source.name,rect=mapped.rect.ToString(),differences,sourceVertices=source.vertices.Select(v=>v.ToString("R")).ToArray(),mappedVertices=mapped.vertices.Select(v=>v.ToString("R")).ToArray(),sourceUV=source.uv.Select(v=>v.ToString("R")).ToArray(),mappedUV=mapped.uv.Select(v=>v.ToString("R")).ToArray()});renderer.sprite=source;
            }
            File.WriteAllText("docs/development/render_batching_20261022/health_atlas_diagnostic.json",JsonConvert.SerializeObject(rows,Formatting.Indented));return rows;
        }
        finally{renderer.sprite=null;type.GetMethod("Release").Invoke(null,null);RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(read);UnityEngine.Object.DestroyImmediate(material);}
    }
}
