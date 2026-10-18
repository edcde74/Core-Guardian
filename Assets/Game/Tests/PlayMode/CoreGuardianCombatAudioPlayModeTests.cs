using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianCombatAudioPlayModeTests
{
    [UnityTest]
    public IEnumerator BossHalfHealthPlaysSpeedUpOnceAndNeverOnLethalDamage()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var gameType=Type.GetType("CoreGuardianPrototype, Assembly-CSharp");
        var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(gameType,FindObjectsSortMode.None).Single();
        var type=Type.GetType("CoreGuardianCombatAudio, Assembly-CSharp");
        Assert.That(type.GetField("BossAccelerationResourcePath"),Is.Not.Null,"Selected boss acceleration audio is missing");
        var audio=(MonoBehaviour)game.GetComponent(type);
        bool had=PlayerPrefs.HasKey("CoreGuardian.SoundVolume");float volume=PlayerPrefs.GetFloat("CoreGuardian.SoundVolume");float time=Time.timeScale;
        try
        {
            Time.timeScale=0;game.enabled=false;PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);
            var session=(Session)gameType.GetProperty("Session").GetValue(game);session.StartDiagnosticEncounter();
            var count=type.GetProperty("BossAccelerationPlayedCount");var hits=type.GetProperty("MonsterHitPlayedCount");
            var boss=session.DebugSpawn(EnemyKind.Boss);session.ApplyDamage(boss,1,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(0));
            var voices=audio.GetComponentsInChildren<AudioSource>();foreach(var voice in voices)voice.Stop();
            type.GetMethod("PreviewCoreDamage").Invoke(audio,null);for(int i=0;i<3;i++)type.GetMethod("Preview").Invoke(audio,null);
            boss.Health=boss.Spec.Health/2+1;Assert.That(boss.Enraged,Is.False);int beforeHits=(int)hits.GetValue(audio);
            session.ApplyDamage(boss,2,"Player");Assert.That(boss.Enraged,Is.True);Assert.That(boss.MovementSpeed,Is.EqualTo(boss.Spec.Speed*1.3f).Within(.00001f));
            Assert.That(count.GetValue(audio),Is.EqualTo(1));Assert.That(hits.GetValue(audio),Is.EqualTo(beforeHits),"Transition replaces ordinary hit audio");
            var clip=Resources.Load<AudioClip>((string)type.GetField("BossAccelerationResourcePath").GetRawConstantValue());Assert.That(clip,Is.Not.Null);
            Assert.That(voices.Any(v=>v.isPlaying&&v.clip==clip),Is.True);Assert.That(voices.Any(v=>v.isPlaying&&v.clip.name=="06_Aura_Glyph_2"),Is.True);
            type.GetMethod("Configure").Invoke(audio,new object[]{session});session.ApplyDamage(boss,1,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(1));
            session.ApplyDamage(session.DebugSpawn(EnemyKind.Boss),10000,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(1),"Lethal damage never announces acceleration");
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",0f);var muted=session.DebugSpawn(EnemyKind.Boss);muted.Health=muted.Spec.Health/2+1;session.ApplyDamage(muted,2,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(1));
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);foreach(var voice in voices)voice.Stop();audio.enabled=false;audio.enabled=true;
            session.ApplyDamage(muted,1,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(1),"Muted transition must not replay later");
            var other=session.DebugSpawn(EnemyKind.Boss);other.Health=other.Spec.Health/2+1;session.ApplyDamage(other,2,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(2));
            yield return null;Assert.That(voices.Where(v=>v.isPlaying).All(v=>Mathf.Abs(v.volume-.37f*.55f)<.00001f),Is.True);
        }
        finally{Time.timeScale=time;if(had)PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",volume);else PlayerPrefs.DeleteKey("CoreGuardian.SoundVolume");PlayerPrefs.Save();}
#else
        yield return null;
#endif
    }
    [UnityTest]
    public IEnumerator BossViewAppearsWithEncounterOnceAndRespectsMuteAndCorePriority()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var gameType=Type.GetType("CoreGuardianPrototype, Assembly-CSharp");
        var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(gameType,FindObjectsSortMode.None).Single();
        var type=Type.GetType("CoreGuardianCombatAudio, Assembly-CSharp");
        Assert.That(type.GetField("BossSpawnResourcePath"),Is.Not.Null,"Selected boss spawn audio is missing");
        var audio=(MonoBehaviour)game.GetComponent(type);
        bool had=PlayerPrefs.HasKey("CoreGuardian.SoundVolume");float volume=PlayerPrefs.GetFloat("CoreGuardian.SoundVolume");float time=Time.timeScale;
        try
        {
            Time.timeScale=0;game.enabled=false;PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);
            var session=(Session)gameType.GetProperty("Session").GetValue(game);session.StartDiagnosticEncounter();
            var sync=gameType.GetMethod("SynchronizeViews",BindingFlags.Instance|BindingFlags.NonPublic);
            var count=type.GetProperty("BossSpawnPlayedCount");var notify=type.GetMethod("NotifyBossSpawn");
            session.DebugSpawn(EnemyKind.Attack);sync.Invoke(game,null);Assert.That(count.GetValue(audio),Is.EqualTo(0));
            var voices=audio.GetComponentsInChildren<AudioSource>();
            type.GetMethod("PreviewCoreDamage").Invoke(audio,null);for(int i=0;i<3;i++)type.GetMethod("Preview").Invoke(audio,null);
            var boss=session.DebugSpawn(EnemyKind.Boss);sync.Invoke(game,null);
            var clip=Resources.Load<AudioClip>((string)type.GetField("BossSpawnResourcePath").GetRawConstantValue());
            Assert.That(clip,Is.Not.Null);Assert.That(count.GetValue(audio),Is.EqualTo(1));
            Assert.That(voices.Count(v=>v.isPlaying&&v.clip==clip),Is.EqualTo(1));
            Assert.That(voices.Any(v=>v.isPlaying&&v.clip.name=="06_Aura_Glyph_2"),Is.True,"Boss must not stop the core warning");
            sync.Invoke(game,null);Assert.That(count.GetValue(audio),Is.EqualTo(1));
            type.GetMethod("Configure").Invoke(audio,new object[]{session});
            Assert.That(notify.Invoke(audio,new object[]{boss.Id}),Is.False,"Reconfiguring the same session must not replay");
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",0f);var muted=session.DebugSpawn(EnemyKind.Boss);sync.Invoke(game,null);Assert.That(count.GetValue(audio),Is.EqualTo(1));
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);foreach(var voice in voices)voice.Stop();
            Assert.That(notify.Invoke(audio,new object[]{muted.Id}),Is.False,"Muted spawn must not be delayed until unmute");
            audio.enabled=false;audio.enabled=true;Assert.That(notify.Invoke(audio,new object[]{boss.Id}),Is.False);
            session.DebugSpawn(EnemyKind.Boss);sync.Invoke(game,null);Assert.That(count.GetValue(audio),Is.EqualTo(2));
            yield return null;Assert.That(voices.Where(v=>v.isPlaying).All(v=>Mathf.Abs(v.volume-.37f*.55f)<.00001f),Is.True);
            Assert.That(voices.All(v=>!v.loop&&v.pitch==1),Is.True);
        }
        finally{Time.timeScale=time;if(had)PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",volume);else PlayerPrefs.DeleteKey("CoreGuardian.SoundVolume");PlayerPrefs.Save();}
#else
        yield return null;
#endif
    }
    [UnityTest]
    public IEnumerator MonsterDeathsReplaceLethalHitAndIgnoreCoreExitsMuteAndRepeatedDamage()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var gameType=Type.GetType("CoreGuardianPrototype, Assembly-CSharp");
        var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(gameType,FindObjectsSortMode.None).Single();
        var type=Type.GetType("CoreGuardianCombatAudio, Assembly-CSharp");
        Assert.That(type.GetField("MonsterDeathResourcePath"),Is.Not.Null,"Selected monster death audio is missing");
        var audio=(MonoBehaviour)game.GetComponent(type);
        bool had=PlayerPrefs.HasKey("CoreGuardian.SoundVolume");float volume=PlayerPrefs.GetFloat("CoreGuardian.SoundVolume");float time=Time.timeScale;
        try
        {
            Time.timeScale=0;game.enabled=false;PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);
            var session=(Session)gameType.GetProperty("Session").GetValue(game);
            session.StartDiagnosticEncounter();session.SetBoard(20,20,new Vector2Int(10,0),new[]{Vector2Int.zero,new Vector2Int(10,0)});
            var configure=type.GetMethod("Configure");configure.Invoke(audio,new object[]{session});configure.Invoke(audio,new object[]{session});
            var count=type.GetProperty("MonsterDeathPlayedCount");var hits=type.GetProperty("MonsterHitPlayedCount");
            var enemy=session.DebugSpawn(EnemyKind.Attack);session.ApplyDamage(enemy,1,"Player");
            Assert.That(hits.GetValue(audio),Is.EqualTo(1));
            var voices=audio.GetComponentsInChildren<AudioSource>();foreach(var voice in voices)voice.Stop();
            int gold=session.Gold;int reward=enemy.Spec.Gold;
            session.ApplyDamage(enemy,10000,"Tower:1");
            Assert.That(enemy.Health,Is.Zero);Assert.That(session.Gold,Is.EqualTo(gold+reward));
            Assert.That(count.GetValue(audio),Is.EqualTo(1));Assert.That(hits.GetValue(audio),Is.EqualTo(1),"Lethal damage must not add the hit clip");
            var clip=Resources.Load<AudioClip>((string)type.GetField("MonsterDeathResourcePath").GetRawConstantValue());
            Assert.That(voices.Count(v=>v.isPlaying&&v.clip==clip),Is.EqualTo(1));
            session.ApplyDamage(enemy,10000,"Player");
            session.ApplyDamage(session.DebugSpawn(EnemyKind.Attack),10000,"Player");
            Assert.That(count.GetValue(audio),Is.EqualTo(1),"Same-time kills are coalesced; dead targets cannot replay");
            yield return new WaitForSecondsRealtime(.12f);
            session.StartCombat();session.DebugSpawn(EnemyKind.Attack,9.99f);session.Tick(.1f,new Vector2(-100,-100));
            Assert.That(session.CoreEnergy,Is.LessThan(session.CoreMaximum));Assert.That(count.GetValue(audio),Is.EqualTo(1),"Core exit is not a kill");
            foreach(var voice in voices)voice.Stop();
            for(int i=0;i<4;i++)type.GetMethod("Preview").Invoke(audio,null);
            session.ApplyDamage(session.DebugSpawn(EnemyKind.Attack),10000,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(1),"Full pool drops ordinary death audio");
            foreach(var voice in voices)voice.Stop();
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",0f);session.ApplyDamage(session.DebugSpawn(EnemyKind.Attack),10000,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(1));
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);audio.enabled=false;
            session.ApplyDamage(session.DebugSpawn(EnemyKind.Attack),10000,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(1));
            audio.enabled=true;session.ApplyDamage(session.DebugSpawn(EnemyKind.Attack),10000,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(2));
            yield return null;Assert.That(voices.Where(v=>v.isPlaying).All(v=>Mathf.Abs(v.volume-.37f*.55f)<.00001f),Is.True);
        }
        finally{Time.timeScale=time;if(had)PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",volume);else PlayerPrefs.DeleteKey("CoreGuardian.SoundVolume");PlayerPrefs.Save();}
#else
        yield return null;
#endif
    }
    [UnityTest]
    public IEnumerator MonsterHitsUseSelectedClipAndBoundOverlapWithoutChangingDamage()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var gameType=Type.GetType("CoreGuardianPrototype, Assembly-CSharp");
        var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(gameType,FindObjectsSortMode.None).Single();
        var type=Type.GetType("CoreGuardianCombatAudio, Assembly-CSharp");
        Assert.That(type.GetField("MonsterHitResourcePath"),Is.Not.Null,"Selected monster hit audio is missing");
        var audio=(MonoBehaviour)game.GetComponent(type);
        bool had=PlayerPrefs.HasKey("CoreGuardian.SoundVolume");float volume=PlayerPrefs.GetFloat("CoreGuardian.SoundVolume");float time=Time.timeScale;
        try
        {
            Time.timeScale=0;game.enabled=false;PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);
            var session=(Session)gameType.GetProperty("Session").GetValue(game);
            session.StartDiagnosticEncounter();
            var configure=type.GetMethod("Configure");configure.Invoke(audio,new object[]{session});configure.Invoke(audio,new object[]{session});
            var count=type.GetProperty("MonsterHitPlayedCount");
            var enemy=session.DebugSpawn(EnemyKind.Attack);float hp=enemy.Health;
            session.ApplyDamage(enemy,0,"Player");session.ApplyDamage(null,10,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(0));
            session.ApplyDamage(enemy,1,"Player");Assert.That(enemy.Health,Is.EqualTo(hp-1));
            Assert.That(count.GetValue(audio),Is.EqualTo(1),"Configure twice must not double-subscribe");
            var clip=Resources.Load<AudioClip>((string)type.GetField("MonsterHitResourcePath").GetRawConstantValue());
            var voices=audio.GetComponentsInChildren<AudioSource>();
            Assert.That(voices.Any(v=>v.isPlaying&&v.clip==clip),Is.True);
            session.ApplyDamage(enemy,1,"Tower:1");Assert.That(count.GetValue(audio),Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(.12f);
            session.ApplyDamage(enemy,10000,"Tower:1");Assert.That(enemy.Alive,Is.False);Assert.That(count.GetValue(audio),Is.EqualTo(1),"Lethal hit uses the separate death clip");
            yield return new WaitForSecondsRealtime(.12f);
            session.ApplyDamage(enemy,10000,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(1),"Dead targets must not play again");
            foreach(var voice in voices)voice.Stop();
            for(int i=0;i<4;i++)type.GetMethod("Preview").Invoke(audio,null);
            var other=session.DebugSpawn(EnemyKind.Attack);session.ApplyDamage(other,1,"Player");
            Assert.That(count.GetValue(audio),Is.EqualTo(1),"Full pool drops ordinary hit audio");
            Assert.That(voices.All(v=>v.clip.name=="03_Aura"),Is.True,"Hit must not replace attacks");
            foreach(var voice in voices)voice.Stop();
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",0f);session.ApplyDamage(other,1,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(1));
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);
            audio.enabled=false;session.ApplyDamage(other,1,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(1));
            audio.enabled=true;session.ApplyDamage(other,1,"Player");Assert.That(count.GetValue(audio),Is.EqualTo(2));
            yield return null;
            Assert.That(voices.Where(v=>v.isPlaying).All(v=>Mathf.Abs(v.volume-.37f*.55f)<.00001f),Is.True);
        }
        finally{Time.timeScale=time;if(had)PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",volume);else PlayerPrefs.DeleteKey("CoreGuardian.SoundVolume");PlayerPrefs.Save();}
#else
        yield return null;
#endif
    }
    [UnityTest]
    public IEnumerator CoreArrivalPlaysSelectedGlyphAndHonorsMuteCooldownAndFullPool()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var gameType=Type.GetType("CoreGuardianPrototype, Assembly-CSharp");
        var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(gameType,FindObjectsSortMode.None).Single();
        var type=Type.GetType("CoreGuardianCombatAudio, Assembly-CSharp");
        Assert.That(type.GetField("CoreDamageResourcePath"),Is.Not.Null,"Selected core damage audio is missing");
        var audio=game.GetComponent(type);
        bool had=PlayerPrefs.HasKey("CoreGuardian.SoundVolume");float volume=PlayerPrefs.GetFloat("CoreGuardian.SoundVolume");
        float time=Time.timeScale;
        try
        {
            Time.timeScale=0;game.enabled=false;PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);
            var session=(Session)gameType.GetProperty("Session").GetValue(game);
            session.StartDiagnosticEncounter();
            session.SetBoard(20,20,new Vector2Int(10,0),new[]{Vector2Int.zero,new Vector2Int(10,0)});
            gameType.GetField("previousCore",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,session.CoreEnergy);
            var sources=audio.GetComponentsInChildren<AudioSource>();
            for(int i=0;i<4;i++)Assert.That(type.GetMethod("Preview").Invoke(audio,null),Is.True);
            session.StartCombat();session.DebugSpawn(EnemyKind.Attack,9.99f);
            session.Tick(.1f,new Vector2(-100,-100));
            gameType.GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
            var clip=Resources.Load<AudioClip>((string)type.GetField("CoreDamageResourcePath").GetRawConstantValue());
            Assert.That(clip,Is.Not.Null);
            Assert.That(sources.Any(s=>s.isPlaying&&s.clip==clip),Is.True,"Actual core arrival must be audible with a full attack pool");
            var count=type.GetProperty("CoreDamagePlayedCount");Assert.That(count.GetValue(audio),Is.EqualTo(1));
            var notify=type.GetMethod("NotifyCoreDamage");
            Assert.That(notify.Invoke(audio,new object[]{2}),Is.False,"Rapid arrivals are coalesced");
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(notify.Invoke(audio,new object[]{0}),Is.False);
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",0f);
            yield return null;
            Assert.That(notify.Invoke(audio,new object[]{2}),Is.False);
            Assert.That(sources.All(s=>s.volume==0),Is.True);
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);
            Assert.That(notify.Invoke(audio,new object[]{2}),Is.True);
            yield return null; // Existing active voices receive slider changes in Update.
            Assert.That(sources.Where(s=>s.isPlaying).All(s=>Mathf.Abs(s.volume-.37f*.55f)<.00001f),Is.True);
            ((MonoBehaviour)audio).enabled=false;
            Assert.That(sources.All(s=>!s.isPlaying),Is.True);
        }
        finally
        {
            Time.timeScale=time;
            if(had)PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",volume);else PlayerPrefs.DeleteKey("CoreGuardian.SoundVolume");
            PlayerPrefs.Save();
        }
