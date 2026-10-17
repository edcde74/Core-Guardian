using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CaptureSpawn
{
    const string Dir="docs/development/spawn_tunnel02_applied_20261018/";
    static void Require(bool v,string m){if(!v)throw new Exception(m);}
    static void Screen(string name){
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
        var result=type.GetMethod("CaptureGameView",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{1920,1080,null,null,false,0,"screen"});
        File.WriteAllBytes(Dir+name+".png",Convert.FromBase64String((string)result.GetType().GetProperty("Base64").GetValue(result)));
    }
    static Button Button(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b=>b.name==name);
    public static async Task<object> Main()
    {
        Require(EditorApplication.isPlaying,"Owned Play required");var time=Time.timeScale;var delta=Time.captureDeltaTime;
        try{
            Button("Reload").onClick.Invoke();await Task.Delay(650);var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();Require(g.Ready&&!g.Session.Diagnostic,"Normal fresh session required");
            Button("Start").onClick.Invoke();var end=DateTime.UtcNow.AddSeconds(10);CoreGuardian.EnemyState first=null;
            while(DateTime.UtcNow<end){first=g.Session.Enemies.FirstOrDefault(e=>e.Alive);if(first!=null)break;await Task.Delay(20);}
            Require(first!=null,"No natural spawn");
            Time.timeScale=0;await Task.Delay(60);Screen("spawn_early");var early=first.Position;
            var view=GameObject.Find("Enemy_"+first.Kind+"_"+first.Id);Require(view!=null&&Vector3.Distance(view.transform.position,g.World(first.Position))<.0001f,"Spawn view off route");
            Time.timeScale=time;await Task.Delay(950);Time.timeScale=0;await Task.Delay(60);Screen("spawn_exit");var later=first.Position;
            Require(later.x>early.x&&Mathf.Abs(later.y-10)<.0001f,"Spawn did not leave along original eastward path");
            var result=new{passed=true,naturalSpawn=true,diagnostic=false,early=early.ToString(),later=later.ToString(),originalEastwardRoute=true,method="Own normal Play; Reload/Start HUD callbacks, natural enemy, two Game View captures. No spawn/time-speed/currency injection; pause only for screenshots. Not human play or balance."};
            File.WriteAllText(Dir+"spawn_qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }finally{Time.timeScale=time;Time.captureDeltaTime=delta;}
    }
}
