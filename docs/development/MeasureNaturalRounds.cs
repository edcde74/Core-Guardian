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
    }
    [Serializable] public class Report
    {
        public string policy, scene, method, outcome, error;
        public float timeScale;
        public int completedWave, core, gold, crystals;
        public List<Round> rounds = new List<Round>();
        public List<string> runtimeErrors = new List<string>();
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

    public static string Start(string policy)
    {
        if(running) throw new InvalidOperationException("Measurement already active");
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Play required");
        p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        if(p==null || !p.Ready || p.Session.Wave!=1 || p.Session.Phase!=SessionPhase.Preparation)
            throw new InvalidOperationException("Fresh normal session required");
        if(policy!="single_save" && policy!="single_upgrade" && policy!="multi_save" && policy!="coverage_save")
            throw new ArgumentException("Unknown policy");
        oldScale=Time.timeScale;
        report=new Report {policy=policy,scene=p.gameObject.scene.path,timeScale=5,
            method="Actual Play Mode Update; normal Place/UpgradePlayer/UpgradeTower/StartCombat/TryStep APIs; no forced damage or currency; preparation skipped with normal start command; stationary player after legal movement; accelerated time"};
        output=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/natural_rounds_20261010_"+policy+".json");
        preparedWave=0; active=null; actions.Clear();
        started=runStarted=Time.time;realStarted=EditorApplication.timeSinceStartup;
        running=true;
        Application.logMessageReceived+=OnLog;
        EditorApplication.update+=Observe;
        Time.timeScale=5;
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
