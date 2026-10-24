using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class CGSpritePixelProbe
{
    public static object Run()
    {
        if(EditorApplication.isPlaying||UnityEngine.Profiling.Profiler.enabled)throw new Exception("Idle EditMode required");
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSpriteBatching")).First(t=>t!=null);
        var root=new GameObject("Temporary sprite shader pixel comparison"){hideFlags=HideFlags.HideAndDontSave};
        var cameraRoot=new GameObject("Temporary sprite comparison camera"){hideFlags=HideFlags.HideAndDontSave};
        var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1;
        camera.transform.position=new Vector3(10000,10000,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
        var spriteRenderer=root.AddComponent<SpriteRenderer>();root.layer=31;root.transform.position=new Vector3(10000,10000,0);
        var material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));material.SetColor("_Color",new Color(.83f,.8f,.71f,.7f));spriteRenderer.sharedMaterial=material;
        var read=new Texture2D(128,128,TextureFormat.RGBA32,false);var old=RenderTexture.active;
        var rows=new List<object>();int differences=0;IDisposable lease=null;
        try
        {
            // Keep a real lease alive while switching the renderer between the two shared materials.
            lease=(IDisposable)type.GetMethod("Apply").Invoke(null,new object[]{root.transform});var mapped=spriteRenderer.sharedMaterial;
            string folder="Assets/Art/ScreenTests/20260930/Revision2/Sprites/";
            foreach(var name in new[]{"enemy_attack_v2","enemy_defense_v2","enemy_buff_v2"})
            foreach(bool flip in new[]{false,true})foreach(float xScale in new[]{.6f,-.6f})
            {
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(folder+name+".png");if(sprite==null)throw new Exception("Missing reference "+name);
                spriteRenderer.sprite=sprite;spriteRenderer.flipX=flip;root.transform.localScale=new Vector3(xScale,.6f,1);
                spriteRenderer.color=new Color(.91f,.87f,.8f,.9f);
                Color32[] Capture(Material m){spriteRenderer.sharedMaterial=m;camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();return read.GetPixels32();}
                var before=Capture(material);var after=Capture(mapped);int changed=before.Where((p,i)=>!p.Equals(after[i])).Count();
                int visible=before.Count(p=>p.a>0);if(visible==0)throw new Exception("Blank reference frame");
                differences+=changed;rows.Add(new{name,flip,xScale,visible,changed});
            }
            var result=new{cases=rows.Count,differences,rows};File.WriteAllText("docs/development/render_batching_20261022/a1_sprite_pixels.json",JsonConvert.SerializeObject(result,Formatting.Indented));return new{cases=rows.Count,differences};
        }
        finally
        {
            lease?.Dispose();RenderTexture.active=old;camera.targetTexture=null;
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);
            target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(read);UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
