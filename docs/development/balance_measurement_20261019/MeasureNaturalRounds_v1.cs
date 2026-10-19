using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoreGuardian;
using UnityEditor;
using UnityEngine;

// Ephemeral editor observer. No damage injection, CSV edits, or scene saves.
public static class MeasureNaturalRoundsLegacy
{
    [Serializable] public class Round
    {
        public int wave, coreStart, coreEnd, goldStart, goldEnd, crystalsStart, crystalsEnd, reward, expected;
        public float seconds;
        public string outcome;
        public string[] actions, damage;
        public int peakAlive, peakAliveFrom8To18;
        public float minimumRemaining=-1;
    }
    [Serializable] public class Report
    {
        public string policy, scene, method, outcome, error;
        public float timeScale;
        public int completedWave, core, gold, crystals;
        public List<Round> rounds = new List<Round>();
        public List<string> runtimeErrors = new List<string>();
        public string variant="baseline";
        public int repeat;
        public bool scheduleInvariant;
        public int attackHealth;
        public string[] testedSchedules;
    }
    static CoreGuardianPrototype p;
    static Report report;
    static Round active;
    static readonly List<string> actions = new List<string>();
    static string output;
    static float oldScale, started, runStarted;
    static double realStarted;
    static int preparedWave;
    static bool running;
    static int stopAfterWave;

