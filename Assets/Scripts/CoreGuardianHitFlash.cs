using System;
using System.Linq;
using UnityEngine;

// Presentation only. Shared shader, separate property blocks; no damage or movement changes.
public sealed class CoreGuardianHitFlash
{
    public const float Duration=.065f,RetriggerDelay=.1f,Strength=.22f;
    static readonly int AmountId=Shader.PropertyToID("_HitFlashAmount");
    static Material shared;
    readonly SpriteRenderer[] renderers;
    readonly Material[] originals;
    readonly MaterialPropertyBlock[] saved,working;
    float age=Duration,cooldown;
    bool active;
    public CoreGuardianHitFlash(SpriteRenderer artwork)
    {
        if(shared==null)
        {
            var shader=Resources.Load<Shader>("CoreGuardian/BodyHitFlash");
            if(shader==null)throw new InvalidOperationException("Missing CoreGuardian body hit shader");
            shared=new Material(shader){name="CoreGuardian Body Hit (shared)",hideFlags=HideFlags.HideAndDontSave};
        }
        renderers=artwork.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.sprite==null||!r.sprite.name.Contains("Shadow")).ToArray();
        originals=new Material[renderers.Length];saved=new MaterialPropertyBlock[renderers.Length];working=new MaterialPropertyBlock[renderers.Length];
        for(int i=0;i<renderers.Length;i++){saved[i]=new MaterialPropertyBlock();working[i]=new MaterialPropertyBlock();}
    }
    public bool Trigger()
    {
        if(cooldown>0)return false;
        for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)
        {
            originals[i]=renderers[i].sharedMaterial;renderers[i].GetPropertyBlock(saved[i]);renderers[i].GetPropertyBlock(working[i]);
            renderers[i].sharedMaterial=shared;
        }
        age=0;cooldown=RetriggerDelay;active=true;Apply(Strength);return true;
    }
    public void Advance(float delta)
    {
        delta=Mathf.Max(0,delta);cooldown=Mathf.Max(0,cooldown-delta);
        if(!active)return;age+=delta;
        if(age>=Duration){Clear();return;}
        Apply(Strength*(1-age/Duration));
    }
    void Apply(float amount)
    {
        for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)
        {working[i].SetFloat(AmountId,amount);renderers[i].SetPropertyBlock(working[i]);}
    }
    public void Clear()
    {
        if(!active)return;
        for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)
        {renderers[i].sharedMaterial=originals[i];renderers[i].SetPropertyBlock(saved[i]);}
        active=false;
    }
}
