using System.Linq;
using CoreGuardian;
using UnityEngine;

// Presentation only. A bounded pool avoids unbounded PlayOneShot stacking in dense combat.
public sealed class CoreGuardianCombatAudio : MonoBehaviour
{
    public const int VoiceLimit = 4;
    const float ShotGain = .55f;
    public const string SingleShotResourcePath = "CoreGuardian/Audio/SelectedB/single";
    public const string PlayerShotResourcePath = "CoreGuardian/Audio/SelectedB/player";
    public const string MultiShotResourcePath = "CoreGuardian/Audio/SelectedB/multi_3";
    public const string CoreDamageResourcePath = "CoreGuardian/Audio/SelectedB/core";
    public const string MonsterHitResourcePath = "CoreGuardian/Audio/SelectedB/hit";
    public const string MonsterDeathResourcePath = "CoreGuardian/Audio/SelectedB/death";
    public const string BossSpawnResourcePath = "CoreGuardian/Audio/SelectedB/boss_spawn";
    public const string BossAccelerationResourcePath = "CoreGuardian/Audio/SelectedB/boss_accel";
    public const string TowerInstallResourcePath = "CoreGuardian/Audio/SelectedB/install";
    public const string UpgradeSuccessResourcePath = "CoreGuardian/Audio/SelectedB/upgrade_ok";
    public const string UpgradeFailureResourcePath = "CoreGuardian/Audio/SelectedB/upgrade_fail";
    public const string RoundStartResourcePath = "CoreGuardian/Audio/SelectedB/round_start";
    public const string RoundCompleteResourcePath = "CoreGuardian/Audio/SelectedB/round_complete";
    public const string VictoryResourcePath = "CoreGuardian/Audio/SelectedB/victory";
    AudioClip singleClip,
        playerClip,
        multiClip,
        coreDamageClip,
        monsterHitClip,
        monsterDeathClip,
        bossSpawnClip,
        bossAccelerationClip,
        towerInstallClip,
        upgradeSuccessClip,
        upgradeFailureClip,
        roundStartClip,
        roundCompleteClip,
        victoryClip;
    readonly System.Collections.Generic.HashSet<int> announcedBosses = new System.Collections.Generic.HashSet<int>();
    readonly System.Collections.Generic.HashSet<int> acceleratedBosses = new System.Collections.Generic.HashSet<int>();
    readonly AudioSource[] voices = new AudioSource[VoiceLimit];
    readonly CoreGuardianAudioCue[] voiceCues = new CoreGuardianAudioCue[VoiceLimit];
    CoreGuardianMusicAudio music;
    Session session;
    float nextShot,
        nextPlayerShot,
        nextCoreDamage,
        nextMonsterHit,
        nextMonsterDeath,
        nextUpgradeFailure;
    public int MonsterDeathPlayedCount { get; private set; }
    public int BossSpawnPlayedCount { get; private set; }
    public int BossAccelerationPlayedCount { get; private set; }
    public int TowerInstallPlayedCount { get; private set; }
    public int UpgradeSuccessPlayedCount { get; private set; }
    public int UpgradeFailurePlayedCount { get; private set; }
    public int RoundStartPlayedCount { get; private set; }
    public int RoundCompletePlayedCount { get; private set; }
    public int VictoryPlayedCount { get; private set; }
    public int MonsterHitPlayedCount { get; private set; }
    public int CoreDamagePlayedCount { get; private set; }
    public int PlayedCount { get; private set; }
    public int DroppedCount { get; private set; }
    public int MultiVolleyPlayedCount { get; private set; }
    public int LastMultiShotCount { get; private set; }
    public int DefeatPlayedCount { get; private set; }
    public int ActiveVoices => voices.Count(v => v != null && v.isPlaying);