    public static string Start(string policy)
    {
        return StartInternal(policy,"baseline",0,5,0);
    }
    public static string StartComparison(string policy,string variant,int repeat,float speed=5)
    {
        if(variant!="baseline"&&variant!="clusterA")throw new ArgumentException("Unknown variant");
        if(repeat<1||speed<=0||speed>5)throw new ArgumentException("Invalid comparison settings");
        string comparisonPath=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/tension_test_20261013/"+policy+"_"+variant+"_"+repeat+".json");
        if(File.Exists(comparisonPath))throw new InvalidOperationException("Refusing to overwrite previous result");
        Directory.CreateDirectory(Path.GetDirectoryName(comparisonPath));
        return StartInternal(policy,variant,repeat,speed,6);
    }
    public static string StartEarlyComparison(string policy,string variant,int repeat,float speed=5)
    {
        if(variant!="baseline"&&variant!="fast"&&variant!="fastHp"&&variant!="fastHp8")throw new ArgumentException("Unknown early variant");
        if(repeat<1||speed<=0||speed>5)throw new ArgumentException("Invalid comparison settings");
        string dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/early_pressure_test_20261013");
        if(File.Exists(Path.Combine(dir,policy+"_"+variant+"_"+repeat+".json")))throw new InvalidOperationException("Refusing to overwrite previous result");
        Directory.CreateDirectory(dir);
        return StartInternal(policy,variant,repeat,speed,2);
    }
    static string StartInternal(string policy,string variant,int repeat,float speed,int stopWave)
    {
        if(running) throw new InvalidOperationException("Measurement already active");
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Play required");
        p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        if(p==null || !p.Ready || p.Session.Wave!=1 || p.Session.Phase!=SessionPhase.Preparation)
            throw new InvalidOperationException("Fresh normal session required");
        if(policy!="single_save" && policy!="single_upgrade" && policy!="multi_save" && policy!="coverage_save")
            throw new ArgumentException("Unknown policy");
        oldScale=Time.timeScale;
        report=new Report {policy=policy,variant=variant,repeat=repeat,scene=p.gameObject.scene.path,timeScale=speed,
            method="Actual Play Mode Update; normal Place/UpgradePlayer/UpgradeTower/StartCombat/TryStep APIs; no forced damage or currency; preparation skipped with normal start command; stationary player after legal movement; accelerated time"};
        output=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/natural_rounds_20261010_"+policy+".json");
        if(stopWave>0)output=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/"+(stopWave==2?"early_pressure_test_20261013":"tension_test_20261013")+"/"+policy+"_"+variant+"_"+repeat+".json");
        stopAfterWave=stopWave;
        var wave=p.Session.Data.Waves.Where(x=>x.Wave==6).OrderBy(x=>x.Start).ToArray();
        if(variant=="clusterA")
        {
            if(wave.Length!=18||wave.Any(x=>x.Count!=1)||Math.Abs(wave.Last().Start-25.5f)>.001f)throw new InvalidOperationException("Unexpected current wave6 schema");
            for(int i=6;i<=10;i++)wave[i].Start=9+(i-6)*.75f;
        }
        report.scheduleInvariant=wave.Sum(x=>x.Count)==18&&wave.Sum(x=>x.Count*p.Session.Data.Enemies[x.Kind].Health)==555&&wave.Sum(x=>x.Count*p.Session.Data.Enemies[x.Kind].Gold)==61&&Math.Abs(wave.Max(x=>x.Start+(x.Count-1)*x.Interval)-25.5f)<.001f;
        if(stopWave==2)
        {
            var early=p.Session.Data.Waves.Where(x=>x.Wave<=2).ToArray();
            if(early.Where(x=>x.Wave==1).Sum(x=>x.Count)!=15||early.Where(x=>x.Wave==2).Sum(x=>x.Count)!=16||early.Any(x=>x.Kind!=EnemyKind.Attack)||p.Session.Data.Enemies[EnemyKind.Attack].Health!=25)throw new InvalidOperationException("Unexpected early schema");
            if(variant=="fast"||variant=="fastHp"||variant=="fastHp8")foreach(var r in early){r.Start*=.9f;r.Interval*=.9f;}
            if(variant=="fastHp")p.Session.Data.Enemies[EnemyKind.Attack].Health=26;
            if(variant=="fastHp8")p.Session.Data.Enemies[EnemyKind.Attack].Health=27;
            report.scheduleInvariant=early.Sum(x=>x.Count)==31&&early.Sum(x=>x.Count*p.Session.Data.Enemies[x.Kind].Gold)==93;
            report.testedSchedules=early.Select(x=>"wave="+x.Wave+" start="+x.Start.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" interval="+x.Interval.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" count="+x.Count).ToArray();
        }
        report.attackHealth=p.Session.Data.Enemies[EnemyKind.Attack].Health;
        preparedWave=0; active=null; actions.Clear();
        started=runStarted=Time.time;realStarted=EditorApplication.timeSinceStartup;
        running=true;
        Application.logMessageReceived+=OnLog;
        EditorApplication.update+=Observe;
        Time.timeScale=speed;
        return "Observer started: "+output;
    }
    static void OnLog(string text,string stack,LogType type)
    {
        if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)
            report.runtimeErrors.Add(text+"\n"+stack);
    }
    static void Observe()
    {
        try
        {
            if(!EditorApplication.isPlaying || p==null){Finish("interrupted");return;}
            var s=p.Session;
            if(active!=null&&s.Phase==SessionPhase.Combat)
            {
                active.peakAlive=Math.Max(active.peakAlive,s.Enemies.Count(e=>e.Alive));
                float elapsed=Time.time-started;
                if(elapsed>=8&&elapsed<=18)active.peakAliveFrom8To18=Math.Max(active.peakAliveFrom8To18,s.Enemies.Count(e=>e.Alive));
                foreach(var e in s.Enemies.Where(e=>e.Alive))active.minimumRemaining=active.minimumRemaining<0?e.PathRemaining:Math.Min(active.minimumRemaining,e.PathRemaining);
            }
            if(active!=null && (s.Phase!=SessionPhase.Combat || s.Wave!=active.wave))
            {
                active.seconds=Time.time-started; active.coreEnd=s.CoreEnergy;
                active.goldEnd=s.Gold;active.crystalsEnd=s.Crystals;
                active.reward=s.CompletedWave==active.wave?s.LastCompletionReward:0;
                active.outcome=s.Phase==SessionPhase.Defeat?"Defeat":"Completed";
                active.damage=s.DamageTotals.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value).ToArray();
                report.rounds.Add(active);active=null;Write();
            }
            if(s.Phase==SessionPhase.Victory || s.Phase==SessionPhase.Defeat){Finish(s.Phase.ToString());return;}
            if(stopAfterWave>0&&s.CompletedWave>=stopAfterWave){Finish("CompletedComparison");return;}
            if(Time.time-runStarted>2000 || EditorApplication.timeSinceStartup-realStarted>480)
            {Finish("timeout");return;}
            if(s.Phase!=SessionPhase.Preparation)return;
            int row=report.policy=="coverage_save"?16:15;
            // Only legal one-cell moves from the default (15,10).
            if(s.Wave==1 && p.player.CurrentCell.y<row)
            {p.player.TryStep(Vector2Int.up);return;}
            if(s.Wave==1 && Time.time-runStarted<1)return;
            if(preparedWave==s.Wave)return;
            preparedWave=s.Wave;
            actions.Clear();
            var kind=report.policy=="multi_save"?TowerKind.Multi:TowerKind.Single;
            if(s.Wave==1)Install(kind,new Vector2Int(14,row));
            if(report.policy=="single_upgrade")
            {
                var tower=s.Towers.FirstOrDefault();
                if(tower!=null && s.Crystals>=s.TowerUpgradePrice(tower.Id,"damage"))
                {string reason;int cost=s.TowerUpgradePrice(tower.Id,"damage");if(s.UpgradeTower(tower.Id,"damage",out reason))actions.Add("tower damage + cost="+cost);}
            }
            else if(s.Wave>1 && s.Crystals>=s.Data.Towers[TowerKind.Multi].Cost)
                Install(TowerKind.Multi,new Vector2Int(17,row));
            if(report.policy!="single_upgrade" && s.Towers.Count>=2 && s.Crystals>=s.Data.Towers[TowerKind.Support].Cost)
                Install(TowerKind.Support,new Vector2Int(14,report.policy=="coverage_save"?15:16));
            // Purchase damage in preparation when affordable; never inject gold.
            while(s.Gold>=s.PlayerUpgradePrice("damage"))
            {int cost=s.PlayerUpgradePrice("damage");string reason;if(!s.UpgradePlayer("damage",out reason))break;actions.Add("player damage + cost="+cost);}
            active=new Round {wave=s.Wave,coreStart=s.CoreEnergy,goldStart=s.Gold,crystalsStart=s.Crystals,
                expected=s.Data.Waves.Where(x=>x.Wave==s.Wave).Sum(x=>x.Count),actions=actions.ToArray()};
            started=Time.time;
            p.StartCombat();
            Write();
        }
        catch(Exception e){report.error=e.ToString();Finish("observer_error");}
    }
    static void Install(TowerKind kind,Vector2Int cell)
    {
        if(p.Session.Towers.Any(t=>t.Position==cell))return;
        int cost=p.Session.Data.Towers[kind].Cost;
        bool ok=p.Place(kind,cell);
        actions.Add("install "+kind+" "+cell+" cost="+cost+" success="+ok);
    }
    static void Write()
    {
        var s=p.Session;
        report.completedWave=s.CompletedWave;report.core=s.CoreEnergy;report.gold=s.Gold;report.crystals=s.Crystals;
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
    }
    static void Finish(string outcome)
    {
        if(!running)return;
        running=false;EditorApplication.update-=Observe;Application.logMessageReceived-=OnLog;
        Time.timeScale=oldScale;report.outcome=outcome;
        if(p!=null)Write();
    }
    public static string Status(){return report==null?"not started":Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);}
    public static string Stop(){Finish("stopped");return Status();}
}

