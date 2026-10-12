using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Executes the actual movement component in an isolated, disposable editor scene.
public static class TestSlabMovement
{
    static void Set(object target,string name,object value) {
        var f=target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance);
        if(f!=null) f.SetValue(target,value);
    }
    static void Require(bool condition,string message) { if(!condition)throw new Exception(message); }
    public static object Main() {
        var original=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var tile=ScriptableObject.CreateInstance<Tile>();
        var results=new List<object>();
        string[] cases={"start_center","one_slab_step","path_in_first_half","path_in_second_half","tower_in_footprint","core_in_footprint","missing_ground","map_edge","diagonal_rejected","negative_start","legacy_one_cell"};
        try {
            foreach(var name in cases) {
                var root=new GameObject("Fixture");
                var grid=root.AddComponent<Grid>();
                var maps=new Tilemap[4];
                for(int i=0;i<4;i++){var go=new GameObject("Map"+i);go.transform.SetParent(root.transform);maps[i]=go.AddComponent<Tilemap>();}
                for(int x=-4;x<8;x++)for(int y=-4;y<8;y++)maps[0].SetTile(new Vector3Int(x,y,0),tile);
                var player=new GameObject("Player");player.SetActive(false);player.transform.SetParent(root.transform);
                var movement=player.AddComponent<PrototypeGridMovement>();
                Set(movement,"ground",maps[0]);Set(movement,"enemyPath",maps[1]);Set(movement,"core",maps[2]);Set(movement,"occupiedTowerCells",maps[3]);
                Set(movement,"spriteOffset",Vector3.zero);Set(movement,"movementTileSize",new Vector2Int(2,2));Set(movement,"movementTileOrigin",Vector2Int.zero);
                player.transform.position=new Vector3(.5f,.5f,0);
                if(name=="negative_start")player.transform.position=new Vector3(-.5f,-.5f,0);
                if(name=="legacy_one_cell")Set(movement,"movementTileSize",Vector2Int.one);
                typeof(PrototypeGridMovement).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(movement,null);
                try {
                    if(name=="start_center")Require(Vector3.Distance(player.transform.position,new Vector3(1,1,0))<.001f,"Expected slab center (1,1); got "+player.transform.position);
                    else if(name=="negative_start")Require(Vector3.Distance(player.transform.position,new Vector3(-1,-1,0))<.001f,"Negative coordinates must align to (-1,-1)");
                    else if(name=="legacy_one_cell") {
                        Require(movement.TryStep(Vector2Int.right),"Legacy step refused");
                        Require(movement.CurrentCell==new Vector3Int(1,0,0),"Default one-cell movement changed");
                    } else if(name=="one_slab_step") {
                        Require(movement.TryStep(Vector2Int.right),"Clear step refused");
                        Require(movement.CurrentCell==new Vector3Int(2,0,0),"Expected next slab origin (2,0); got "+movement.CurrentCell);
                        var end=(Vector3)typeof(PrototypeGridMovement).GetField("stepEnd",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(movement);
                        Require(Vector3.Distance(end,new Vector3(3,1,0))<.001f,"Expected destination center (3,1)");
                        Require(!movement.TryStep(Vector2Int.right),"Second step accepted while moving");
                    } else {
                        if(name=="path_in_first_half")maps[1].SetTile(new Vector3Int(2,0,0),tile);
                        if(name=="path_in_second_half")maps[1].SetTile(new Vector3Int(3,1,0),tile);
                        if(name=="tower_in_footprint")maps[3].SetTile(new Vector3Int(3,1,0),tile);
                        if(name=="core_in_footprint")maps[2].SetTile(new Vector3Int(3,1,0),tile);
                        if(name=="missing_ground")maps[0].SetTile(new Vector3Int(3,1,0),null);
                        var direction=name=="diagonal_rejected"?Vector2Int.one:Vector2Int.right;
                        if(name=="map_edge")direction=Vector2Int.down;
                        if(name=="map_edge")for(int x=0;x<2;x++)for(int y=-2;y<0;y++)maps[0].SetTile(new Vector3Int(x,y,0),null);
                        Require(!movement.TryStep(direction),"Blocked or invalid step accepted");
                        Require(movement.CurrentCell==Vector3Int.zero,"Rejected step changed current tile");
                    }
                    results.Add(new {test=name,passed=true});
                }catch(Exception e){results.Add(new {test=name,passed=false,error=e.Message});}
                UnityEngine.Object.DestroyImmediate(root);
            }
        } finally {UnityEngine.Object.DestroyImmediate(tile);SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
        return Newtonsoft.Json.JsonConvert.SerializeObject(results);
    }
}
