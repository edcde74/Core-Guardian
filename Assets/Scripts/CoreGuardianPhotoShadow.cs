using System;
using System.Linq;
using UnityEngine;
using Unity.Profiling;

// Approved photo-derived floor mass. Body art, animation and combat remain independent.
[DisallowMultipleComponent, DefaultExecutionOrder(200)]
public sealed class CoreGuardianPhotoShadow : MonoBehaviour
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
    readonly CoreGuardianSpriteReferences shadowSources=new CoreGuardianSpriteReferences();
    public static CoreGuardianPhotoShadow Attach(GameObject root,string role)
    {
        var shadow=root.GetComponent<CoreGuardianPhotoShadow>();
        if(shadow!=null)return shadow;
        shadow=root.AddComponent<CoreGuardianPhotoShadow>();shadow.Configure(role);return shadow;
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