    void Awake()
    {
        var musicRoot = new GameObject("CoreGuardianMusic");
        musicRoot.transform.SetParent(transform, false);
        music = musicRoot.AddComponent<CoreGuardianMusicAudio>();
        singleClip = CoreGuardianAudioCatalog.Find("single").Clip;
        playerClip = CoreGuardianAudioCatalog.Find("player").Clip;
        multiClip = CoreGuardianAudioCatalog.Find("multi_3").Clip;
        coreDamageClip = CoreGuardianAudioCatalog.Find("core").Clip;
        monsterHitClip = CoreGuardianAudioCatalog.Find("hit").Clip;
        monsterDeathClip = CoreGuardianAudioCatalog.Find("death").Clip;
        bossSpawnClip = CoreGuardianAudioCatalog.Find("boss_spawn").Clip;
        bossAccelerationClip = CoreGuardianAudioCatalog.Find("boss_accel").Clip;
        towerInstallClip = CoreGuardianAudioCatalog.Find("install").Clip;
        upgradeSuccessClip = CoreGuardianAudioCatalog.Find("upgrade_ok").Clip;
        upgradeFailureClip = CoreGuardianAudioCatalog.Find("upgrade_fail").Clip;
        roundStartClip = CoreGuardianAudioCatalog.Find("round_start").Clip;
        roundCompleteClip = CoreGuardianAudioCatalog.Find("round_complete").Clip;
        victoryClip = CoreGuardianAudioCatalog.Find("victory").Clip;
        for (int i = 0; i < voices.Length; i++)
        {
            var child = new GameObject("MagicCannonVoice_" + i);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            voices[i] = source;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            source.pitch = 1;
            source.volume = CoreGuardianSoundSettings.Volume * ShotGain;
        }
    }

    public void Configure(Session value)
    {
        if (!ReferenceEquals(session, value))
        {
            announcedBosses.Clear();
            acceleratedBosses.Clear();
        }
        Unsubscribe();
        session = value;
        nextShot = nextPlayerShot = nextCoreDamage = nextMonsterHit = nextMonsterDeath = nextUpgradeFailure = 0;
        music.Configure(value);
        if (isActiveAndEnabled)
        {
            Subscribe();
        }
    }

    void Subscribe()
    {
        if (session != null)
        {
            session.ProjectileLaunched += OnLaunch;
            session.AttackLaunched += OnAttackLaunched;
            session.DefeatReached += OnDefeatReached;
            session.DamageApplied += OnMonsterHit;
            session.CombatStarted += OnCombatStarted;
            session.RoundCompleted += OnRoundCompleted;
            session.VictoryReached += OnVictoryReached;
            session.Cue += OnSessionCue;
        }
    }

    void OnEnable()
    {
        Subscribe();
        if (music != null)
        {
            music.enabled = true;
        }
    }

    void Unsubscribe()
    {
        if (session != null)
        {
            session.ProjectileLaunched -= OnLaunch;
            session.AttackLaunched -= OnAttackLaunched;
            session.DefeatReached -= OnDefeatReached;
            session.DamageApplied -= OnMonsterHit;
            session.CombatStarted -= OnCombatStarted;
            session.RoundCompleted -= OnRoundCompleted;
            session.VictoryReached -= OnVictoryReached;
            session.Cue -= OnSessionCue;
        }
    }

    void OnDisable()
    {
        Unsubscribe();
        foreach (var voice in voices)
        {
            if (voice != null)
            {
                voice.Stop();
            }
        }

        if (music != null)
        {
            music.enabled = false;
        }
    }

