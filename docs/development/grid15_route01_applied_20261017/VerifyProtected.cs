using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class VerifyProtected
{
    static void Require(bool v,string m){if(!v)throw new Exception(m);}
    public static object Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling&&!EditorUtility.scriptCompilationFailed&&!scene.isDirty,"Stopped clean scene required");
        Require(UnityEngine.SceneManagement.SceneManager.sceneCount==1&&scene.path=="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity","One combat scene required");
        var before=JObject.Parse(File.ReadAllText("docs/development/grid15_route01_applied_20261017/before.json"));
        var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        Require(EditorJsonUtility.ToJson(Camera.main)==(string)before["camera"],"Camera settings changed");
        Require(EditorJsonUtility.ToJson(Camera.main.transform)==(string)before["cameraTransform"],"Camera moved");
        Require(EditorJsonUtility.ToJson(Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>())==(string)before["cameraData"],"Pipeline changed");
        Require(EditorJsonUtility.ToJson(g.player)==(string)before["movement"],"Movement rules/timing changed");
        foreach(var item in before["protectedComponents"]){var c=g.GetComponents<Component>().Single(c=>c.GetType().FullName==(string)item["type"]);Require(EditorJsonUtility.ToJson(c)==(string)item["json"],"Protected component changed: "+c.GetType().Name);}
        var bg=GameObject.Find("Background_Ruins_VisualOnly").GetComponent<SpriteRenderer>();
        Require(EditorJsonUtility.ToJson(bg)==(string)before["background"]&&EditorJsonUtility.ToJson(bg.transform)==(string)before["backgroundTransform"],"Approved natural floor/background changed");
        foreach(var csv in before["csv"]){var a=AssetDatabase.LoadAssetAtPath<TextAsset>((string)csv["path"]);Require(Hash128.Compute(a.text).ToString()==(string)csv["hash"],"Combat CSV changed");}
        Require(g.BoardSize==new Vector2Int(39,22)&&g.CoreCell==new Vector2Int(25,8),"Runtime layout not serialized");
        Require(g.ground.layoutGrid.cellSize==new Vector3(.69f,.69f,1),"Actual grid size wrong");
        Require(g.ground.cellBounds.size==new Vector3Int(39,22,1)&&g.ground.cellBounds.min==Vector3Int.zero,"Logical floor bounds wrong");
        int floors=0,path=0,core=0,occupied=0;
        foreach(var p in g.ground.cellBounds.allPositionsWithin){
            Require(g.ground.HasTile(p),"Ground hole");floors++;
            if(g.enemyPath.HasTile(p))path++;if(g.coreMap.HasTile(p))core++;if(g.occupied.HasTile(p))occupied++;
            Require(g.ground.GetTile<Tile>(p).colliderType==Tile.ColliderType.None,"Added ground collision");
        }
        Require(floors==858&&path==80&&core==1&&occupied==0,"Mask counts incorrect");
        Require(g.coreMap.HasTile(new Vector3Int(25,8,0)),"Missing core mask");
        Require(Vector3.Distance(g.player.transform.position,g.ground.GetCellCenterWorld(new Vector3Int(13,9,0)))<.0001f,"Player start mismatch");
        var coreArt=GameObject.Find("NewArt_SizeReview_Medium").transform.Find("core");
        Require(Vector3.Distance(coreArt.position,g.World(g.CoreCell))<.0001f,"Core art mismatch");
        var markers=GameObject.Find("EnemyRoute_Waypoints_Prototype").transform.Cast<Transform>().ToArray();
        for(int i=0;i<g.RouteCells.Count;i++)Require(Vector3.Distance(markers[i].position,g.World(g.RouteCells[i]))<.0001f,"Stale waypoint marker");
        foreach(var n in new[]{"PathArt_WideStraight","PathArt_ConnectedCorners"}){var map=GameObject.Find(n).GetComponent<Tilemap>();Require(!map.GetComponent<TilemapRenderer>().enabled&&map.GetUsedTilesCount()==0,"Old road remains visible");}
        var visual=GameObject.Find("CoreGuardian_Route01_VisualOnly");
        Require(visual.GetComponentsInChildren<Collider2D>().Length==0&&visual.GetComponentsInChildren<Collider>().Length==0&&visual.GetComponentsInChildren<MonoBehaviour>().Length==0,"Road added collision/behavior");
        var road=visual.GetComponentsInChildren<SpriteRenderer>();
        foreach(var r in road){
            Require(r.sprite!=null&&r.sprite.name.StartsWith("Route01_"),"Unexpected road sprite");
            var cell=g.ground.WorldToCell(r.transform.position);Require(g.enemyPath.HasTile(cell),"Road is not over its blocked logical cell");
            var center=g.ground.GetCellCenterWorld(cell);
            Require(r.bounds.min.x>=center.x-.3451f&&r.bounds.max.x<=center.x+.3451f&&r.bounds.min.y>=center.y-.3451f&&r.bounds.max.y<=center.y+.3451f,"Road decor covers adjacent build cell");
            Require(r.sprite.texture.filterMode==FilterMode.Point,"Blurred road atlas");
        }
        Require(Time.timeScale==1&&Time.captureDeltaTime==0,"Temporary capture time remains");
        var result=new{passed=true,scene.path,scene.isDirty,rootCount=scene.rootCount,cameraAndPipelineUnchanged=true,approvedBackgroundUnchanged=true,protectedFeedbackUnchanged=true,movementRulesUnchanged=true,combatCsvHashesUnchanged=true,
            floor=floors,enemyPath=path,core,occupied,roadRenderers=road.Length,roadInsideBlockedCells=true,waypointMarkersMatchRuntime=true,newColliders=0,newRuntimeBehaviours=0,timeScale=Time.timeScale,captureDelta=Time.captureDeltaTime};
        File.WriteAllText("docs/development/grid15_route01_applied_20261017/protected_verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
