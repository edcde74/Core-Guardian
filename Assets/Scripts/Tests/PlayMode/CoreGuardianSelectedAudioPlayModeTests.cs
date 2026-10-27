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

// Injected functional QA only: no balance or human listening claim.
[Category("ThirtyExperiment")]
public class CoreGuardianSelectedAudioPlayModeTests
{
    MonoBehaviour game,audio;
    Type gameType,audioType,settings,category;
    float oldTime;string[] keys;bool[] existed;float[] saved;
    AudioSource[] Voices=>audio.GetComponentsInChildren<AudioSource>().Where(v=>v.name.StartsWith("MagicCannonVoice_")).ToArray();
    object Call(string method,params object[] args)=>audioType.GetMethod(method).Invoke(audio,args);
    void Category(string name,float value)=>settings.GetMethod("SetCategoryVolume").Invoke(null,new[]{Enum.Parse(category,name),(object)value});
    void ClearVoices(){foreach(var voice in Voices)voice.Stop();}
    Session Current=>(Session)gameType.GetProperty("Session").GetValue(game);
    CombatData Data()=>CombatData.Load(Resources.Load<TextAsset>("CoreGuardian/enemies").text,Resources.Load<TextAsset>("CoreGuardian/towers").text,Resources.Load<TextAsset>("CoreGuardian/stage").text,Resources.Load<TextAsset>("CoreGuardian/upgrades").text,Resources.Load<TextAsset>("CoreGuardian/waves").text);
    Session Bind(CombatData data)
    {
        var s=new Session(data,123);s.SetBoard(20,20,new Vector2Int(10,0),new[]{Vector2Int.zero,new Vector2Int(10,0)});
        Call("Configure",s);Call("Configure",s);ClearVoices();return s;
    }
    static void Finish(Session s)
    {
        s.Tick(.01f,new Vector2(-100,-100));foreach(var e in s.Enemies.ToArray())s.ApplyDamage(e,10000,"Player");s.Tick(.01f,new Vector2(-100,-100));
    }
    [UnitySetUp]
    public IEnumerator SetUp()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
        yield return null;
        gameType=Type.GetType("CoreGuardianPrototype, Assembly-CSharp");audioType=Type.GetType("CoreGuardianCombatAudio, Assembly-CSharp");
        game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(gameType,FindObjectsSortMode.None).Single();audio=(MonoBehaviour)game.GetComponent(audioType);
        settings=Type.GetType("CoreGuardianSoundSettings, Assembly-CSharp");category=Type.GetType("CoreGuardianAudioCategory, Assembly-CSharp");
        keys=Enum.GetNames(category).Select(n=>"CoreGuardian.Audio."+n).Concat(new[]{"CoreGuardian.SoundVolume"}).ToArray();
        existed=keys.Select(PlayerPrefs.HasKey).ToArray();saved=keys.Select(k=>PlayerPrefs.GetFloat(k)).ToArray();oldTime=Time.timeScale;
        game.enabled=false;Time.timeScale=0;PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);
        foreach(string name in Enum.GetNames(category))Category(name,1);ClearVoices();
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale=oldTime;
        if(keys!=null)for(int i=0;i<keys.Length;i++){if(existed[i])PlayerPrefs.SetFloat(keys[i],saved[i]);else PlayerPrefs.DeleteKey(keys[i]);}
        PlayerPrefs.Save();yield return null;
    }
    [UnityTest]
    public IEnumerator LiveCategoryChangesOnlyAffectTheirOwnVoicesAndMasterStillMutesEverything()
    {
        Assert.That(Call("Preview"),Is.True);Assert.That(Call("PreviewPlayer"),Is.True);
        Category("Tower",.25f);Category("Player",.5f);yield return null;
        Assert.That(Voices.Single(v=>v.clip!=null&&v.clip.name=="single").volume,Is.EqualTo(.37f*.55f*.25f).Within(.00001f));
        Assert.That(Voices.Single(v=>v.clip!=null&&v.clip.name=="player").volume,Is.EqualTo(.37f*.55f*.5f).Within(.00001f));
        Category("Tower",0);Assert.That(Call("Preview"),Is.False);Assert.That(Call("PlayCue","ui_open"),Is.True);
        yield return null;Assert.That(Voices.Single(v=>v.clip!=null&&v.clip.name=="single").volume,Is.Zero);
        PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",0);yield return null;
        Assert.That(audio.GetComponentsInChildren<AudioSource>().All(v=>v.volume==0),Is.True);
        Assert.That(Call("PlayCue","aug_gold"),Is.False);Assert.That(Call("PreviewPlayer"),Is.False);
    }
    [UnityTest]
    public IEnumerator ActualMultiVolleysUseExactlyOneMatchingOneTwoOrThreeShotClipWithSupportSilent()
    {
        for(int count=1;count<=3;count++)
        {
            var data=Data();data.Waves.Clear();var s=Bind(data);typeof(Session).GetProperty("Crystals").SetValue(s,10000);
            string reason;var tower=s.TryInstall(TowerKind.Multi,new Vector2Int(1,1),out reason);Assert.That(tower,Is.Not.Null,reason);
            Assert.That(s.TryInstall(TowerKind.Support,new Vector2Int(1,2),out reason),Is.Not.Null,reason);
            int launches=0,batches=0,reported=0;
            s.ProjectileLaunched+=p=>{if(p.Source=="Tower:"+tower.Id)launches++;};
            s.AttackLaunched+=(source,n)=>{if(source=="Tower:"+tower.Id){batches++;reported=n;}};
            for(int n=0;n<count;n++)s.DebugSpawn(EnemyKind.Defense,n*.2f).Health=10000;
            s.StartCombat();ClearVoices();int before=(int)audioType.GetProperty("PlayedCount").GetValue(audio);
            s.Tick(.01f,new Vector2(-100,-100));
            Assert.That(launches,Is.EqualTo(count));Assert.That(batches,Is.EqualTo(1));Assert.That(reported,Is.EqualTo(count));
            Assert.That((int)audioType.GetProperty("PlayedCount").GetValue(audio)-before,Is.EqualTo(1));
            Assert.That(Voices.Count(v=>v.isPlaying&&v.clip.name=="multi_"+count),Is.EqualTo(1));
            Assert.That(audioType.GetProperty("LastMultiShotCount").GetValue(audio),Is.EqualTo(count));
            yield return null;
        }
    }
    [UnityTest]
    public IEnumerator MusicCrossfadesAtRealAugmentationAndReturnsThenStopsAtFinalVictory()
    {
        var data=Data();data.Waves.Clear();// Augmentation cadence from data (every 3 rounds): round 9 opens a choice, round 10 is final.
        data.Waves.Add(new WaveSpec{Wave=9,Kind=EnemyKind.Boss,Count=1,Interval=1});data.Waves.Add(new WaveSpec{Wave=10,Kind=EnemyKind.Attack,Count=1,Interval=1});
        var s=Bind(data);typeof(Session).GetProperty("Wave").SetValue(s,9);
        var musicType=Type.GetType("CoreGuardianMusicAudio, Assembly-CSharp");var music=audio.GetComponentInChildren(musicType);
        yield return new WaitForSecondsRealtime(.45f);
        Assert.That(musicType.GetProperty("CurrentCue").GetValue(music),Is.EqualTo("bgm"));
        var tracks=music.GetComponentsInChildren<AudioSource>();Assert.That(tracks.Length,Is.EqualTo(2));
        float battleTime=tracks.Single(v=>v.clip.name=="bgm").time;
        s.StartCombat();Finish(s);Assert.That(s.Phase,Is.EqualTo(SessionPhase.Augmentation));
        yield return new WaitForSecondsRealtime(.45f);
        Assert.That(musicType.GetProperty("CurrentCue").GetValue(music),Is.EqualTo("aug_bgm"));
        Assert.That(tracks.Single(v=>v.clip.name=="bgm").isPlaying,Is.False);
        Category("Music",0);yield return null;Assert.That(tracks.All(v=>v.volume==0),Is.True);
        Assert.That(Call("Preview"),Is.True,"Music mute must leave tower effects audible");Category("Music",1);
        Assert.That(s.ChooseAugmentation(0),Is.True);yield return new WaitForSecondsRealtime(.45f);
        Assert.That(musicType.GetProperty("CurrentCue").GetValue(music),Is.EqualTo("bgm"));
        Assert.That(tracks.Single(v=>v.clip.name=="bgm").time,Is.GreaterThan(battleTime));
        s.StartCombat();Finish(s);Assert.That(s.Phase,Is.EqualTo(SessionPhase.Victory));
        yield return new WaitForSecondsRealtime(.45f);Assert.That(tracks.All(v=>!v.isPlaying&&v.volume==0),Is.True);
        Assert.That(audioType.GetProperty("VictoryPlayedCount").GetValue(audio),Is.EqualTo(1));
        Assert.That(audioType.GetProperty("RoundCompletePlayedCount").GetValue(audio),Is.EqualTo(1));
    }
    [UnityTest]
    public IEnumerator TerminalDefeatSurvivesTheAdaptersFollowingCoreDamageWhenEffectsPoolIsFull()
    {
        var s=Current;s.SetBoard(20,20,new Vector2Int(10,0),new[]{Vector2Int.zero,new Vector2Int(10,0)});
        typeof(Session).GetProperty("CoreEnergy").SetValue(s,1);
        gameType.GetField("previousCore",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,1);
        s.StartCombat();ClearVoices();for(int i=0;i<4;i++)Assert.That(Call("Preview"),Is.True);
        s.DebugSpawn(EnemyKind.Attack,9.99f);s.Tick(.1f,new Vector2(-100,-100));
        gameType.GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        Assert.That(s.Phase,Is.EqualTo(SessionPhase.Defeat));
        Assert.That(Voices.Any(v=>v.isPlaying&&v.clip.name=="defeat"),Is.True,"The lethal core cue must not replace terminal defeat in the same frame");
        Assert.That(Voices.Any(v=>v.isPlaying&&v.clip.name=="core"),Is.True);
        yield return null;
    }
    [UnityTest]
    public IEnumerator DefeatIsOneTerminalCueAndMutedDefeatNeverReplaysOnUnmute()
    {
        var data=Data();data.Waves.Clear();var s=Bind(data);typeof(Session).GetProperty("CoreEnergy").SetValue(s,1);
        s.StartCombat();ClearVoices();s.DebugSpawn(EnemyKind.Attack,9.99f);s.Tick(.1f,new Vector2(-100,-100));
        Assert.That(s.Phase,Is.EqualTo(SessionPhase.Defeat));Assert.That(audioType.GetProperty("DefeatPlayedCount").GetValue(audio),Is.EqualTo(1));
        Assert.That(Voices.Count(v=>v.isPlaying&&v.clip.name=="defeat"),Is.EqualTo(1));s.Tick(1,new Vector2(-100,-100));
        var other=Bind(data);typeof(Session).GetProperty("CoreEnergy").SetValue(other,1);PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",0);
        other.StartCombat();other.DebugSpawn(EnemyKind.Attack,9.99f);other.Tick(.1f,new Vector2(-100,-100));
        PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",.37f);Call("Configure",other);yield return null;
        Assert.That(audioType.GetProperty("DefeatPlayedCount").GetValue(audio),Is.EqualTo(1));
    }
    [UnityTest]
    public IEnumerator RealPlacementFailureDemolitionAndEscapeUseTheirSeparateCues()
    {
        var s=Current;typeof(Session).GetProperty("Crystals").SetValue(s,10000);
        Assert.That(gameType.GetMethod("Place").Invoke(game,new object[]{TowerKind.Single,new Vector2Int(-1,-1)}),Is.False);
        Assert.That(Voices.Any(v=>v.isPlaying&&v.clip.name=="install_fail"),Is.True);ClearVoices();
        var can=gameType.GetMethod("CanInstall",BindingFlags.Instance|BindingFlags.NonPublic);
        var cell=(from y in Enumerable.Range(0,22) from x in Enumerable.Range(0,39) let c=new Vector2Int(x,y) where s.IsBuildable(c)&&(bool)can.Invoke(game,new object[]{c}) select c).First();
        Assert.That(gameType.GetMethod("Place").Invoke(game,new object[]{TowerKind.Single,cell}),Is.True);ClearVoices();
        int before=s.Towers.Count;gameType.GetMethod("DestroySelected").Invoke(game,null);Assert.That(s.Towers.Count,Is.EqualTo(before-1));
        Assert.That(Voices.Any(v=>v.isPlaying&&v.clip.name=="demolish"),Is.True);ClearVoices();
        gameType.GetMethod("ChooseTower").Invoke(game,new object[]{TowerKind.Single});
        var hudType=Type.GetType("CoreGuardianHud, Assembly-CSharp");var hud=game.GetComponent(hudType);
        Assert.That(hudType.GetMethod("CancelInstallation").Invoke(hud,null),Is.True);
        Assert.That(Voices.Any(v=>v.isPlaying&&v.clip.name=="cancel_install"),Is.True);yield return null;
    }
    [UnityTest]
    public IEnumerator EachCardGradeUsesItsSelectedFileAndRevealOnlyPlaysOnce()
    {
        var viewType=Type.GetType("CoreGuardianAugmentationView, Assembly-CSharp");
        var buttonType=Type.GetType("UnityEngine.UI.Button, UnityEngine.UI");var imageType=Type.GetType("UnityEngine.UI.Image, UnityEngine.UI");
        foreach(AugmentationGrade grade in Enum.GetValues(typeof(AugmentationGrade)))
        {
            ClearVoices();var root=new GameObject("AudioRevealQA",typeof(RectTransform));root.transform.SetParent(game.transform,false);((RectTransform)root.transform).sizeDelta=new Vector2(350,460);
            root.AddComponent(imageType);root.AddComponent(buttonType);var view=root.AddComponent(viewType);
            viewType.GetMethod("Configure").Invoke(view,new object[]{new AugmentationCard(new AugmentSpec{Id=22001,Number=1,Name="QA",Grade=grade,Category="Combat",Subcategories=new[]{"Tower"},Values=new float[]{8},Text="QA"}),Resources.Load<Font>("CoreGuardian/Fonts/Galmuri11-Bold"),(Action)(()=>{}),(Func<bool>)(()=>true)});
            int before=(int)audioType.GetProperty("PlayedCount").GetValue(audio);
            viewType.GetMethod("Click").Invoke(view,null);viewType.GetMethod("Click").Invoke(view,null);
            Assert.That((int)audioType.GetProperty("PlayedCount").GetValue(audio),Is.EqualTo(before+1));
            Assert.That(Voices.Any(v=>v.isPlaying&&v.clip.name=="aug_"+grade.ToString().ToLowerInvariant()),Is.True);
            UnityEngine.Object.Destroy(root);yield return null;
        }
    }
}
