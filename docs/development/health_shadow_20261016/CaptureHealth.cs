using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using CoreGuardian;
public static class CaptureHealth
{
 public static object Main()
 {
  if(!EditorApplication.isPlaying)throw new Exception("Own Play required");
  EditorApplication.isPaused=true;
  const string folder="docs/development/health_shadow_20261016/";Directory.CreateDirectory(folder);
  var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();game.enabled=false;
  var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  var camera=Camera.main;var oldPosition=camera.transform.position;float oldSize=camera.orthographicSize;
  var shadow=game.player.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="바닥 밀착선");
  var report=new System.Collections.Generic.List<object>();
  var render=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.ScreenshotCommand")).First(t=>t!=null).GetMethod("RenderToPng",BindingFlags.NonPublic|BindingFlags.Static);
  try
  {
   game.Session.StartCombat();int i=0;
   foreach(var kind in new[]{EnemyKind.Attack,EnemyKind.Defense,EnemyKind.Buff,EnemyKind.Boss}){var e=game.Session.DebugSpawn(kind,0);e.Position=new Vector2(12+i++*4,12);}
   game.player.transform.position=game.World(new Vector2(8,12));
   typeof(CoreGuardianPrototype).GetMethod("SynchronizeViews",flags).Invoke(game,null);
   var views=(System.Collections.IDictionary)typeof(CoreGuardianPrototype).GetField("enemyViews",flags).GetValue(game);
   camera.transform.position=game.World(new Vector2(16,14))+Vector3.back*10;camera.orthographicSize=3.4f;
   foreach(string label in new[]{"before","after"})
   {
    shadow.enabled=label=="before";
    foreach(var e in game.Session.Enemies)
    {
     var view=(GameObject)views[e.Id];var bar=view.transform.Find("Health");
     if(label=="before")bar.localPosition=new Vector3(0,e.Kind==EnemyKind.Boss?1.4f:.8f,0);
     else bar.position=view.GetComponent<CoreGuardianUnitAnimation>().HealthBarWorld;
     if(label=="after")report.Add(new{kind=e.Kind.ToString(),localY=bar.localPosition.y,sourceTop=view.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite").bounds.max.y,barBottom=bar.GetComponent<SpriteRenderer>().bounds.min.y});
    }
    File.WriteAllBytes(folder+label+".png",(byte[])render.Invoke(null,new object[]{camera,1600,900}));
   }
   File.WriteAllText(folder+"capture.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{source="Actual combat scene camera, paused QA spawns and zoom, not balance data",playerStaticSoles=shadow.enabled,report},Newtonsoft.Json.Formatting.Indented));
  }
  finally{shadow.enabled=false;camera.transform.position=oldPosition;camera.orthographicSize=oldSize;}
  return report;
 }
}
