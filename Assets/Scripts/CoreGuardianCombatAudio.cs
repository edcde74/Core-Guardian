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
    public const string MonsterHitResourcePath="CoreGuardian/Audio/MonsterHit/03_Hit";
    public const string MonsterDeathResourcePath="CoreGuardian/Audio/MonsterDeath/02_Flesh";
    public const string BossSpawnResourcePath="CoreGuardian/Audio/BossSpawn/01_Encounter";
    public const string BossAccelerationResourcePath="CoreGuardian/Audio/BossAcceleration/01_SpeedUp";
    public const string TowerInstallResourcePath="CoreGuardian/Audio/TowerInstall/02_Unequip";
    public const string UpgradeSuccessResourcePath="CoreGuardian/Audio/UpgradeSuccess/01_Confirmation";
    public const string UpgradeFailureResourcePath="CoreGuardian/Audio/UpgradeFailure/03_Error006";
    public const string RoundStartResourcePath="CoreGuardian/Audio/RoundStart/03_Confirmation004";
    AudioClip singleClip,playerClip,multiClip,coreDamageClip,monsterHitClip,monsterDeathClip,bossSpawnClip,bossAccelerationClip,towerInstallClip,upgradeSuccessClip,upgradeFailureClip,roundStartClip;
    readonly System.Collections.Generic.HashSet<int> announcedBosses=new System.Collections.Generic.HashSet<int>();
    readonly System.Collections.Generic.HashSet<int> acceleratedBosses=new System.Collections.Generic.HashSet<int>();
    readonly System.Collections.Generic.Dictionary<int,float> nextMultiShots=new System.Collections.Generic.Dictionary<int,float>();
    readonly AudioSource[] voices=new AudioSource[VoiceLimit];
    Session session;
    float nextShot,nextPlayerShot,nextCoreDamage,nextMonsterHit,nextMonsterDeath,nextUpgradeFailure;
    public int MonsterDeathPlayedCount {get;private set;}
    public int BossSpawnPlayedCount {get;private set;}
    public int BossAccelerationPlayedCount {get;private set;}
    public int TowerInstallPlayedCount {get;private set;}
    public int UpgradeSuccessPlayedCount {get;private set;}
    public int UpgradeFailurePlayedCount {get;private set;}
    public int RoundStartPlayedCount {get;private set;}
    public int MonsterHitPlayedCount {get;private set;}
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
        monsterHitClip=Resources.Load<AudioClip>(MonsterHitResourcePath);
        monsterDeathClip=Resources.Load<AudioClip>(MonsterDeathResourcePath);
        bossSpawnClip=Resources.Load<AudioClip>(BossSpawnResourcePath);
        bossAccelerationClip=Resources.Load<AudioClip>(BossAccelerationResourcePath);
        towerInstallClip=Resources.Load<AudioClip>(TowerInstallResourcePath);
        upgradeSuccessClip=Resources.Load<AudioClip>(UpgradeSuccessResourcePath);
        upgradeFailureClip=Resources.Load<AudioClip>(UpgradeFailureResourcePath);
        roundStartClip=Resources.Load<AudioClip>(RoundStartResourcePath);
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
        if(!ReferenceEquals(session,value)){announcedBosses.Clear();acceleratedBosses.Clear();}
        Unsubscribe();session=value;nextShot=nextPlayerShot=nextCoreDamage=nextMonsterHit=nextMonsterDeath=nextUpgradeFailure=0;
        nextMultiShots.Clear();
        if(isActiveAndEnabled)Subscribe();
    }
    void Subscribe(){if(session!=null){session.ProjectileLaunched+=OnLaunch;session.DamageApplied+=OnMonsterHit;session.CombatStarted+=OnCombatStarted;}}
    void OnEnable(){Subscribe();}
    void Unsubscribe(){if(session!=null){session.ProjectileLaunched-=OnLaunch;session.DamageApplied-=OnMonsterHit;session.CombatStarted-=OnCombatStarted;}}
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
    void OnCombatStarted()
    {
        if(PlayClip(roundStartClip,true,true))RoundStartPlayedCount++;
    }
    public bool NotifyTowerInstalled()
    {
        if(!PlayClip(towerInstallClip,true,true))return false;
        TowerInstallPlayedCount++;return true;
    }
    public bool NotifyUpgradeSucceeded()
    {
        if(!PlayClip(upgradeSuccessClip,true,true))return false;
        UpgradeSuccessPlayedCount++;return true;
    }
    public bool NotifyUpgradeFailed()
    {
        if(Time.unscaledTime<nextUpgradeFailure||!PlayClip(upgradeFailureClip,true,true))return false;
        nextUpgradeFailure=Time.unscaledTime+.2f;UpgradeFailurePlayedCount++;return true;
    }
    public bool NotifyBossSpawn(int enemyId)
    {
        if(!isActiveAndEnabled||enemyId<=0||!announcedBosses.Add(enemyId))return false;
        // Muted appearances remain announced: do not replay them after unmuting.
        if(!PlayClip(bossSpawnClip,true,true))return false;
        BossSpawnPlayedCount++;return true;
    }
    void OnMonsterHit(EnemyState target,int amount)
    {
        // A lethal hit gets only the selected death clip, even during the hit cooldown.
        if(target==null||amount<=0||target.Exited)return;
        if(target.Health<=0)
        {
            if(Time.unscaledTime<nextMonsterDeath||!PlayClip(monsterDeathClip))return;
            nextMonsterDeath=Time.unscaledTime+.1f;MonsterDeathPlayedCount++;return;
        }
        if(target.Enraged&&acceleratedBosses.Add(target.Id))
        {
            // The transition replaces this ordinary hit. Muted transitions are consumed too.
            if(PlayClip(bossAccelerationClip,true,true))BossAccelerationPlayedCount++;
            return;
        }
        // Bound the whole hit bus, not each enemy.
        if(Time.unscaledTime<nextMonsterHit)return;
        if(!PlayClip(monsterHitClip))return;
        nextMonsterHit=Time.unscaledTime+.1f;MonsterHitPlayedCount++;
    }
    public bool NotifyCoreDamage(int amount)
    {
        if(amount<=0||Time.unscaledTime<nextCoreDamage)return false;
        if(!PreviewCoreDamage())return false;
        nextCoreDamage=Time.unscaledTime+.3f;CoreDamagePlayedCount++;return true;
    }
    bool PlayClip(AudioClip clip,bool important=false,bool protectCore=false)
    {
        if(!isActiveAndEnabled||CoreGuardianSoundSettings.Volume<=0||clip==null)return false;
        var voice=voices.FirstOrDefault(v=>v!=null&&!v.isPlaying);
        // Core danger remains audible when attacks occupy the bounded pool.
        if(voice==null&&important){voice=protectCore?voices.FirstOrDefault(v=>v!=null&&v.clip!=coreDamageClip):voices[0];if(voice!=null)voice.Stop();}
        if(voice==null){DroppedCount++;return false;}
        voice.clip=clip;
        voice.volume=CoreGuardianSoundSettings.Volume*ShotGain;
        voice.Play();PlayedCount++;return true;
    }
}
