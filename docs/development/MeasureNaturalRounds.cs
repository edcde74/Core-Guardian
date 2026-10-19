using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoreGuardian;
using UnityEditor;
using UnityEngine;

// Current-data entry. Original observer is archived beside the 2026-10-19 results.
public static class MeasureNaturalRounds
{
 static CoreGuardianPrototype p; static Session s; static bool running;
 static string output,policy,playerStat,signature,layout; static float oldScale,startTime,roundTime;
 static double startReal,roundReal; static int prepared,selectedCount,peakAlive;
 static float minimum; static bool augmentSeen; static object pendingChoices;
 static Dictionary<string,object> report,active;
 static readonly List<object> rounds=new List<object>(),events=new List<object>();
 static readonly List<string> errors=new List<string>();
 static readonly HashSet<int> seen=new HashSet<int>();
 static readonly HashSet<int> recordedExits=new HashSet<int>();
 static readonly List<EnemyState> observed=new List<EnemyState>();
 static readonly Dictionary<string,int> kills=new Dictionary<string,int>();
 static readonly System.Reflection.FieldInfo waveTime=typeof(Session).GetField("waveTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
 static object Stats(CombatStats t)=>new {t.Damage,t.Rate,t.Range,t.ProjectileSpeed,t.BuffDamage,steps=new Dictionary<string,int>(t.UpgradeSteps)};
 static object Wallet()=>new {s.Gold,s.Crystals,s.CoreEnergy};
 static object Snapshot()=>new {wallet=Wallet(),playerCell=p.player.CurrentCell.ToString(),player=Stats(s.Player),towers=s.Towers.Select(t=>new {t.Id,kind=t.Kind.ToString(),cell=t.Position.ToString(),t.Spent,stats=Stats(t.Stats)}).ToArray(),selected=s.Augments.Selected.ToArray(),s.Augments.TowerBuffPercent,s.Augments.ExtraPlayerProjectiles,s.Augments.PlayerSlow};
 static string DataSignature(CombatData d)=>Newtonsoft.Json.JsonConvert.SerializeObject(new {d.Enemies,d.Towers,d.Waves,d.Upgrades,d.WaveRewards,d.CoreEnergy,d.StartGold,d.StartCrystals,d.Player});
 static string Signature()=>DataSignature(s.Data);
 static void Event(string kind,object detail)=>events.Add(new {kind,wave=active!=null?(int)active["wave"]:s.Wave,game=Time.time-startTime,real=EditorApplication.timeSinceStartup-startReal,detail});
 public static string StartCurrent(string selectedPolicy,int repeat=1,float speed=5,string stat="damage",string placement="baseline")
 {
  if(running||!EditorApplication.isPlaying||EditorApplication.isPaused||EditorApplication.isCompiling)throw new InvalidOperationException("Idle fresh Play required");
  if(!new[]{"single_save","single_upgrade","multi_save","mixed_support"}.Contains(selectedPolicy)||!new[]{"damage","rate"}.Contains(stat)||!new[]{"baseline","late","late_player"}.Contains(placement)||repeat<1||speed<1||speed>5)throw new ArgumentException("Unknown condition");
  p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();s=p?.Session;
  if(s==null||s.Diagnostic||s.Wave!=1||s.CompletedWave!=0||s.Phase!=SessionPhase.Preparation||s.Towers.Count!=0||s.Gold!=s.Data.StartGold||s.Crystals!=s.Data.StartCrystals||s.CoreEnergy!=s.Data.CoreEnergy||s.Augments.Selected.Count!=0||s.Player.UpgradeSteps.Count!=0||p.player.CurrentCell!=new Vector3Int(13,9,0))throw new InvalidOperationException("Normal initial session required");
  if(s.LastWave!=11||p.BoardSize!=new Vector2Int(39,22)||p.gameObject.scene.isDirty)throw new InvalidOperationException("Unexpected scene/last wave");
  if(Signature()!=DataSignature(CombatData.Load(p.enemiesCsv.text,p.towersCsv.text,p.stageCsv.text,p.upgradesCsv.text,p.wavesCsv.text)))throw new InvalidOperationException("Session data differs from current scene CSV loader");
  layout=placement;output="docs/development/balance_measurement_20261019/"+selectedPolicy+"_"+stat+"_"+(layout=="baseline"?"":layout+"_")+repeat+".json";
  if(File.Exists(output))throw new IOException("Refusing overwrite "+output);
  using(File.Open(output,FileMode.CreateNew)){}
  var rng=typeof(Session).GetField("augmentationRandom",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(s);
  var rngState=rng.GetType().GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).ToDictionary(f=>f.Name,f=>f.GetValue(rng));
  policy=selectedPolicy;playerStat=stat;oldScale=Time.timeScale;startTime=Time.time;startReal=EditorApplication.timeSinceStartup;
  rounds.Clear();events.Clear();errors.Clear();active=null;prepared=0;selectedCount=0;augmentSeen=false;pendingChoices=null;signature=Signature();
  report=new Dictionary<string,object>{{"policy",policy},{"playerStat",stat},{"repeat",repeat},{"speed",speed},{"head","18279e1"},{"utc",DateTime.UtcNow},{"scene",p.gameObject.scene.path},{"method","Actual scene Update/Session.Tick; legal TryStep/Place/Upgrade/StartCombat; no data edits, forced damage, debug spawns or currency injection; preparation skipped; stationary after legal movement; automatic 30s augmentation; accelerated, not human play"},{"randomSeed","normal constructor, unspecified"},{"initialRandomState",rngState},{"initial",Snapshot()},{"volumeBefore",CoreGuardianSoundSettings.Volume},{"csv",new[]{p.enemiesCsv,p.towersCsv,p.stageCsv,p.upgradesCsv,p.wavesCsv}.Select(a=>new {path=UnityEditor.AssetDatabase.GetAssetPath(a),sha=BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(UnityEditor.AssetDatabase.GetAssetPath(a)))).Replace("-","")}).ToArray()},{"rounds",rounds},{"events",events},{"errors",errors}};
  report["runtimeMatchesFreshLoader"]=true;report["placement"]=layout;report["observerSHA256"]=BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes("docs/development/MeasureNaturalRounds.cs"))).Replace("-","");
  s.DamageApplied+=OnDamage;s.RoundCompleted+=CloseRound;s.VictoryReached+=CloseRound;s.DefeatReached+=CloseRound;
  Application.logMessageReceived+=OnLog;EditorApplication.update+=Observe;running=true;Time.timeScale=speed;Write();return output;
 }
 static void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
 static void OnDamage(EnemyState e,int amount){if(e.Health==0){var k=e.Kind.ToString();if(!kills.ContainsKey(k))kills[k]=0;kills[k]++;if(e.Kind==EnemyKind.Boss)Event("boss_killed",new {e.Id,waveSeconds=(float)waveTime.GetValue(s),goldReward=e.Spec.Gold,wallet=Wallet()});}}
 static void Sample(){if(active==null)return;peakAlive=Math.Max(peakAlive,s.Enemies.Count(e=>e.Alive));foreach(var e in s.Enemies){if(seen.Add(e.Id))observed.Add(e);if(e.Alive)minimum=Math.Min(minimum,e.PathRemaining);}foreach(var e in observed.Where(e=>e.Exited)){if(recordedExits.Add(e.Id))Event("core_arrival_sample",new {e.Id,kind=e.Kind.ToString(),remainingHealth=e.Health,e.Spec.CoreDamage,waveSeconds=(float)waveTime.GetValue(s)});}}
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
   if(layout=="late_player"&&s.Wave>=10&&(p.player.CurrentCell.x<16||p.player.CurrentCell.y>12)){
    var dir=p.player.CurrentCell.x<16?Vector2Int.right:Vector2Int.down;
    if(p.player.TryStep(dir))Event("move",new {to=p.player.CurrentCell.ToString()});return;
   }
   prepared=s.Wave;var preparation=Snapshot();int eventStart=events.Count;
   if(s.Wave==1)Install(policy=="multi_save"?TowerKind.Multi:TowerKind.Single,new Vector2Int(12,13));
   if(policy=="single_upgrade") {var t=s.Towers.First();int cost=s.TowerUpgradePrice(t.Id,"damage");if(s.Crystals>=cost){string reason;var before=Wallet();bool ok=s.UpgradeTower(t.Id,"damage",out reason);Event("tower_upgrade",new {t.Id,stat="damage",cost,ok,reason,before,after=Wallet()});}}
   else if(s.Towers.Count==1&&s.Crystals>=100)Install(policy=="single_save"?TowerKind.Single:TowerKind.Multi,layout!="baseline"?new Vector2Int(16,11):new Vector2Int(14,13));
   else if(s.Towers.Count==2&&s.Crystals>=100)Install(policy=="single_save"?TowerKind.Single:TowerKind.Support,policy=="single_save"?new Vector2Int(13,12):layout!="baseline"?new Vector2Int(16,12):new Vector2Int(13,13));
   while(s.Gold>=s.PlayerUpgradePrice(playerStat)){string reason;int cost=s.PlayerUpgradePrice(playerStat);var before=Wallet();bool ok=s.UpgradePlayer(playerStat,out reason);Event("player_upgrade",new {stat=playerStat,cost,ok,reason,before,after=Wallet()});if(!ok)break;}
   active=new Dictionary<string,object>{{"wave",s.Wave},{"preparationStart",preparation},{"combatStart",Snapshot()},{"actions",events.Skip(eventStart).ToArray()},{"expected",s.Data.Waves.Where(x=>x.Wave==s.Wave).Sum(x=>x.Count)}};
   observed.Clear();seen.Clear();recordedExits.Clear();kills.Clear();peakAlive=0;minimum=float.PositiveInfinity;roundTime=Time.time;roundReal=EditorApplication.timeSinceStartup;
   p.StartCombat();if(s.Phase!=SessionPhase.Combat)throw new InvalidOperationException("Normal start failed");Write();
  }catch(Exception e){errors.Add(e.ToString());Finish("observer_error");}
 }
 static void Write(){report["completedWave"]=s.CompletedWave;report["phase"]=s.Phase.ToString();report["final"]=Snapshot();report["elapsedGame"]=Time.time-startTime;report["elapsedReal"]=EditorApplication.timeSinceStartup-startReal;File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));}
 static void Finish(string outcome){if(!running)return;running=false;EditorApplication.update-=Observe;Application.logMessageReceived-=OnLog;s.DamageApplied-=OnDamage;s.RoundCompleted-=CloseRound;s.VictoryReached-=CloseRound;s.DefeatReached-=CloseRound;report["outcome"]=outcome;report["dataUnchanged"]=Signature()==signature;report["volumeAfter"]=CoreGuardianSoundSettings.Volume;Time.timeScale=oldScale;report["restoredTimeScale"]=Time.timeScale;Write();}
 public static object Status()=>new {running,output,wave=s?.Wave,phase=s?.Phase.ToString(),rounds=rounds.Count,outcome=report!=null&&report.ContainsKey("outcome")?report["outcome"]:null};
 public static object Stop(){Finish("stopped");return Status();}
}
