using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class CheckLayout
{
    public static object Main(string phase="red")
    {
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        var g=game.ground;int path=0,floor=0;
        foreach(var p in g.cellBounds.allPositionsWithin){if(g.HasTile(p))floor++;if(game.enemyPath.HasTile(p))path++;}
        var failures=new System.Collections.Generic.List<string>();
        if(g.layoutGrid.cellSize!=new Vector3(.69f,.69f,1))failures.Add("Actual cell must be 0.69");
        if(g.cellBounds.size!=new Vector3Int(39,22,1)||floor!=858)failures.Add("Board must contain 39x22 cells");
        if(path!=80)failures.Add("Approved route must contain 80 logical cells");
        if(!game.coreMap.HasTile(new Vector3Int(25,8,0)))failures.Add("Core must occupy approved new cell");
        var result=new{phase,passed=failures.Count==0,failures,floor,path,size=g.layoutGrid.cellSize.ToString()};
        File.WriteAllText("docs/development/grid15_route01_applied_20261017/"+phase+"_layout.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
