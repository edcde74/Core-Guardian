using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public static class CGShadowNameQuality
{
    const string Root="docs/development/shadow_name_cache_20261023/";
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.Profiling.Profiler.enabled||UnityEditorInternal.ProfilerDriver.deepProfiling)throw new Exception("Stopped idle Editor required");
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        var cameraRoot=new GameObject("Shadow QA camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
        var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.21f,1);camera.cullingMask=1<<31;
        var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
        var read=new Texture2D(256,256,TextureFormat.RGBA32,false);var rows=new List<object>();int failures=0;
        try
        {
            foreach(string role in new[]{"Attack","Defense","Buff","Single","Multi","Support","Player","Core","Boss"})
            for(int variant=0;variant<2;variant++)
            {
                string prefab=role=="Player"?"Assets/Prefabs/Gameplay/Units/Player.prefab":role=="Core"?"Assets/Prefabs/Gameplay/Units/core.prefab":role=="Boss"?"Assets/Prefabs/Characters/Enemies/Resources/CoreGuardian/Animation/Boss/BossAnimated.prefab":"Assets/Prefabs/Gameplay/Units/"+(role=="Attack"||role=="Defense"||role=="Buff"?"enemy_":"tower_")+role.ToLowerInvariant()+".prefab";
                var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab));SceneManager.MoveGameObjectToScene(actor,scene);
                try
                {
                    actor.transform.position=new Vector3(variant*1.25f,variant*-.5f,0);
                    if(role=="Core")CoreGuardianPhotoShadow.Attach(actor,role);
                    else {var animation=CoreGuardianUnitAnimation.Attach(actor,role);animation.Move(variant==0?Vector2.left:Vector2.right,variant==0?.14f:.22f);animation.Present(.2f);}
                    foreach(var node in actor.GetComponentsInChildren<Transform>(true))node.gameObject.layer=31;
                    var current=actor.GetComponent<CoreGuardianPhotoShadow>();var legacy=actor.AddComponent<CGShadowNameBaseline>();
                    var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                    foreach(string name in new[]{"cast","art","offset","role","replace","size"})typeof(CGShadowNameBaseline).GetField(name,flags).SetValue(legacy,typeof(CoreGuardianPhotoShadow).GetField(name,flags).GetValue(current));
                    var renderers=actor.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach(var renderer in renderers)if(new[]{"바닥 밀착선","바깥 그림자","접촉 그림자","짧은 투사 그림자","바닥 수정빛"}.Contains(renderer.name))renderer.enabled=true;
                    var enabled=renderers.Select(r=>r.enabled).ToArray();
                    var art=renderers.First(r=>r.name=="2D Sprite");camera.transform.position=new Vector3(art.bounds.center.x,art.bounds.center.y,-10);
                    legacy.Present(null);camera.Render();var before=Read();
                    for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];
                    current.InvalidateSources();current.Present(null);camera.Render();var after=Read();
                    if(before.Count(c=>!c.Equals(before[0]))<20)throw new Exception("Empty original "+role);
                    int differences=0;for(int i=0;i<before.Length;i++)if(!before[i].Equals(after[i]))differences++;
                    if(differences>0)failures++;rows.Add(new{role,variant,differences});
                    read.SetPixels32(before);read.Apply();File.WriteAllBytes(Root+role+"_"+variant+"_before.png",read.EncodeToPNG());
                    read.SetPixels32(after);read.Apply();File.WriteAllBytes(Root+role+"_"+variant+"_after.png",read.EncodeToPNG());
                }
                finally{UnityEngine.Object.DestroyImmediate(actor);}
                if(failures>0)goto Completed;
            }
            Completed: ;
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(read);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
        File.WriteAllText(Root+"quality.json",JsonConvert.SerializeObject(new{pairs=rows.Count,failures,width=256,height=256,ortho=2,scope="same original actors and cast renderer, nine roles two poses, no HUD/full game approval",rows},Formatting.Indented));return new{pairs=rows.Count,failures};
        Color32[] Read(){RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();return read.GetPixels32();}
    }
}