// Current-data entry. Existing legacy results and code retained for provenance; never called here.
public static class MeasureNaturalRounds
{
 static CoreGuardianPrototype p; static Session s; static bool running;
 static string output,policy,playerStat,signature; static float oldScale,startTime,roundTime;
 static double startReal,roundReal; static int prepared,selectedCount,peakAlive;
 static float minimum; static bool augmentSeen; static object pendingChoices;
 static Dictionary<string,object> report,active;
 static readonly List<object> rounds=new List<object>(),events=new List<object>();
 static readonly List<string> errors=new List<string>();
 static readonly HashSet<int> seen=new HashSet<int>();
 static readonly List<EnemyState> observed=new List<EnemyState>();
 static readonly Dictionary<string,int> kills=new Dictionary<string,int>();
 static readonly System.Reflection.FieldInfo waveTime=typeof(Session).GetField("waveTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
 static object Stats(CombatStats t)=>new {t.Damage,t.Rate,t.Range,t.ProjectileSpeed,t.BuffDamage,steps=new Dictionary<string,int>(t.UpgradeSteps)};
 static object Wallet()=>new {s.Gold,s.Crystals,s.CoreEnergy};
 static object Snapshot()=>new {wallet=Wallet(),playerCell=p.player.CurrentCell.ToString(),player=Stats(s.Player),towers=s.Towers.Select(t=>new {t.Id,kind=t.Kind.ToString(),cell=t.Position.ToString(),t.Spent,stats=Stats(t.Stats)}).ToArray(),selected=s.Augments.Selected.ToArray(),s.Augments.TowerBuffPercent,s.Augments.ExtraPlayerProjectiles,s.Augments.PlayerSlow};
 static string Signature()=>Newtonsoft.Json.JsonConvert.SerializeObject(new {s.Data.Enemies,s.Data.Towers,s.Data.Waves,s.Data.Upgrades,s.Data.WaveRewards,s.Data.CoreEnergy,s.Data.StartGold,s.Data.StartCrystals,s.Data.Player});
 static void Event(string kind,object detail)=>events.Add(new {kind,wave=s.Wave,game=Time.time-startTime,real=EditorApplication.timeSinceStartup-startReal,detail});
 public static string StartCurrent(string selectedPolicy,int repeat=1,float speed=5,string stat="damage")
 {
  if(running||!EditorApplication.isPlaying||EditorApplication.isPaused||EditorApplication.isCompiling)throw new InvalidOperationException("Idle fresh Play required");
  if(!new[]{"single_save","single_upgrade","multi_save","mixed_support"}.Contains(selectedPolicy)||!new[]{"damage","rate"}.Contains(stat)||repeat<1||speed<1||speed>5)throw new ArgumentException("Unknown condition");
  p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();s=p?.Session;
  if(s==null||s.Diagnostic||s.Wave!=1||s.CompletedWave!=0||s.Phase!=SessionPhase.Preparation||s.Towers.Count!=0||s.Gold!=s.Data.StartGold||s.Crystals!=s.Data.StartCrystals||s.CoreEnergy!=s.Data.CoreEnergy||s.Augments.Selected.Count!=0||s.Player.UpgradeSteps.Count!=0||p.player.CurrentCell!=new Vector3Int(13,9,0))throw new InvalidOperationException("Normal initial session required");
  if(s.LastWave!=11||p.BoardSize!=new Vector2Int(39,22)||p.gameObject.scene.isDirty)throw new InvalidOperationException("Unexpected scene/last wave");
  output="docs/development/balance_measurement_20261019/"+selectedPolicy+"_"+stat+"_"+repeat+".json";
  if(File.Exists(output))throw new IOException("Refusing overwrite "+output);
  using(File.Open(output,FileMode.CreateNew)){}
  var rng=typeof(Session).GetField("augmentationRandom",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(s);
  var rngState=rng.GetType().GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).ToDictionary(f=>f.Name,f=>f.GetValue(rng));
  policy=selectedPolicy;playerStat=stat;oldScale=Time.timeScale;startTime=Time.time;startReal=EditorApplication.timeSinceStartup;
  rounds.Clear();events.Clear();errors.Clear();active=null;prepared=0;selectedCount=0;augmentSeen=false;pendingChoices=null;signature=Signature();
  report=new Dictionary<string,object>{{"policy",policy},{"playerStat",stat},{"repeat",repeat},{"speed",speed},{"head","18279e1"},{"utc",DateTime.UtcNow},{"scene",p.gameObject.scene.path},{"method","Actual scene Update/Session.Tick; legal TryStep/Place/Upgrade/StartCombat; no data edits, forced damage, debug spawns or currency injection; preparation skipped; stationary after legal movement; automatic 30s augmentation; accelerated, not human play"},{"randomSeed","normal constructor, unspecified"},{"initialRandomState",rngState},{"initial",Snapshot()},{"volumeBefore",CoreGuardianSoundSettings.Volume},{"csv",new[]{p.enemiesCsv,p.towersCsv,p.stageCsv,p.upgradesCsv,p.wavesCsv}.Select(a=>new {path=UnityEditor.AssetDatabase.GetAssetPath(a),sha=BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(UnityEditor.AssetDatabase.GetAssetPath(a)))).Replace("-","")}).ToArray()},{"rounds",rounds},{"events",events},{"errors",errors}};
  s.DamageApplied+=OnDamage;s.RoundCompleted+=CloseRound;s.VictoryReached+=CloseRound;s.DefeatReached+=CloseRound;
  Application.logMessageReceived+=OnLog;EditorApplication.update+=Observe;running=true;Time.timeScale=speed;Write();return output;
 }
 static void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
 static void OnDamage(EnemyState e,int amount){if(e.Health==0){var k=e.Kind.ToString();if(!kills.ContainsKey(k))kills[k]=0;kills[k]++;if(e.Kind==EnemyKind.Boss)Event("boss_killed",new {e.Id,waveSeconds=(float)waveTime.GetValue(s),goldReward=e.Spec.Gold,wallet=Wallet()});}}
 static void Sample(){if(active==null)return;peakAlive=Math.Max(peakAlive,s.Enemies.Count(e=>e.Alive));foreach(var e in s.Enemies){if(seen.Add(e.Id))observed.Add(e);if(e.Alive)minimum=Math.Min(minimum,e.PathRemaining);}}
 static void CloseRound(){if(active==null)return;Sample();active["seconds"]=(float)waveTime.GetValue(s);active["elapsedGameClock"]=Time.time-roundTime;active["realSeconds"]=EditorApplication.timeSinceStartup-roundReal;active["end"]=Snapshot();active["outcome"]=s.Phase==SessionPhase.Defeat?"Defeat":"Completed";active["reward"]=s.CompletedWave==(int)active["wave"]?s.LastCompletionReward:0;active["damage"]=new Dictionary<string,int>(s.DamageTotals);active["kills"]=new Dictionary<string,int>(kills);active["peakAliveSample"]=peakAlive;active["minimumRemainingSample"]=float.IsPositiveInfinity(minimum)?-1:minimum;active["observedEnemies"]=observed.Count;active["arrivals"]=observed.Where(e=>e.Exited).Select(e=>new {kind=e.Kind.ToString(),e.Id,e.Spec.CoreDamage}).ToArray();active["bossKilled"]=s.BossKilled;rounds.Add(active);active=null;Write();}
 static void Install(TowerKind kind,Vector2Int cell){var before=Wallet();int cost=s.Data.Towers[kind].Cost;bool ok=p.Place(kind,cell);Event("install",new {kind=kind.ToString(),cell=cell.ToString(),cost,ok,before,after=Wallet()});if(!ok)throw new InvalidOperationException("Planned install failed "+cell);}
 static void Observe()
 {
  if(!running)return;
  try {
   if(!EditorApplication.isPlaying||p==null){Finish("interrupted");return;}
   if(EditorApplication.isPaused){Finish("external_pause");return;}
   Sample();
   if(s.Augments.Selected.Count>selectedCount){Event("augmentation_selected",new {mode="automatic timeout; no manual Choose call",candidates=pendingChoices,selected=s.Augments.Selected.Last(),after=Snapshot()});selectedCount=s.Augments.Selected.Count;Write();}
   if(s.Phase==SessionPhase.Victory||s.Phase==SessionPhase.Defeat){Finish(s.Phase.ToString());return;}
   if(Time.time-startTime>2400||EditorApplication.timeSinceStartup-startReal>900){Finish("timeout");return;}
   if(s.Phase==SessionPhase.Augmentation){if(!augmentSeen){pendingChoices=s.AugmentationChoices.Select((c,i)=>new {index=i,c.Id,grade=c.Grade.ToString(),family=c.Family.ToString(),c.Description}).ToArray();Event("augmentation_open",new {candidates=pendingChoices,s.AugmentationRemaining,before=Snapshot()});augmentSeen=true;Write();}return;}
   if(s.Phase!=SessionPhase.Preparation||prepared==s.Wave)return;
   if(s.Wave==1&&p.player.CurrentCell.y<14){if(p.player.TryStep(Vector2Int.up))Event("move",new {to=p.player.CurrentCell.ToString()});return;}
   if(s.Wave==1&&Time.time-startTime<2)return;
   prepared=s.Wave;var preparation=Snapshot();int eventStart=events.Count;
   if(s.Wave==1)Install(policy=="multi_save"?TowerKind.Multi:TowerKind.Single,new Vector2Int(12,13));
   if(policy=="single_upgrade") {var t=s.Towers.First();int cost=s.TowerUpgradePrice(t.Id,"damage");if(s.Crystals>=cost){string reason;var before=Wallet();bool ok=s.UpgradeTower(t.Id,"damage",out reason);Event("tower_upgrade",new {t.Id,stat="damage",cost,ok,reason,before,after=Wallet()});}}
   else if(s.Towers.Count==1&&s.Crystals>=100)Install(policy=="single_save"?TowerKind.Single:TowerKind.Multi,new Vector2Int(14,13));
   else if(s.Towers.Count==2&&s.Crystals>=100)Install(policy=="single_save"?TowerKind.Single:TowerKind.Support,policy=="single_save"?new Vector2Int(13,12):new Vector2Int(13,13));
   while(s.Gold>=s.PlayerUpgradePrice(playerStat)){string reason;int cost=s.PlayerUpgradePrice(playerStat);var before=Wallet();bool ok=s.UpgradePlayer(playerStat,out reason);Event("player_upgrade",new {stat=playerStat,cost,ok,reason,before,after=Wallet()});if(!ok)break;}
   active=new Dictionary<string,object>{{"wave",s.Wave},{"preparationStart",preparation},{"combatStart",Snapshot()},{"actions",events.Skip(eventStart).ToArray()},{"expected",s.Data.Waves.Where(x=>x.Wave==s.Wave).Sum(x=>x.Count)}};
   observed.Clear();seen.Clear();kills.Clear();peakAlive=0;minimum=float.PositiveInfinity;roundTime=Time.time;roundReal=EditorApplication.timeSinceStartup;
   p.StartCombat();if(s.Phase!=SessionPhase.Combat)throw new InvalidOperationException("Normal start failed");Write();
  }catch(Exception e){errors.Add(e.ToString());Finish("observer_error");}
 }
 static void Write(){report["completedWave"]=s.CompletedWave;report["phase"]=s.Phase.ToString();report["final"]=Snapshot();report["elapsedGame"]=Time.time-startTime;report["elapsedReal"]=EditorApplication.timeSinceStartup-startReal;File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));}
 static void Finish(string outcome){if(!running)return;running=false;EditorApplication.update-=Observe;Application.logMessageReceived-=OnLog;s.DamageApplied-=OnDamage;s.RoundCompleted-=CloseRound;s.VictoryReached-=CloseRound;s.DefeatReached-=CloseRound;report["outcome"]=outcome;report["dataUnchanged"]=Signature()==signature;report["volumeAfter"]=CoreGuardianSoundSettings.Volume;Time.timeScale=oldScale;report["restoredTimeScale"]=Time.timeScale;Write();}
 public static object Status()=>new {running,output,wave=s?.Wave,phase=s?.Phase.ToString(),rounds=rounds.Count,outcome=report!=null&&report.ContainsKey("outcome")?report["outcome"]:null};
 public static object Stop(){Finish("stopped");return Status();}
}
