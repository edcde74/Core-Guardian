using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoreGuardian;
using UnityEditor;
using UnityEngine;

// Ephemeral editor observer. No damage injection, CSV edits, or scene saves.
public static class MeasureNaturalRounds
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
        if(variant!="baseline"&&variant!="fast"&&variant!="fastHp")throw new ArgumentException("Unknown early variant");
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
            if(variant=="fast"||variant=="fastHp")foreach(var r in early){r.Start*=.9f;r.Interval*=.9f;}
            if(variant=="fastHp")p.Session.Data.Enemies[EnemyKind.Attack].Health=26;
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
