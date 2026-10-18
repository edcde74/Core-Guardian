using System.Linq;
using CoreGuardian;
using UnityEngine;

// Presentation only. A bounded pool avoids unbounded PlayOneShot stacking in dense combat.
public sealed class CoreGuardianCombatAudio : MonoBehaviour
{
    public const int VoiceLimit=4;
    const float ShotGain=.55f;
    public static readonly string[] CandidateNames={"① 대지","② 화염","③ 오라"};
    AudioClip[] clips;
    readonly AudioSource[] voices=new AudioSource[VoiceLimit];
    Session session;
    float nextShot;
    public int PlayedCount {get;private set;}
    public int DroppedCount {get;private set;}
    public int ActiveVoices=>voices.Count(v=>v!=null&&v.isPlaying);

    void Awake()
    {
        clips=Resources.LoadAll<AudioClip>("CoreGuardian/Audio/TowerShot").OrderBy(c=>c.name).ToArray();
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
        Unsubscribe();session=value;nextShot=0;
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
        if(projectile==null||session==null||projectile.Source==null||!projectile.Source.StartsWith("Tower:"))return;
        if(!int.TryParse(projectile.Source.Substring(6),out int id))return;
        var tower=session.Towers.FirstOrDefault(t=>t.Id==id);
        if(tower==null||tower.Kind!=TowerKind.Single)return;
        if(Time.unscaledTime<nextShot){DroppedCount++;return;}
        if(Preview())nextShot=Time.unscaledTime+.035f;
    }
    public bool Preview()
    {
        if(!isActiveAndEnabled||CoreGuardianSoundSettings.Volume<=0||clips==null||clips.Length!=3)return false;
        var voice=voices.FirstOrDefault(v=>v!=null&&!v.isPlaying);
        if(voice==null){DroppedCount++;return false;}
        voice.clip=clips[CoreGuardianSoundSettings.Candidate];
        voice.volume=CoreGuardianSoundSettings.Volume*ShotGain;
        voice.Play();PlayedCount++;return true;
    }
}
