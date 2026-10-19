using System;
using System.IO;
using System.Linq;
using CoreGuardian;
using UnityEditor;
using UnityEngine;
public static class BalancePreflight20261019 {
 public static object Main() => Capture("scene_loader");
 public static object End() => Capture("end_state");
 static object Capture(string name) {
  var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
  var d=CombatData.Load(p.enemiesCsv.text,p.towersCsv.text,p.stageCsv.text,p.upgradesCsv.text,p.wavesCsv.text);
  var r=new {utc=DateTime.UtcNow,play=EditorApplication.isPlaying,paused=EditorApplication.isPaused,compiling=EditorApplication.isCompiling,
   scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>{var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);return new{s.path,s.isDirty,s.isLoaded};}).ToArray(),
   board=p.BoardSize.ToString(),core=p.CoreCell.ToString(),route=p.RouteCells.Select(c=>c.ToString()).ToArray(),grid=p.ground.layoutGrid.cellSize.ToString(),
   refs=new[]{p.enemiesCsv,p.towersCsv,p.stageCsv,p.upgradesCsv,p.wavesCsv}.Select(a=>new {path=AssetDatabase.GetAssetPath(a),sha=BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(a)))).Replace("-","")}).ToArray(),
   d.CoreEnergy,d.StartGold,d.StartCrystals,attack=d.Enemies[EnemyKind.Attack].Health,
   rounds=Enumerable.Range(1,11).Select(w=>new {wave=w,count=d.Waves.Where(x=>x.Wave==w).Sum(x=>x.Count),hp=d.Waves.Where(x=>x.Wave==w).Sum(x=>x.Count*d.Enemies[x.Kind].Health),gold=d.Waves.Where(x=>x.Wave==w).Sum(x=>x.Count*d.Enemies[x.Kind].Gold),reward=d.RewardForWave(w),last=d.Waves.Where(x=>x.Wave==w).Max(x=>x.Start+(x.Count-1)*x.Interval)}).ToArray(),
   volume=CoreGuardianSoundSettings.Volume,categories=Enum.GetValues(typeof(CoreGuardianAudioCategory)).Cast<CoreGuardianAudioCategory>().Select(c=>new {category=c.ToString(),volume=CoreGuardianSoundSettings.GetCategoryVolume(c),hasKey=PlayerPrefs.HasKey("CoreGuardian.Audio."+c)}).ToArray(),timeScale=Time.timeScale,captureDelta=Time.captureDeltaTime};
  var destination="docs/development/balance_measurement_20261019/"+name+".json";
  if(File.Exists(destination))throw new IOException("Refusing to overwrite "+destination);
  File.WriteAllText(destination,Newtonsoft.Json.JsonConvert.SerializeObject(r,Newtonsoft.Json.Formatting.Indented));
  return r;
 }
}
