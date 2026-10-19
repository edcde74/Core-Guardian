using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using CoreGuardian;

public static class CompareThirty20261020
{
 public static async Task<object> Main(string tag="v1")
 {
  if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Idle editor required");
  var p=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();
  var csv=new[]{p.enemiesCsv.text,p.towersCsv.text,p.stageCsv.text,p.upgradesCsv.text,p.wavesCsv.text};
  var route=p.RouteCells.ToArray();var size=p.BoardSize;var core=p.CoreCell;
  var destination="docs/development/aggressive_30r_20261020/simulation_"+tag+".json";
  if(File.Exists(destination))throw new IOException("Refusing overwrite");
  var runs=await Task.Run(()=>
  {
   var results=new List<object>();
   foreach(var policy in tag.EndsWith("replay")?new[]{"single"}:new[]{"mixed","single","multi"})foreach(int seed in tag.EndsWith("replay")?new[]{109}:new[]{17,42,73,109})
   {
    var d=CombatData.Load(csv[0],csv[1],csv[2],csv[3],csv[4]);
    if(tag.StartsWith("funding"))foreach(int w in Enumerable.Range(1,30))d.WaveRewards[w]=d.RewardForWave(w)+20;
    if(tag=="soft75_probe")foreach(var row in d.Waves.Where(w=>w.Wave>=11&&w.Kind!=EnemyKind.Boss))row.Enemy.Health=Math.Max(1,(int)Math.Round(row.Enemy.Health*.75,MidpointRounding.AwayFromZero));
    var s=new Session(d,seed);s.SetBoard(size.x,size.y,core,route);
    var actions=new List<object>();var rounds=new List<object>();int prepared=0,playerPurchases=0,towerPurchases=0,installed=0;float elapsed=0,started=0;int coreStart=0;bool captured=false;
    var samples=new List<Vector2>();for(int i=1;i<route.Length;i++){float distance=Vector2.Distance(route[i-1],route[i]);for(float t=0;t<distance;t+=.5f)samples.Add(Vector2.Lerp(route[i-1],route[i],t/distance));}
    Action finish=()=>{rounds.Add(new{wave=s.Phase==SessionPhase.Defeat?s.Wave:s.CompletedWave,seconds=elapsed-started,coreStart,coreEnd=s.CoreEnergy,towers=s.Towers.Count,gold=s.Gold,crystals=s.Crystals,bossKilled=s.BossKilled,damage=new Dictionary<string,int>(s.DamageTotals)});captured=true;};
    s.RoundCompleted+=finish;s.VictoryReached+=finish;s.DefeatReached+=finish;
    string[] playerStats={"damage","rate","damage","range","rate","projectileSpeed"};
    while(s.Phase!=SessionPhase.Victory&&s.Phase!=SessionPhase.Defeat&&elapsed<4200)
    {
     if(s.Phase==SessionPhase.Augmentation)
     {
      // Same grade-first/leftmost policy for every seed and variant. No card or currency injection.
      var cards=s.AugmentationChoices;int index=AugmentationDeck.BestIndex(cards);var card=cards[index];
      actions.Add(new{wave=s.CompletedWave,kind="augment",candidates=cards.Select(c=>c.Id).ToArray(),selected=card.Id});s.ChooseAugmentation(index);continue;
     }
     if(s.Phase==SessionPhase.Preparation&&prepared!=s.Wave)
     {
      prepared=s.Wave;captured=false;string reason;
      if(s.Wave%3==0&&s.Towers.Count>=3)
      {
       var attack=s.Towers.First(t=>t.Kind!=TowerKind.Support);string stat=towerPurchases%3==2?"rate":"damage";int cost=s.TowerUpgradePrice(attack.Id,stat);
       if(s.UpgradeTower(attack.Id,stat,out reason)){towerPurchases++;actions.Add(new{wave=s.Wave,kind="shared_upgrade",towerKind=attack.Kind.ToString(),stat,cost});}
      }
      for(int purchase=0;purchase<2&&s.Towers.Count<(tag.StartsWith("expansion")?32:18);purchase++)
      {
       int count=s.Towers.Count;TowerKind kind=count%5==4?TowerKind.Support:policy=="single"?TowerKind.Single:policy=="multi"?TowerKind.Multi:count%3==1?TowerKind.Single:TowerKind.Multi;
       if(s.Crystals<d.Towers[kind].Cost)break;
       Vector2Int best=new Vector2Int(-1,-1);float bestScore=-1;
       for(int x=1;x<size.x-1;x++)for(int y=1;y<size.y-1;y++)
       {
        var cell=new Vector2Int(x,y);if(!s.IsBuildable(cell))continue;float score=0;
        if(kind==TowerKind.Support)score=s.Towers.Where(t=>t.Kind!=TowerKind.Support).Sum(t=>Mathf.Max(0,2.2f-Vector2.Distance(cell,t.Position)));
        else foreach(var point in samples)
        {
         if(Vector2.Distance(cell,point)>d.Towers[kind].Range)continue;
         int existing=s.Towers.Count(t=>t.Kind!=TowerKind.Support&&Vector2.Distance(t.Position,point)<=t.Stats.Range);score+=1/(1+.45f*existing);
        }
        if(score>bestScore){bestScore=score;best=cell;}
       }
       if(count==0)best=new Vector2Int(12,13);
       var tower=s.TryInstall(kind,best,out reason);if(tower==null)throw new InvalidOperationException(reason);installed++;
       actions.Add(new{wave=s.Wave,kind="install",towerKind=kind.ToString(),cost=d.Towers[kind].Cost,x=best.x,y=best.y});
      }
      coreStart=s.CoreEnergy;started=elapsed;s.StartCombat();
     }
     if(s.Phase==SessionPhase.Combat)
     {
      string stat=playerStats[playerPurchases%playerStats.Length];int price=s.PlayerUpgradePrice(stat);
      if(s.Gold>=price){string reason;if(!s.UpgradePlayer(stat,out reason))throw new InvalidOperationException(reason);playerPurchases++;actions.Add(new{wave=s.Wave,kind="player_upgrade",stat,cost=price});}
     }
     // Stationary counterexample policy; each ten-round boss uses the prior verified late-lane position.
     var position=s.Wave%10==0?new Vector2(16,12):new Vector2(13,14);
     s.Tick(.05f,position);elapsed+=.05f;
    }
    if(!captured&&s.Phase!=SessionPhase.Victory&&s.Phase!=SessionPhase.Defeat)finish();
    results.Add(new{policy,policyVersion="coverage-greedy-v1/grade-first/one-shared-every-third/player-cycle"+(tag.StartsWith("expansion")?"/cap32":"/cap18"),seed,win=s.Phase==SessionPhase.Victory,phase=s.Phase.ToString(),completed=s.CompletedWave,reached=s.Wave,core=s.CoreEnergy,installed,playerPurchases,towerPurchases,elapsed,gold=s.Gold,crystals=s.Crystals,augments=s.Augments.Selected.ToArray(),rounds,actions});
   }
   return results;
  });
  File.WriteAllText(destination,Newtonsoft.Json.JsonConvert.SerializeObject(new{kind="actual Session fixed-step simulation; no live session, no rendering, no human play",step=.05,preparation="immediate legal start; preparation/card human waiting not included",runs},Newtonsoft.Json.Formatting.Indented));
  return new{destination,count=runs.Count};
 }
}
