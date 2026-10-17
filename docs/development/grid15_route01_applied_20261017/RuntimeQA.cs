using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimeQA
{
    const string Dir="docs/development/grid15_route01_applied_20261017/";
    static void Require(bool v,string m){if(!v)throw new Exception(m);}
    static void Screen(string name){
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
        var capture=type.GetMethod("CaptureGameView",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{1920,1080,null,null,false,0,"screen"});
        File.WriteAllBytes(Dir+name+".png",Convert.FromBase64String((string)capture.GetType().GetProperty("Base64").GetValue(capture)));
    }
    static Button Button(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b=>b.name==name);
    public static async Task<object> Main()
    {
        Require(EditorApplication.isPlaying,"Owned Play required");var time=Time.timeScale;var captureDelta=Time.captureDeltaTime;
        try{
            Button("Reload").onClick.Invoke();await Task.Delay(700);
            var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();Require(g.Ready&&!g.Session.Diagnostic,"Normal session required");
            var session=g.Session;var fields=BindingFlags.Instance|BindingFlags.NonPublic;var t=session.GetType();
            Require((int)t.GetField("width",fields).GetValue(session)==39&&(int)t.GetField("height",fields).GetValue(session)==22,"Runtime uses obsolete board size");
            Require((Vector2Int)t.GetField("core",fields).GetValue(session)==g.CoreCell,"Runtime core mismatch");
            var route=((IEnumerable<Vector2>)t.GetField("path",fields).GetValue(session)).ToArray();
            Require(route.SequenceEqual(g.RouteCells.Select(c=>(Vector2)c)),"Runtime route mismatch");
            var cells=(HashSet<Vector2Int>)t.GetField("pathCells",fields).GetValue(session);Require(cells.Count==80,"Wrong runtime path mask");
            int boardChecked=0;for(int y=-1;y<=22;y++)for(int x=-1;x<=39;x++){
                var p=new Vector2Int(x,y);var buildable=(bool)g.GetType().GetMethod("CanInstall",fields).Invoke(g,new object[]{p});Require(buildable==session.IsBuildable(p),"Scene/model build mask differs "+p);boardChecked++;
            }
            var cameraJson=EditorJsonUtility.ToJson(Camera.main);var cameraTransform=EditorJsonUtility.ToJson(Camera.main.transform);
            var start=g.player.CurrentCell;var worldStart=g.player.transform.position;int steps=0;
            Action checkPlayer=()=>{var logical=(Vector2)g.GetType().GetMethod("PlayerTile",fields).Invoke(g,null);Require(Vector3.Distance(g.World(logical),g.player.transform.position)<.00001f,"Player combat coordinates differ from visible feet");};
            checkPlayer();
            foreach(var d in new[]{Vector2Int.right,Vector2Int.left,Vector2Int.up,Vector2Int.down}){
                var before=g.player.transform.position;Require(g.player.TryStep(d),"Normal step rejected");await Task.Delay(50);checkPlayer();await Task.Delay(200);checkPlayer();
                Require(Mathf.Abs(Vector3.Distance(before,g.player.transform.position)-.69f)<.0001f,"One step is not 0.69 world units");steps++;
            }
            Require(g.player.CurrentCell==start&&Vector3.Distance(worldStart,g.player.transform.position)<.0001f,"Four steps did not return");
            Require(g.player.TryStep(Vector2Int.right),"Approach path");await Task.Delay(250);
            Require(!g.player.TryStep(Vector2Int.right),"Player entered enemy path at 15,9");
            Require(g.player.TryStep(Vector2Int.left),"Return from path boundary");await Task.Delay(250);steps+=2;
            Time.timeScale=0;await Task.Delay(100);Screen("applied_hud_ready");Time.timeScale=time;
            g.ChooseTower(CoreGuardian.TowerKind.Single);await Task.Delay(150);
            var highlights=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.name=="InstallationTile").ToArray();
            int green=highlights.Count(r=>r.color.g>r.color.r),red=highlights.Count(r=>r.color.r>r.color.g);
            Require(highlights.Length==858&&green==778&&red==80,"Wrong installation mask");
            Require(highlights.All(h=>Mathf.Abs(h.transform.lossyScale.x-.6325f)<.00001f&&Mathf.Abs(h.transform.lossyScale.y-.6325f)<.00001f),"Installation overlay not enlarged");
            Time.timeScale=0;await Task.Delay(100);Screen("applied_hud_installation");Time.timeScale=time;g.CancelInstall();
            int money=session.Crystals;
            foreach(var cell in new[]{new Vector2Int(15,9),g.CoreCell,new Vector2Int(-1,9),new Vector2Int(39,9)})Require(!g.Place(CoreGuardian.TowerKind.Single,cell),"Blocked purchase accepted "+cell);
            Require(session.Crystals==money,"Rejected purchases spent currency");
            // Walk normally to a legal cell next to the purchased tower, then prove its occupancy blocks a step.
            for(int i=0;i<10;i++){Require(g.player.TryStep(Vector2Int.left),"Approach tower cell");await Task.Delay(200);steps++;}
            var towerCell=new Vector2Int(3,8);Require(g.Place(CoreGuardian.TowerKind.Single,towerCell),"Normal purchase failed");
            int spent=money-session.Crystals;Require(spent==session.Data.Towers[CoreGuardian.TowerKind.Single].Cost,"Purchase cost changed");
            Require(!g.player.TryStep(Vector2Int.down),"Player entered occupied tower cell");Require(!g.Place(CoreGuardian.TowerKind.Single,towerCell),"Duplicate placement accepted");
            Require(!session.IsBuildable(towerCell)&&g.occupied.HasTile(new Vector3Int(3,8,0)),"Model/scene occupancy mismatch");
            Require(Vector3.Distance(g.TowerView(session.Towers.Single().Id).position,g.World(towerCell))<.0001f,"Tower is not on enlarged cell center");
            for(int i=0;i<10;i++){Require(g.player.TryStep(Vector2Int.right),"Return from tower");await Task.Delay(200);steps++;}
            Require(g.player.CurrentCell==start,"Final player cell changed");
            Button("Start").onClick.Invoke();bool hit=false;int observed=0;var deadline=DateTime.UtcNow.AddSeconds(20);
            while(EditorApplication.isPlaying&&DateTime.UtcNow<deadline){
                foreach(var enemy in session.Enemies.Where(e=>e.Alive)){
                    observed++;var world=g.World(enemy.Position);Require(cells.Contains(Vector2Int.RoundToInt(enemy.Position)),"Enemy left logical route");
                    var view=GameObject.Find("Enemy_"+enemy.Kind+"_"+enemy.Id);Require(view!=null&&Vector3.Distance(view.transform.position,world)<.0001f,"Enemy is not rendered on runtime route");
                }
                if(UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>().Any(v=>v.name=="HitDamage")){hit=true;break;}await Task.Delay(75);
            }
            Require(hit&&observed>0,"Natural combat not observed");Time.timeScale=0;await Task.Delay(100);Screen("applied_hud_combat");
            Require(cameraJson==EditorJsonUtility.ToJson(Camera.main)&&cameraTransform==EditorJsonUtility.ToJson(Camera.main.transform),"Camera changed in Play");
            var result=new{passed=true,boardMaskChecks=boardChecked,routePoints=route.Length,routeCells=cells.Count,logicalSteps=steps,stepWorldDistance=.69,
                installationTiles=highlights.Length,green,red,overlaySize=.6325,playerPathBlocked=true,occupiedMovementBlocked=true,corePathOutsidePlacementBlocked=true,
                normalPurchase=spent,naturalHit=hit,enemyObservations=observed,returnedCell=start.ToString(),playerCombatCoordinatesMatchDuringMovement=true,cameraUnchanged=true,diagnostic=session.Diagnostic,
                method="Own actual-scene Play; HUD callbacks/public TryStep/normal purchase/natural combat. Not human input, full regression, balance or visual approval."};
            File.WriteAllText(Dir+"runtime_qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{Time.timeScale=time;Time.captureDeltaTime=captureDelta;}
    }
}
