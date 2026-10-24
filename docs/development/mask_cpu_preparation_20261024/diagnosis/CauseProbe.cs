using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;

// Fixed 512 cases; one isolated preview-scene case per editor update, no Play or user-scene edits.
public static class CGMaskCauseProbe
{
    const string Root="docs/development/mask_cpu_preparation_20261024/diagnosis/";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly string[] roles={"Attack","Defense","Buff","Boss","Player","Single","Multi","Support"};
    static readonly string[] files={"enemy_attack","enemy_defense","enemy_buff",null,"Player","tower_single","tower_multi","tower_support"};
    static readonly Vector2[] directions={Vector2.right,Vector2.left,Vector2.up,Vector2.down};
    static string label;static int index;static bool running;static double deadline;static List<object> rows;
    public static object Baseline()=>Start("original");
    public static object Candidate()=>Start("candidate");
    static object Start(string name)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Idle clean editor required");
        label=name;index=0;rows=new List<object>(4);running=true;deadline=EditorApplication.timeSinceStartup+30;EditorApplication.update+=Poll;
        return new{started=true,cases=4,policy="case320 and321 each twice; one case/update; 30s safety bound"};
    }
    static void Poll()
    {
        if(!running)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Pixel QA safety bound exceeded");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("External editor state change");
            int caseId=(index%2==0?320:321);index++;rows.Add(Capture(caseId));
            if(index==4)Finish(null);
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static object Capture(int id)
    {
        int overlap=id%2,zoom=(id/2)%2,phase=(id/4)%4,direction=(id/16)%4,role=id/64;
        var scene=EditorSceneManager.NewPreviewScene();var owned=new List<GameObject>();var outlines=new List<CoreGuardianSelectionOutline>();
        RenderTexture target=null;Camera camera=null;
        try
        {
            GameObject Make(string n){var go=new GameObject(n){hideFlags=HideFlags.HideAndDontSave};UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);owned.Add(go);return go;}
            camera=Make("Mask probe camera").AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.orthographicSize=zoom==0?8.5f:4.25f;camera.aspect=1920f/1080;
            camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0,0,0,0);
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
            var masks=new List<object>();var geometry=new List<object>();var bodyRenderers=new List<SpriteRenderer>();Bounds union=default;
            for(int part=0;part<=overlap;part++)
            {
                var prefab=role==3?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/"+files[role]+".prefab");
                if(prefab==null)throw new Exception("Missing prefab: "+roles[role]);
                var body=UnityEngine.Object.Instantiate(prefab);body.hideFlags=HideFlags.HideAndDontSave;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(body,scene);owned.Add(body);body.transform.position=new Vector3(part*.11f,part*.07f,0);
                var unit=CoreGuardianUnitAnimation.Attach(body,roles[role]);
                for(int step=0;step<=phase;step++)
                {
                    if(role<5)unit.Move(directions[direction],.08f);else unit.Aim(directions[direction],.08f);
                    unit.Present(.08f);
                }
                var outline=Make("Probe mask owner").AddComponent<CoreGuardianSelectionOutline>();outline.enabled=false;outline.widthPixels=1;outlines.Add(outline);typeof(CoreGuardianSelectionOutline).GetMethod("PresentMask",Hidden).Invoke(outline,new object[]{body.transform,camera,true,false,null,null});bodyRenderers.AddRange(body.GetComponentsInChildren<SpriteRenderer>(true));
                var type=typeof(CoreGuardianSelectionOutline);var mask=(RenderTexture)type.GetField("mask",Hidden).GetValue(outline);
                if(mask==null)throw new Exception("Missing mask for "+roles[role]);
                var quad=(Mesh)type.GetField("quad",Hidden).GetValue(outline);var output=(MeshRenderer)type.GetField("output",Hidden).GetValue(outline);
                masks.Add(new{mask.width,mask.height,hash=Hash(mask,new RectInt(0,0,mask.width,mask.height))});
                geometry.Add(new{vertices=quad.vertices.Select(v=>new[]{v.x,v.y,v.z}).ToArray(),uv=quad.uv.Select(v=>new[]{v.x,v.y}).ToArray(),output.sortingLayerID,output.sortingOrder});
                if(part==0)union=outline.TargetBounds;else union.Encapsulate(outline.TargetBounds);
            }
            var lo=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var hi=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            for(int i=0;i<8;i++){var p=union.center+Vector3.Scale(union.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var s=camera.WorldToScreenPoint(p);lo=Vector2.Min(lo,s);hi=Vector2.Max(hi,s);}
            int x=Mathf.Max(0,Mathf.FloorToInt(lo.x)-8),y=Mathf.Max(0,Mathf.FloorToInt(lo.y)-8),right=Mathf.Min(1920,Mathf.CeilToInt(hi.x)+8),top=Mathf.Min(1080,Mathf.CeilToInt(hi.y)+8);
            var rect=new RectInt(x,y,right-x,top-y);if(rect.width<=0||rect.height<=0)throw new Exception("Empty output region");
            var stateBeforeDisable=RendererState(scene);foreach(var bodyRenderer in bodyRenderers)bodyRenderer.enabled=false;var stateBeforeRender=RendererState(scene);var globalTime=Shader.GetGlobalVector("_Time").ToString("R");camera.Render();var outputHash=Hash(target,rect);var saved=SavePixels(target,rect,id);var stateAfterRender=RendererState(scene);
            return new{id,role=roles[role],direction,phase,zoom=camera.orthographicSize,overlap,rect=new[]{rect.x,rect.y,rect.width,rect.height},masks,geometry,outputHash,saved,globalTime,stateBeforeDisable,stateBeforeRender,stateAfterRender,cameraState=new{projection=Matrix(camera.projectionMatrix),view=Matrix(camera.worldToCameraMatrix),camera.aspect,camera.nearClipPlane,camera.farClipPlane,camera.allowHDR,camera.allowMSAA,renderPipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name,colorSpace=QualitySettings.activeColorSpace.ToString()}};
        }
        finally
        {
            foreach(var outline in outlines)if(outline!=null)typeof(CoreGuardianSelectionOutline).GetMethod("ReleaseResources",Hidden).Invoke(outline,null);
            if(camera!=null)camera.targetTexture=null;
            foreach(var go in owned)if(go!=null)UnityEngine.Object.DestroyImmediate(go);
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
    static float[] Matrix(Matrix4x4 m)=>Enumerable.Range(0,16).Select(i=>m[i]).ToArray();
    static object Tex(Texture t)=>t==null?null:new{name=t.name,path=AssetDatabase.GetAssetPath(t),t.width,t.height,t.dimension,t.filterMode,t.wrapMode,t.anisoLevel,t.mipmapCount,t.updateCount,format=t.graphicsFormat.ToString()};
    static object[] RendererState(UnityEngine.SceneManagement.Scene s)
    {
        return s.GetRootGameObjects().SelectMany((root,index)=>root.GetComponentsInChildren<Renderer>(true).Select(r=>new{rootIndex=index,renderer=r})).Select(entry=>
        {
            var r=entry.renderer;var names=new List<string>();for(var t=r.transform;t!=null;t=t.parent)names.Insert(0,t.name);
            var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);
            var meshFilter=r.GetComponent<MeshFilter>();var mesh=meshFilter!=null?meshFilter.sharedMesh:null;var sprite=(r as SpriteRenderer)?.sprite;
            return (object)new{entry.rootIndex,path=string.Join("/",names),type=r.GetType().Name,identity=r.GetHashCode(),r.enabled,active=r.gameObject.activeInHierarchy,r.forceRenderingOff,r.sortingLayerID,r.sortingOrder,r.sortingLayerName,matrix=Matrix(r.localToWorldMatrix),bounds=new[]{r.bounds.center.x,r.bounds.center.y,r.bounds.center.z,r.bounds.extents.x,r.bounds.extents.y,r.bounds.extents.z},sprite=sprite?.name,spriteColor=r is SpriteRenderer sr?new[]{sr.color.r,sr.color.g,sr.color.b,sr.color.a}:null,materials=r.sharedMaterials.Select(m=>
            {
                if(m==null)return null;var shader=m.shader;var properties=new Dictionary<string,object>();
                for(int p=0;p<shader.GetPropertyCount();p++)
                {
                    var key=shader.GetPropertyName(p);var type=shader.GetPropertyType(p);
                    if(type==UnityEngine.Rendering.ShaderPropertyType.Texture)properties[key]=new{texture=Tex(m.GetTexture(key)),scale=new[]{m.GetTextureScale(key).x,m.GetTextureScale(key).y},offset=new[]{m.GetTextureOffset(key).x,m.GetTextureOffset(key).y}};
                    else if(type==UnityEngine.Rendering.ShaderPropertyType.Color||type==UnityEngine.Rendering.ShaderPropertyType.Vector){var v=m.GetVector(key);properties[key]=new[]{v.x,v.y,v.z,v.w};}
                    else properties[key]=m.GetFloat(key);
                }
                return (object)new{name=m.name,identity=m.GetHashCode(),shader=shader.name,m.renderQueue,keywords=m.shaderKeywords,properties};
            }).ToArray(),propertyBlock=new{empty=block.isEmpty,main=Tex(block.GetTexture("_MainTex")),texel=block.GetVector("_MainTex_TexelSize").ToString("R"),origin=block.GetVector("_MaskOriginPx").ToString("R"),color=block.GetColor("_Color").ToString("R"),width=block.GetInt("_Width")},mesh=mesh==null?null:new{vertices=mesh.vertices.Select(v=>new[]{v.x,v.y,v.z}).ToArray(),uv=mesh.uv.Select(v=>new[]{v.x,v.y}).ToArray(),triangles=mesh.triangles}};
        }).ToArray();
    }
    static object SavePixels(RenderTexture source,RectInt rect,int id)
    {
        var old=RenderTexture.active;var read=new Texture2D(rect.width,rect.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=source;read.ReadPixels(new Rect(rect.x,rect.y,rect.width,rect.height),0,0);read.Apply();var bytes=read.GetRawTextureData<byte>().ToArray();var prefix=Root+label+"_"+(index-1)+"_"+id;File.WriteAllBytes(prefix+".rgba",bytes);File.WriteAllBytes(prefix+".png",read.EncodeToPNG());return new{prefix,bytes=bytes.Length};}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(read);}
    }

    static string Hash(RenderTexture source,RectInt region)
    {
        var old=RenderTexture.active;var read=new Texture2D(region.width,region.height,TextureFormat.RGBA32,false);
        try
        {
            RenderTexture.active=source;read.ReadPixels(new Rect(region.x,region.y,region.width,region.height),0,0);read.Apply();
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(read.GetRawTextureData<byte>().ToArray())).Replace("-","");
        }
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(read);}
    }
    static void Finish(string error)
    {
        running=false;EditorApplication.update-=Poll;
        File.WriteAllText(Root+label+".json",JsonConvert.SerializeObject(new{cases=rows.Count,error,rows},Formatting.Indented));
        File.WriteAllText(Root+label+"_completion.json",JsonConvert.SerializeObject(new{cases=rows.Count,error,playing=EditorApplication.isPlaying,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty},Formatting.Indented));
    }
}
