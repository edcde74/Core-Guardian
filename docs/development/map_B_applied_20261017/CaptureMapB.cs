using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CaptureMapB
{
    const string Dir="docs/development/map_B_applied_20261017/";
    static void Require(bool v,string m){if(!v)throw new Exception(m);}
    static void Screen(string name)
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
        var capture=type.GetMethod("CaptureGameView",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{1920,1080,null,null,false,0,"screen"});
        File.WriteAllBytes(Dir+name+".png",Convert.FromBase64String((string)capture.GetType().GetProperty("Base64").GetValue(capture)));
    }
    static Button Button(string n)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b=>b.name==n);
    public static async Task<object> Main()
    {
        Require(EditorApplication.isPlaying,"Owned QA Play required");
        float originalTime=Time.timeScale,originalCapture=Time.captureDeltaTime;
        try{
            Button("Reload").onClick.Invoke();await Task.Delay(800);
            var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
            Require(game!=null&&game.Ready&&!game.Session.Diagnostic,"Normal fresh session required");
            var cameraJson=EditorJsonUtility.ToJson(Camera.main);
            var start=game.player.CurrentCell;
            foreach(var direction in new[]{Vector2Int.right,Vector2Int.left,Vector2Int.up,Vector2Int.down}){
                Require(game.player.TryStep(direction),"Normal step rejected "+direction);
                await Task.Delay(350);
            }
            Require(game.player.CurrentCell==start,"Player did not return");
            Time.timeScale=0; await Task.Delay(150);Screen("applied_hud_ready");Time.timeScale=originalTime;
            game.ChooseTower(CoreGuardian.TowerKind.Single);await Task.Delay(200);
            var highlights=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.name=="InstallationTile").ToArray();
            int green=highlights.Count(r=>r.color.g>r.color.r),red=highlights.Count(r=>r.color.r>r.color.g);
            Require(highlights.Length==1125&&green>0&&red>0,"Installation overlay missing");
            Time.timeScale=0;await Task.Delay(120);Screen("applied_hud_installation");Time.timeScale=originalTime;
            game.CancelInstall();
            int crystals=game.Session.Crystals;
            Require(!game.Place(CoreGuardian.TowerKind.Single,new Vector2Int(11,12)),"Path placement unexpectedly allowed");
            Require(game.Session.Crystals==crystals,"Rejected placement spent currency");
            Require(game.Place(CoreGuardian.TowerKind.Single,new Vector2Int(3,11)),"Normal tower purchase rejected");
            int spent=crystals-game.Session.Crystals;
            Require(spent==game.Session.Data.Towers[CoreGuardian.TowerKind.Single].Cost,"Normal purchase cost changed");
            Button("Start").onClick.Invoke();
            var deadline=DateTime.UtcNow.AddSeconds(18);bool naturalHit=false;
            while(EditorApplication.isPlaying&&DateTime.UtcNow<deadline){
                if(UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>().Any(t=>t.name=="HitDamage")){naturalHit=true;break;}
                await Task.Delay(80);
            }
            Require(naturalHit,"Natural hit not observed");
            Time.timeScale=0;await Task.Delay(120);Screen("applied_hud_combat");
            Require(EditorJsonUtility.ToJson(Camera.main)==cameraJson,"Camera mutated");
            var result=new{passed=true,method="Own actual-scene Play; Reload/Start HUD callbacks, public TryStep, normal crystal purchase and natural damage. Not direct human input or balance measurement.",
                logicalSteps=4,start=start.ToString(),returned=game.player.CurrentCell.ToString(),installationTiles=highlights.Length,green,red,
                pathPlacementRejected=true,purchaseCost=spent,naturalHit,diagnostic=game.Session.Diagnostic,
                damage=game.Session.DamageTotals.ToArray(),compositedOverlayHUD=true,cameraUnchanged=true,
                temporaryTimeScaleZeroForScreenshots=true,screen=new{UnityEngine.Screen.width,UnityEngine.Screen.height}};
            File.WriteAllText(Dir+"runtime_visual_qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{Time.timeScale=originalTime;Time.captureDeltaTime=originalCapture;}
    }
}
