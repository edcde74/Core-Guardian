using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class DecorationCombatCheck {
 const string Dir="docs/development/environment_decoration_applied_20261018/";
 static void Require(bool v,string why){if(!v)throw new Exception(why);}
 static void Capture(string name){
 var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
 var capture=type.GetMethod("CaptureGameView",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{1920,1080,null,null,false,0,"screen"});
 File.WriteAllBytes(Dir+name+".png",Convert.FromBase64String((string)capture.GetType().GetProperty("Base64").GetValue(capture)));
 }
 public static async Task<object> Main(){
 Require(EditorApplication.isPlaying&&!EditorApplication.isPaused,"Own running Play required");
 var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();Require(g.Ready&&!g.Session.Diagnostic&&g.Session.Towers.Count==0,"Fresh normal session required");
 float ts=Time.timeScale,cd=Time.captureDeltaTime;string cam=EditorJsonUtility.ToJson(Camera.main);
 try{
 var start=g.player.CurrentCell;foreach(var d in new[]{Vector2Int.right,Vector2Int.left,Vector2Int.up,Vector2Int.down}){Require(g.player.TryStep(d),"Step blocked");await Task.Delay(350);}Require(g.player.CurrentCell==start,"Return cell mismatch");
 g.ChooseTower(CoreGuardian.TowerKind.Multi);await Task.Delay(140);
 var marks=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.name=="InstallationTile").ToArray();Require(marks.Length==858,"Unexpected installation overlay count");
 int green=marks.Count(r=>r.color.g>r.color.r),red=marks.Count(r=>r.color.r>r.color.g);Require(green==778&&red==80,"Installation mask changed");Time.timeScale=0;await Task.Delay(100);Capture("applied_installation");Time.timeScale=ts;g.CancelInstall();
 Require(!g.Place(CoreGuardian.TowerKind.Multi,new Vector2Int(0,10)),"Path became installable");
 Require(g.Place(CoreGuardian.TowerKind.Multi,new Vector2Int(4,8)),"Normal purchase failed");g.StartCombat();
 var deadline=DateTime.UtcNow.AddSeconds(18);while(g.Session.Enemies.Count(e=>e.Alive)<4&&DateTime.UtcNow<deadline)await Task.Delay(70);
 int enemies=g.Session.Enemies.Count(e=>e.Alive);Require(enemies>=2,"No natural enemies");Time.timeScale=0;await Task.Delay(150);Capture("applied_combat");
 var root=GameObject.Find("EnvironmentDecoration_Hybrid_Selected");Require(root.GetComponentsInChildren<MeshRenderer>().All(r=>r.sortingOrder<1),"Props above path");Require(root.GetComponentsInChildren<MonoBehaviour>().Length==0&&root.GetComponentsInChildren<Collider2D>().Length==0&&root.GetComponentsInChildren<Collider>().Length==0,"Decoration has gameplay components");Require(cam==EditorJsonUtility.ToJson(Camera.main),"Camera changed");
 var report=new{passed=true,normalGridSteps=4,installationTiles=marks.Length,green,red,pathPlacementRejected=true,normalMultiPurchase=true,naturalEnemies=enemies,diagnostic=g.Session.Diagnostic,propsBelowPath=true,newCollision=0,newBehaviours=0,cameraUnchanged=true,method="Own actual-scene API TryStep/ChooseTower/Place/StartCombat; natural spawns and composited Overlay captures. No human play, injection, balance or whole regression claim."};File.WriteAllText(Dir+"combat_check.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }finally{Time.timeScale=ts;Time.captureDeltaTime=cd;}
 }
}
