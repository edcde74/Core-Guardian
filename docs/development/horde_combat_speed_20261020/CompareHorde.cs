using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CoreGuardian;
using UnityEngine;
using UnityEditor;

// Isolated Session simulation. No live Session, scene, asset or preferences mutations.
public static class CompareHorde20261020
{
 public static object Main()
 {
  if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Idle editor required");
  var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
  var output="docs/development/horde_combat_speed_20261020/comparison_v2.json";
  if(File.Exists(output))throw new IOException("Refusing overwrite");
  var results=new List<object>();
  int hp=40; foreach(float interval in new[]{-1f,.8f,.6f,.5f})foreach(string policy in new[]{"single_upgrade","single_save","multi_save"})
  {
   var d=CombatData.Load(p.enemiesCsv.text,p.towersCsv.text,p.stageCsv.text,p.upgradesCsv.text,p.wavesCsv.text);
   if(interval>0){foreach(int wave in Enumerable.Range(3,7)){int slot=0;foreach(var row in d.Waves.Where(w=>w.Wave==wave).OrderBy(w=>w.Start)){row.Start=slot++*interval;row.Interval=interval;}}foreach(var row in d.Waves.Where(w=>w.Wave==10&&w.Kind==EnemyKind.Attack))row.Interval=interval;}
   var s=new Session(d,20261020);s.SetBoard(p.BoardSize.x,p.BoardSize.y,p.CoreCell,p.RouteCells);
   var rows=new List<object>();var actions=new List<object>();int prepared=0;float elapsed=0,roundStart=0;int coreStart=0,goldStart=0;
   Action close=()=>rows.Add(new {wave=s.Phase==SessionPhase.Defeat?s.Wave:s.CompletedWave,seconds=elapsed-roundStart,coreStart,coreEnd=s.CoreEnergy,goldStart,goldEnd=s.Gold,crystals=s.Crystals,bossKilled=s.BossKilled,damage=new Dictionary<string,int>(s.DamageTotals)});
   s.RoundCompleted+=close;s.DefeatReached+=close;s.VictoryReached+=close;
   while(s.Phase!=SessionPhase.Victory&&s.Phase!=SessionPhase.Defeat&&elapsed<2400)
   {
    if(s.Phase==SessionPhase.Preparation&&prepared!=s.Wave)
    {
     prepared=s.Wave;string reason;
     if(s.Wave==1){var tower=s.TryInstall(policy=="multi_save"?TowerKind.Multi:TowerKind.Single,new Vector2Int(12,13),out reason);if(tower==null)throw new InvalidOperationException(reason);actions.Add(new {wave=s.Wave,kind="install",cost=100});}
     if(policy=="single_upgrade")
     {
      var t=s.Towers.First();int cost=s.TowerUpgradePrice(t.Id,"damage");if(s.Crystals>=cost){if(!s.UpgradeTower(t.Id,"damage",out reason))throw new InvalidOperationException(reason);actions.Add(new {wave=s.Wave,kind="tower_upgrade",cost});}
     }
     else if(s.Towers.Count<3&&s.Crystals>=100)
     {
      var kind=policy=="multi_save"?TowerKind.Multi:TowerKind.Single;
      var cell=s.Towers.Count==1?new Vector2Int(16,11):new Vector2Int(13,12);
      if(s.TryInstall(kind,cell,out reason)==null)throw new InvalidOperationException(reason);actions.Add(new {wave=s.Wave,kind="install",cost=100});
     }
     coreStart=s.CoreEnergy;goldStart=s.Gold;roundStart=elapsed;s.StartCombat();
    }
    // Same stationary location as prior observer after its normal setup walk.
    // Combat purchases are a fixed policy here, unlike the older prep-only observer.
    if(s.Phase==SessionPhase.Combat||s.Phase==SessionPhase.Preparation)
    {
     while(s.Gold>=s.PlayerUpgradePrice("damage")){string reason;int cost=s.PlayerUpgradePrice("damage");if(!s.UpgradePlayer("damage",out reason))throw new InvalidOperationException(reason);actions.Add(new {wave=s.Wave,kind="player_upgrade",cost,elapsed});}
    }
    s.Tick(.05f,new Vector2(13,14));elapsed+=.05f;
   }
   results.Add(new {hp,spawnInterval=interval,policy,policyVersion="hp-probe-v1/stationary13,14/legal purchases/player-damage-immediate",seed=20261020,step=.05,phase=s.Phase.ToString(),completed=s.CompletedWave,core=s.CoreEnergy,gold=s.Gold,crystals=s.Crystals,elapsed,augments=s.Augments.Selected.ToArray(),rows,actions});
  }
  var report=new {roundSchemaVersion=2,kind="isolated actual Session simulation, not scene Update or human play",seed=20261020,variants="3-9 spawn interval/start and 10 attack interval only; 1-2/11 and boss screen retained",results};
  File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
  return results;
 }
}
