using System;
using System.Collections.Generic;
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
    readonly List<SpriteRenderer> suppressedSources=new List<SpriteRenderer>();
    int classifiedRevision=-1;
    public static CoreGuardianPhotoShadow Attach(GameObject root,string role)
    {
        var shadow=root.GetComponent<CoreGuardianPhotoShadow>();
        if(shadow!=null)return shadow;
        shadow=root.AddComponent<CoreGuardianPhotoShadow>();shadow.Configure(role);return shadow;
    }
    void Configure(string value)
    {
        role=value;
        bool bossLike=role=="Boss"||role=="IronKnight"||role=="CrimsonLord";
        replace=role=="Defense"||bossLike||role=="Core"||role=="Multi"||role=="Support";
        art=GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
        // New towers share the single-tower floor mass; new bosses reuse the boss treatment.
        string source=bossLike?"Defense":role=="Frost"||role=="Flame"||role=="Rail"?"Single":role;
        if(registration==null)registration=JsonUtility.FromJson<Entries>(Resources.Load<TextAsset>("CoreGuardian/PhotoShadows/registration").text);
        var e=registration.entries.FirstOrDefault(r=>r.role==source)??new Entry{role=source,x=.12f,y=.06f};
        size=bossLike?art.bounds.size.x/(33*.6f/32):Mathf.Abs(art.transform.lossyScale.x)/.6f;
        offset=new Vector3(e.x,e.y,0)*size;
        cast=new GameObject("PhotoCastShadow").AddComponent<SpriteRenderer>();cast.transform.SetParent(transform,false);
        cast.sprite=Resources.Load<Sprite>("CoreGuardian/PhotoShadows/"+source);
        if(cast.sprite==null)throw new InvalidOperationException("Missing approved shadow "+source);
        // 피드백 1 (2026-10-27): 보스는 방패병 그림자를 몸 전체 폭(방패·망토 포함)에 맞춰 키워 1.6배로 퍼졌다.
        // 일반 적과 같은 비율(몸 폭의 약 0.9)로 맞춘다.
        if(bossLike&&cast.sprite.bounds.size.x>0){size=art.bounds.size.x*.9f/cast.sprite.bounds.size.x;offset=new Vector3(e.x,e.y,0)*size;}
        cast.sharedMaterial=art.sharedMaterial;
        Present(null);
    }
    public void Present(Camera camera)
    {
        using var presentScope=presentSample.Auto();
        if(cast==null||art==null)return;
        using(castSample.Auto())
        {
            var sources=shadowSources.Read(transform);
            if(classifiedRevision!=shadowSources.Revision)
            {
                suppressedSources.Clear();
                foreach(var renderer in sources)
                {
                    string name=renderer.name;
                    if(name=="바닥 밀착선"||(replace&&(name=="바깥 그림자"||name=="접촉 그림자"||name=="짧은 투사 그림자"||name=="바닥 수정빛")))suppressedSources.Add(renderer);
                }
                classifiedRevision=shadowSources.Revision;
            }
            foreach(var renderer in suppressedSources)if(renderer!=null&&renderer.enabled)renderer.enabled=false;
            cast.transform.position=transform.position+offset;
            cast.transform.rotation=Quaternion.identity;
            cast.transform.localScale=new Vector3(size/Mathf.Abs(transform.lossyScale.x),size/Mathf.Abs(transform.lossyScale.y),1);
            cast.sortingLayerID=art.sortingLayerID;cast.sortingOrder=42;
        }
    }
    void LateUpdate(){Present(null);}
    // Names are classified on structural refresh; callers renaming an existing part must invalidate explicitly.
    public void InvalidateSources(){shadowSources.Invalidate();classifiedRevision=-1;}
    void OnDisable(){if(cast!=null)cast.enabled=false;}
    void OnEnable(){InvalidateSources();if(cast!=null)cast.enabled=true;}
}