    void OnDestroy()
    {
        PlayerPrefs.Save();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            PlayerPrefs.Save();
        }
    }

    void Update()
    {
        for (int i = delayedCues.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime >= delayedCues[i].at)
            {
                string id = delayedCues[i].id;
                delayedCues.RemoveAt(i);
                PlayGroupedCue(id);
            }
        }

        for (int i = 0; i < voices.Length; i++)
        {
            if (voices[i] != null)
            {
                voices[i].volume = voiceCues[i]?.Volume ?? CoreGuardianSoundSettings.Volume * ShotGain;
            }
        }
    }

    static readonly string[] MultiCues = { "multi_1", "multi_2", "multi_3" };

    void OnLaunch(ProjectileSnapshot projectile)
    {
        if (session == null || projectile.Source == null)
            return;

        if (projectile.Source == "Player")
        {
            if (Time.unscaledTime < nextPlayerShot)
            {
                DroppedCount++;
                return;
            }
            if (PreviewPlayer())
            {
                nextPlayerShot = Time.unscaledTime + .035f;
            }

            return;
        }
        var tower = session.TowerFromSource(projectile.Source);
        if (tower == null)
            return;

        if (tower.Kind == TowerKind.Multi)
        {
            return; // AttackLaunched supplies one exact count per volley.
        }

        if (tower.Kind != TowerKind.Single)
            return;

        if (Time.unscaledTime < nextShot)
        {
            DroppedCount++;
            return;
        }
        if (Preview())
        {
            nextShot = Time.unscaledTime + .035f;
        }
    }

    public bool Preview()
    {
        return PlayClip(singleClip);
    }

    public bool PreviewPlayer() => PlayClip(playerClip);

    public bool PreviewMulti() => PlayClip(multiClip);

    void OnAttackLaunched(string source, int count)
    {
        if (count < 1)
            return;

        var tower = session.TowerFromSource(source);
        if (tower == null || tower.Kind != TowerKind.Multi)
            return;

        if (PlayCue(MultiCues[Mathf.Clamp(count, 1, 3) - 1]))
        {
            MultiVolleyPlayedCount++;
            LastMultiShotCount = Mathf.Clamp(count, 1, 3);
        }
    }

    // New SFX (2026-10-27 slice): one voice per cue id per short window so dense fights stay readable.
    readonly System.Collections.Generic.Dictionary<string, float> cueCooldowns =
        new System.Collections.Generic.Dictionary<string, float>();
    public int SessionCuePlayedCount { get; private set; }

    // 사운드 확정표 (Notion 사운드 2단계): 같은 소리 묶음 간격(초). 0 = 묶지 않음.
    static float CueGap(string id)
    {
        switch (id)
        {
            case "cold_pulse":
            case "stun":
            case "freeze":
                return .3f;
            case "flame_shot":
                return .08f;
            case "rail_shot":
                return .1f;
            case "heal_pulse":
                return 1f;
            case "chain":
            case "execute":
            case "shatter":
            case "burn_explode":
                return .15f;
            case "reroll":
                return .2f;
            case "star_merge":
            case "area_blast":
            case "teleport":
            case "shield_deploy":
            case "summon":
                return 0f;
            default:
                return .12f;
        }
    }

    readonly System.Collections.Generic.List<(float at, string id)> delayedCues = new System.Collections.Generic.List<(
        float at,
        string id
    )>();

    /// <summary>앞 소리를 끊지 않고 조금 뒤에 낸다 (S12 순간 이동 0.1초, S19 빙결 0.05초).</summary>
    public void PlayCueAfter(string id, float delay)
    {
        delayedCues.Add((Time.unscaledTime + delay, id));
    }

    void OnSessionCue(Session.CueEvent cue)
    {
        // 분열은 소리 없음. 치명타는 피격음 자리에서 대신 난다(OnMonsterHit).
        if (cue.Id == "split" || cue.Id == "crit")
            return;

        if (cue.Id == "freeze")
        {
            PlayCueAfter(cue.Id, .05f);
            return;
        }

        PlayGroupedCue(cue.Id);
    }

    bool PlayGroupedCue(string id)
    {
        float now = Time.unscaledTime;
        // H4·H5·H6 포탑 피격음은 일반 피격음과 같은 줄(0.1초)을 쓴다.
        if (id.StartsWith("hit_", System.StringComparison.Ordinal))
        {
            if (now < nextMonsterHit)
            {
                DroppedCount++;
                return false;
            }
            if (!PlayCue(id))
                return false;

            nextMonsterHit = now + .1f;
            SessionCuePlayedCount++;
            return true;
        }
        if (cueCooldowns.TryGetValue(id, out float next) && now < next)
        {
            DroppedCount++;
            return false;
        }
        if (!PlayCue(id))
            return false;

        SessionCuePlayedCount++;
        float gap = CueGap(id);
        if (gap > 0)
        {
            cueCooldowns[id] = now + gap;
        }

        return true;
    }

    public bool PlayCue(string id)
    {
        var cue = CoreGuardianAudioCatalog.Find(id);
        return cue != null
            && cue.Category != CoreGuardianAudioCategory.Music
            && PlayClip(cue.Clip, cue.important, cue.protectCore);
    }

    void OnDefeatReached()
    {
        if (PlayCue("defeat"))
        {
            DefeatPlayedCount++;
        }
    }

    public bool PreviewCoreDamage() => PlayClip(coreDamageClip, true);

    void OnCombatStarted()
    {
        if (PlayClip(roundStartClip, true, true))
        {
            RoundStartPlayedCount++;
        }
    }

    void OnRoundCompleted()
    {
        if (PlayClip(roundCompleteClip, true, true))
        {
            RoundCompletePlayedCount++;
        }
    }

    void OnVictoryReached()
    {
        if (PlayClip(victoryClip, true, true))
        {
            VictoryPlayedCount++;
        }
    }

    public bool NotifyTowerInstalled()
    {
        if (!PlayClip(towerInstallClip, true, true))
            return false;

        TowerInstallPlayedCount++;
        return true;
    }

    public bool NotifyUpgradeSucceeded()
    {
        if (!PlayClip(upgradeSuccessClip, true, true))
            return false;

        UpgradeSuccessPlayedCount++;
        return true;
    }

    public bool NotifyUpgradeFailed()
    {
        if (Time.unscaledTime < nextUpgradeFailure || !PlayClip(upgradeFailureClip, true, true))
            return false;

        nextUpgradeFailure = Time.unscaledTime + .2f;
        UpgradeFailurePlayedCount++;
        return true;
    }

    public bool NotifyBossSpawn(int enemyId)
    {
        if (!isActiveAndEnabled || enemyId <= 0 || !announcedBosses.Add(enemyId))
            return false;
        // Muted appearances remain announced: do not replay them after unmuting.
        if (!PlayClip(bossSpawnClip, true, true))
            return false;

        BossSpawnPlayedCount++;
        return true;
    }

    void OnMonsterHit(EnemySnapshot target, int amount)
    {
        // A lethal hit gets only the selected death clip, even during the hit cooldown.
        if (amount <= 0 || target.Exited)
            return;

        if (target.Health <= 0)
        {
            // 처형·빙결 파쇄 처치는 그 효과음이 죽는 소리를 대신한다. 치명타 처치는 죽는 소리만.
            if (session != null && session.HitReplacesDeath)
                return;

            if (Time.unscaledTime < nextMonsterDeath || !PlayClip(monsterDeathClip))
                return;

            nextMonsterDeath = Time.unscaledTime + .1f;
            MonsterDeathPlayedCount++;
            return;
        }
        if (target.Enraged && acceleratedBosses.Add(target.Id))
        {
            // The transition replaces this ordinary hit. Muted transitions are consumed too.
            if (PlayClip(bossAccelerationClip, true, true))
            {
                BossAccelerationPlayedCount++;
            }

            return;
        }
        // Bound the whole hit bus, not each enemy.
        if (Time.unscaledTime < nextMonsterHit)
            return;

        // S18 치명타음은 피격음을 대신한다(같은 줄 0.1초).
        bool crit = session != null && session.HitCritical;
        if (crit ? !PlayCue("crit") : !PlayClip(monsterHitClip))
            return;

        nextMonsterHit = Time.unscaledTime + .1f;
        MonsterHitPlayedCount++;
    }

    public bool NotifyCoreDamage(int amount)
    {
        if (amount <= 0 || Time.unscaledTime < nextCoreDamage)
            return false;

        if (!PreviewCoreDamage())
            return false;

        nextCoreDamage = Time.unscaledTime + .3f;
        CoreDamagePlayedCount++;
        return true;
    }

    bool PlayClip(AudioClip clip, bool important = false, bool protectCore = false)
    {
        if (!isActiveAndEnabled || clip == null)
            return false;

        var cue = CoreGuardianAudioCatalog.ForClip(clip);
        if (cue == null || cue.Volume <= 0)
            return false;

        AudioSource voice = null;
        foreach (var candidate in voices)
        {
            if (candidate != null && !candidate.isPlaying)
            {
                voice = candidate;
                break;
            }
        }
        // Core danger remains audible when attacks occupy the bounded pool.
        if (voice == null && important)
        {
            // A following core arrival/UI cue cannot erase terminal victory or defeat.
            // Lowest priority wins, first index on ties: the same pick as the stable OrderBy it replaces (F14).
            int index = -1,
                lowest = 0;
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] == null || protectCore && voices[i].clip == coreDamageClip)
                    continue;
                int priority = voiceCues[i]?.priority ?? 0;
                if (priority > cue.priority)
                    continue;
                if (index < 0 || priority < lowest)
                {
                    index = i;
                    lowest = priority;
                }
            }
            if (index >= 0)
            {
                voice = voices[index];
                voice.Stop();
            }
        }
        if (voice == null)
        {
            DroppedCount++;
            return false;
        }
        voice.clip = clip;
        voiceCues[System.Array.IndexOf(voices, voice)] = cue;
        voice.volume = cue.Volume;
        if (important && music != null)
        {
            music.Duck(Mathf.Min(clip.length, .8f));
        }

        voice.Play();
        PlayedCount++;
        return true;
    }
}