// Approved photo-derived floor mass. Body art, animation and combat remain independent.
[DisallowMultipleComponent, DefaultExecutionOrder(200)]
public sealed class CGShadowNameBaseline : MonoBehaviour
{
    static readonly ProfilerMarker presentSample=new ProfilerMarker("CG.PhotoShadow.Present");
    static readonly ProfilerMarker castSample=new ProfilerMarker("CG.PhotoShadow.Cast");
    [Serializable] sealed class Entry {public string role;public float x,y;}
    [Serializable] sealed class Entries {public Entry[] entries;}
    static Entries registration;
    SpriteRenderer cast,art;
    Vector3 offset;
    string role;
    bool replace;
    float size=1;
    readonly CGShadowOriginalReferences shadowSources=new CGShadowOriginalReferences();
    public static CGShadowNameBaseline Attach(GameObject root,string role)
    {
        var shadow=root.GetComponent<CGShadowNameBaseline>();
        if(shadow!=null)return shadow;
        shadow=root.AddComponent<CGShadowNameBaseline>();shadow.Configure(role);return shadow;
    }
    void Configure(string value)
    {
        role=value;
        replace=role=="Defense"||role=="Boss"||role=="Core"||role=="Multi"||role=="Support";
        art=GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
        string source=role=="Boss"?"Defense":role;
        if(registration==null)registration=JsonUtility.FromJson<Entries>(Resources.Load<TextAsset>("CoreGuardian/PhotoShadows/registration").text);
        var e=registration.entries.First(r=>r.role==source);
        size=role=="Boss"?art.bounds.size.x/(33*.6f/32):Mathf.Abs(art.transform.lossyScale.x)/.6f;
        offset=new Vector3(e.x,e.y,0)*size;
        cast=new GameObject("PhotoCastShadow").AddComponent<SpriteRenderer>();cast.transform.SetParent(transform,false);
        cast.sprite=Resources.Load<Sprite>("CoreGuardian/PhotoShadows/"+source);
        if(cast.sprite==null)throw new InvalidOperationException("Missing approved shadow "+source);
        cast.sharedMaterial=art.sharedMaterial;
        Present(null);
    }
    public void Present(Camera camera)
    {
        using var presentScope=presentSample.Auto();
        if(cast==null||art==null)return;
        using(castSample.Auto())
        {
            foreach(var r in shadowSources.Read(transform))
                if(r.name=="바닥 밀착선"||(replace&&(r.name=="바깥 그림자"||r.name=="접촉 그림자"||r.name=="짧은 투사 그림자"||r.name=="바닥 수정빛")))r.enabled=false;
            cast.transform.position=transform.position+offset;
            cast.transform.rotation=Quaternion.identity;
            cast.transform.localScale=new Vector3(size/Mathf.Abs(transform.lossyScale.x),size/Mathf.Abs(transform.lossyScale.y),1);
            cast.sortingLayerID=art.sortingLayerID;cast.sortingOrder=42;
        }
    }
    void LateUpdate(){Present(null);}
    public void InvalidateSources(){shadowSources.Invalidate();}
    void OnDisable(){if(cast!=null)cast.enabled=false;}
    void OnEnable(){if(cast!=null)cast.enabled=true;}
}

// Per-consumer references only; this does not share masks or alter presentation order.
// Hierarchy checks do not use Transform.hasChanged: walking moves transforms every frame.
internal sealed class CGShadowOriginalReferences
{
    static readonly ProfilerMarker readSample=new ProfilerMarker("CG.SpriteReferences.Read");
    Transform root;
    bool dirty=true;
    readonly List<SpriteRenderer> renderers=new List<SpriteRenderer>();
    readonly List<Transform> nodes=new List<Transform>();
    readonly List<int> childCounts=new List<int>();
    readonly List<Transform> parents=new List<Transform>();
    public void Invalidate()=>dirty=true;
    public List<SpriteRenderer> Read(Transform target)
    {
        using var readScope=readSample.Auto();
        bool refresh=dirty||root!=target;
        if(!refresh)
        {
            for(int i=0;i<nodes.Count;i++)
                if(nodes[i]==null||nodes[i].childCount!=childCounts[i]||nodes[i].parent!=parents[i]){refresh=true;break;}
            if(!refresh)for(int i=0;i<renderers.Count;i++)if(renderers[i]==null){refresh=true;break;}
        }
        if(refresh)
        {
            root=target;dirty=false;renderers.Clear();nodes.Clear();childCounts.Clear();parents.Clear();
            if(root!=null)
            {
                root.GetComponentsInChildren(true,renderers);root.GetComponentsInChildren(true,nodes);
                foreach(var node in nodes){childCounts.Add(node.childCount);parents.Add(node.parent);}
            }
        }
        return renderers;
    }
}