#else
        yield return null;
#endif
    }
    [UnityTest]
    public IEnumerator RealSingleTowerLaunchesAndDeveloperControlsRespectMuteAndVoiceLimit()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var gameType=Type.GetType("CoreGuardianPrototype, Assembly-CSharp");
        var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(gameType,FindObjectsSortMode.None).Single();
        var audioType=Type.GetType("CoreGuardianCombatAudio, Assembly-CSharp");
        var settings=Type.GetType("CoreGuardianSoundSettings, Assembly-CSharp");
        var audio=(MonoBehaviour)game.GetComponent(audioType);
        var volume=settings.GetProperty("Volume");var candidate=settings.GetProperty("Candidate");
        bool hadVolume=PlayerPrefs.HasKey("CoreGuardian.SoundVolume"),hadCandidate=PlayerPrefs.HasKey("CoreGuardian.ShotCandidate");
        float oldVolume=PlayerPrefs.GetFloat("CoreGuardian.SoundVolume");int oldCandidate=PlayerPrefs.GetInt("CoreGuardian.ShotCandidate");
        float oldTime=Time.timeScale;
        try
        {
            Time.timeScale=0;game.enabled=false;
            volume.SetValue(null,.65f);candidate.SetValue(null,0);
            var session=(Session)gameType.GetProperty("Session").GetValue(game);
            // Injected functional QA: isolated board/resources, real Session shots. No balance claim.
            session.StartDiagnosticEncounter();
            session.SetBoard(30,30,new Vector2Int(20,20),new[]{Vector2Int.zero,new Vector2Int(20,0),new Vector2Int(20,20)});
            string reason;var tower=session.TryInstall(TowerKind.Single,new Vector2Int(1,1),out reason);
            Assert.That(tower,Is.Not.Null,reason);
            var multi=session.TryInstall(TowerKind.Multi,new Vector2Int(2,1),out reason);Assert.That(multi,Is.Not.Null,reason);
            var multiTwo=session.TryInstall(TowerKind.Multi,new Vector2Int(2,2),out reason);Assert.That(multiTwo,Is.Not.Null,reason);
            var support=session.TryInstall(TowerKind.Support,new Vector2Int(1,2),out reason);Assert.That(support,Is.Not.Null,reason);
            var played=audioType.GetProperty("PlayedCount");
            audioType.GetMethod("Configure").Invoke(audio,new object[]{session});
            audioType.GetMethod("Configure").Invoke(audio,new object[]{session});
            int singleShots=0,multiShots=0,multiTwoShots=0;
            session.ProjectileLaunched+=p=>{if(p.Source=="Tower:"+tower.Id)singleShots++;if(p.Source=="Tower:"+multi.Id)multiShots++;if(p.Source=="Tower:"+multiTwo.Id)multiTwoShots++;};
            session.StartCombat();
            foreach(float distance in new[]{0f,1f,2f})session.DebugSpawn(EnemyKind.Defense,distance).Health=10000;
            session.Tick(.05f,new Vector2(-100,-100));
            Assert.That(singleShots,Is.EqualTo(1));Assert.That(multiShots,Is.EqualTo(3));Assert.That(multiTwoShots,Is.GreaterThan(0));
            Assert.That((int)played.GetValue(audio),Is.EqualTo(3),"Single plus one audio volley per multi tower, with support silent");
            var voices=audio.GetComponentsInChildren<AudioSource>();Assert.That(voices.Length,Is.EqualTo(4));
            Assert.That(voices.Count(v=>v.isPlaying&&v.clip.name=="B_BrighterAttack"),Is.EqualTo(2));
            foreach(var source in voices){Assert.That(source.loop,Is.False);Assert.That(source.spatialBlend,Is.Zero);}
            yield return new WaitForSecondsRealtime(.85f);
            // Before tower cooldowns expire, a real player launch uses approved candidate3.
            int beforePlayer=(int)played.GetValue(audio);
            session.Tick(.01f,new Vector2(1,1));
            Assert.That((int)played.GetValue(audio),Is.EqualTo(beforePlayer+1));
            Assert.That(voices.Any(v=>v.isPlaying&&v.clip.name=="03_Glyph"),Is.True);
            foreach(var voice in voices)voice.Stop();
            var preview=audioType.GetMethod("Preview");
            for(int i=0;i<3;i++)
            {
                candidate.SetValue(null,i);Assert.That(preview.Invoke(audio,null),Is.EqualTo(true));
                Assert.That(voices.Any(v=>v.isPlaying&&v.clip.name=="03_Aura"),Is.True,"Old candidate preferences must not change approved single audio");
            }
            preview.Invoke(audio,null);
            Assert.That(preview.Invoke(audio,null),Is.EqualTo(false),"Pool must reject a fifth overlapping sound");
            audio.enabled=false;Assert.That(voices.All(v=>!v.isPlaying),Is.True);audio.enabled=true;
            Assert.That(audioType.GetMethod("PreviewMulti").Invoke(audio,null),Is.EqualTo(true));
            Assert.That(voices.Any(v=>v.isPlaying&&v.clip.name=="B_BrighterAttack"),Is.True);
            foreach(var voice in voices)voice.Stop();
            var canvas=game.transform.Find("CombatHUD");
            var buttonType=Type.GetType("UnityEngine.UI.Button, UnityEngine.UI");
            var sliderType=Type.GetType("UnityEngine.UI.Slider, UnityEngine.UI");
            var menu=canvas.Find("DeveloperMenu").GetComponent(buttonType);
            yield return new WaitForSecondsRealtime(.25f); // HUD phase refresh closes developer tools on phase changes.
            var click=buttonType.GetProperty("onClick").GetValue(menu);click.GetType().GetMethod("Invoke").Invoke(click,null);
            yield return new WaitForSecondsRealtime(.25f);
            var panel=canvas.Find("SoundControls");Assert.That(panel.gameObject.activeInHierarchy,Is.True);
            Assert.That(panel.Find("Listen_Support"),Is.Null);
            Assert.That(panel.Find("PreviewPlayer"),Is.Not.Null);
            Assert.That(panel.Find("PreviewMulti"),Is.Not.Null);
            Assert.That(panel.Find("Audition_Multi"),Is.Null);
            var slider=panel.Find("SoundVolume").GetComponent(sliderType);
            var fill=(RectTransform)sliderType.GetProperty("fillRect").GetValue(slider);
            var handle=(RectTransform)sliderType.GetProperty("handleRect").GetValue(slider);
            Assert.That(fill.rect.height,Is.LessThanOrEqualTo(8.01f),"Thin track must stay inside the panel");
            Assert.That(handle.rect.height,Is.LessThanOrEqualTo(28.01f));
            sliderType.GetProperty("value").SetValue(slider,0f);yield return null;
            Assert.That(volume.GetValue(null),Is.EqualTo(0f));Assert.That(voices.All(v=>v.volume==0),Is.True);
            Assert.That(preview.Invoke(audio,null),Is.EqualTo(false));
            sliderType.GetProperty("value").SetValue(slider,.37f);yield return null;
            Assert.That(voices.All(v=>Mathf.Abs(v.volume-.37f*.55f)<.00001f),Is.True);
            Assert.That(PlayerPrefs.GetFloat("CoreGuardian.SoundVolume"),Is.EqualTo(.37f));
            click.GetType().GetMethod("Invoke").Invoke(click,null);yield return new WaitForSecondsRealtime(.25f);
            Assert.That(panel.gameObject.activeSelf,Is.False);
        }
        finally
        {
            Time.timeScale=oldTime;
            if(hadVolume)PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",oldVolume);else PlayerPrefs.DeleteKey("CoreGuardian.SoundVolume");
            if(hadCandidate)PlayerPrefs.SetInt("CoreGuardian.ShotCandidate",oldCandidate);else PlayerPrefs.DeleteKey("CoreGuardian.ShotCandidate");
            PlayerPrefs.Save();
        }
#else
        yield return null;
#endif
    }
}
