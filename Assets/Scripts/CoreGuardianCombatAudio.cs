using System.Linq;
using CoreGuardian;
using UnityEngine;

// Presentation only. A bounded pool avoids unbounded PlayOneShot stacking in dense combat.
public sealed class CoreGuardianCombatAudio : MonoBehaviour
{
    public const int VoiceLimit=4;
    const float ShotGain=.55f;
    public const string SingleShotResourcePath="CoreGuardian/Audio/TowerShot/03_Aura";
    public const string PlayerShotResourcePath="CoreGuardian/Audio/PlayerShot/03_Glyph";
    public const string MultiShotResourcePath="CoreGuardian/Audio/MultiShot/B_BrighterAttack";
    public const string CoreDamageResourcePath="CoreGuardian/Audio/CoreDamage/06_Aura_Glyph_2";
    AudioClip singleClip,playerClip,multiClip,coreDamageClip;
    readonly System.Collections.Generic.Dictionary<int,float> nextMultiShots=new System.Collections.Generic.Dictionary<int,float>();
    readonly AudioSource[] voices=new AudioSource[VoiceLimit];
    Session session;
    float nextShot,nextPlayerShot,nextCoreDamage;
    public int CoreDamagePlayedCount {get;private set;}
    public int PlayedCount {get;private set;}
    public int DroppedCount {get;private set;}
    public int ActiveVoices=>voices.Count(v=>v!=null&&v.isPlaying);

    void Awake()
    {
        singleClip=Resources.Load<AudioClip>(SingleShotResourcePath);
        playerClip=Resources.Load<AudioClip>(PlayerShotResourcePath);
        multiClip=Resources.Load<AudioClip>(MultiShotResourcePath);
        coreDamageClip=Resources.Load<AudioClip>(CoreDamageResourcePath);
        for(int i=0;i<voices.Length;i++)
        {
            var child=new GameObject("MagicCannonVoice_"+i);child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();voices[i]=source;
            source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.pitch=1;
            source.volume=CoreGuardianSoundSettings.Volume*ShotGain;
        }
    }
    public void Configure(Session value)
    {
        Unsubscribe();session=value;nextShot=nextPlayerShot=nextCoreDamage=0;
        nextMultiShots.Clear();
        if(isActiveAndEnabled&&session!=null)session.ProjectileLaunched+=OnLaunch;
    }
    void OnEnable(){if(session!=null)session.ProjectileLaunched+=OnLaunch;}
    void Unsubscribe(){if(session!=null)session.ProjectileLaunched-=OnLaunch;}
    void OnDisable(){Unsubscribe();foreach(var voice in voices)if(voice!=null)voice.Stop();}
    void OnDestroy(){PlayerPrefs.Save();}
    void OnApplicationPause(bool paused){if(paused)PlayerPrefs.Save();}
    void Update(){foreach(var voice in voices)if(voice!=null)voice.volume=CoreGuardianSoundSettings.Volume*ShotGain;}
    void OnLaunch(ProjectileState projectile)
    {
        if(projectile==null||session==null||projectile.Source==null)return;
        if(projectile.Source=="Player")
        {
            if(Time.unscaledTime<nextPlayerShot){DroppedCount++;return;}
            if(PreviewPlayer())nextPlayerShot=Time.unscaledTime+.035f;
            return;
        }
        if(!projectile.Source.StartsWith("Tower:"))return;
        if(!int.TryParse(projectile.Source.Substring(6),out int id))return;
        var tower=session.Towers.FirstOrDefault(t=>t.Id==id);
        if(tower==null)return;
        if(tower.Kind==TowerKind.Multi)
        {
            // One clip already contains all three barrel pulses. Coalesce targets per tower.
            if(nextMultiShots.TryGetValue(id,out var next)&&Time.unscaledTime<next){DroppedCount++;return;}
            if(PreviewMulti())nextMultiShots[id]=Time.unscaledTime+.035f;
            return;
        }
        if(tower.Kind!=TowerKind.Single)return;
        if(Time.unscaledTime<nextShot){DroppedCount++;return;}
        if(Preview())nextShot=Time.unscaledTime+.035f;
    }
    public bool Preview()
    {
        return PlayClip(singleClip);
    }
    public bool PreviewPlayer()=>PlayClip(playerClip);
    public bool PreviewMulti()=>PlayClip(multiClip);
    public bool PreviewCoreDamage()=>PlayClip(coreDamageClip,true);
    public bool NotifyCoreDamage(int amount)
    {
        if(amount<=0||Time.unscaledTime<nextCoreDamage)return false;
        if(!PreviewCoreDamage())return false;
        nextCoreDamage=Time.unscaledTime+.3f;CoreDamagePlayedCount++;return true;
    }
    bool PlayClip(AudioClip clip,bool important=false)
    {
        if(!isActiveAndEnabled||CoreGuardianSoundSettings.Volume<=0||clip==null)return false;
        var voice=voices.FirstOrDefault(v=>v!=null&&!v.isPlaying);
        // Core danger remains audible when attacks occupy the bounded pool.
        if(voice==null&&important){voice=voices[0];if(voice!=null)voice.Stop();}
        if(voice==null){DroppedCount++;return false;}
        voice.clip=clip;
        voice.volume=CoreGuardianSoundSettings.Volume*ShotGain;
        voice.Play();PlayedCount++;return true;
    }
}
