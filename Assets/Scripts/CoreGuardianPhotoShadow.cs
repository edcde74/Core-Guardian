using System;
using System.Linq;
using UnityEngine;
using Unity.Profiling;

// Approved photo-derived floor mass. Body art, animation and combat remain independent.
[DisallowMultipleComponent, DefaultExecutionOrder(200)]
public sealed class CoreGuardianPhotoShadow : MonoBehaviour
{
    static readonly ProfilerMarker cameraSample=new ProfilerMarker("CG.PhotoShadow.Camera");
    static readonly ProfilerMarker presentSample=new ProfilerMarker("CG.PhotoShadow.Present");
    static readonly ProfilerMarker castSample=new ProfilerMarker("CG.PhotoShadow.Cast");
    static readonly ProfilerMarker contactSample=new ProfilerMarker("CG.PhotoShadow.Contact");
    [Serializable] sealed class Entry {public string role;public float x,y;}
    [Serializable] sealed class Entries {public Entry[] entries;}
    static Entries registration;
    SpriteRenderer cast,art;
    CoreGuardianSelectionOutline contact;
    Material contactMaterial;
    CoreGuardianSharedMaskMaterial contactLease;
    Vector3 offset;
    string role;
    bool mobile,replace;
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
        role=value;mobile=role=="Player"||role=="Attack"||role=="Defense"||role=="Buff"||role=="Boss";
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
        if(mobile)
        {
            var host=new GameObject("PhotoDynamicContact");host.transform.SetParent(transform,false);
            contact=host.AddComponent<CoreGuardianSelectionOutline>();contact.enabled=false;
            contactLease=CoreGuardianSharedMaskMaterial.Acquire("CoreGuardian/PhotoShadowContact");contactMaterial=contactLease.Material;
        }
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
        if(contact!=null)
        {
            using var contactScope=contactSample.Auto();
            if(camera==null)camera=Camera.main;
            if(camera!=null)
            {
                float ratio=camera.pixelHeight/(2*camera.orthographicSize)/63.52941f;
                contact.PresentGroundContactWithBand(transform,camera,contactMaterial,(role=="Boss"?22:16)*ratio*size);
            }
        }
    }
    void LateUpdate()
    {
        Camera camera;
        using(cameraSample.Auto())camera=Camera.main;
        Present(camera);
    }
    public void InvalidateSources(){shadowSources.Invalidate();contact?.InvalidateSources();}
    void OnDisable(){if(cast!=null)cast.enabled=false;if(contact!=null){contact.ReleasePresentation();contact.gameObject.SetActive(false);}}
    void OnEnable(){if(cast!=null)cast.enabled=true;if(contact!=null)contact.gameObject.SetActive(true);}
    void OnDestroy(){contactLease?.Release();contactLease=null;contactMaterial=null;}
}
